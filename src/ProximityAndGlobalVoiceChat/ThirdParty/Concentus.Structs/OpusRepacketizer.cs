using System;
using Concentus.Common.CPlusPlus;

namespace Concentus.Structs;

internal class OpusRepacketizer
{
	internal byte toc;

	internal int nb_frames;

	internal readonly byte[][] frames = new byte[48][];

	internal readonly int[] frames_ptrs = new int[48];

	internal readonly short[] len = new short[48];

	internal int framesize;

	public void Reset()
	{
		nb_frames = 0;
	}

	public OpusRepacketizer()
	{
		Reset();
	}

	internal int opus_repacketizer_cat_impl(byte[] data, int data_ptr, int len, int self_delimited)
	{
		if (len < 1)
		{
			return -4;
		}
		if (nb_frames == 0)
		{
			toc = data[data_ptr];
			framesize = OpusPacketInfo.GetNumSamplesPerFrame(data, data_ptr, 8000);
		}
		else if ((toc & 0xFC) != (data[data_ptr] & 0xFC))
		{
			return -4;
		}
		int numFrames = OpusPacketInfo.GetNumFrames(data, data_ptr, len);
		if (numFrames < 1)
		{
			return -4;
		}
		if ((numFrames + nb_frames) * framesize > 960)
		{
			return -4;
		}
		int num = OpusPacketInfo.opus_packet_parse_impl(data, data_ptr, len, self_delimited, out var _, frames, frames_ptrs, nb_frames, this.len, nb_frames, out int payload_offset, out payload_offset);
		if (num < 1)
		{
			return num;
		}
		nb_frames += numFrames;
		return 0;
	}

	public int AddPacket(byte[] data, int data_offset, int len)
	{
		return opus_repacketizer_cat_impl(data, data_offset, len, 0);
	}

	public int GetNumFrames()
	{
		return nb_frames;
	}

	internal int opus_repacketizer_out_range_impl(int begin, int end, byte[] data, int data_ptr, int maxlen, int self_delimited, int pad)
	{
		if (begin < 0 || begin >= end || end > nb_frames)
		{
			return -1;
		}
		int num = end - begin;
		int num2 = ((self_delimited != 0) ? (1 + ((len[num - 1] >= 252) ? 1 : 0)) : 0);
		int num3 = data_ptr;
		switch (num)
		{
		case 1:
			num2 += len[0] + 1;
			if (num2 > maxlen)
			{
				return -2;
			}
			data[num3++] = (byte)(toc & 0xFC);
			break;
		case 2:
			if (len[1] == len[0])
			{
				num2 += 2 * len[0] + 1;
				if (num2 > maxlen)
				{
					return -2;
				}
				data[num3++] = (byte)((toc & 0xFC) | 1);
				break;
			}
			num2 += len[0] + len[1] + 2 + ((len[0] >= 252) ? 1 : 0);
			if (num2 > maxlen)
			{
				return -2;
			}
			data[num3++] = (byte)((toc & 0xFC) | 2);
			num3 += OpusPacketInfo.encode_size(len[0], data, num3);
			break;
		}
		if (num > 2 || (pad != 0 && num2 < maxlen))
		{
			int num4 = 0;
			num3 = data_ptr;
			num2 = ((self_delimited != 0) ? (1 + ((len[num - 1] >= 252) ? 1 : 0)) : 0);
			int num5 = 0;
			for (int i = 1; i < num; i++)
			{
				if (len[i] != len[0])
				{
					num5 = 1;
					break;
				}
			}
			if (num5 != 0)
			{
				num2 += 2;
				for (int i = 0; i < num - 1; i++)
				{
					num2 += 1 + ((len[i] >= 252) ? 1 : 0) + len[i];
				}
				num2 += len[num - 1];
				if (num2 > maxlen)
				{
					return -2;
				}
				data[num3++] = (byte)((toc & 0xFC) | 3);
				data[num3++] = (byte)(num | 0x80);
			}
			else
			{
				num2 += num * len[0] + 2;
				if (num2 > maxlen)
				{
					return -2;
				}
				data[num3++] = (byte)((toc & 0xFC) | 3);
				data[num3++] = (byte)num;
			}
			num4 = ((pad != 0) ? (maxlen - num2) : 0);
			if (num4 != 0)
			{
				data[data_ptr + 1] |= 64;
				int num6 = (num4 - 1) / 255;
				for (int i = 0; i < num6; i++)
				{
					data[num3++] = byte.MaxValue;
				}
				data[num3++] = (byte)(num4 - 255 * num6 - 1);
				num2 += num4;
			}
			if (num5 != 0)
			{
				for (int i = 0; i < num - 1; i++)
				{
					num3 += OpusPacketInfo.encode_size(len[i], data, num3);
				}
			}
		}
		if (self_delimited != 0)
		{
			int num7 = OpusPacketInfo.encode_size(len[num - 1], data, num3);
			num3 += num7;
		}
		for (int i = begin; i < num + begin; i++)
		{
			if (frames[i] == data)
			{
				Arrays.MemMove(data, frames_ptrs[i], num3, len[i]);
			}
			else
			{
				Array.Copy(frames[i], frames_ptrs[i], data, num3, len[i]);
			}
			num3 += len[i];
		}
		if (pad != 0)
		{
			while (num3 < data_ptr + maxlen)
			{
				data[num3++] = 0;
			}
		}
		return num2;
	}

