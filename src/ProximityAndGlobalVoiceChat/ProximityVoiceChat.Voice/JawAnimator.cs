using System.Collections.Generic;
using ProximityVoiceChat.Voice.Audio;
using UnityEngine;

namespace ProximityVoiceChat.Voice;

internal sealed class JawAnimator : MonoBehaviour
{
	private sealed class Mouth
	{
		internal Transform? Jaw;

		internal Quaternion Rest;

		internal float Open;

		internal float Seed;

		internal bool Missing;
	}

	private const string JawBone = "Jaw";

	private const float AttackPerSecond = 14f;

	private const float ReleasePerSecond = 7f;

	private const float Floor = 0.012f;

	private const float Closed = 0.0005f;

	private const float WobbleBase = 0.85f;

	private const float WobbleDepth = 0.15f;

	private const float WobbleHz = 9f;

	private const float SeedRange = 10f;

	private readonly Dictionary<Player, Mouth> _mouths = new Dictionary<Player, Mouth>();

	private readonly List<Player> _stale = new List<Player>();

	private void LateUpdate()
	{
		if (ProximityVoiceChatPlugin.JawMovement.Value == ProximityVoiceChatPlugin.Toggle.Off || ZNet.instance == null)
		{
			if (_mouths.Count > 0)
			{
				ReleaseAll();
			}
			return;
		}
		VoiceRuntime instance = VoiceRuntime.Instance;
		if (instance == null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		VoiceEncoder encoder = instance.Encoder;
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null && (bool)localPlayer)
		{
			float level = ((encoder != null && encoder.IsSpeaking && VoiceRuntime.GateOpen()) ? encoder.LastRms : 0f);
			if (ProximityVoiceChatPlugin.LoopbackTest.Value.IsOn() && instance.Playback != null && instance.Playback.Streams.TryGetValue(1uL, out PeerVoiceStream value))
			{
				level = (value.Speaking ? value.OutputRms : 0f);
			}
			Drive(localPlayer, level, deltaTime);
		}
		if (instance.Playback != null)
		{
			IReadOnlyList<RosterEntry> entries = instance.Roster.Entries;
			for (int i = 0; i < entries.Count; i++)
			{
				RosterEntry rosterEntry = entries[i];
				if (!(rosterEntry.Player == null) && (bool)rosterEntry.Player)
				{
					float level2 = ((instance.Playback.Streams.TryGetValue(rosterEntry.SteamId, out PeerVoiceStream value2) && value2.Speaking) ? value2.OutputRms : 0f);
					Drive(rosterEntry.Player, level2, deltaTime);
				}
			}
		}
		Sweep();
	}

	private void Drive(Player player, float level, float deltaTime)
	{
		if (!_mouths.TryGetValue(player, out Mouth value))
		{
			value = new Mouth
			{
				Seed = Random.Range(0f, 10f)
			};
			_mouths[player] = value;
		}
		if (value.Missing)
		{
			return;
		}
		if (value.Jaw == null)
		{
			value.Jaw = FindJaw(player);
			if (value.Jaw == null)
			{
				value.Missing = true;
				return;
			}
			value.Rest = value.Jaw.localRotation;
		}
		float num = ((level <= 0.012f) ? 0f : Mathf.Clamp01(Mathf.Sqrt(level * ProximityVoiceChatPlugin.JawSensitivity.Value)));
		if (num > 0f)
		{
			num *= 0.85f + 0.15f * Mathf.PerlinNoise(value.Seed, Time.time * 9f);
		}
		float num2 = ((num > value.Open) ? 14f : 7f);
		value.Open = Mathf.MoveTowards(value.Open, num, num2 * deltaTime);
		if (value.Open <= 0.0005f)
		{
			value.Jaw.localRotation = value.Rest;
			return;
		}
		float angle = value.Open * ProximityVoiceChatPlugin.JawMaxAngle.Value;
		value.Jaw.localRotation = value.Rest * Quaternion.AngleAxis(angle, Axis());
	}

	private static Vector3 Axis()
	{
		return ProximityVoiceChatPlugin.JawAxis.Value switch
		{
			ProximityVoiceChatPlugin.Bone.Y => Vector3.up, 
			ProximityVoiceChatPlugin.Bone.Z => Vector3.forward, 
			_ => Vector3.right, 
		};
	}

	private static Transform? FindJaw(Player player)
	{
		Transform[] componentsInChildren = player.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.name == "Jaw")
			{
				return transform;
			}
		}
		return null;
	}

	private void Sweep()
	{
		_stale.Clear();
		foreach (KeyValuePair<Player, Mouth> mouth in _mouths)
		{
			if (mouth.Key == null || !mouth.Key)
			{
				_stale.Add(mouth.Key);
			}
		}
		for (int i = 0; i < _stale.Count; i++)
		{
			_mouths.Remove(_stale[i]);
		}
	}

	private void ReleaseAll()
	{
		foreach (KeyValuePair<Player, Mouth> mouth in _mouths)
		{
			Mouth value = mouth.Value;
			if (value.Jaw != null)
			{
				value.Jaw.localRotation = value.Rest;
			}
		}
		_mouths.Clear();
	}

	private void OnDestroy()
	{
		ReleaseAll();
	}
}
