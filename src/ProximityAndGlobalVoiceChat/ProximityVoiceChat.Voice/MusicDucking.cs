using HarmonyLib;
using UnityEngine;

namespace ProximityVoiceChat.Voice;

internal static class MusicDucking
{
	[HarmonyPatch(typeof(MusicMan), "UpdateMusic")]
	private static class MusicMan_UpdateMusic_Patch
	{
		private static void Postfix(MusicMan __instance)
		{
			if (!(_duck >= 0.999f) && !(__instance.m_musicSource == null))
			{
				__instance.m_musicSource.volume *= _duck;
			}
		}
	}

	private const float FadeRate = 3f;

	private static float _duck = 1f;

	internal static void Tick(float deltaTime, bool someoneSpeaking)
	{
		float target = 1f;
		if (someoneSpeaking && ProximityVoiceChatPlugin.DuckMusic.Value.IsOn())
		{
			target = Mathf.Clamp01(ProximityVoiceChatPlugin.DuckMusicAmount.Value);
		}
		_duck = Mathf.MoveTowards(_duck, target, 3f * deltaTime);
	}

	internal static void Reset()
	{
		_duck = 1f;
	}
}
