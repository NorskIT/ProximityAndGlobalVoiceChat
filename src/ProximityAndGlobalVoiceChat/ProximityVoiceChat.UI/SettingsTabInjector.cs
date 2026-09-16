using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProximityVoiceChat.UI;

internal static class SettingsTabInjector
{
	internal const string TabLabel = "Voice";

	internal static void Inject(Settings? settings)
	{
		if (settings == null)
		{
			return;
		}
		TabHandler tabHandler = ((settings.m_tabHandler != null) ? settings.m_tabHandler : settings.GetComponentInChildren<TabHandler>());
		if (tabHandler == null)
		{
			ProximityVoiceChatPlugin.Log.LogWarning("Voice tab: no TabHandler in the settings prefab");
			return;
		}
		if (tabHandler.m_tabs.Count == 0)
		{
			ProximityVoiceChatPlugin.Log.LogWarning("Voice tab: settings prefab has no tabs");
			return;
		}
		foreach (TabHandler.Tab tab in tabHandler.m_tabs)
		{
			if (tab.m_button != null && tab.m_button.name == "Voice")
			{
				return;
			}
		}
		Button button = null;
		RectTransform rectTransform = null;
		foreach (TabHandler.Tab tab2 in tabHandler.m_tabs)
		{
			if (tab2.m_button != null)
			{
				button = tab2.m_button;
			}
			if (tab2.m_page != null && rectTransform == null)
			{
				rectTransform = tab2.m_page;
			}
		}
		if (button == null || rectTransform == null)
		{
			ProximityVoiceChatPlugin.Log.LogWarning($"Voice tab: no usable template, button {button != null}, page {rectTransform != null}");
			return;
		}
		Button button2 = Object.Instantiate(button, button.transform.parent);
		button2.name = "Voice";
		button2.transform.SetAsLastSibling();
		button2.onClick.RemoveAllListeners();
		SetLabel(button2.gameObject, "Voice");
		Transform transform = button2.transform.Find("KeyHint");
		if (transform != null)
		{
			transform.gameObject.SetActive(value: false);
		}
		GameObject gameObject = new GameObject("VoicePage", typeof(RectTransform));
		gameObject.transform.SetParent(rectTransform.parent, worldPositionStays: false);
		RectTransform rectTransform2 = (RectTransform)gameObject.transform;
		rectTransform2.anchorMin = rectTransform.anchorMin;
		rectTransform2.anchorMax = rectTransform.anchorMax;
		rectTransform2.pivot = rectTransform.pivot;
		rectTransform2.offsetMin = rectTransform.offsetMin;
		rectTransform2.offsetMax = rectTransform.offsetMax;
		rectTransform2.localScale = Vector3.one;
		gameObject.AddComponent<VoiceSettingsTab>();
		gameObject.SetActive(value: false);
		tabHandler.m_tabs.Add(new TabHandler.Tab
		{
			m_button = button2,
			m_page = rectTransform2,
			m_default = false,
			m_onClick = new UnityEvent()
		});
		ProximityVoiceChatPlugin.Log.LogInfo($"Added the Voice tab to the settings menu, {tabHandler.m_tabs.Count} tabs now");
	}

	private static void SetLabel(GameObject button, string text)
	{
		TMP_Text[] componentsInChildren = button.GetComponentsInChildren<TMP_Text>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].text = text;
		}
		Text[] componentsInChildren2 = button.GetComponentsInChildren<Text>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].text = text;
		}
		Localize[] componentsInChildren3 = button.GetComponentsInChildren<Localize>(includeInactive: true);
		for (int j = 0; j < componentsInChildren3.Length; j++)
		{
			Object.Destroy(componentsInChildren3[j]);
		}
	}
}
