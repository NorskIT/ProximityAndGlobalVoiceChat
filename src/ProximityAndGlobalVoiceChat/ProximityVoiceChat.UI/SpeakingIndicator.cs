using System.Collections.Generic;
using ProximityVoiceChat.Voice;
using ProximityVoiceChat.Voice.Audio;
using UIManager;
using UnityEngine;
using UnityEngine.UI;

namespace ProximityVoiceChat.UI;

internal sealed class SpeakingIndicator : MonoBehaviour
{
	private sealed class Marker
	{
		internal GameObject Root;

		internal Image Icon; internal Text Channel;

		internal RectTransform Rect;

		internal float Shown;
	}

	private const float FadeRate = 6f;

	private const float IconSize = 34f;

	private const float PeerHeadHeight = 1.5f;

	private const float FadeBandFraction = 0.25f;

	private const float BounceBase = 0.9f;

	private const float BounceDepth = 0.25f;

	private const float Invisible = 0.001f;

	private static readonly List<RosterEntry> NoPeers = new List<RosterEntry>();

	private readonly List<Marker> _peers = new List<Marker>();

	private Marker? _self;

	private Hud? _hud;

	private void OnDestroy()
	{
		Teardown();
	}

	private void LateUpdate()
	{
		bool flag = ProximityVoiceChatPlugin.ShowSpeakingIndicator.Value.IsOn();
		bool flag2 = ProximityVoiceChatPlugin.PeerIndicators.Value.IsOn();
		if (!flag && !flag2)
		{
			Teardown();
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		Hud instance = Hud.instance;
		Camera mainCamera = Utils.GetMainCamera();
		if (localPlayer == null || !localPlayer || instance == null || !instance || mainCamera == null || Hud.IsUserHidden())
		{
			HideAll();
			return;
		}
		if (_hud != instance)
		{
			Teardown();
			_hud = instance;
		}
		if (flag)
		{
			DrawSelf(instance, mainCamera, localPlayer);
		}
		else
		{
			Fade(_self);
		}
		DrawPeers(instance, mainCamera, flag2);
	}

	private void DrawSelf(Hud hud, Camera camera, Player player)
	{
		if (_self == null)
		{
			_self = Build(hud, "PVC_Speaking");
		}
		_self.Channel.text = VoiceRuntime.Instance?.SelectedChannel == VoiceChannel.Global ? "Global" : "Local";
VoiceEncoder voiceEncoder = VoiceRuntime.Instance?.Encoder;
		bool flag = ProximityVoiceChatPlugin.MuteSelf.Value.IsOn() || ProximityVoiceChatPlugin.Deafen.Value.IsOn();
		bool flag2 = !flag && voiceEncoder != null && voiceEncoder.IsSpeaking && VoiceRuntime.GateOpen();
		float level = ((!flag && voiceEncoder != null) ? UIBuild.MeterScale(voiceEncoder.LastRms) : 0f);
		_self.Icon.sprite = (flag ? VoiceSprites.Muted : VoiceSprites.SpeakerTalking);
		Vector3 world = player.GetHeadPoint() + Vector3.up * ProximityVoiceChatPlugin.SpeakingIndicatorHeight.Value;
		Place(_self, camera, world, flag2 | flag, flag ? GameColors.Danger : GameColors.Beige, level, 1f, 1f);
	}

	private void DrawPeers(Hud hud, Camera camera, bool wantPeers)
	{
		VoiceRuntime instance = VoiceRuntime.Instance;
		IReadOnlyList<RosterEntry> readOnlyList;
		if (!((instance != null) & wantPeers))
		{
			IReadOnlyList<RosterEntry> noPeers = NoPeers;
			readOnlyList = noPeers;
		}
		else
		{
			readOnlyList = instance.Roster.Entries;
		}
		IReadOnlyList<RosterEntry> readOnlyList2 = readOnlyList;
		float value = ProximityVoiceChatPlugin.PeerIndicatorScale.Value;
		bool flag = ProximityVoiceChatPlugin.PeerIndicatorThroughWalls.Value.IsOn();
		float value2 = ProximityVoiceChatPlugin.MaxRange.Value;
		while (_peers.Count < readOnlyList2.Count)
		{
			_peers.Add(Build(hud, "PVC_PeerSpeaking"));
		}
		for (int i = 0; i < _peers.Count; i++)
		{
			Marker marker = _peers[i];
			if (i >= readOnlyList2.Count)
			{
				Fade(marker);
				continue;
			}
			RosterEntry rosterEntry = readOnlyList2[i];
			instance.Links.TryGetValue(rosterEntry.SteamId, out PeerLink value3);
			bool flag2 = PeerPreferences.IsSilenced(rosterEntry.SteamId);
			PeerVoiceStream peerVoiceStream = null;
			if (instance.Playback != null && instance.Playback.Streams.TryGetValue(rosterEntry.SteamId, out PeerVoiceStream value4))
			{
				peerVoiceStream = value4;
			}
			bool num = ((!flag2) ? (peerVoiceStream?.Speaking ?? false) : (value3?.Transmitting ?? false));
			bool flag3 = !flag && (peerVoiceStream?.Occluded ?? false);
			bool wanted = num && !flag3 && rosterEntry.HasPosition && (value3?.InRange ?? false);
			marker.Icon.sprite = (flag2 ? VoiceSprites.Muted : VoiceSprites.SpeakerTalking);
			float level = ((!flag2 && peerVoiceStream != null) ? UIBuild.MeterScale(peerVoiceStream.OutputRms) : 0f);
			float num2 = value3?.Distance ?? value2;
			float fade = Mathf.Clamp01((value2 - num2) / Mathf.Max(1f, value2 * 0.25f));
			Vector3 world = HeadOf(rosterEntry) + Vector3.up * ProximityVoiceChatPlugin.PeerIndicatorHeight.Value;
			Place(marker, camera, world, wanted, flag2 ? GameColors.Danger : GameColors.Beige, level, value, fade);
		}
	}

	private static Vector3 HeadOf(RosterEntry entry)
	{
		if (!(entry.Player != null) || !entry.Player)
		{
			return entry.LastKnownPosition + Vector3.up * 1.5f;
		}
		return entry.Player.GetHeadPoint();
	}

	private static void Place(Marker marker, Camera camera, Vector3 world, bool wanted, Color color, float level, float scale, float fade)
	{
		marker.Shown = Mathf.MoveTowards(marker.Shown, wanted ? 1f : 0f, 6f * Time.unscaledDeltaTime);
		float num = marker.Shown * fade;
		if (num <= 0.001f)
		{
			Hide(marker);
			return;
		}
		Vector3 position = camera.WorldToScreenPointScaled(world);
		if (position.z <= 0f)
		{
			Hide(marker);
			return;
		}
		marker.Root.SetActive(value: true);
		marker.Rect.position = position;
		marker.Rect.localScale = Vector3.one * scale * (0.9f + 0.25f * level) * marker.Shown;
		color.a = num;
		marker.Icon.color = color; marker.Channel.color = color;
	}

	private static Marker Build(Hud hud, string name)
	{
		VoiceSprites.Resolve();
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		gameObject.transform.SetParent(hud.transform, worldPositionStays: false);
		RectTransform rectTransform = (RectTransform)gameObject.transform;
		rectTransform.sizeDelta = new Vector2(34f, 34f);
		Image component = gameObject.GetComponent<Image>();
		component.sprite = VoiceSprites.SpeakerTalking;
		component.raycastTarget = false;
		component.preserveAspect = true;
		Text label = UIFactory.Text("Channel", gameObject.transform, "", TextRole.Body, 13, TextAnchor.MiddleCenter);
label.raycastTarget = false; label.rectTransform.sizeDelta = new Vector2(100, 24); label.rectTransform.anchoredPosition = new Vector2(0, -28);
gameObject.SetActive(value: false);
		return new Marker
		{
			Root = gameObject,
			Icon = component, Channel = label,
			Rect = rectTransform
		};
	}

	private static void Fade(Marker? marker)
	{
		if (marker != null)
		{
			marker.Shown = Mathf.MoveTowards(marker.Shown, 0f, 6f * Time.unscaledDeltaTime);
			if (marker.Shown <= 0.001f)
			{
				Hide(marker);
			}
		}
	}

	private static void Hide(Marker marker)
	{
		if (marker.Root != null && marker.Root.activeSelf)
		{
			marker.Root.SetActive(value: false);
		}
	}

	private void HideAll()
	{
		if (_self != null)
		{
			Hide(_self);
		}
		for (int i = 0; i < _peers.Count; i++)
		{
			Hide(_peers[i]);
		}
	}

	private void Teardown()
	{
		if (_self != null)
		{
			Object.Destroy(_self.Root);
			_self = null;
		}
		for (int i = 0; i < _peers.Count; i++)
		{
			Object.Destroy(_peers[i].Root);
		}
		_peers.Clear();
		_hud = null;
	}
}
