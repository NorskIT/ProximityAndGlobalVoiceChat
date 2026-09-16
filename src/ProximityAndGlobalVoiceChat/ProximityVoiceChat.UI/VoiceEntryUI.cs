using System.Linq;
using ProximityVoiceChat.Voice;
using ProximityVoiceChat.Voice.Audio;
using UIManager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Valheim.UI;

namespace ProximityVoiceChat.UI;

internal sealed class VoiceEntryUI : MonoBehaviour
{
	private static readonly Color Speaking = new Color(0.45f, 1f, 0.45f, 1f);

	private static readonly Color Silent = new Color(1f, 1f, 1f, 0.55f);

	private static readonly Color Blocked = new Color(1f, 0.45f, 0.45f, 1f);

	private const float FallbackSpacing = -60f;

	private const float IconSize = 48f;

	private const float Gap = 12f;

	private const float RefreshInterval = 0.1f;

	private const float ExtraHeight = 40f;

	private const float LabelWidth = 64f;

	private const float SliderWidth = 296f;

	private const float SliderHeight = 14f;

	private const float ValueWidth = 46f;

	private const float TextHeight = 20f;

	private const int FontSize = 14;

	private const float DefaultNameLeft = 76f;

	private const float DefaultNameBottom = -28f;

	private static bool _warned;

	private Image? _icon;

	private Slider? _volume;

	private Text? _volumeText;

	private ulong _steamId;

	private bool _own;

	private bool _suppress;

	private float _timer;

	internal void Bind(SessionPlayerListEntry entry, bool isOwnPlayer)
	{
		_own = isOwnPlayer;
		if (PeerPreferences.TryGetSteamId(entry.User, out _steamId))
		{
			VoiceSprites.Resolve();
			GrowRow((RectTransform)entry.transform);
			BuildMute(entry);
			BuildVolume(entry);
			Refresh();
		}
	}

	private void BuildMute(SessionPlayerListEntry entry)
	{
		if (_icon != null)
		{
			return;
		}
		Transform transform = entry.transform.Find("PVC_MuteImage");
		if (transform != null)
		{
			_icon = transform.GetComponent<Image>();
			Wire((transform.childCount > 0) ? transform.GetChild(0).GetComponent<Button>() : null);
			return;
		}
		Transform transform2 = entry.transform.Find("MuteImage");
		if (transform2 != null)
		{
			transform2.gameObject.SetActive(value: true);
			_icon = transform2.GetComponent<Image>();
			Wire(transform2.Find("Mute")?.GetComponent<Button>());
			return;
		}
		RectTransform rectTransform = entry.transform.Find("BlockImage") as RectTransform;
		RectTransform rectTransform2 = entry.transform.Find("KickImage") as RectTransform;
		if (rectTransform == null)
		{
			if (!_warned)
			{
				_warned = true;
				ProximityVoiceChatPlugin.Log.LogWarning("No BlockImage on the player list row, so there is nothing to clone a mute button from. Player list voice controls are off.");
			}
			return;
		}
		float num = ((rectTransform2 != null) ? (rectTransform.anchoredPosition.x - rectTransform2.anchoredPosition.x) : (-60f));
		GameObject gameObject = Object.Instantiate(rectTransform.gameObject, entry.transform);
		gameObject.name = "PVC_MuteImage";
		gameObject.SetActive(value: true);
		Pin((RectTransform)gameObject.transform, rectTransform.anchoredPosition.x + num, 48f, 48f);
		_icon = gameObject.GetComponent<Image>();
		Transform transform3 = ((gameObject.transform.childCount > 0) ? gameObject.transform.GetChild(0) : null);
		if (transform3 != null)
		{
			transform3.name = "PVC_Mute";
			Wire(transform3.GetComponent<Button>());
		}
	}

	private void Wire(Button? button)
	{
		if (!(button == null))
		{
			for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
			{
				button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
			}
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(OnClick);
		}
	}

	private void BuildVolume(SessionPlayerListEntry entry)
	{
		if (_own || _volume != null)
		{
			return;
		}
		Slider slider = entry.GetComponentsInChildren<Slider>(includeInactive: true).FirstOrDefault((Slider x) => x.name == "PVC_Volume");
		if (slider != null)
		{
			_volume = slider;
			_volumeText = slider.transform.parent.Find("PVC_VolumeValue")?.GetComponent<Text>();
			_volume.onValueChanged.RemoveAllListeners();
			_volume.onValueChanged.AddListener(OnVolumeChanged);
			return;
		}
		RectTransform rectTransform = (RectTransform)entry.transform;
		RectTransform rectTransform2 = entry.transform.Find("Background") as RectTransform;
		if (!(rectTransform2 == null))
		{
			float y = (NameBottomEdge(rectTransform) - rectTransform.sizeDelta.y * 0.5f) * 0.5f;
			float num = NameLeftEdge(rectTransform);
			Text text = UIFactory.Text("PVC_VolumeLabel", rectTransform2, UIBuild.Localize("$pvc_volume_label"), TextRole.Body, 14, TextAnchor.MiddleLeft);
			PinLeft((RectTransform)text.transform, num, y, 64f, 20f);
			text.color = GameColors.Muted;
			_volume = UIFactory.Slider("PVC_Volume", rectTransform2, new Vector2(296f, 14f), 0f, 2f, PeerPreferences.GetVolume(_steamId), OnVolumeChanged);
			PinLeft((RectTransform)_volume.transform, num + 64f + 12f, y, 296f, 14f);
			_volumeText = UIFactory.Text("PVC_VolumeValue", rectTransform2, "", TextRole.Body, 14, TextAnchor.MiddleLeft);
			PinLeft((RectTransform)_volumeText.transform, num + 64f + 12f + 296f + 12f, y, 46f, 20f);
			_volumeText.color = GameColors.Beige;
		}
	}

