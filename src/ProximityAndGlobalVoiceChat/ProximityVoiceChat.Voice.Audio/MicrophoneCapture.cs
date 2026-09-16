using System;
using UnityEngine;

namespace ProximityVoiceChat.Voice.Audio;

internal sealed class MicrophoneCapture : IDisposable
{
	private const int ClipSeconds = 1;

	private const int ChunksPerSecond = 100;

	private const float RecoveryInterval = 2f;

	private const float DeviceScanInterval = 1f;

	private const float ToneHz = 200f;

	private const float ToneAmplitude = 0.2f;

	private const float PeakDecayPerChunk = 0.02f;

	private const int ResampleSlack = 4;

	private const int ResampleGrowth = 2;

	private readonly PcmRingBuffer _ring;

	private float[] _chunk = Array.Empty<float>();

	private float[] _mono = Array.Empty<float>();

	private float[] _resampled = Array.Empty<float>();

	private AudioClip? _clip;

	private string? _device;

	private int _frames;

	private int _chunkFrames;

	private int _channels = 1;

	private int _deviceRate;

	private int _readPos;

	

	private float _recoveryTimer;

	private float _deviceScanTimer;

	private string[] _knownDevices = Array.Empty<string>();

	private float _tonePhase;

	private float _toneCarry;

	private readonly float[] _toneChunk = new float[960];

	private bool _disposed;

	internal float PeakLevel { get; private set; }

	internal bool Dead
	{
		get
		{
			if (!(_clip == null))
			{
				return _device == null;
			}
			return true;
		}
	}

	internal bool IsCapturing
	{
		get
		{
			if (_clip != null && _device != null)
			{
				return Microphone.IsRecording(_device);
			}
			return false;
		}
	}

	internal string? Device => _device;

	internal int DeviceRate => _deviceRate;

	internal int Channels => _channels;

	internal static string[] Devices => Microphone.devices;

	internal MicrophoneCapture(PcmRingBuffer ring)
	{
		_ring = ring;
	}

	internal bool Start(string requestedDevice)
	{
		if (_disposed)
		{
			return false;
		}
		Stop();
		string[] devices = Microphone.devices;
		if (devices.Length == 0)
		{
			ProximityVoiceChatPlugin.Log.LogWarning("No microphone devices, voice capture stays off");
			return false;
		}
		string text = requestedDevice;
		if (string.IsNullOrEmpty(text) || Array.IndexOf(devices, text) < 0)
		{
			text = devices[0];
		}
		_deviceRate = ResolveRate(text);
		AudioClip audioClip = Microphone.Start(text, loop: true, 1, _deviceRate);
		if (audioClip == null)
		{
			ProximityVoiceChatPlugin.Log.LogError("Microphone.Start failed for \"" + text + "\"");
			return false;
		}
		_clip = audioClip;
		_device = text;
		_channels = Mathf.Max(1, audioClip.channels);
		_frames = audioClip.samples;
		_chunkFrames = Math.Max(1, _frames / 100);
		if (_chunk.Length != _chunkFrames * _channels)
		{
			_chunk = new float[_chunkFrames * _channels];
		}
		if (_mono.Length != _chunkFrames)
		{
			_mono = new float[_chunkFrames];
		}
		_readPos = 0;
		
		_recoveryTimer = 0f;
		_ring.Clear();
		ProximityVoiceChatPlugin.Log.LogInfo($"Capturing from \"{text}\" at {_deviceRate} Hz, {_channels} ch, {_chunkFrames} frame chunks");
		return true;
	}

	private static int ResolveRate(string device)
	{
		Microphone.GetDeviceCaps(device, out var minFreq, out var maxFreq);
		if (minFreq == 0 && maxFreq == 0)
		{
			return 48000;
		}
		if (48000 >= minFreq && 48000 <= maxFreq)
		{
			return 48000;
		}
		return Mathf.Clamp(48000, minFreq, maxFreq);
	}

	internal void Stop()
	{
		if (_device != null && Microphone.IsRecording(_device))
		{
			Microphone.End(_device);
		}
		if (_clip != null)
		{
			UnityEngine.Object.Destroy(_clip);
			_clip = null;
		}
		_device = null;
		_frames = 0;
		_chunkFrames = 0; _resampler = null;
		_channels = 1;
		_readPos = 0;
		PeakLevel = 0f;
	}

	internal void DiscardPending() { if (_clip != null && _device != null) _readPos = Math.Max(0, Microphone.GetPosition(_device)); _resampler = null; }
internal void Poll(float deltaTime)
	{
		if (_disposed || _clip == null || _device == null)
		{
			return;
		}
		ScanDevices(deltaTime);
		if (ProximityVoiceChatPlugin.TestTone.Value.IsOn())
		{
			PushTone(deltaTime);
			return;
		}
		if (!Microphone.IsRecording(_device))
		{
			Recover(deltaTime);
			return;
		}
		_recoveryTimer = 0f;
		int position = Microphone.GetPosition(_device);
		if (position < 0 || position > _frames)
		{
			return;
		}
		int num = 101;
		while (num-- > 0 && Pending(position) >= _chunkFrames)
		{
			if (_readPos + _chunkFrames > _frames)
			{
				_readPos = 0;
				continue;
			}
			if (!_clip.GetData(_chunk, _readPos))
			{
				break;
			}
			_readPos += _chunkFrames;
			if (_readPos >= _frames)
			{
				_readPos = 0;
			}
			Downmix();
			TrackPeak(_mono, _chunkFrames);
			if (_deviceRate == 48000)
			{
				_ring.Write(_mono, 0, _chunkFrames);
				continue;
			}
			int count = Resample();
			_ring.Write(_resampled, 0, count);
		}
	}

