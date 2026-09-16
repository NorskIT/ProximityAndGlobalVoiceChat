namespace ProximityVoiceChat.Voice;

internal struct VoiceHeader
{
	public uint StreamId;
	public VoiceChannel Channel;
	public VoicePacketType Type;

	public VoiceFlags Flags;

	public ushort Sequence;

	public uint TimestampMs;

	public ushort PayloadLength;
}