	public int CreatePacket(int begin, int end, byte[] data, int data_offset, int maxlen)
	{
		return opus_repacketizer_out_range_impl(begin, end, data, data_offset, maxlen, 0, 0);
	}

	public int CreatePacket(byte[] data, int data_offset, int maxlen)
	{
		return opus_repacketizer_out_range_impl(0, nb_frames, data, data_offset, maxlen, 0, 0);
	}

	public static int PadPacket(byte[] data, int data_offset, int len, int new_len)
	{
		OpusRepacketizer opusRepacketizer = new OpusRepacketizer();
		if (len < 1)
		{
			return -1;
		}
		if (len == new_len)
		{
			return 0;
		}
		if (len > new_len)
		{
			return -1;
		}
		opusRepacketizer.Reset();
		Arrays.MemMove(data, data_offset, data_offset + new_len - len, len);
		opusRepacketizer.AddPacket(data, data_offset + new_len - len, len);
		int num = opusRepacketizer.opus_repacketizer_out_range_impl(0, opusRepacketizer.nb_frames, data, data_offset, new_len, 0, 1);
		if (num > 0)
		{
			return 0;
		}
		return num;
	}

	public static int UnpadPacket(byte[] data, int data_offset, int len)
	{
		if (len < 1)
		{
			return -1;
		}
		OpusRepacketizer opusRepacketizer = new OpusRepacketizer();
		opusRepacketizer.Reset();
		int num = opusRepacketizer.AddPacket(data, data_offset, len);
		if (num < 0)
		{
			return num;
		}
		return opusRepacketizer.opus_repacketizer_out_range_impl(0, opusRepacketizer.nb_frames, data, data_offset, len, 0, 0);
	}

	public static int PadMultistreamPacket(byte[] data, int data_offset, int len, int new_len, int nb_streams)
	{
		short[] sizes = new short[48];
		if (len < 1)
		{
			return -1;
		}
		if (len == new_len)
		{
			return 0;
		}
		if (len > new_len)
		{
			return -1;
		}
		int num = new_len - len;
		for (int i = 0; i < nb_streams - 1; i++)
		{
			if (len <= 0)
			{
				return -4;
			}
			int num2 = OpusPacketInfo.opus_packet_parse_impl(data, data_offset, len, 1, out var _, null, null, 0, sizes, 0, out var _, out var packet_offset);
			if (num2 < 0)
			{
				return num2;
			}
			data_offset += packet_offset;
			len -= packet_offset;
		}
		return PadPacket(data, data_offset, len, len + num);
	}

	public static int UnpadMultistreamPacket(byte[] data, int data_offset, int len, int nb_streams)
	{
		short[] sizes = new short[48];
		OpusRepacketizer opusRepacketizer = new OpusRepacketizer();
		if (len < 1)
		{
			return -1;
		}
		int num = data_offset;
		int num2 = 0;
		for (int i = 0; i < nb_streams; i++)
		{
			int self_delimited = ((i != nb_streams) ? 1 : 0) - 1;
			if (len <= 0)
			{
				return -4;
			}
			opusRepacketizer.Reset();
			int num3 = OpusPacketInfo.opus_packet_parse_impl(data, data_offset, len, self_delimited, out var _, null, null, 0, sizes, 0, out var _, out var packet_offset);
			if (num3 < 0)
			{
				return num3;
			}
			num3 = opusRepacketizer.opus_repacketizer_cat_impl(data, data_offset, packet_offset, self_delimited);
			if (num3 < 0)
			{
				return num3;
			}
			num3 = opusRepacketizer.opus_repacketizer_out_range_impl(0, opusRepacketizer.nb_frames, data, num, len, self_delimited, 0);
			if (num3 < 0)
			{
				return num3;
			}
			num2 += num3;
			num += num3;
			data_offset += packet_offset;
			len -= packet_offset;
		}
		return num2;
	}
}
