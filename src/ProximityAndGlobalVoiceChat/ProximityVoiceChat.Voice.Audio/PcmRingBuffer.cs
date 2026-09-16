using System;
namespace ProximityVoiceChat.Voice.Audio;
internal sealed class PcmRingBuffer
{
    private readonly object _sync = new object();
    private readonly float[] _samples;
    private readonly CaptureSettings?[] _settings;
    private int _read, _count;
    internal CaptureSettings? Settings;
    internal int Dropped, Starved;
    internal int Available { get { lock (_sync) return _count; } }
    internal PcmRingBuffer(int capacity) { _samples = new float[capacity]; _settings = new CaptureSettings?[capacity]; }
    internal void Clear() { lock (_sync) { _read = _count = 0; Array.Clear(_settings, 0, _settings.Length); } }
    internal void Write(float[] source, int offset, int count)
    {
        lock (_sync)
        {
            if (_count + count > Math.Min(_samples.Length, 9600)) { Dropped += _count; _read = _count = 0; }
            for (int i = 0; i < count && _count < _samples.Length; i++)
            { int p = (_read + _count++) % _samples.Length; _samples[p] = source[offset + i]; _settings[p] = Settings; }
        }
    }
    internal bool TryReadFrame(float[] destination, out CaptureSettings? settings)
    {
        lock (_sync)
        {
            while (_count >= destination.Length)
            {
                settings = _settings[_read]; int contiguous = 1;
                while (contiguous < destination.Length && settings?.Generation == _settings[(_read + contiguous) % _samples.Length]?.Generation) contiguous++;
                if (contiguous < destination.Length) { _read = (_read + contiguous) % _samples.Length; _count -= contiguous; continue; }
                for (int i = 0; i < destination.Length; i++) destination[i] = _samples[(_read + i) % _samples.Length];
                _read = (_read + destination.Length) % _samples.Length; _count -= destination.Length; return true;
            }
            settings = null; return false;
        }
    }
}
