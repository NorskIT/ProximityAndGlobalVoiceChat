using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIManager;

internal class UITooltipHost : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public string Topic = "";

	public string Text = "";

	public RectTransform Bounds;

	public float Gap = 10f;

	private static UITooltipHost _current;

	private static GameObject _shared;

	private float _timer;

	private Canvas _canvas;

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!string.IsNullOrEmpty(Topic) || !string.IsNullOrEmpty(Text))
		{
			_current = this;
			_timer = 0f;
			if (_shared != null)
			{
				Write();
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (_current == this)
		{
			HideShared();
		}
	}

	private void OnDisable()
	{
		if (_current == this)
		{
			HideShared();
		}
	}

	public void Refresh()
	{
		if (_current == this && _shared != null)
		{
			Write();
			Place();
		}
	}

	internal static void HideShared()
	{
		_current = null;
		if (_shared != null)
		{
			Object.Destroy(_shared);
		}
		_shared = null;
	}

	private void LateUpdate()
	{
		if (_current != this)
		{
			return;
		}
		if (_canvas == null)
		{
			_canvas = GetComponentInParent<Canvas>();
		}
		if (_canvas == null || _shared != null)
		{
			return;
		}
		_timer += Time.unscaledDeltaTime;
		if (!(_timer < 0.5f))
		{
			_shared = UITooltips.Spawn(_canvas);
			if (!(_shared == null))
			{
				Write();
				Place();
			}
		}
	}

	private void Write()
	{
		if (_shared == null)
		{
			return;
		}
		Transform transform = Utils.FindChild(_shared.transform, "Text");
		if (transform != null)
		{
			TMP_Text component = transform.GetComponent<TMP_Text>();
			if (component != null)
			{
				component.text = Text;
			}
		}
		Transform transform2 = Utils.FindChild(_shared.transform, "Topic");
		if (transform2 != null)
		{
			TMP_Text component2 = transform2.GetComponent<TMP_Text>();
			if (component2 != null)
			{
				component2.text = Topic;
			}
		}
		RectTransform rectTransform = Panel();
		if (rectTransform != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
		}
	}

	private RectTransform Panel()
	{
		if (_shared == null)
		{
			return null;
		}
		RectTransform rectTransform = _shared.transform as RectTransform;
		if (rectTransform == null)
		{
			return null;
		}
		RectTransform rectTransform2 = ((rectTransform.childCount > 0) ? (rectTransform.GetChild(0) as RectTransform) : null);
		if (!(rectTransform2 != null))
		{
			return rectTransform;
		}
		return rectTransform2;
	}

	private void Place()
	{
		RectTransform rectTransform = ((_shared != null) ? (_shared.transform as RectTransform) : null);
		RectTransform rectTransform2 = base.transform as RectTransform;
		RectTransform rectTransform3 = Panel();
		if (rectTransform == null || rectTransform2 == null || rectTransform3 == null)
		{
			return;
		}
		float width = rectTransform3.rect.width;
		float num = rectTransform2.rect.width * 0.5f + Gap;
		rectTransform.position = rectTransform2.TransformPoint(new Vector3(num, rectTransform2.rect.height * 0.5f, 0f));
		if (!(Bounds == null))
		{
			if (Overflows(rectTransform3, out var _))
			{
				rectTransform.position = rectTransform2.TransformPoint(new Vector3(0f - (num + width), rectTransform2.rect.height * 0.5f, 0f));
			}
			if (Overflows(rectTransform3, out var shift2))
			{
				rectTransform.position += Bounds.TransformVector(new Vector3(shift2.x, shift2.y, 0f));
			}
		}
	}

	private bool Overflows(RectTransform panel, out Vector2 shift)
	{
		shift = Vector2.zero;
		if (Bounds == null)
		{
			return false;
		}
		Vector3[] array = new Vector3[4];
		panel.GetWorldCorners(array);
		Vector2 vector = Bounds.InverseTransformPoint(array[0]);
		Vector2 vector2 = Bounds.InverseTransformPoint(array[2]);
		Rect rect = Bounds.rect;
		if (vector2.x > rect.xMax)
		{
			shift.x = rect.xMax - vector2.x;
		}
		if (vector.x + shift.x < rect.xMin)
		{
			shift.x = rect.xMin - vector.x;
		}
		if (vector.y < rect.yMin)
		{
			shift.y = rect.yMin - vector.y;
		}
		if (vector2.y + shift.y > rect.yMax)
		{
			shift.y = rect.yMax - vector2.y;
		}
		return shift != Vector2.zero;
	}
}
