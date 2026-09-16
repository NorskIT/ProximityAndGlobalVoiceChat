using System;
using BepInEx.Logging;
using UnityEngine;

namespace UIManager;

internal static class UILog
{
	public static string Prefix = "[UIManager]";

	public static bool Verbose;

	private static ManualLogSource _source;

	public static void Use(ManualLogSource source)
	{
		_source = source;
	}

	public static void Debug(string message)
	{
		if (Verbose)
		{
			if (_source != null)
			{
				_source.LogDebug(Prefix + " " + message);
			}
			else
			{
				UnityEngine.Debug.Log(Prefix + " " + message);
			}
		}
	}

	public static void Info(string message)
	{
		if (_source != null)
		{
			_source.LogInfo(Prefix + " " + message);
		}
		else
		{
			UnityEngine.Debug.Log(Prefix + " " + message);
		}
	}

	public static void Warning(string message)
	{
		if (_source != null)
		{
			_source.LogWarning(Prefix + " " + message);
		}
		else
		{
			UnityEngine.Debug.LogWarning(Prefix + " " + message);
		}
	}

	public static void Error(string message)
	{
		if (_source != null)
		{
			_source.LogError(Prefix + " " + message);
		}
		else
		{
			UnityEngine.Debug.LogError(Prefix + " " + message);
		}
	}

	public static void Error(string message, Exception e)
	{
		Error(message + "\n" + e);
	}
}
