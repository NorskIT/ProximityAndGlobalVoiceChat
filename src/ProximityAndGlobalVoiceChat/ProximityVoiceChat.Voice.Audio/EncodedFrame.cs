namespace ProximityVoiceChat.Voice.Audio;

internal sealed class EncodedFrame
{
	internal int Generation;
	internal uint StreamId;
	internal VoiceChannel Channel;
	internal bool RawMonitor;
	internal readonly float[] Raw = new float[960];
	internal readonly byte[] Data = new byte[1100];

	internal int Length;

	internal uint TimestampMs;

	internal VoiceFlags Flags;
}
