using System;
using UnityEngine;
namespace ProximityVoiceChat.Voice.Audio;

// Only received Local voice takes this path. Unity spatializes the mono clip;
// no OnAudioFilterRead generator is attached to its AudioSource.
internal sealed class LocalSpatialOutput : ILocalClipSink, IDisposable
{
    private readonly AudioSource _source;
    private readonly AudioClip _clip;
    private readonly float[] _silence = new float[960], _scaled = new float[960];
    private readonly LocalClipBuffer _buffer;
    internal float Rms => _buffer.Rms;
    internal string Diagnostics => $"Local mono | queued {_buffer.Pending * 20} ms | underruns {_buffer.Underruns} | dropped {_buffer.Dropped}";
    internal LocalSpatialOutput(AudioSource source)
    {
        _source = source;
        _clip = AudioClip.Create("PAGVC_Local", 48000, 1, 48000, false);
        _source.clip = _clip; _source.loop = true; _source.pitch = 1;
        AudioSettings.GetDSPBufferSize(out int frames, out _);
        _buffer = new LocalClipBuffer(this, AudioSettings.outputSampleRate, frames);
    }
    internal void Push(float[] frame, float gain)
    {
        for (int i = 0; i < 960; i++) _scaled[i] = Math.Max(-1f, Math.Min(1f, frame[i] * gain));
        _buffer.Push(_scaled, AudioSettings.dspTime);
    }
    internal void Tick(bool complete) => _buffer.Tick(AudioSettings.dspTime, complete);
    public void Write(float[] samples, int offset) { if (!_clip.SetData(samples, offset)) throw new InvalidOperationException("Local voice clip write failed."); }
    public void Clear(int offset) => _clip.SetData(_silence, offset);
    public void Play(int offset, double start, double end)
    { _source.timeSamples = offset; _source.PlayScheduled(start); _source.SetScheduledEndTime(end); }
    public void EndAt(double end) => _source.SetScheduledEndTime(end);
    public void Stop() => _source.Stop();
    public void Dispose() { _buffer.Dispose(); _source.clip = null; UnityEngine.Object.Destroy(_clip); }
}
