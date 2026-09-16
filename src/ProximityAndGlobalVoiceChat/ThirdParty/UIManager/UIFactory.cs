using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UIManager;

internal static class UIFactory
{
	private static DefaultControls.Resources Resources => default(DefaultControls.Resources);

	public static GameObject Panel(string name, Transform parent, Vector2 size, bool window = true)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		Attach(gameObject, parent, size);
		Image component = gameObject.GetComponent<Image>();
		if (window)
		{
			Skin.Window(component);
			return gameObject;
		}
		Skin.Panel(component);
		return gameObject;
	}

	public static Text Text(string name, Transform parent, string content, TextRole role = TextRole.Body, int fontSize = 0, TextAnchor alignment = TextAnchor.UpperLeft)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Text component = gameObject.GetComponent<Text>();
		component.text = content;
		component.alignment = alignment;
		component.supportRichText = true;
		Skin.Text(component, role, fontSize);
		return component;
	}

	public static TextMeshProUGUI TmpText(string name, Transform parent, string content, TextRole role = TextRole.Body, int fontSize = 0, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		TextMeshProUGUI textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
		textMeshProUGUI.text = content;
		textMeshProUGUI.alignment = alignment;
		Skin.Text(textMeshProUGUI, role, fontSize);
		return textMeshProUGUI;
	}

	public static Button Button(string name, Transform parent, string label, Vector2 size, UnityAction onClick = null, int fontSize = 0)
	{
		GameObject gameObject = DefaultControls.CreateButton(Resources);
		gameObject.name = name;
		Attach(gameObject, parent, size);
		Button component = gameObject.GetComponent<Button>();
		Text componentInChildren = gameObject.GetComponentInChildren<Text>();
		if (componentInChildren != null)
		{
			componentInChildren.text = label;
		}
		Skin.Button(component, fontSize);
		if (onClick != null)
		{
			component.onClick.AddListener(onClick);
		}
		return component;
	}

	public static InputField InputField(string name, Transform parent, Vector2 size, string placeholder = "", UnityAction<string> onChanged = null, int fontSize = 0)
	{
		GameObject gameObject = DefaultControls.CreateInputField(Resources);
		gameObject.name = name;
		Attach(gameObject, parent, size);
		InputField component = gameObject.GetComponent<InputField>();
		if (component.placeholder is Text text)
		{
			text.text = placeholder;
		}
		Skin.InputField(component, fontSize);
		if (onChanged != null)
		{
			component.onValueChanged.AddListener(onChanged);
		}
		return component;
	}

	public static Toggle Toggle(string name, Transform parent, string label, bool value = false, UnityAction<bool> onChanged = null)
	{
		GameObject gameObject = DefaultControls.CreateToggle(Resources);
		gameObject.name = name;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Toggle component = gameObject.GetComponent<Toggle>();
		component.isOn = value;
		Text componentInChildren = gameObject.GetComponentInChildren<Text>();
		if (componentInChildren != null)
		{
			componentInChildren.text = label;
			Skin.Text(componentInChildren);
		}
		Skin.Toggle(component);
		if (onChanged != null)
		{
			component.onValueChanged.AddListener(onChanged);
		}
		return component;
	}

	public static Dropdown Dropdown(string name, Transform parent, Vector2 size, IEnumerable<string> options = null, UnityAction<int> onChanged = null)
	{
		GameObject gameObject = DefaultControls.CreateDropdown(Resources);
		gameObject.name = name;
		Attach(gameObject, parent, size);
		Dropdown component = gameObject.GetComponent<Dropdown>();
		if (options != null)
		{
			component.ClearOptions();
			component.AddOptions(options.ToList());
		}
		Text[] componentsInChildren = gameObject.GetComponentsInChildren<Text>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Skin.Text(componentsInChildren[i]);
		}
		Image componentInChildren = gameObject.GetComponentInChildren<Image>();
		if ((object)componentInChildren != null)
		{
			Skin.Panel(componentInChildren);
		}
		Scrollbar componentInChildren2 = gameObject.GetComponentInChildren<Scrollbar>(includeInactive: true);
		if ((object)componentInChildren2 != null)
		{
			Skin.Scrollbar(componentInChildren2);
		}
		if (onChanged != null)
		{
			component.onValueChanged.AddListener(onChanged);
		}
		return component;
	}

	public static Slider Slider(string name, Transform parent, Vector2 size, float min = 0f, float max = 1f, float value = 0f, UnityAction<float> onChanged = null)
	{
		GameObject gameObject = DefaultControls.CreateSlider(Resources);
		gameObject.name = name;
		Attach(gameObject, parent, size);
		Slider component = gameObject.GetComponent<Slider>();
		component.minValue = min;
		component.maxValue = max;
		component.value = Mathf.Clamp(value, min, max);
		Skin.Slider(component);
		if (onChanged != null)
		{
			component.onValueChanged.AddListener(onChanged);
		}
		return component;
	}

	public static ScrollRect ScrollView(string name, Transform parent, Vector2 size, out RectTransform content, bool vertical = true, bool horizontal = false)
	{
		GameObject gameObject = DefaultControls.CreateScrollView(Resources);
		gameObject.name = name;
		Attach(gameObject, parent, size);
		ScrollRect component = gameObject.GetComponent<ScrollRect>();
		component.vertical = vertical;
		component.horizontal = horizontal;
		content = component.content;
		if (content != null)
		{
			content.anchorMin = new Vector2(0f, 1f);
			content.anchorMax = new Vector2(1f, 1f);
			content.pivot = new Vector2(0.5f, 1f);
			content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			VerticalLayoutGroup verticalLayoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
			verticalLayoutGroup.childControlHeight = false;
			verticalLayoutGroup.childForceExpandHeight = false;
		}
		if (component.viewport != null)
		{
			Image component2 = component.viewport.GetComponent<Image>();
			if ((object)component2 != null)
			{
				component2.color = Color.white;
				component2.raycastTarget = true;
			}
			Mask component3 = component.viewport.GetComponent<Mask>();
			if ((object)component3 != null)
			{
				component3.showMaskGraphic = false;
			}
		}
		Image component4 = gameObject.GetComponent<Image>();
		if ((object)component4 != null)
		{
			component4.enabled = false;
		}
		Skin.ScrollRect(component);
		if (component.verticalScrollbar != null)
		{
			Skin.Scrollbar(component.verticalScrollbar);
		}
		if (component.horizontalScrollbar != null)
		{
			Skin.Scrollbar(component.horizontalScrollbar);
		}
		return component;
	}

	private static void Attach(GameObject go, Transform parent, Vector2 size)
	{
		go.transform.SetParent(parent, worldPositionStays: false);
		RectTransform obj = (RectTransform)go.transform;
		obj.localScale = Vector3.one;
		obj.sizeDelta = size;
	}
}
