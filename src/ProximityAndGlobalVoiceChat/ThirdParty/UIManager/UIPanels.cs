using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine;

namespace UIManager;

internal static class UIPanels
{
	private sealed class Pending
	{
		public UIPanel Panel;

		public Action<Transform> Action;

		public string Owner;
	}

	private static readonly List<Pending> Waiting = new List<Pending>();

	private const string FrontName = "UIManager.CustomFront";

	public static event Action<Hud> HudReady;

	public static event Action<InventoryGui> InventoryReady;

	public static event Action<FejdStartup> MainMenuReady;

	public static Transform Get(UIPanel panel)
	{
		if (!TryGet(panel, out var transform))
		{
			return null;
		}
		return transform;
	}

	public static bool TryGet(UIPanel panel, out Transform transform)
	{
		transform = Resolve(panel);
		return transform != null;
	}

	public static void When(UIPanel panel, Action<Transform> action)
	{
		if (action != null)
		{
			string owner = new StackFrame(1).GetMethod()?.DeclaringType?.Name ?? "unknown";
			if (TryGet(panel, out var transform))
			{
				Invoke(action, transform, owner);
				return;
			}
			Waiting.Add(new Pending
			{
				Panel = panel,
				Action = action,
				Owner = owner
			});
		}
	}

	public static GameObject Attach(GameObject prefab, UIPanel panel, bool style = true, StyleOptions options = null)
	{
		if (!TryGet(panel, out var transform))
		{
			UILog.Error(string.Format("panel '{0}' is not up yet - use UIPanels.When before attaching '{1}'", panel, (prefab != null) ? prefab.name : "null"));
			return null;
		}
		return UIRoot.Instantiate(prefab, transform, style, options);
	}

	public static Transform Find(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		Transform transform = Resolve(UIPanel.IngameGui);
		if (transform != null)
		{
			Transform transform2 = transform.Find(path);
			if (transform2 != null)
			{
				return transform2;
			}
		}
		GameObject gameObject = GameObject.Find(path);
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.transform;
	}

	private static Transform Resolve(UIPanel panel)
	{
		switch (panel)
		{
		case UIPanel.Front:
			return UIRoot.Front;
		case UIPanel.Back:
			return UIRoot.Back;
		case UIPanel.PixelFix:
			return PixelFix();
		case UIPanel.IngameGui:
			return IngameGui();
		case UIPanel.Hud:
			if (!(Hud.instance != null) || !(Hud.instance.m_rootObject != null))
			{
				return null;
			}
			return Hud.instance.m_rootObject.transform;
		case UIPanel.Inventory:
			if (!(InventoryGui.instance != null))
			{
				return Find("Inventory_screen");
			}
			return InventoryGui.instance.transform;
		case UIPanel.Crafting:
			return Find("Inventory_screen/root/Crafting");
		case UIPanel.Chat:
			if (!(Chat.instance != null))
			{
				return Find("Chat");
			}
			return Chat.instance.transform;
		case UIPanel.Menu:
			if (!(Menu.instance != null))
			{
				return Find("Menu");
			}
			return Menu.instance.transform;
		case UIPanel.TopLeftMessage:
			return Find("TopLeftMessage");
		case UIPanel.MainMenu:
			return MainMenuRoot();
		case UIPanel.CustomFront:
			return CustomFront();
		default:
			return null;
		}
	}

	private static Transform PixelFix()
	{
		GameObject gameObject = GameObject.Find("_GameMain/LoadingGUI");
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.transform.Find("PixelFix");
	}

	private static Transform IngameGui()
	{
		Transform transform = PixelFix();
		if (transform == null)
		{
			return null;
		}
		foreach (Transform item in transform)
		{
			if (item.name.StartsWith("IngameGui", StringComparison.Ordinal))
			{
				return item;
			}
		}
		return null;
	}

	private static Transform CustomFront()
	{
		GameObject gameObject = GameObject.Find("_GameMain/LoadingGUI");
		if (gameObject == null)
		{
			return null;
		}
		Transform transform = gameObject.transform.Find("UIManager.CustomFront");
		if (transform != null)
		{
			return transform;
		}
		RectTransform obj = (RectTransform)new GameObject("UIManager.CustomFront", typeof(RectTransform), typeof(GuiPixelFix))
		{
			layer = UIRoot.UILayer
		}.transform;
		obj.SetParent(gameObject.transform, worldPositionStays: false);
		obj.SetAsLastSibling();
		obj.anchorMin = Vector2.zero;
		obj.anchorMax = Vector2.one;
		obj.offsetMin = Vector2.zero;
		obj.offsetMax = Vector2.zero;
		obj.localScale = Vector3.one;
		obj.anchoredPosition3D = Vector3.zero;
		UILog.Info("created UIManager.CustomFront under " + UIExtensions.PathOf(gameObject.transform));
		return obj;
	}

	private static Transform MainMenuRoot()
	{
		GameObject gameObject = GameObject.Find("GuiRoot/GUI");
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.transform;
	}

	private static void Invoke(Action<Transform> action, Transform target, string owner)
	{
		try
		{
			action(target);
		}
		catch (Exception e)
		{
			UILog.Error("a UIPanels.When handler from " + owner + " threw", e);
		}
	}

	internal static void Tick()
	{
		for (int num = Waiting.Count - 1; num >= 0; num--)
		{
			Pending pending = Waiting[num];
			if (TryGet(pending.Panel, out var transform))
			{
				Waiting.RemoveAt(num);
				Invoke(pending.Action, transform, pending.Owner);
			}
		}
	}

	[HarmonyPatch(typeof(Hud), "Awake")]
	[HarmonyPostfix]
	private static void HudAwakePostfix(Hud __instance)
	{
		Raise(HudReady, __instance, "HudReady");
	}

	[HarmonyPatch(typeof(InventoryGui), "Awake")]
	[HarmonyPostfix]
	private static void InventoryAwakePostfix(InventoryGui __instance)
	{
		Raise(InventoryReady, __instance, "InventoryReady");
	}

	[HarmonyPatch(typeof(FejdStartup), "SetupGui")]
	[HarmonyPostfix]
	private static void MainMenuPostfix(FejdStartup __instance)
	{
		Raise(MainMenuReady, __instance, "MainMenuReady");
	}

	private static void Raise<T>(Action<T> handlers, T argument, string name)
	{
		if (handlers == null)
		{
			return;
		}
		try
		{
			handlers(argument);
		}
		catch (Exception e)
		{
			UILog.Error("a UIPanels." + name + " handler threw", e);
		}
	}
}
