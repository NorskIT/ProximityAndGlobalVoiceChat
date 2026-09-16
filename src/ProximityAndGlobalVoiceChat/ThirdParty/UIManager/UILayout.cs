using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UIManager;

internal static class UILayout
{
	public class Overlap
	{
		public Transform Target;

		public string Path;

		public Rect Rect;

		public float ShiftUp;

		public float ShiftDown;

		public float ShiftLeft;

		public float ShiftRight;

		public float Smallest => Mathf.Min(Mathf.Min(ShiftUp, ShiftDown), Mathf.Min(ShiftLeft, ShiftRight));
	}

	public static Rect ScreenRect(RectTransform rect)
	{
		if (rect == null)
		{
			return default(Rect);
		}
		Vector3[] array = new Vector3[4];
		rect.GetWorldCorners(array);
		Canvas componentInParent = rect.GetComponentInParent<Canvas>();
		Camera cam = ((componentInParent != null && componentInParent.renderMode != RenderMode.ScreenSpaceOverlay) ? componentInParent.worldCamera : null);
		Vector2 vector = RectTransformUtility.WorldToScreenPoint(cam, array[0]);
		Vector2 vector2 = RectTransformUtility.WorldToScreenPoint(cam, array[2]);
		return Rect.MinMaxRect(Mathf.Min(vector.x, vector2.x), Mathf.Min(vector.y, vector2.y), Mathf.Max(vector.x, vector2.x), Mathf.Max(vector.y, vector2.y));
	}

	public static bool TryVisibleRect(GameObject target, out Rect result)
	{
		result = default(Rect);
		if (target == null)
		{
			return false;
		}
		bool flag = false;
		Graphic[] componentsInChildren = target.GetComponentsInChildren<Graphic>(includeInactive: false);
		foreach (Graphic graphic in componentsInChildren)
		{
			if (IsVisible(graphic) && ClippedRect(graphic, out var result2))
			{
				result = (flag ? Union(result, result2) : result2);
				flag = true;
			}
		}
		return flag;
	}

	private static bool ClippedRect(Graphic graphic, out Rect result)
	{
		result = ScreenRect(graphic.rectTransform);
		if (result.width <= 0f || result.height <= 0f)
		{
			return false;
		}
		Transform parent = graphic.transform.parent;
		while (parent != null)
		{
			RectMask2D component = parent.GetComponent<RectMask2D>();
			if (!(component == null) && component.enabled && component.gameObject.activeInHierarchy)
			{
				result = Intersect(result, ScreenRect(component.rectTransform));
				if (result.width <= 0f || result.height <= 0f)
				{
					return false;
				}
			}
			parent = parent.parent;
		}
		return true;
	}

	private static Rect Intersect(Rect a, Rect b)
	{
		return Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
	}

	private static bool IsVisible(Graphic graphic)
	{
		if (graphic == null || !graphic.isActiveAndEnabled)
		{
			return false;
		}
		if (graphic.color.a <= 0.05f)
		{
			return false;
		}
		if (graphic.canvasRenderer != null && graphic.canvasRenderer.GetAlpha() <= 0.05f)
		{
			return false;
		}
		return true;
	}

	private static Rect Union(Rect a, Rect b)
	{
		return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
	}

	private static Rect PaintedRect(RectTransform panel)
	{
		Rect rect = ScreenRect(panel);
		if (!TryVisibleRect(panel.gameObject, out var result))
		{
			return rect;
		}
		return Union(result, rect);
	}

