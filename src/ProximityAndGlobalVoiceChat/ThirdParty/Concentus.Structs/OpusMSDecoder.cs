using System;
using Concentus.Common;
using Concentus.Enums;

namespace Concentus.Structs;

internal class OpusMSDecoder
{
	internal delegate void opus_copy_channel_out_func<T>(T[] dst, int dst_ptr, int dst_stride, int dst_channel, short[] src, int src_ptr, int src_stride, int frame_size);

	internal ChannelLayout layout = new ChannelLayout();

	internal OpusDecoder[] decoders;

	public OpusBandwidth Bandwidth
	{
		get
		{
			if (decoders == null || decoders.Length == 0)
			{
				throw new InvalidOperationException("Decoder not initialized");
			}
			return decoders[0].Bandwidth;
		}
	}

	public int SampleRate
	{
		get
		{
			if (decoders == null || decoders.Length == 0)
			{
				throw new InvalidOperationException("Decoder not initialized");
			}
			return decoders[0].SampleRate;
		}
	}

	public int Gain
	{
		get
		{
			if (decoders == null || decoders.Length == 0)
			{
				return -6;
			}
			return decoders[0].Gain;
		}
		set
		{
			for (int i = 0; i < layout.nb_streams; i++)
			{
				decoders[i].Gain = value;
			}
		}
	}

	public int LastPacketDuration
	{
		get
		{
			if (decoders == null || decoders.Length == 0)
			{
				return -6;
			}
			return decoders[0].LastPacketDuration;
		}
	}

	public uint FinalRange
	{
		get
		{
			uint num = 0u;
			for (int i = 0; i < layout.nb_streams; i++)
			{
				num ^= decoders[i].FinalRange;
			}
			return num;
		}
	}

	private OpusMSDecoder(int nb_streams, int nb_coupled_streams)
	{
		decoders = new OpusDecoder[nb_streams];
		for (int i = 0; i < nb_streams; i++)
		{
			decoders[i] = new OpusDecoder();
		}
	}

	internal int opus_multistream_decoder_init(int Fs, int channels, int streams, int coupled_streams, byte[] mapping)
	{
		int num = 0;
		if (channels > 255 || channels < 1 || coupled_streams > streams || streams < 1 || coupled_streams < 0 || streams > 255 - coupled_streams)
		{
			throw new ArgumentException("Invalid channel or coupled stream count");
		}
		layout.nb_channels = channels;
		layout.nb_streams = streams;
		layout.nb_coupled_streams = coupled_streams;
		int i;
		for (i = 0; i < layout.nb_channels; i++)
		{
			layout.mapping[i] = mapping[i];
		}
		if (OpusMultistream.validate_layout(layout) == 0)
		{
			throw new ArgumentException("Invalid surround channel layout");
		}
		for (i = 0; i < layout.nb_coupled_streams; i++)
		{
			int num2 = decoders[num].opus_decoder_init(Fs, 2);
			if (num2 != 0)
			{
				return num2;
			}
			num++;
		}
		for (; i < layout.nb_streams; i++)
		{
			int num2 = decoders[num].opus_decoder_init(Fs, 1);
			if (num2 != 0)
			{
				return num2;
			}
			num++;
		}
		return 0;
	}

	public OpusMSDecoder(int Fs, int channels, int streams, int coupled_streams, byte[] mapping)
		: this(streams, coupled_streams)
	{
		if (channels > 255 || channels < 1 || coupled_streams > streams || streams < 1 || coupled_streams < 0 || streams > 255 - coupled_streams)
		{
			throw new ArgumentException("Invalid channel / stream configuration");
		}
		int num = opus_multistream_decoder_init(Fs, channels, streams, coupled_streams, mapping);
		switch (num)
		{
		case -1:
			throw new ArgumentException("Bad argument while creating MS decoder");
		default:
			throw new OpusException("Could not create MS decoder", num);
		case 0:
			break;
		}
	}

	internal static int opus_multistream_packet_validate(byte[] data, int data_ptr, int len, int nb_streams, int Fs)
	{
		short[] sizes = new short[48];
		int num = 0;
		for (int i = 0; i < nb_streams; i++)
		{
			if (len <= 0)
			{
				return -4;
			}
			int num2 = OpusPacketInfo.opus_packet_parse_impl(data, data_ptr, len, (i != nb_streams - 1) ? 1 : 0, out var _, null, null, 0, sizes, 0, out var _, out var packet_offset);
			if (num2 < 0)
			{
				return num2;
			}
			int numSamples = OpusPacketInfo.GetNumSamples(data, data_ptr, packet_offset, Fs);
			if (i != 0 && num != numSamples)
			{
				return -4;
			}
			num = numSamples;
			data_ptr += packet_offset;
			len -= packet_offset;
		}
		return num;
	}

