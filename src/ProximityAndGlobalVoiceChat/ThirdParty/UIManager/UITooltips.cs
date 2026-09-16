using UnityEngine;
using UnityEngine.UI;

namespace UIManager;

internal static class UITooltips
{
	public const float ShowDelay = 0.5f;

	private static GameObject _prefab;

	private static bool _warned;

	public static GameObject Prefab
	{
		get
		{
			if (_prefab != null)
			{
				return _prefab;
			}
			UITooltip[] array = Resources.FindObjectsOfTypeAll<UITooltip>();
			foreach (UITooltip uITooltip in array)
			{
				if (!(uITooltip == null) && !(uITooltip.m_tooltipPrefab == null))
				{
					_prefab = uITooltip.m_tooltipPrefab;
					break;
				}
			}
			if (_prefab == null && !_warned)
			{
				_warned = true;
				UILog.Warning("no vanilla tooltip found to copy; tooltips will not appear");
			}
			return _prefab;
		}
	}

	public static bool Available => Prefab != null;

	public static void Set(GameObject target, string topic, string text, RectTransform bounds = null)
	{
		if (target == null)
		{
			return;
		}
		UITooltipHost uITooltipHost = target.GetComponent<UITooltipHost>();
		if (uITooltipHost == null)
		{
			if (string.IsNullOrEmpty(topic) && string.IsNullOrEmpty(text))
			{
				return;
			}
			uITooltipHost = target.AddComponent<UITooltipHost>();
		}
		uITooltipHost.Topic = topic ?? string.Empty;
		uITooltipHost.Text = text ?? string.Empty;
		if (bounds != null)
		{
			uITooltipHost.Bounds = bounds;
		}
		uITooltipHost.Refresh();
	}

	public static void Clear(GameObject target)
	{
		Set(target, string.Empty, string.Empty);
	}

	public static void HideNow()
	{
		UITooltipHost.HideShared();
	}

	internal static GameObject Spawn(Canvas canvas)
	{
		GameObject prefab = Prefab;
		if (prefab == null || canvas == null)
		{
			return null;
		}
		GameObject gameObject = Object.Instantiate(prefab, canvas.transform);
		gameObject.name = "UIManager.Tooltip";
		gameObject.SetActive(value: true);
		gameObject.transform.SetAsLastSibling();
		Graphic[] componentsInChildren = gameObject.GetComponentsInChildren<Graphic>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].raycastTarget = false;
		}
		if (gameObject.transform is RectTransform rectTransform)
		{
			rectTransform.localScale = Vector3.one;
		}
		return gameObject;
	}
}
