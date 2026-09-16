namespace ProximityVoiceChat.Voice;
internal enum VoiceChannel : byte { None, Local, Global }
internal static class VoiceRouting
{
    internal static VoiceChannel Select(bool blocked, bool localHeld, bool globalHeld, bool activation)
        => blocked ? VoiceChannel.None : globalHeld ? VoiceChannel.Global : localHeld || activation ? VoiceChannel.Local : VoiceChannel.None;
    internal static bool Receives(VoiceChannel channel, bool compatible, bool inRange)
        => compatible && (channel == VoiceChannel.Global || channel == VoiceChannel.Local && inRange);
}
