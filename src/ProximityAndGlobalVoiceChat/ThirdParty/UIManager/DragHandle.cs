using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIManager;

internal class DragHandle : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler
{
	public RectTransform Target;

	private Vector2 _grabOffset;

	public static DragHandle Attach(GameObject handle, RectTransform target)
	{
		if (handle == null || target == null)
		{
			return null;
		}
		if (handle.GetComponent<Graphic>() == null)
		{
			handle.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
		}
		DragHandle obj = handle.GetComponent<DragHandle>() ?? handle.AddComponent<DragHandle>();
		obj.Target = target;
		return obj;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!(Target == null))
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(Target.parent as RectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);
			_grabOffset = Target.anchoredPosition - localPoint;
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!(Target == null) && RectTransformUtility.ScreenPointToLocalPointInRectangle(Target.parent as RectTransform, eventData.position, eventData.pressEventCamera, out var localPoint))
		{
			Target.anchoredPosition = localPoint + _grabOffset;
		}
	}
}
