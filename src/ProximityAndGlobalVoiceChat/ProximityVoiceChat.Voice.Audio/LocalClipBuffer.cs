using System;
namespace ProximityVoiceChat.Voice.Audio;

internal interface ILocalClipSink
{
    void Write(float[] samples, int offset);
    void Clear(int offset);
    void Play(int offset, double start, double end);
    void EndAt(double end);
    void Stop();
}

// Main-thread timeline for a non-streaming 48 kHz mono clip. Absolute frame
// indices disambiguate ring wraps; DSP-scheduled end prevents stale replay even
// if the game thread stops updating for longer than the entire clip.
internal sealed class LocalClipBuffer
{
    internal const int FrameSamples = 960, Slots = 50, MaximumFrames = 10;
    private readonly ILocalClipSink _sink;
    private readonly int _prefill;
    private readonly double _lead;
    private readonly float[] _rms = new float[Slots];
    private long _read, _write, _startFrame;
    private double _start, _end;
    private bool _playing, _disposed;
    internal int Underruns, Dropped;
    internal int Pending => (int)(_write - _read);
    internal bool Playing => _playing;
    internal float Rms { get; private set; }
    internal LocalClipBuffer(ILocalClipSink sink, int outputRate, int dspFrames)
    {
        _sink = sink;
        _lead = Math.Max(.02, 2.0 * dspFrames / outputRate);
        _prefill = Math.Min(MaximumFrames, Math.Max(3, (int)Math.Ceiling(_lead / .02)));
    }
    internal void Advance(double now, bool complete)
    {
        if (_disposed) return;
        Rms = 0;
        if (_playing && now >= _start)
        {
            long consumed = Math.Min(_write, _startFrame + (long)Math.Floor((now - _start + 1e-9) / .02));
            ClearUntil(consumed);
            if (now >= _end - 1e-9)
            {
                ClearUntil(_write); _sink.Stop(); _playing = false;
                if (!complete) Underruns++;
            }
            else if (_read < _write) Rms = _rms[(int)(_read % Slots)];
        }
    }
    private void ClearUntil(long until)
    {
        while (_read < until) { _sink.Clear((int)(_read % Slots) * FrameSamples); _rms[(int)(_read % Slots)] = 0; _read++; }
    }
    internal void Push(float[] frame, double now)
    {
        if (_disposed) return;
        if (frame.Length != FrameSamples) throw new ArgumentException("Expected 20 ms mono PCM.");
        Advance(now, false);
        if (Pending >= MaximumFrames)
        {
            _sink.Stop(); _playing = false; Rms = 0;
            ClearUntil(_read + 1); Dropped++;
        }
        int slot = (int)(_write % Slots);
        _sink.Write(frame, slot * FrameSamples);
        double energy = 0; foreach (float value in frame) energy += (double)value * value;
        _rms[slot] = (float)Math.Sqrt(energy / frame.Length); _write++;
        if (_playing) { _end = _start + (_write - _startFrame) * .02; _sink.EndAt(_end); }
    }
    internal void Tick(double now, bool complete)
    {
        Advance(now, complete);
        if (_disposed || _playing || Pending == 0 || Pending < _prefill && !complete) return;
        _startFrame = _read; _start = now + _lead; _end = _start + Pending * .02;
        _sink.Play((int)(_read % Slots) * FrameSamples, _start, _end); _playing = true;
    }
    internal void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _sink.Stop(); ClearUntil(_write); Rms = 0; _playing = false;
    }
}
