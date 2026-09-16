using System;
using UnityEngine;
namespace ProximityVoiceChat.Voice.Audio;
internal sealed class PeerVoiceStream : IDisposable
{
    internal const float OpenCutoff = 22000f;
    private readonly ulong _steamId;
    private readonly bool _spatial;
    internal readonly uint StreamId;
    internal readonly VoiceChannel Channel;
    private readonly VoiceJitterBuffer _jitter = new VoiceJitterBuffer();
    private readonly float[] _decodedFrame = new float[960];
    private LocalSpatialOutput? _local;
    private PlaybackBuffer _buffer;
    private PlaybackResampler _resampler;
    private VoiceAudioOutput _output;
    private int _configurationChanged;
    
    
    private GameObject? _object;
    private AudioSource? _source;
    
    private AudioLowPassFilter? _lowPass;
    private static int _viewBlockMask;
    private bool _occluded; private volatile bool _disposed;
    private float _occlusionTimer, _lastPacketTime, _lastDecoded = -100f;
    private float _cutoff = 22000f, _muffleVolume = 1f, _minDistance = -1f, _maxDistance = -1f;
    private volatile float _gain = 1f;
    internal float OutputRms => _local != null ? _local.Rms : _buffer.Rms;
    internal string Diagnostics => _local != null ? _local.Diagnostics : $"Output {_buffer.Rate} Hz | buffer {1000.0 * _buffer.Buffered / _buffer.Rate:0} ms | DSP {_buffer.CallbackFrames} frames | underruns {_buffer.Underruns} | dropped {_buffer.Dropped} | startup {_buffer.StartupMs:0} ms";
    internal ulong SteamId => _steamId;
    internal bool Speaking => !_disposed && Time.unscaledTime - _lastDecoded < 0.2f;
    internal bool Occluded => _occluded;
    internal float IdleFor => Time.unscaledTime - _lastPacketTime;
    internal bool Expired => IdleFor > 8f;
    internal byte TakeLossPercent() => _jitter.TakeLossPercent();
    internal PeerVoiceStream(ulong id, Transform parent, bool spatial, uint streamId = 0, VoiceChannel channel = VoiceChannel.Local)
    {
        _steamId = id; _spatial = spatial; StreamId = streamId; Channel = channel; _lastPacketTime = Time.unscaledTime;
        _object = new GameObject("PAGVC_Voice_" + id); _object.transform.SetParent(parent, false);
        
        _source = _object.AddComponent<AudioSource>(); _source.loop = true;
        if (!spatial) _output = _object.AddComponent<VoiceAudioOutput>();
        ConfigureOutput(); AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        _source.playOnAwake = false; _source.dopplerLevel = 0; _source.priority = 64;
        _source.pitch = 1; _source.spatialBlend = spatial ? 1 : 0; _source.bypassReverbZones = true;
        if (spatial)
        {
            _source.spread = 35; _source.minDistance = ProximityVoiceChatPlugin.FullVolumeRange.Value;
            _source.maxDistance = ProximityVoiceChatPlugin.MaxRange.Value; ApplyRolloff();
            bool reverb = ProximityVoiceChatPlugin.VoiceReverb.Value.IsOn();
            _source.bypassReverbZones = !reverb; _source.reverbZoneMix = reverb ? ProximityVoiceChatPlugin.ReverbMix.Value : 0;
            _lowPass = _object.AddComponent<AudioLowPassFilter>(); _lowPass.cutoffFrequency = 22000; _lowPass.lowpassResonanceQ = 1;
        }
        // Global and monitor bypass the world's mixer/environment processing.
        _source.outputAudioMixerGroup = spatial ? VoiceMixer.Group : null; if (!spatial) _source.Play();
    }
    internal void Push(in VoiceHeader header, byte[] buffer, int offset, int length)
    {
        if (_disposed) return; _lastPacketTime = Time.unscaledTime;
        _jitter.Push(header.Sequence, header.TimestampMs, buffer, offset, length, (header.Flags & VoiceFlags.EndOfTalkspurt) != 0, Time.unscaledTime);
    }
    internal void PushRaw(float[] samples)
    { if (_disposed) return; _lastPacketTime = Time.unscaledTime; Queue(samples); }
    internal void Tick(float deltaTime)
    {
        if (_disposed) return;
        if (System.Threading.Interlocked.Exchange(ref _configurationChanged, 0) != 0) { _source!.Stop(); ConfigureOutput(); if (!_spatial) _source.Play(); }
        _jitter.TargetSeconds = ProximityVoiceChatPlugin.JitterTargetMs.Value / 1000.0;
        _jitter.MaximumSeconds = ProximityVoiceChatPlugin.JitterMaxMs.Value / 1000.0;
        _jitter.Adaptive = ProximityVoiceChatPlugin.AdaptiveJitter.Value.IsOn();
        int budget = 12;
        while (budget-- > 0 && _jitter.TryDecode(Time.unscaledTime, _decodedFrame)) Queue(_decodedFrame);
        _local?.Tick(_jitter.Complete);
        
    }
    private void OnAudioConfigurationChanged(bool changed) => System.Threading.Interlocked.Exchange(ref _configurationChanged, 1);
    private void ConfigureOutput()
    {
        if (_spatial) { _local?.Dispose(); _local = new LocalSpatialOutput(_source!); return; }
        _buffer?.Stop();
        int rate = AudioSettings.outputSampleRate;
        AudioSettings.GetDSPBufferSize(out int frames, out int blocks);
        _buffer = new PlaybackBuffer(rate, frames); _resampler = new PlaybackResampler(rate); _output.Buffer = _buffer;
        ProximityVoiceChatPlugin.Log.LogInfo($"Voice output {rate} Hz, DSP {frames} x {blocks}, prefill {_buffer.Prefill} samples");
    }
    private void Queue(float[] samples)
    {
        if (_local != null) _local.Push(samples, _gain); else _resampler.Write(samples, _buffer); _lastDecoded = Time.unscaledTime;
    }
	internal bool CheckOccluded(Vector3 from, Vector3 to, float deltaTime)
	{
		_occlusionTimer -= deltaTime;
		if (_occlusionTimer > 0f)
		{
			return _occluded;
		}
		_occlusionTimer = 0.2f;
		if (_viewBlockMask == 0)
		{
			_viewBlockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "viewblock", "vehicle");
		}
		_occluded = Physics.Linecast(from, to, _viewBlockMask);
		return _occluded;
	}

	internal void SetMuffle(float targetCutoff, float targetVolume, float deltaTime)
	{
		_muffleVolume = Mathf.MoveTowards(_muffleVolume, targetVolume, 2f * deltaTime);
		if (!(_lowPass == null))
		{
			_cutoff = Mathf.MoveTowards(_cutoff, targetCutoff, 26000f * deltaTime);
			_lowPass.cutoffFrequency = _cutoff;
		}
	}

	internal void UpdateSpatial(Vector3 position, float volume)
	{
		if (_disposed || _object == null || _source == null)
		{
			return;
		}
		volume *= _muffleVolume;
		_source.volume = ((volume > 1f) ? 1f : volume);
		_gain = ((volume > 1f) ? volume : 1f); if (_output != null) _output.Gain = _gain;
		if (_spatial)
		{
			_object.transform.position = position;
			float value = ProximityVoiceChatPlugin.FullVolumeRange.Value;
			float value2 = ProximityVoiceChatPlugin.MaxRange.Value;
			if (value != _minDistance || value2 != _maxDistance)
			{
				_minDistance = value;
				_maxDistance = value2;
				_source.minDistance = value;
				_source.maxDistance = value2;
				ApplyRolloff();
			}
		}
	}

	private void ApplyRolloff()
	{
		if (!(_source == null))
		{
			float num = Mathf.Clamp01(_source.minDistance / Mathf.Max(1f, _source.maxDistance));
			AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(num, 1f), new Keyframe(num + (1f - num) * 0.45f, 0.6f), new Keyframe(1f, 0f));
			for (int i = 0; i < animationCurve.length; i++)
			{
				animationCurve.SmoothTangents(i, 0.6f);
			}
			_source.rolloffMode = AudioRolloffMode.Custom;
			_source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, animationCurve);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true; AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged; _local?.Dispose(); _buffer?.Stop(); if (_output != null) _output.Buffer = null;
			if (_source != null)
			{
				_source.Stop();
				_source.clip = null;
				_source = null;
			}
			if (_object != null)
			{
				UnityEngine.Object.Destroy(_object);
				_object = null;
			}
			_jitter.Dispose();
		}
	}
}
