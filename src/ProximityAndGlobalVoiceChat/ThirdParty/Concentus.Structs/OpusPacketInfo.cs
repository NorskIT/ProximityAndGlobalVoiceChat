using System;
using System.Collections.Generic;
using Concentus.Common.CPlusPlus;
using Concentus.Enums;

namespace Concentus.Structs;

internal class OpusPacketInfo
{
	public readonly byte TOCByte;

	public readonly IList<byte[]> Frames;

	public readonly int PayloadOffset;

	private OpusPacketInfo(byte toc, IList<byte[]> frames, int payloadOffset)
	{
		TOCByte = toc;
		Frames = frames;
		PayloadOffset = payloadOffset;
	}

	public static OpusPacketInfo ParseOpusPacket(byte[] packet, int packet_offset, int len)
	{
		int numFrames = GetNumFrames(packet, packet_offset, len);
		byte[][] array = new byte[numFrames][];
		int[] array2 = new int[numFrames];
		short[] array3 = new short[numFrames];
		int num = opus_packet_parse_impl(packet, packet_offset, len, 0, out var out_toc, array, array2, 0, array3, 0, out var payload_offset, out var _);
		if (num < 0)
		{
			throw new OpusException("An error occurred while parsing the packet", num);
		}
		IList<byte[]> list = new List<byte[]>();
		for (int i = 0; i < numFrames; i++)
		{
			byte[] array4 = new byte[array3[i]];
			Array.Copy(array[i], array2[i], array4, 0, array4.Length);
			list.Add(array4);
		}
		return new OpusPacketInfo(out_toc, list, payload_offset);
	}

	public static int GetNumSamplesPerFrame(byte[] packet, int packet_offset, int Fs)
	{
		int num;
		if ((packet[packet_offset] & 0x80) != 0)
		{
			num = (packet[packet_offset] >> 3) & 3;
			return (Fs << num) / 400;
		}
		if ((packet[packet_offset] & 0x60) == 96)
		{
			return ((packet[packet_offset] & 8) != 0) ? (Fs / 50) : (Fs / 100);
		}
		num = (packet[packet_offset] >> 3) & 3;
		if (num == 3)
		{
			return Fs * 60 / 1000;
		}
		return (Fs << num) / 100;
	}

	public static OpusBandwidth GetBandwidth(byte[] packet, int packet_offset)
	{
		OpusBandwidth opusBandwidth;
		if ((packet[packet_offset] & 0x80) == 0)
		{
			opusBandwidth = (((packet[packet_offset] & 0x60) != 96) ? ((OpusBandwidth)(1101 + ((packet[packet_offset] >> 5) & 3))) : (((packet[packet_offset] & 0x10) != 0) ? OpusBandwidth.OPUS_BANDWIDTH_FULLBAND : OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND));
		}
		else
		{
			opusBandwidth = (OpusBandwidth)(1102 + ((packet[packet_offset] >> 5) & 3));
			if (opusBandwidth == OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND)
			{
				opusBandwidth = OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND;
			}
		}
		return opusBandwidth;
	}

	public static int GetNumEncodedChannels(byte[] packet, int packet_offset)
	{
		if ((packet[packet_offset] & 4) == 0)
		{
			return 1;
		}
		return 2;
	}

	public static int GetNumFrames(byte[] packet, int packet_offset, int len)
	{
		if (len < 1)
		{
			return -1;
		}
		switch (packet[packet_offset] & 3)
		{
		case 0:
			return 1;
		default:
			return 2;
		case 3:
			if (len < 2)
			{
				return -4;
			}
			return packet[packet_offset + 1] & 0x3F;
		}
	}

	public static int GetNumSamples(byte[] packet, int packet_offset, int len, int Fs)
	{
		int numFrames = GetNumFrames(packet, packet_offset, len);
		if (numFrames < 0)
		{
			return numFrames;
		}
		int num = numFrames * GetNumSamplesPerFrame(packet, packet_offset, Fs);
		if (num * 25 > Fs * 3)
		{
			return -4;
		}
		return num;
	}

	public static int GetNumSamples(OpusDecoder dec, byte[] packet, int packet_offset, int len)
	{
		return GetNumSamples(packet, packet_offset, len, dec.Fs);
	}

	public static OpusMode GetEncoderMode(byte[] packet, int packet_offset)
	{
		if ((packet[packet_offset] & 0x80) != 0)
		{
			return OpusMode.MODE_CELT_ONLY;
		}
		if ((packet[packet_offset] & 0x60) == 96)
		{
			return OpusMode.MODE_HYBRID;
		}
		return OpusMode.MODE_SILK_ONLY;
	}

	internal static int encode_size(int size, byte[] data, int data_ptr)
	{
		if (size < 252)
		{
			data[data_ptr] = (byte)size;
			return 1;
		}
		data[data_ptr] = (byte)(252 + (size & 3));
		data[data_ptr + 1] = (byte)(size - data[data_ptr] >> 2);
		return 2;
	}