	private static void GrowRow(RectTransform row)
	{
		if (!(row.Find("PVC_Tall") != null))
		{
			GameObject obj = new GameObject("PVC_Tall", typeof(RectTransform));
			obj.transform.SetParent(row, worldPositionStays: false);
			obj.SetActive(value: false);
			row.sizeDelta = new Vector2(row.sizeDelta.x, row.sizeDelta.y + 40f);
			LayoutRebuilder.MarkLayoutForRebuild(row);
		}
	}

	private static float NameLeftEdge(RectTransform row)
	{
		RectTransform[] componentsInChildren = row.GetComponentsInChildren<RectTransform>(includeInactive: true);
		foreach (RectTransform rectTransform in componentsInChildren)
		{
			if (rectTransform.name == "CharacterName" && Mathf.Approximately(rectTransform.anchorMin.x, 0f))
			{
				return rectTransform.anchoredPosition.x - rectTransform.pivot.x * rectTransform.rect.width;
			}
		}
		return 76f;
	}

	private static float NameBottomEdge(RectTransform row)
	{
		float num = float.PositiveInfinity;
		RectTransform[] componentsInChildren = row.GetComponentsInChildren<RectTransform>(includeInactive: true);
		foreach (RectTransform rectTransform in componentsInChildren)
		{
			if (!(rectTransform.name != "CharacterName") || !(rectTransform.name != "Gamertag"))
			{
				num = Mathf.Min(num, rectTransform.anchoredPosition.y - (1f - rectTransform.pivot.y) * rectTransform.rect.height);
			}
		}
		if (!float.IsPositiveInfinity(num))
		{
			return num;
		}
		return -28f;
	}

	private static void Pin(RectTransform rect, float x, float width, float height)
	{
		rect.anchorMin = new Vector2(1f, 0.5f);
		rect.anchorMax = new Vector2(1f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.localScale = Vector3.one;
		rect.sizeDelta = new Vector2(width, height);
		rect.anchoredPosition = new Vector2(x, 0f);
	}

	private static void PinLeft(RectTransform rect, float x, float y, float width, float height)
	{
		rect.anchorMin = new Vector2(0f, 0.5f);
		rect.anchorMax = new Vector2(0f, 0.5f);
		rect.pivot = new Vector2(0f, 0.5f);
		rect.localScale = Vector3.one;
		rect.sizeDelta = new Vector2(width, height);
		rect.anchoredPosition = new Vector2(x, y);
	}

	private void OnVolumeChanged(float value)
	{
		if (!_suppress)
		{
			PeerPreferences.SetVolume(_steamId, value);
			ProximityVoiceChatPlugin.SaveConfig();
		}
	}

	private void OnClick()
	{
		if (_own)
		{
			ProximityVoiceChatPlugin.MuteSelf.Value = ((!ProximityVoiceChatPlugin.MuteSelf.Value.IsOn()) ? ProximityVoiceChatPlugin.Toggle.On : ProximityVoiceChatPlugin.Toggle.Off);
		}
		else
		{
			PeerPreferences.ToggleMuted(_steamId);
			VoiceRuntime.Instance?.Playback?.Remove(_steamId);
			ProximityVoiceChatPlugin.SaveConfig();
		}
		Refresh();
	}

	private void Update()
	{
		if (!(_icon == null))
		{
			_timer += Time.unscaledDeltaTime;
			if (!(_timer < 0.1f))
			{
				_timer = 0f;
				Refresh();
			}
		}
	}

	private void Refresh()
	{
		if (!(_icon == null))
		{
			bool flag = (_own ? ProximityVoiceChatPlugin.MuteSelf.Value.IsOn() : PeerPreferences.IsSilenced(_steamId));
			if (VoiceSprites.Speaker != null && VoiceSprites.Muted != null)
			{
				_icon.sprite = (flag ? VoiceSprites.Muted : VoiceSprites.Speaker);
			}
			_icon.color = (flag ? Blocked : (IsTalking() ? Speaking : Silent));
			RefreshVolume(flag);
		}
	}

	private void RefreshVolume(bool muted)
	{
		if (!(_volume == null) && !(_volumeText == null))
		{
			float volume = PeerPreferences.GetVolume(_steamId);
			_suppress = true;
			_volume.SetValueWithoutNotify(volume);
			_suppress = false;
			_volume.interactable = !PeerPreferences.IsBlockedByGame(_steamId);
			_volumeText.text = (muted ? UIBuild.Localize("$pvc_mute") : volume.ToString("0.00"));
		}
	}

	private bool IsTalking()
	{
		VoiceRuntime instance = VoiceRuntime.Instance;
		if (instance == null)
		{
			return false;
		}
		if (_own)
		{
			return instance.Encoder?.IsTransmitting ?? false;
		}
		if (instance.Playback != null && instance.Playback.Streams.TryGetValue(_steamId, out PeerVoiceStream value))
		{
			return value.Speaking;
		}
		return false;
	}
}