	internal int opus_multistream_decode_native<T>(byte[] data, int data_ptr, int len, T[] pcm, int pcm_ptr, opus_copy_channel_out_func<T> copy_channel_out, int frame_size, int decode_fec, int soft_clip)
	{
		int num = 0;
		int sampleRate = SampleRate;
		frame_size = Inlines.IMIN(frame_size, sampleRate / 25 * 3);
		short[] array = new short[2 * frame_size];
		int num2 = 0;
		if (len == 0)
		{
			num = 1;
		}
		if (len < 0)
		{
			return -1;
		}
		if (num == 0 && len < 2 * layout.nb_streams - 1)
		{
			return -4;
		}
		if (num == 0)
		{
			int num3 = opus_multistream_packet_validate(data, data_ptr, len, layout.nb_streams, sampleRate);
			if (num3 < 0)
			{
				return num3;
			}
			if (num3 > frame_size)
			{
				return -2;
			}
		}
		for (int i = 0; i < layout.nb_streams; i++)
		{
			OpusDecoder opusDecoder = decoders[num2++];
			if (num == 0 && len <= 0)
			{
				return -3;
			}
			int num4 = opusDecoder.opus_decode_native(data, data_ptr, len, array, 0, frame_size, decode_fec, (i != layout.nb_streams - 1) ? 1 : 0, out var packet_offset, soft_clip);
			data_ptr += packet_offset;
			len -= packet_offset;
			if (num4 <= 0)
			{
				return num4;
			}
			frame_size = num4;
			if (i < layout.nb_coupled_streams)
			{
				int prev = -1;
				int num5;
				while ((num5 = OpusMultistream.get_left_channel(layout, i, prev)) != -1)
				{
					copy_channel_out(pcm, pcm_ptr, layout.nb_channels, num5, array, 0, 2, frame_size);
					prev = num5;
				}
				prev = -1;
				while ((num5 = OpusMultistream.get_right_channel(layout, i, prev)) != -1)
				{
					copy_channel_out(pcm, pcm_ptr, layout.nb_channels, num5, array, 1, 2, frame_size);
					prev = num5;
				}
			}
			else
			{
				int prev2 = -1;
				int num6;
				while ((num6 = OpusMultistream.get_mono_channel(layout, i, prev2)) != -1)
				{
					copy_channel_out(pcm, pcm_ptr, layout.nb_channels, num6, array, 0, 1, frame_size);
					prev2 = num6;
				}
			}
		}
		for (int j = 0; j < layout.nb_channels; j++)
		{
			if (layout.mapping[j] == byte.MaxValue)
			{
				copy_channel_out(pcm, pcm_ptr, layout.nb_channels, j, null, 0, 0, frame_size);
			}
		}
		return frame_size;
	}

	internal static void opus_copy_channel_out_float(float[] dst, int dst_ptr, int dst_stride, int dst_channel, short[] src, int src_ptr, int src_stride, int frame_size)
	{
		if (src != null)
		{
			for (int i = 0; i < frame_size; i++)
			{
				dst[i * dst_stride + dst_channel + dst_ptr] = 3.0517578E-05f * (float)src[i * src_stride + src_ptr];
			}
		}
		else
		{
			for (int i = 0; i < frame_size; i++)
			{
				dst[i * dst_stride + dst_channel + dst_ptr] = 0f;
			}
		}
	}

	internal static void opus_copy_channel_out_short(short[] dst, int dst_ptr, int dst_stride, int dst_channel, short[] src, int src_ptr, int src_stride, int frame_size)
	{
		if (src != null)
		{
			for (int i = 0; i < frame_size; i++)
			{
				dst[i * dst_stride + dst_channel + dst_ptr] = src[i * src_stride + src_ptr];
			}
		}
		else
		{
			for (int i = 0; i < frame_size; i++)
			{
				dst[i * dst_stride + dst_channel + dst_ptr] = 0;
			}
		}
	}

	public int DecodeMultistream(byte[] data, int data_offset, int len, short[] out_pcm, int out_pcm_offset, int frame_size, int decode_fec)
	{
		return opus_multistream_decode_native(data, data_offset, len, out_pcm, out_pcm_offset, opus_copy_channel_out_short, frame_size, decode_fec, 0);
	}

	public int DecodeMultistream(byte[] data, int data_offset, int len, float[] out_pcm, int out_pcm_offset, int frame_size, int decode_fec)
	{
		return opus_multistream_decode_native(data, data_offset, len, out_pcm, out_pcm_offset, opus_copy_channel_out_float, frame_size, decode_fec, 0);
	}

	public void ResetState()
	{
		for (int i = 0; i < layout.nb_streams; i++)
		{
			decoders[i].ResetState();
		}
	}

	public OpusDecoder GetMultistreamDecoderState(int streamId)
	{
		return decoders[streamId];
	}
}
