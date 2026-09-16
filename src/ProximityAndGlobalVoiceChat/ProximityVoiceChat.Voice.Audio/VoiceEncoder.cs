using System;
using System.Threading;
namespace ProximityVoiceChat.Voice.Audio;
internal sealed class VoiceEncoder : IDisposable
{
    private readonly PcmRingBuffer _pcm;
    private readonly EncodedFrameRing _output;
    private readonly float[] _frame = new float[960];
    private NativeOpus? _codec;
    private NoiseSuppressor? _noise;
    private Thread? _thread;
    private volatile bool _running, _speaking;
    private uint _stream;
    private int _generation = -1, _hangover;
    private float _autoGain = 1f, _limiter = 1f;
    internal volatile string? Error;
    internal int FramesEncoded, FramesSuppressed, ClippedFrames, FramesSinceClip = int.MaxValue;
    internal float LastRms { get; private set; }
    internal float LastRawRms { get; private set; }
    internal float AutoGainFactor => _autoGain;
    internal bool IsSpeaking => _speaking;
    internal bool IsTransmitting => _speaking;
    internal VoiceEncoder(PcmRingBuffer pcm, EncodedFrameRing output) { _pcm = pcm; _output = output; }
    internal void Start()
    {
        NativeAudio.Ensure(); if (_running) return;
        _running = true; _thread = new Thread(Run) { IsBackground = true, Name = "PAGVC audio encoder" }; _thread.Start();
    }
    private void Run()
    {
        try
        {
            _codec = new NativeOpus(true); _noise = new NoiseSuppressor();
            while (_running)
            { if (!_pcm.TryReadFrame(_frame, out var settings)) { Thread.Sleep(2); continue; } if (settings != null) Process(settings); }
        }
        catch (Exception ex) { Error = ex.Message; }
        finally { _speaking = false; _running = false; _codec?.Dispose(); _noise?.Dispose(); }
    }
    private void Process(CaptureSettings s)
    {
        if (_generation != s.Generation)
        {
            _generation = s.Generation; _speaking = false; _hangover = 0; _autoGain = _limiter = 1f;
            _noise!.Dispose(); _noise = new NoiseSuppressor();
        }
        LastRawRms = Rms(_frame);
        bool raw = s.RawMonitor;
        if (s.Noise && !raw) _noise!.Process(_frame);
        float rms = Rms(_frame);
        if (!raw)
        {
            if (!s.AutoGain) _autoGain = 1f;
            else if (rms > Math.Max(0.008f, s.Threshold * 0.5f))
            { float target = Math.Max(0.25f, Math.Min(4f, s.Target / rms)); _autoGain += (target - _autoGain) * (target < _autoGain ? 0.12f : 0.004f); }
            float gain = s.Gain * _autoGain, peak = 0f;
            for (int i = 0; i < 960; i++) peak = Math.Max(peak, Math.Abs(_frame[i] * gain));
            float wanted = peak > 0.95f ? 0.95f / peak : 1f;
            _limiter = wanted < _limiter ? wanted : Math.Min(wanted, _limiter + 0.02f);
            if (peak > 1f) { ClippedFrames++; FramesSinceClip = 0; } else if (FramesSinceClip < int.MaxValue) FramesSinceClip++;
            for (int i = 0; i < 960; i++) _frame[i] *= gain * _limiter;
        }
        LastRms = Rms(_frame);
        bool open = s.Channel != VoiceChannel.None;
        if (s.Activation && open) { if (rms >= s.Threshold) _hangover = 12; else open = _hangover-- > 0; }
        if (!open)
        { if (_speaking) Emit(s, true, raw); _speaking = false; FramesSuppressed++; return; }
        if (!_speaking) { _stream++; _codec!.Dispose(); _codec = new NativeOpus(true); }
        _codec!.Configure(s.Bitrate, s.Loss, s.Dtx);
        Emit(s, false, raw); _speaking = true;
    }
    private void Emit(CaptureSettings settings, bool end, bool raw)
    {
        var output = _output.BeginWrite(); if (output == null) return;
        output.Generation = settings.Generation; output.StreamId = _stream; output.Channel = settings.Channel;
        output.TimestampMs = unchecked((uint)Environment.TickCount); output.Flags = end ? VoiceFlags.EndOfTalkspurt : VoiceFlags.None; output.RawMonitor = raw;
        if (raw && !end) Array.Copy(_frame, output.Raw, 960);
        output.Length = end || raw ? 0 : _codec!.Encode(_frame, output.Data);
        _output.CommitWrite(); FramesEncoded++;
    }
    private static float Rms(float[] frame) { double sum = 0; foreach (float v in frame) sum += (double)v * v; return (float)Math.Sqrt(sum / frame.Length); }
    public void Dispose()
    {
        _running = false;
        if (_thread != null && !_thread.Join(5000)) throw new InvalidOperationException("Audio worker did not stop; buffers must not be reused.");
        _thread = null;
    }
}
