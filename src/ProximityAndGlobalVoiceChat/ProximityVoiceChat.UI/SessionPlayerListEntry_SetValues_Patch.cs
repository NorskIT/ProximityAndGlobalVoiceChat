using HarmonyLib;
using Valheim.UI;

namespace ProximityVoiceChat.UI;

[HarmonyPatch(typeof(SessionPlayerListEntry), "SetValues")]
internal static class SessionPlayerListEntry_SetValues_Patch
{
	private static void Postfix(SessionPlayerListEntry __instance)
	{
		VoiceEntryUI voiceEntryUI = __instance.gameObject.GetComponent<VoiceEntryUI>();
		if (voiceEntryUI == null)
		{
			voiceEntryUI = __instance.gameObject.AddComponent<VoiceEntryUI>();
		}
		voiceEntryUI.Bind(__instance, __instance.IsOwnPlayer);
	}
}
