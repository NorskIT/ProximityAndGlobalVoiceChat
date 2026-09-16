using System;
using HarmonyLib;
using UnityEngine;

namespace UIManager;

internal static class InputBlocker
{
	private class BlockScope : IDisposable
	{
		private bool _disposed;

		public BlockScope()
		{
			Block(state: true);
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				Block(state: false);
			}
		}
	}

	private static int _blocks;

	private static bool _restorePending;

	public static bool IsBlocked => _blocks > 0;

	public static void Block(bool state)
	{
		_blocks = Mathf.Max(0, _blocks + (state ? 1 : (-1)));
		if (_blocks == 0)
		{
			_restorePending = true;
		}
	}

	public static void Reset()
	{
		_blocks = 0;
		_restorePending = true;
	}

	public static IDisposable Scope()
	{
		return new BlockScope();
	}

	internal static void Tick()
	{
		if (IsBlocked)
		{
			if (Cursor.lockState != CursorLockMode.None)
			{
				Cursor.lockState = CursorLockMode.None;
			}
			if (!Cursor.visible)
			{
				Cursor.visible = true;
			}
			GameCamera instance = GameCamera.instance;
			if (instance != null)
			{
				instance.m_mouseCapture = false;
			}
		}
		else if (_restorePending)
		{
			_restorePending = false;
			GameCamera instance2 = GameCamera.instance;
			if (instance2 != null)
			{
				instance2.m_mouseCapture = true;
			}
		}
	}

	[HarmonyPatch(typeof(Player), "TakeInput")]
	[HarmonyPostfix]
	private static void PlayerTakeInputPostfix(ref bool __result)
	{
		if (IsBlocked)
		{
			__result = false;
		}
	}

	[HarmonyPatch(typeof(PlayerController), "TakeInput", new Type[] { typeof(bool) })]
	[HarmonyPostfix]
	private static void PlayerControllerTakeInputPostfix(ref bool __result)
	{
		if (IsBlocked)
		{
			__result = false;
		}
	}

	[HarmonyPatch(typeof(TextInput), "IsVisible")]
	[HarmonyPostfix]
	private static void TextInputIsVisiblePostfix(ref bool __result)
	{
		if (IsBlocked)
		{
			__result = true;
		}
	}
}
