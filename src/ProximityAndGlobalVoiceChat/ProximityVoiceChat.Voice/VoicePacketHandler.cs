namespace ProximityVoiceChat.Voice;

internal delegate void VoicePacketHandler(ulong senderSteamId, in VoiceHeader header, byte[] buffer, int offset, int length);
