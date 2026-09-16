using System;
using HarmonyLib;

namespace ProximityVoiceChat.UI;

[HarmonyPatch(typeof(Settings), "SetAvailableTabs")]
internal static class Settings_SetAvailableTabs_Patch
{
	private static void Prefix(Settings __instance)
	{
		try
		{
			SettingsTabInjector.Inject(__instance);
		}
		catch (Exception arg)
		{
			ProximityVoiceChatPlugin.Log.LogError($"Could not add the Voice settings tab: {arg}");
		}
	}
}