	public static List<Overlap> Overlaps(Rect mine, Transform ignoreSubtree = null, float minimumArea = 400f)
	{
		List<Overlap> list = new List<Overlap>();
		if (mine.width <= 0f || mine.height <= 0f)
		{
			return list;
		}
		float num = 1f;
		if (UIRoot.Front != null)
		{
			Canvas componentInParent = UIRoot.Front.GetComponentInParent<Canvas>();
			if (componentInParent != null && componentInParent.scaleFactor > 0f)
			{
				num = componentInParent.scaleFactor;
			}
		}
		foreach (Graphic item in VanillaGraphics())
		{
			if (IsVisible(item) && (!(ignoreSubtree != null) || !item.transform.IsChildOf(ignoreSubtree)))
			{
				Rect rect = ScreenRect(item.rectTransform);
				if (!(rect.width * rect.height < minimumArea) && (!(rect.width >= (float)Screen.width * 0.98f) || !(rect.height >= (float)Screen.height * 0.98f)) && mine.Overlaps(rect))
				{
					list.Add(new Overlap
					{
						Target = item.transform,
						Path = UIExtensions.PathOf(item.transform),
						Rect = rect,
						ShiftUp = (rect.yMax - mine.yMin) / num,
						ShiftDown = (mine.yMax - rect.yMin) / num,
						ShiftLeft = (mine.xMax - rect.xMin) / num,
						ShiftRight = (rect.xMax - mine.xMin) / num
					});
				}
			}
		}
		Collapse(list);
		list.Sort((Overlap a, Overlap b) => b.ShiftUp.CompareTo(a.ShiftUp));
		return list;
	}

	private static void Collapse(List<Overlap> overlaps)
	{
		for (int num = overlaps.Count - 1; num >= 0; num--)
		{
			for (int i = 0; i < overlaps.Count; i++)
			{
				if (num != i && !(overlaps[i].Target == null) && !(overlaps[num].Target == null) && overlaps[num].Target.IsChildOf(overlaps[i].Target))
				{
					overlaps.RemoveAt(num);
					break;
				}
			}
		}
	}

