namespace ProximityVoiceChat.Voice;

internal enum VoicePacketType : byte
{
	Frame,
	Hello,
	Bye,
	Ping,
	Pong
}
