using System;
using BepInEx.Configuration;
using ProximityVoiceChat.Voice.Audio;
using UIManager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProximityVoiceChat.UI;

internal static class UIBuild
{
	internal const float RowHeight = 26f;

	internal const float LabelWidth = 210f;

	internal const float ValueWidth = 56f;

	private const float RowSpacing = 10f;

	private const float HeadingHeight = 28f;

	private const float NoteHeight = 20f;

	private const float ControlHeight = 24f;

	private const float ToggleHeight = 22f;

	private const float SliderHeight = 18f;

	private const float SliderWidth = 200f;

	private const float ButtonWidth = 160f;

	internal const float TallRowHeight = 28f;

	private const float ArrowWidth = 28f;

	private const float ArrowCell = 32f;

	private const float MeterRowHeight = 20f;

	private const float MeterLabelWidth = 70f;

	private const float MeterTrackWidth = 200f;

	private const float MeterTrackHeight = 12f;

	private const int BodyFontSize = 16;

	private const int TitleFontSize = 18;

	private const int NoteFontSize = 15;

	private const int SmallFontSize = 14;

	internal const float MeterFloorDb = -60f;

	private const float MeterSilence = 0.0001f;

	private const float AmplitudeToDb = 20f;

	internal static string Localize(string key)
	{
		if (Localization.instance == null)
		{
			return key;
		}
		return Localization.instance.Localize(key);
	}

	internal static string Localize(string key, params object[] args)
	{
		return string.Format(Localize(key), args);
	}

	internal static GameObject Row(Transform parent, float height)
	{
		GameObject gameObject = new GameObject("Row", typeof(RectTransform));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		HorizontalLayoutGroup horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
		horizontalLayoutGroup.spacing = 10f;
		horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
		horizontalLayoutGroup.childControlWidth = true;
		horizontalLayoutGroup.childControlHeight = true;
		horizontalLayoutGroup.childForceExpandWidth = false;
		horizontalLayoutGroup.childForceExpandHeight = false;
		LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
		layoutElement.minHeight = height;
		layoutElement.preferredHeight = height;
		return gameObject;
	}

	internal static LayoutElement Element(Component component)
	{
		LayoutElement component2 = component.gameObject.GetComponent<LayoutElement>();
		if (!(component2 != null))
		{
			return component.gameObject.AddComponent<LayoutElement>();
		}
		return component2;
	}

	internal static T Fixed<T>(T component, float width, float height = 0f) where T : Component
	{
		LayoutElement layoutElement = Element(component);
		layoutElement.minWidth = width;
		layoutElement.preferredWidth = width;
		if (height > 0f)
		{
			layoutElement.minHeight = height;
			layoutElement.preferredHeight = height;
		}
		return component;
	}

	internal static T Flex<T>(T component) where T : Component
	{
		Element(component).flexibleWidth = 1f;
		return component;
	}

	internal static Text Label(Transform parent, string text)
	{
		return UIFactory.Text("Label", parent, text, TextRole.Body, 16, TextAnchor.MiddleLeft);
	}

	internal static Text Heading(Transform parent, string text)
	{
		Text text2 = UIFactory.Text("Heading", parent, text, TextRole.Title, 18, TextAnchor.MiddleLeft);
		Element(text2).preferredHeight = 28f;
		return text2;
	}

	internal static Text Note(Transform parent, string text)
	{
		Text text2 = UIFactory.Text("Note", parent, text, TextRole.Body, 15, TextAnchor.MiddleLeft);
		text2.color = GameColors.Beige;
		Element(text2).preferredHeight = 20f;
		return text2;
	}

	internal static float MeterScale(float amplitude)
	{
		if (amplitude <= 0.0001f)
		{
			return 0f;
		}
		return Mathf.Clamp01((20f * Mathf.Log10(amplitude) - -60f) / 60f);
	}

	internal static RectTransform Meter(Transform parent, string label)
	{
		GameObject gameObject = Row(parent, 20f);
		Fixed(UIFactory.Text("Label", gameObject.transform, label, TextRole.Body, 14, TextAnchor.MiddleLeft), 70f);
		GameObject gameObject2 = UIFactory.Panel("Track", gameObject.transform, new Vector2(200f, 12f), window: false);
		Flex(gameObject2.transform);
		Element(gameObject2.transform).preferredHeight = 12f;
		GameObject gameObject3 = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		RectTransform obj = (RectTransform)gameObject3.transform;
		obj.anchorMin = Vector2.zero;
		obj.anchorMax = new Vector2(0f, 1f);
		obj.pivot = new Vector2(0f, 0.5f);
		obj.offsetMin = Vector2.zero;
		obj.offsetMax = Vector2.zero;
		gameObject3.GetComponent<Image>().color = GameColors.Beige;
		return obj;
	}

