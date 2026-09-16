using BepInEx.Configuration;
using ProximityVoiceChat.UI;
using UnityEngine;

namespace ProximityVoiceChat.Voice;

internal sealed class VoiceHotkeys : MonoBehaviour
{
	private void Update()
	{
		if (Typing() || KeyBinder.Listening)
		{
			return;
		}
		if (ProximityVoiceChatPlugin.MuteSelfKey.Value.IsKeyDown())
		{
			Say(Flip(ProximityVoiceChatPlugin.MuteSelf) ? "$pvc_msg_muted" : "$pvc_msg_unmuted");
		}
		if (ProximityVoiceChatPlugin.DeafenKey.Value.IsKeyDown())
		{
			bool num = Flip(ProximityVoiceChatPlugin.Deafen);
			Say(num ? "$pvc_msg_deafened" : "$pvc_msg_undeafened");
			if (num)
			{
				VoiceRuntime.Instance?.Playback?.Clear();
			}
		}
	}

	private static bool Flip(ConfigEntry<ProximityVoiceChatPlugin.Toggle> entry)
	{
		bool flag = entry.Value == ProximityVoiceChatPlugin.Toggle.Off;
		entry.Value = (flag ? ProximityVoiceChatPlugin.Toggle.On : ProximityVoiceChatPlugin.Toggle.Off);
		return flag;
	}

	private static void Say(string key)
	{
		if (ProximityVoiceChatPlugin.HotkeyMessages.Value != ProximityVoiceChatPlugin.Toggle.Off && MessageHud.instance != null)
		{
			MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, UIBuild.Localize(key));
		}
	}

	private static bool Typing()
	{
		if (Chat.instance != null && Chat.instance.HasFocus())
		{
			return true;
		}
		if (Settings.instance != null || Menu.IsVisible())
		{
			return true;
		}
		if (!Console.IsVisible())
		{
			return TextInput.IsVisible();
		}
		return true;
	}
}