	private static IEnumerable<Graphic> VanillaGraphics()
	{
		foreach (GameObject item in VanillaUI.RootObjects())
		{
			if (!(item == null))
			{
				Graphic[] componentsInChildren = item.GetComponentsInChildren<Graphic>(includeInactive: false);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					yield return componentsInChildren[i];
				}
			}
		}
	}

	private static float ScaleFactor(RectTransform rect)
	{
		Canvas canvas = ((rect != null) ? rect.GetComponentInParent<Canvas>() : null);
		if (!(canvas != null) || !(canvas.scaleFactor > 0f))
		{
			return 1f;
		}
		return canvas.scaleFactor;
	}

	private static RectTransform CanvasRect(RectTransform rect)
	{
		Canvas canvas = ((rect != null) ? rect.GetComponentInParent<Canvas>() : null);
		if (canvas == null)
		{
			return null;
		}
		return ((canvas.rootCanvas != null) ? canvas.rootCanvas : canvas).transform as RectTransform;
	}

	public static bool ClampToCanvas(RectTransform panel, float margin = 8f)
	{
		if (panel == null)
		{
			return false;
		}
		RectTransform rectTransform = CanvasRect(panel);
		if (rectTransform == null)
		{
			return false;
		}
		float num = ScaleFactor(panel);
		Rect rect = ScreenRect(rectTransform);
		if (rect.height <= 0f)
		{
			return false;
		}
		bool result = false;
		Rect rect2 = PaintedRect(panel);
		float num2 = rect.height - margin * 2f * num;
		if (rect2.height > num2 && Mathf.Approximately(panel.anchorMin.y, panel.anchorMax.y))
		{
			float num3 = (rect2.height - num2) / num;
			panel.sizeDelta = new Vector2(panel.sizeDelta.x, Mathf.Max(64f, panel.sizeDelta.y - num3));
			result = true;
			rect2 = PaintedRect(panel);
		}
		float num4 = 0f;
		float num5 = 0f;
		if (rect2.xMin < rect.xMin + margin * num)
		{
			num4 = rect.xMin + margin * num - rect2.xMin;
		}
		else if (rect2.xMax > rect.xMax - margin * num)
		{
			num4 = rect.xMax - margin * num - rect2.xMax;
		}
		if (rect2.yMin < rect.yMin + margin * num)
		{
			num5 = rect.yMin + margin * num - rect2.yMin;
		}
		else if (rect2.yMax > rect.yMax - margin * num)
		{
			num5 = rect.yMax - margin * num - rect2.yMax;
		}
		if (Mathf.Abs(num4) < 0.5f && Mathf.Abs(num5) < 0.5f)
		{
			return result;
		}
		panel.anchoredPosition += new Vector2(num4, num5) / num;
		return true;
	}

	public static float NudgeClear(RectTransform panel, float maxShift = 200f)
	{
		if (panel == null)
		{
			return 0f;
		}
		if (!TryVisibleRect(panel.gameObject, out var result))
		{
			return 0f;
		}
		float num = 0f;
		foreach (Overlap item in Overlaps(result, panel))
		{
			num = Mathf.Max(num, item.ShiftUp);
		}
		if (num <= 0.5f || num > maxShift)
		{
			return 0f;
		}
		panel.anchoredPosition += new Vector2(0f, num);
		return num;
	}

	public static void Fit(RectTransform panel, float margin = 8f, float maxNudge = 200f)
	{
		if (!(panel == null))
		{
			ClampToCanvas(panel, margin);
			NudgeClear(panel, maxNudge);
			ClampToCanvas(panel, margin);
		}
	}

	public static float FitToContent(RectTransform panel, ScrollRect scroll, float minHeight = 120f)
	{
		if (panel == null || scroll == null || scroll.content == null || scroll.viewport == null)
		{
			return 0f;
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
		float num = panel.rect.height - scroll.viewport.rect.height;
		float num2 = LayoutUtility.GetPreferredHeight(scroll.content);
		if (num2 <= 0f)
		{
			num2 = scroll.content.rect.height;
		}
		float num3 = Mathf.Max(minHeight, num2 + num);
		if (Mathf.Abs(num3 - panel.rect.height) < 1f)
		{
			return panel.rect.height;
		}
		panel.sizeDelta = new Vector2(panel.sizeDelta.x, num3);
		return num3;
	}

	public static string Report(GameObject window, int maxPerPanel = 8)
	{
		if (window == null)
		{
			return "no window";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(window.name + ":");
		bool flag = false;
		foreach (Transform item in window.transform)
		{
			if (item.gameObject.activeInHierarchy && TryVisibleRect(item.gameObject, out var result))
			{
				flag = true;
				ReportPanel(stringBuilder, item.name, result, window.transform, maxPerPanel);
			}
		}
		if (!flag && TryVisibleRect(window, out var result2))
		{
			ReportPanel(stringBuilder, window.name, result2, window.transform, maxPerPanel);
		}
		return stringBuilder.ToString();
	}

	private static void ReportPanel(StringBuilder text, string name, Rect rect, Transform ignore, int max)
	{
		text.AppendLine($"  {name}: x {rect.xMin:0}..{rect.xMax:0}, y {rect.yMin:0}..{rect.yMax:0}");
		List<Overlap> list = Overlaps(rect, ignore);
		if (list.Count == 0)
		{
			text.AppendLine("    clear of vanilla UI");
			return;
		}
		float num = 0f;
		int num2 = 0;
		foreach (Overlap item in list)
		{
			num = Mathf.Max(num, item.ShiftUp);
			if (num2++ < max)
			{
				text.AppendLine("    over " + item.Path);
				text.AppendLine($"      up {item.ShiftUp:0} | down {item.ShiftDown:0} | " + $"left {item.ShiftLeft:0} | right {item.ShiftRight:0}");
			}
		}
		if (num2 > max)
		{
			text.AppendLine($"    ...and {num2 - max} more");
		}
		text.AppendLine($"    move up {num:0} to clear everything (add to this panel's anchoredPosition.y)");
	}
}
