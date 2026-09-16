using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProximityVoiceChat.Voice.Audio;

internal sealed class SpatialPlayback : IDisposable
{
	private const float HeadHeight = 1.5f;

	private const float MonitorDistance = 3f;

	private readonly Dictionary<ulong, PeerVoiceStream> _streams = new Dictionary<ulong, PeerVoiceStream>();

	private readonly List<ulong> _reap = new List<ulong>();

	private GameObject? _root;

	private bool _disposed;

	internal IReadOnlyDictionary<ulong, PeerVoiceStream> Streams => _streams;

	internal bool AnyoneSpeaking
	{
		get
		{
			foreach (KeyValuePair<ulong, PeerVoiceStream> stream in _streams)
			{
				if (stream.Value.Speaking)
				{
					return true;
				}
			}
			return false;
		}
	}

	internal SpatialPlayback(Transform parent)
	{
		_root = new GameObject("PVC_Voices");
		_root.transform.SetParent(parent, worldPositionStays: false);
	}

	internal void Push(ulong steamId, in VoiceHeader header, byte[] buffer, int offset, int length)
	{
		if (!_disposed && !(_root == null) && !PeerPreferences.IsSilenced(steamId) && !ProximityVoiceChatPlugin.Deafen.Value.IsOn())
		{
			if (_streams.TryGetValue(steamId, out PeerVoiceStream previous) && previous.StreamId != header.StreamId) Remove(steamId);
            if (!_streams.TryGetValue(steamId, out PeerVoiceStream value))
			{
				value = new PeerVoiceStream(steamId, _root.transform, steamId != 1 && header.Channel == VoiceChannel.Local, header.StreamId, header.Channel);
				_streams[steamId] = value;
				ProximityVoiceChatPlugin.Log.LogInfo($"Opened a voice stream for {steamId}");
			}
			value.Push(in header, buffer, offset, length);
		}
	}

    internal void PushRaw(float[] samples, uint streamId)
    {
        if (_disposed || _root == null || ProximityVoiceChatPlugin.Deafen.Value.IsOn()) return;
        if (_streams.TryGetValue(1, out var previous) && previous.StreamId != streamId) Remove(1);
        if (!_streams.TryGetValue(1, out var stream))
        { stream = new PeerVoiceStream(1, _root.transform, false, streamId, VoiceChannel.Global); _streams[1] = stream; }
        stream.PushRaw(samples);
    }
	internal void Tick(float deltaTime, VoiceRoster roster)
	{
		if (_disposed)
		{
			return;
		}
		float value = ProximityVoiceChatPlugin.VoiceVolume.Value;
		_reap.Clear();
		foreach (KeyValuePair<ulong, PeerVoiceStream> stream in _streams)
		{
			PeerVoiceStream value2 = stream.Value;
			if (value2.Expired || ProximityVoiceChatPlugin.Deafen.Value.IsOn() || PeerPreferences.IsSilenced(stream.Key))
			{
				_reap.Add(stream.Key);
				continue;
			}
			value2.Tick(deltaTime);
			float volume = ((stream.Key == 1) ? ProximityVoiceChatPlugin.MonitorVolume.Value : (value * PeerPreferences.GetVolume(stream.Key)));
			Vector3 position = ResolvePosition(roster, stream.Key);
			if (stream.Key != 1 && value2.Channel == VoiceChannel.Local)
			{
				Muffle(value2, position, deltaTime);
			}
			value2.UpdateSpatial(position, volume);
		}
		for (int i = 0; i < _reap.Count; i++)
		{
			Remove(_reap[i]);
		}
	}

	private static void Muffle(PeerVoiceStream stream, Vector3 position, float deltaTime)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || !localPlayer)
		{
			stream.SetMuffle(22000f, 1f, deltaTime);
			return;
		}
		if (ProximityVoiceChatPlugin.UnderwaterMuffle.Value.IsOn() && localPlayer.IsSwimming())
		{
			stream.SetMuffle(ProximityVoiceChatPlugin.UnderwaterCutoff.Value, 1f, deltaTime);
			return;
		}
		if (ProximityVoiceChatPlugin.Occlusion.Value == ProximityVoiceChatPlugin.Toggle.Off)
		{
			stream.SetMuffle(22000f, 1f, deltaTime);
			return;
		}
		bool flag = stream.CheckOccluded(localPlayer.GetHeadPoint(), position, deltaTime);
		stream.SetMuffle(flag ? ProximityVoiceChatPlugin.OccludedCutoff.Value : 22000f, flag ? ProximityVoiceChatPlugin.OccludedVolume.Value : 1f, deltaTime);
	}

	private static Vector3 ResolvePosition(VoiceRoster roster, ulong steamId)
	{
		Player localPlayer = Player.m_localPlayer;
		bool flag = localPlayer != null && (bool)localPlayer;
		if (steamId == 1)
		{
			if (!flag)
			{
				return Vector3.zero;
			}
			return localPlayer.transform.position + localPlayer.transform.forward * 3f + Vector3.up;
		}
		if (roster.TryGet(steamId, out RosterEntry entry) && entry.HasPosition)
		{
			return entry.LastKnownPosition + Vector3.up * 1.5f;
		}
		if (!flag)
		{
			return Vector3.zero;
		}
		return localPlayer.transform.position;
	}

	internal void Clear()
	{
		foreach (KeyValuePair<ulong, PeerVoiceStream> stream in _streams)
		{
			stream.Value.Dispose();
		}
		_streams.Clear();
	}

	internal void Remove(ulong steamId)
	{
		if (_streams.TryGetValue(steamId, out PeerVoiceStream value))
		{
			value.Dispose();
			_streams.Remove(steamId);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		foreach (KeyValuePair<ulong, PeerVoiceStream> stream in _streams)
		{
			stream.Value.Dispose();
		}
		_streams.Clear();
		if (_root != null)
		{
			UnityEngine.Object.Destroy(_root);
			_root = null;
		}
	}
}
