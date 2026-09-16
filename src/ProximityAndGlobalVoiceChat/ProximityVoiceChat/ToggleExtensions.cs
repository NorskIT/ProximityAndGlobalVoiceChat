namespace ProximityVoiceChat;

public static class ToggleExtensions
{
	public static bool IsOn(this ProximityVoiceChatPlugin.Toggle toggle)
	{
		return toggle == ProximityVoiceChatPlugin.Toggle.On;
	}

	public static bool IsOff(this ProximityVoiceChatPlugin.Toggle toggle)
	{
		return toggle == ProximityVoiceChatPlugin.Toggle.Off;
	}
}
