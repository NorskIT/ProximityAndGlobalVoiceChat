using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace ProximityVoiceChat.UI;

internal static class KeyBinder
{
	private static readonly KeyCode[] Modifiers = new KeyCode[6]
	{
		KeyCode.LeftControl,
		KeyCode.RightControl,
		KeyCode.LeftShift,
		KeyCode.RightShift,
		KeyCode.LeftAlt,
		KeyCode.RightAlt
	};

	private static KeyCode[]? _candidates;

	private static int _startedFrame;

	private static readonly List<KeyCode> Held = new List<KeyCode>();

	internal static ConfigEntry<KeyboardShortcut>? Target;

	internal static bool Listening => Target != null;

	internal static bool IsListening(ConfigEntry<KeyboardShortcut> entry)
	{
		return Target == entry;
	}

	internal static void Begin(ConfigEntry<KeyboardShortcut> entry)
	{
		Target = entry;
		_startedFrame = Time.frameCount;
	}

	internal static void Cancel()
	{
		Target = null;
	}

	internal static void Tick()
	{
		ConfigEntry<KeyboardShortcut> target = Target;
		if (target == null || !Input.anyKeyDown || Time.frameCount == _startedFrame)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Target = null;
			return;
		}
		if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
		{
			target.Value = new KeyboardShortcut(KeyCode.None);
			Target = null;
			return;
		}
		if (_candidates == null)
		{
			_candidates = BuildCandidates();
		}
		for (int i = 0; i < _candidates.Length; i++)
		{
			KeyCode keyCode = _candidates[i];
			if (!Input.GetKeyDown(keyCode))
			{
				continue;
			}
			Held.Clear();
			for (int j = 0; j < Modifiers.Length; j++)
			{
				if (Modifiers[j] != keyCode && Input.GetKey(Modifiers[j]))
				{
					Held.Add(Modifiers[j]);
				}
			}
			target.Value = new KeyboardShortcut(keyCode, Held.ToArray());
			Target = null;
			break;
		}
	}

	internal static string Describe(KeyboardShortcut shortcut)
	{
		if (shortcut.MainKey == KeyCode.None)
		{
			return UIBuild.Localize("$pvc_key_unbound");
		}
		string text = "";
		foreach (KeyCode modifier in shortcut.Modifiers)
		{
			text = text + modifier.ToString() + " + ";
		}
		return text + shortcut.MainKey;
	}

	private static KeyCode[] BuildCandidates()
	{
		List<KeyCode> list = new List<KeyCode>();
		foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
		{
			if (value != KeyCode.None && value != KeyCode.Mouse0 && !value.ToString().StartsWith("Joystick", StringComparison.Ordinal))
			{
				list.Add(value);
			}
		}
		return list.ToArray();
	}
}
