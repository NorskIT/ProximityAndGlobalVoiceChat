using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace UIManager;

internal static class UIEvents
{
	public static void OnHover(GameObject target, Action enter, Action exit = null)
	{
		if (target == null)
		{
			return;
		}
		if (enter != null)
		{
			Add(target, EventTriggerType.PointerEnter, delegate
			{
				enter();
			});
		}
		if (exit != null)
		{
			Add(target, EventTriggerType.PointerExit, delegate
			{
				exit();
			});
		}
	}

	public static void OnClick(GameObject target, PointerEventData.InputButton button, Action action)
	{
		if (target == null || action == null)
		{
			return;
		}
		Add(target, EventTriggerType.PointerClick, delegate(BaseEventData data)
		{
			if (data is PointerEventData pointerEventData && pointerEventData.button == button)
			{
				action();
			}
		});
	}

	public static void OnLeftClick(GameObject target, Action action)
	{
		OnClick(target, PointerEventData.InputButton.Left, action);
	}

	public static void OnRightClick(GameObject target, Action action)
	{
		OnClick(target, PointerEventData.InputButton.Right, action);
	}

	public static void ClearEvents(GameObject target)
	{
		if (!(target == null))
		{
			EventTrigger component = target.GetComponent<EventTrigger>();
			if (component != null)
			{
				component.triggers.Clear();
			}
		}
	}

	public static void Add(GameObject target, EventTriggerType type, UnityAction<BaseEventData> action)
	{
		if (!(target == null) && action != null)
		{
			EventTrigger eventTrigger = target.GetComponent<EventTrigger>();
			if (eventTrigger == null)
			{
				eventTrigger = target.AddComponent<EventTrigger>();
			}
			EventTrigger.Entry entry = new EventTrigger.Entry
			{
				eventID = type
			};
			entry.callback.AddListener(action);
			eventTrigger.triggers.Add(entry);
		}
	}
}