	private void ScanDevices(float deltaTime)
	{
		_deviceScanTimer += deltaTime;
		if (_deviceScanTimer < 1f)
		{
			return;
		}
		_deviceScanTimer = 0f;
		string[] devices = Microphone.devices;
		bool flag = devices.Length != _knownDevices.Length;
		if (!flag)
		{
			for (int i = 0; i < devices.Length; i++)
			{
				if (Array.IndexOf<string>(_knownDevices, devices[i]) < 0)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			_knownDevices = devices;
			if (_device != null && Array.IndexOf<string>(devices, _device) < 0)
			{
				ProximityVoiceChatPlugin.Log.LogWarning("Microphone \"" + _device + "\" went away, dropping capture");
				Stop();
			}
			else
			{
				ProximityVoiceChatPlugin.Log.LogInfo($"Microphone list changed, {devices.Length} device(s) now");
			}
		}
	}

	private void PushTone(float deltaTime)
	{
		_toneCarry += deltaTime * 48000f;
		int num = (int)_toneCarry;
		if (num <= 0)
		{
			return;
		}
		_toneCarry -= num;
		float num2 = ((float)Math.PI) / 120f;
		while (num > 0)
		{
			int num3 = Math.Min(num, _toneChunk.Length);
			for (int i = 0; i < num3; i++)
			{
				_toneChunk[i] = Mathf.Sin(_tonePhase) * 0.2f;
				_tonePhase += num2;
				if (_tonePhase > ((float)Math.PI) * 2f)
				{
					_tonePhase -= ((float)Math.PI) * 2f;
				}
			}
			_ring.Write(_toneChunk, 0, num3);
			num -= num3;
		}
		PeakLevel = 0.2f;
	}

	internal void ProbeRaw()
	{
		if (_clip == null || _device == null)
		{
			ProximityVoiceChatPlugin.Log.LogWarning("Mic probe: not capturing");
			return;
		}
		float[] array = new float[_frames * _channels];
		if (!_clip.GetData(array, 0))
		{
			ProximityVoiceChatPlugin.Log.LogWarning("Mic probe: GetData refused");
			return;
		}
		float num = 0f;
		double num2 = 0.0;
		int num3 = 0;
		foreach (float num4 in array)
		{
			if (num4 == 0f)
			{
				num3++;
			}
			float num5 = ((num4 < 0f) ? (0f - num4) : num4);
			if (num5 > num)
			{
				num = num5;
			}
			num2 += (double)num4 * (double)num4;
		}
		float num6 = (float)Math.Sqrt(num2 / (double)array.Length);
		float num7 = 100f * (float)num3 / (float)array.Length;
		int position = Microphone.GetPosition(_device);
		ProximityVoiceChatPlugin.Log.LogInfo($"Mic probe on \"{_device}\": peak {num:0.0000} rms {num6:0.0000} exact zeros {num7:0.0}% " + $"over {_frames} frames x {_channels} ch, writePos {position}, myReadPos {_readPos}");
	}

	private void Downmix()
	{
		if (_channels == 1)
		{
			Array.Copy(_chunk, _mono, _chunkFrames);
			return;
		}
		float num = 1f / (float)_channels;
		int num2 = 0;
		for (int i = 0; i < _chunkFrames; i++)
		{
			float num3 = 0f;
			for (int j = 0; j < _channels; j++)
			{
				num3 += _chunk[num2++];
			}
			_mono[i] = num3 * num;
		}
	}

	private int Pending(int writePos)
	{
		int num = writePos - _readPos;
		if (num >= 0)
		{
			return num;
		}
		return num + _frames;
	}

	private void Recover(float deltaTime)
	{
		_recoveryTimer += deltaTime;
		if (!(_recoveryTimer < 2f))
		{
			_recoveryTimer = 0f;
			string text = _device ?? string.Empty;
			ProximityVoiceChatPlugin.Log.LogWarning("Microphone \"" + text + "\" stopped recording, restarting it");
			Start(text);
		}
	}

	private NormalizedAudioResampler? _resampler;
private int _resamplerRate;
private int Resample()
    {
        if (_resampler == null || _resamplerRate != _deviceRate)
        { _resampler = new NormalizedAudioResampler(_deviceRate, 48000, _chunkFrames); _resamplerRate = _deviceRate; }
        int output = _resampler.Process(_mono, _chunkFrames); _resampled = _resampler.Output;
        return output;
    }

    private void TrackPeak(float[] samples, int count)
	{
		float num = 0f;
		for (int i = 0; i < count; i++)
		{
			float num2 = ((samples[i] < 0f) ? (0f - samples[i]) : samples[i]);
			if (num2 > num)
			{
				num = num2;
			}
		}
		PeakLevel = Mathf.Max(num, PeakLevel - 0.02f);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			Stop();
		}
	}
}
