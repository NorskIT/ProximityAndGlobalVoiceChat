using System;
using System.Diagnostics;
namespace ProximityVoiceChat.Voice.Audio;

// Mono samples at the OUTPUT device rate. Main thread writes, DSP thread reads.
internal sealed class PlaybackBuffer
{
    private readonly object _sync = new object();
    private readonly float[] _samples;
    private int _read, _count;
    private bool _playing, _stopped;
    private long _firstWrite;
    internal readonly int Rate, Prefill;
    private int _underruns, _dropped, _callbackFrames;
    private double _startupMs = -1;
    private float _rms;
    internal PlaybackBuffer(int rate, int dspFrames)
    {
        if (rate <= 0 || dspFrames <= 0) throw new ArgumentOutOfRangeException();
        Rate = rate; Prefill = Math.Max((int)Math.Ceiling(rate * .06), dspFrames * 2);
        _samples = new float[Math.Max(rate / 5, Prefill + dspFrames * 2)];
    }
    internal int Buffered { get { lock (_sync) return _count; } }
    internal int Underruns { get { lock (_sync) return _underruns; } }
    internal int Dropped { get { lock (_sync) return _dropped; } }
    internal int CallbackFrames { get { lock (_sync) return _callbackFrames; } }
    internal double StartupMs { get { lock (_sync) return _startupMs; } }
    internal float Rms { get { lock (_sync) return _rms; } }
    internal void Write(float[] samples, int count)
    {
        lock (_sync)
        {
            if (_stopped || count == 0) return;
            if (_firstWrite == 0) _firstWrite = Stopwatch.GetTimestamp();
            int skip = Math.Max(0, count - _samples.Length);
            int discard = Math.Max(0, _count + count - skip - _samples.Length);
            _read = (_read + discard) % _samples.Length; _count -= discard; _dropped += discard + skip;
            for (int i = skip; i < count; i++) _samples[(_read + _count++) % _samples.Length] = samples[i];
        }
    }
    internal void Read(float[] data, int channels, float gain)
    {
        Array.Clear(data, 0, data.Length);
        if (channels <= 0) return;
        lock (_sync)
        {
            int frames = data.Length / channels; _callbackFrames = frames; _rms = 0;
            if (_stopped || frames == 0) return;
            if (!_playing)
            {
                if (_count < Math.Max(Prefill, frames)) return;
                _playing = true;
                if (_startupMs < 0) _startupMs = 1000.0 * (Stopwatch.GetTimestamp() - _firstWrite) / Stopwatch.Frequency;
            }
            int available = Math.Min(frames, _count); double energy = 0;
            for (int i = 0; i < available; i++)
            {
                float sample = Math.Max(-1f, Math.Min(1f, _samples[_read] * gain));
                _read = (_read + 1) % _samples.Length; _count--; energy += (double)sample * sample;
                for (int channel = 0; channel < channels; channel++) data[i * channels + channel] = sample;
            }
            _rms = (float)Math.Sqrt(energy / frames);
            if (available < frames) { _underruns++; _playing = false; }
        }
    }
    internal void Stop() { lock (_sync) { _stopped = true; _count = 0; _rms = 0; } }
}

// Used only on the main thread, ahead of the DSP buffer.
internal sealed class PlaybackResampler
{
    private readonly NormalizedAudioResampler? _resampler;
    internal PlaybackResampler(int outputRate)
    {
        if (outputRate != 48000) _resampler = new NormalizedAudioResampler(48000, outputRate, 960);
    }
    internal void Write(float[] frame, PlaybackBuffer buffer)
    {
        if (_resampler == null) { buffer.Write(frame, frame.Length); return; }
        int count = _resampler.Process(frame, frame.Length);
        buffer.Write(_resampler.Output, count);
    }
}