	internal static int parse_size(byte[] data, int data_ptr, int len, BoxedValueShort size)
	{
		if (len < 1)
		{
			size.Val = -1;
			return -1;
		}
		if (data[data_ptr] < 252)
		{
			size.Val = data[data_ptr];
			return 1;
		}
		if (len < 2)
		{
			size.Val = -1;
			return -1;
		}
		size.Val = (short)(4 * data[data_ptr + 1] + data[data_ptr]);
		return 2;
	}

	internal static int opus_packet_parse_impl(byte[] data, int data_ptr, int len, int self_delimited, out byte out_toc, byte[][] frames, int[] frames_ptrs, int frames_ptr, short[] sizes, int sizes_ptr, out int payload_offset, out int packet_offset)
	{
		int num = 0;
		int num2 = data_ptr;
		out_toc = 0;
		payload_offset = 0;
		packet_offset = 0;
		if (sizes == null || len < 0)
		{
			return -1;
		}
		if (len == 0)
		{
			return -4;
		}
		int numSamplesPerFrame = GetNumSamplesPerFrame(data, data_ptr, 48000);
		int num3 = 0;
		byte b = data[data_ptr++];
		len--;
		int num4 = len;
		int num5;
		switch (b & 3)
		{
		case 0:
			num5 = 1;
			break;
		case 1:
			num5 = 2;
			num3 = 1;
			if (self_delimited == 0)
			{
				if ((len & 1) != 0)
				{
					return -4;
				}
				num4 = len / 2;
				sizes[sizes_ptr] = (short)num4;
			}
			break;
		case 2:
		{
			num5 = 2;
			BoxedValueShort boxedValueShort = new BoxedValueShort(sizes[sizes_ptr]);
			int num8 = parse_size(data, data_ptr, len, boxedValueShort);
			sizes[sizes_ptr] = boxedValueShort.Val;
			len -= num8;
			if (sizes[sizes_ptr] < 0 || sizes[sizes_ptr] > len)
			{
				return -4;
			}
			data_ptr += num8;
			num4 = len - sizes[sizes_ptr];
			break;
		}
		default:
		{
			if (len < 1)
			{
				return -4;
			}
			byte b2 = data[data_ptr++];
			num5 = b2 & 0x3F;
			if (num5 <= 0 || numSamplesPerFrame * num5 > 5760)
			{
				return -4;
			}
			len--;
			if ((b2 & 0x40) != 0)
			{
				int num6;
				do
				{
					if (len <= 0)
					{
						return -4;
					}
					num6 = data[data_ptr++];
					len--;
					int num7 = ((num6 == 255) ? 254 : num6);
					len -= num7;
					num += num7;
				}
				while (num6 == 255);
			}
			if (len < 0)
			{
				return -4;
			}
			num3 = (((b2 & 0x80) == 0) ? 1 : 0);
			if (num3 == 0)
			{
				num4 = len;
				for (int i = 0; i < num5 - 1; i++)
				{
					BoxedValueShort boxedValueShort = new BoxedValueShort(sizes[sizes_ptr + i]);
					int num8 = parse_size(data, data_ptr, len, boxedValueShort);
					sizes[sizes_ptr + i] = boxedValueShort.Val;
					len -= num8;
					if (sizes[sizes_ptr + i] < 0 || sizes[sizes_ptr + i] > len)
					{
						return -4;
					}
					data_ptr += num8;
					num4 -= num8 + sizes[sizes_ptr + i];
				}
				if (num4 < 0)
				{
					return -4;
				}
			}
			else if (self_delimited == 0)
			{
				num4 = len / num5;
				if (num4 * num5 != len)
				{
					return -4;
				}
				for (int i = 0; i < num5 - 1; i++)
				{
					sizes[sizes_ptr + i] = (short)num4;
				}
			}
			break;
		}
		}
		if (self_delimited != 0)
		{
			BoxedValueShort boxedValueShort2 = new BoxedValueShort(sizes[sizes_ptr + num5 - 1]);
			int num8 = parse_size(data, data_ptr, len, boxedValueShort2);
			sizes[sizes_ptr + num5 - 1] = boxedValueShort2.Val;
			len -= num8;
			if (sizes[sizes_ptr + num5 - 1] < 0 || sizes[sizes_ptr + num5 - 1] > len)
			{
				return -4;
			}
			data_ptr += num8;
			if (num3 != 0)
			{
				if (sizes[sizes_ptr + num5 - 1] * num5 > len)
				{
					return -4;
				}
				for (int i = 0; i < num5 - 1; i++)
				{
					sizes[sizes_ptr + i] = sizes[sizes_ptr + num5 - 1];
				}
			}
			else if (num8 + sizes[sizes_ptr + num5 - 1] > num4)
			{
				return -4;
			}
		}
		else
		{
			if (num4 > 1275)
			{
				return -4;
			}
			sizes[sizes_ptr + num5 - 1] = (short)num4;
		}
		payload_offset = data_ptr - num2;
		for (int i = 0; i < num5; i++)
		{
			if (frames != null)
			{
				frames[frames_ptr + i] = data;
			}
			if (frames_ptrs != null)
			{
				frames_ptrs[frames_ptr + i] = data_ptr;
			}
			data_ptr += sizes[sizes_ptr + i];
		}
		packet_offset = num + (data_ptr - num2);
		out_toc = b;
		return num5;
	}
}
