using System;

namespace ProximityVoiceChat.Voice;

[Flags]
internal enum VoiceFlags : byte
{
	None = 0,
	EndOfTalkspurt = 1,
	Whisper = 2,
	Shout = 4
}