	internal static SliderRow SliderControl(Transform parent, string label, float min, float max, float value, UnityAction<float> onChanged, string format = "0.00")
	{
		GameObject gameObject = Row(parent, 26f);
		Fixed(Label(gameObject.transform, label), 210f);
		SliderRow control = new SliderRow
		{
			Format = format
		};
		control.Slider = UIFactory.Slider(label, gameObject.transform, new Vector2(200f, 18f), min, max, value, delegate(float v)
		{
			onChanged(v);
			control.Value.text = v.ToString(control.Format);
		});
		Flex(control.Slider);
		Element(control.Slider).preferredHeight = 18f;
		control.Value = Fixed(UIFactory.Text("Value", gameObject.transform, value.ToString(format), TextRole.Body, 16, TextAnchor.MiddleRight), 56f);
		return control;
	}

	internal static SliderRow SliderFor(Transform parent, string label, ConfigEntry<float> entry, string format = "0.00")
	{
		AcceptableValueRange<float> acceptableValueRange = (AcceptableValueRange<float>)entry.Description.AcceptableValues;
		return SliderControl(parent, label, acceptableValueRange.MinValue, acceptableValueRange.MaxValue, entry.Value, delegate(float v)
		{
			if (!Mathf.Approximately(entry.Value, v))
			{
				entry.Value = v;
			}
		}, format);
	}

	internal static Toggle ToggleFor(Transform parent, string label, ConfigEntry<ProximityVoiceChatPlugin.Toggle> entry)
	{
		return RawToggle(parent, label, entry.Value.IsOn(), delegate(bool on)
		{
			entry.Value = (on ? ProximityVoiceChatPlugin.Toggle.On : ProximityVoiceChatPlugin.Toggle.Off);
		});
	}

	internal static Toggle RawToggle(Transform parent, string label, bool value, UnityAction<bool> onChanged)
	{
		GameObject gameObject = Row(parent, 26f);
		Toggle toggle = UIFactory.Toggle(label, gameObject.transform, label, value, onChanged);
		Flex(toggle);
		Element(toggle).preferredHeight = 22f;
		return toggle;
	}

	internal static KeyRow KeyBinding(Transform parent, string label, ConfigEntry<KeyboardShortcut> entry)
	{
		GameObject gameObject = Row(parent, 26f);
		Fixed(Label(gameObject.transform, label), 210f);
		KeyRow obj = new KeyRow
		{
			Entry = entry,
			Button = Flex(UIFactory.Button("Bind", gameObject.transform, KeyBinder.Describe(entry.Value), new Vector2(160f, 24f), delegate
			{
				KeyBinder.Begin(entry);
			}))
		};
		Element(obj.Button).preferredHeight = 24f;
		obj.Label = obj.Button.GetComponentInChildren<Text>();
		return obj;
	}

	internal static CycleRow<T> Cycle<T>(Transform parent, string label, ConfigEntry<T> entry) where T : struct, Enum
	{
		GameObject gameObject = Row(parent, 26f);
		Fixed(Label(gameObject.transform, label), 210f);
		CycleRow<T> cycleRow = new CycleRow<T>
		{
			Entry = entry
		};
		cycleRow.Button = Flex(UIFactory.Button("Cycle", gameObject.transform, entry.Value.ToString(), new Vector2(160f, 24f), cycleRow.Step));
		Element(cycleRow.Button).preferredHeight = 24f;
		cycleRow.Label = cycleRow.Button.GetComponentInChildren<Text>();
		return cycleRow;
	}

	internal static Text DevicePicker(Transform parent, string label)
	{
		GameObject gameObject = Row(parent, 28f);
		Fixed(Label(gameObject.transform, label), 210f);
		Fixed(UIFactory.Button("Prev", gameObject.transform, "<", new Vector2(28f, 24f), delegate
		{
			StepDevice(-1);
		}), 32f, 24f);
		Text result = Flex(UIFactory.Text("Device", gameObject.transform, "", TextRole.Body, 16, TextAnchor.MiddleCenter));
		Fixed(UIFactory.Button("Next", gameObject.transform, ">", new Vector2(28f, 24f), delegate
		{
			StepDevice(1);
		}), 32f, 24f);
		return result;
	}

	internal static void StepDevice(int direction)
	{
		string[] devices = MicrophoneCapture.Devices;
		if (devices.Length != 0)
		{
			int num = Array.IndexOf<string>(devices, ProximityVoiceChatPlugin.InputDevice.Value);
			if (num < 0)
			{
				num = 0;
			}
			num = (num + direction + devices.Length) % devices.Length;
			ProximityVoiceChatPlugin.InputDevice.Value = devices[num];
		}
	}

	internal static string CurrentDeviceName()
	{
		string value = ProximityVoiceChatPlugin.InputDevice.Value;
		if (!string.IsNullOrEmpty(value))
		{
			return value;
		}
		string[] devices = MicrophoneCapture.Devices;
		if (devices.Length == 0)
		{
			return Localize("$pvc_device_none");
		}
		return devices[0];
	}
}
