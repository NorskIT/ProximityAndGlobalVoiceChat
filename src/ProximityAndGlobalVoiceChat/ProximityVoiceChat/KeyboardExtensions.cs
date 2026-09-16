using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace ProximityVoiceChat;

public static class KeyboardExtensions
{
	public static bool IsKeyDown(this KeyboardShortcut shortcut)
	{
		if (shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey))
		{
			return shortcut.Modifiers.All(Input.GetKey);
		}
		return false;
	}

	public static bool IsKeyHeld(this KeyboardShortcut shortcut)
	{
		if (shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey))
		{
			return shortcut.Modifiers.All(Input.GetKey);
		}
		return false;
	}
}
