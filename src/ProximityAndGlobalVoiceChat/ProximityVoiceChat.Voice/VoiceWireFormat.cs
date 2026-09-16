namespace ProximityVoiceChat.Voice;

internal static class VoiceWireFormat
{
	internal const byte Magic = 86;

	internal const byte Protocol = 2;

	internal const int HeaderSize = 17;

	internal const int MaxPayload = 1100;

	internal const int MaxPacketSize = 1117;

	internal static int WriteHeader(byte[] buf, VoicePacketType type, VoiceFlags flags, ushort seq, uint timestampMs, ushort payloadLength, uint streamId = 0, VoiceChannel channel = VoiceChannel.None)
	{
		buf[0] = 86;
		buf[1] = Protocol;
		buf[2] = (byte)type;
		buf[3] = (byte)flags;
		buf[4] = (byte)(seq & 0xFF);
		buf[5] = (byte)(seq >> 8);
		buf[6] = (byte)(timestampMs & 0xFF);
		buf[7] = (byte)((timestampMs >> 8) & 0xFF);
		buf[8] = (byte)((timestampMs >> 16) & 0xFF);
		buf[9] = (byte)((timestampMs >> 24) & 0xFF);
		buf[10] = (byte)(payloadLength & 0xFF);
		buf[11] = (byte)(payloadLength >> 8);
		buf[12] = (byte)streamId; buf[13] = (byte)(streamId >> 8); buf[14] = (byte)(streamId >> 16); buf[15] = (byte)(streamId >> 24);
		buf[16] = (byte)channel;
		return HeaderSize;
	}

	internal static bool TryReadHeader(byte[] buf, int length, out VoiceHeader header)
	{
		header = default(VoiceHeader);
		if (length < HeaderSize || length > buf.Length || buf[0] != 86 || buf[1] != Protocol || buf[16] > 2)
		{
			return false;
		}
		ushort num = (ushort)(buf[10] | (buf[11] << 8));
		if (num > 1100 || HeaderSize + num != length)
		{
			return false;
		}
		header.Type = (VoicePacketType)buf[2];
		header.Flags = (VoiceFlags)buf[3];
		header.Sequence = (ushort)(buf[4] | (buf[5] << 8));
		header.TimestampMs = (uint)(buf[6] | (buf[7] << 8) | (buf[8] << 16) | (buf[9] << 24));
		header.PayloadLength = num;
		header.StreamId = (uint)(buf[12] | buf[13] << 8 | buf[14] << 16 | buf[15] << 24);
		header.Channel = (VoiceChannel)buf[16];
		if ((buf[3] & ~1) != 0) return false;
if (!System.Enum.IsDefined(typeof(VoicePacketType), header.Type)) return false;
		if (header.Type == VoicePacketType.Frame && (header.StreamId == 0 || header.Channel == VoiceChannel.None)) return false;
		if (header.Type == VoicePacketType.Frame && ((header.Flags & VoiceFlags.EndOfTalkspurt) != 0 ? num != 0 : num == 0)) return false;
		return true;
	}

	internal static int SequenceDelta(ushort a, ushort b)
	{
		return (short)(a - b);
	}
}
