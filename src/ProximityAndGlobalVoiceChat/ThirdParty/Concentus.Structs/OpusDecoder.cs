using System;
using Concentus.Celt.Structs;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Enums;
using Concentus.Silk;
using Concentus.Silk.Structs;

namespace Concentus.Structs;

internal class OpusDecoder
{
	internal int channels;

	internal int Fs;

	internal readonly DecControlState DecControl = new DecControlState();

	internal int decode_gain;

	internal int stream_channels;

	internal OpusBandwidth bandwidth;

	internal OpusMode mode;

	internal OpusMode prev_mode;

	internal int frame_size;

	internal int prev_redundancy;

	internal int last_packet_duration;

	internal uint rangeFinal;

	internal SilkDecoder SilkDecoder = new SilkDecoder();

	internal CeltDecoder Celt_Decoder = new CeltDecoder();

	private static readonly byte[] SILENCE = new byte[2] { 255, 255 };

	public OpusBandwidth Bandwidth => bandwidth;

	public uint FinalRange => rangeFinal;

	public int SampleRate => Fs;

	public int NumChannels => channels;

	public int Pitch
	{
		get
		{
			if (prev_mode == OpusMode.MODE_CELT_ONLY)
			{
				return Celt_Decoder.GetPitch();
			}
			return DecControl.prevPitchLag;
		}
	}

	public int Gain
	{
		get
		{
			return decode_gain;
		}
		set
		{
			if (value < -32768 || value > 32767)
			{
				throw new ArgumentException("Gain must be within the range of a signed int16");
			}
			decode_gain = value;
		}
	}

	public int LastPacketDuration => last_packet_duration;

	internal OpusDecoder()
	{
	}

	internal void Reset()
	{
		channels = 0;
		Fs = 0;
		DecControl.Reset();
		decode_gain = 0;
		PartialReset();
	}

	internal void PartialReset()
	{
		stream_channels = 0;
		bandwidth = (OpusBandwidth)0;
		mode = (OpusMode)0;
		prev_mode = (OpusMode)0;
		frame_size = 0;
		prev_redundancy = 0;
		last_packet_duration = 0;
		rangeFinal = 0u;
	}

	public static OpusDecoder Create(int Fs, int channels)
	{
		return new OpusDecoder(Fs, channels);
	}

	public OpusDecoder(int Fs, int channels)
	{
		if (Fs != 48000 && Fs != 24000 && Fs != 16000 && Fs != 12000 && Fs != 8000)
		{
			throw new ArgumentException("Sample rate is invalid (must be 8/12/16/24/48 Khz)");
		}
		if (channels != 1 && channels != 2)
		{
			throw new ArgumentException("Number of channels must be 1 or 2");
		}
		int num = opus_decoder_init(Fs, channels);
		switch (num)
		{
		case -1:
			throw new ArgumentException("OPUS_BAD_ARG when creating decoder");
		default:
			throw new OpusException("Error while initializing decoder", num);
		case 0:
			break;
		}
	}

	internal int opus_decoder_init(int Fs, int channels)
	{
		if ((Fs != 48000 && Fs != 24000 && Fs != 16000 && Fs != 12000 && Fs != 8000) || (channels != 1 && channels != 2))
		{
			return -1;
		}
		Reset();
		SilkDecoder silkDecoder = SilkDecoder;
		CeltDecoder celt_Decoder = Celt_Decoder;
		stream_channels = (this.channels = channels);
		this.Fs = Fs;
		DecControl.API_sampleRate = this.Fs;
		DecControl.nChannelsAPI = this.channels;
		if (DecodeAPI.silk_InitDecoder(silkDecoder) != 0)
		{
			return -3;
		}
		if (celt_Decoder.celt_decoder_init(Fs, channels) != 0)
		{
			return -3;
		}
		celt_Decoder.SetSignalling(0);
		prev_mode = (OpusMode)0;
		frame_size = Fs / 400;
		return 0;
	}

	internal int opus_decode_frame(byte[] data, int data_ptr, int len, short[] pcm, int pcm_ptr, int frame_size, int decode_fec)
	{
		int num = 0;
		EntropyCoder entropyCoder = new EntropyCoder();
		short[] array = null;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		uint num6 = 0u;
		SilkDecoder silkDecoder = SilkDecoder;
		CeltDecoder celt_Decoder = Celt_Decoder;
		int num7 = Fs / 50;
		int num8 = num7 >> 1;
		int num9 = num8 >> 1;
		int num10 = num9 >> 1;
		if (frame_size < num10)
		{
			return -2;
		}
		frame_size = Inlines.IMIN(frame_size, Fs / 25 * 3);
		if (len <= 1)
		{
			data = null;
			frame_size = Inlines.IMIN(frame_size, this.frame_size);
		}
		int num11;
		OpusMode opusMode;
		if (data != null)
		{
			num11 = this.frame_size;
			opusMode = mode;
			entropyCoder.dec_init(data, data_ptr, (uint)len);
		}
		else
		{
			num11 = frame_size;
			opusMode = prev_mode;
			if (opusMode == (OpusMode)0)
			{
				for (int i = pcm_ptr; i < pcm_ptr + num11 * channels; i++)
				{
					pcm[i] = 0;
				}
				return num11;
			}
			if (num11 > num7)
			{
				do
				{
					int num12 = opus_decode_frame(null, 0, 0, pcm, pcm_ptr, Inlines.IMIN(num11, num7), 0);
					if (num12 < 0)
					{
						return num12;
					}
					pcm_ptr += num12 * channels;
					num11 -= num12;
				}
				while (num11 > 0);
				return frame_size;
			}
			if (num11 < num7)
			{
				if (num11 > num8)
				{
					num11 = num8;
				}
				else if (opusMode != OpusMode.MODE_SILK_ONLY && num11 > num9 && num11 < num8)
				{
					num11 = num9;
				}
			}
		}
		int num13 = ((opusMode != OpusMode.MODE_CELT_ONLY && frame_size >= num8) ? 1 : 0);
		int num14 = 0;
		int num15 = 0;
		if (data != null && prev_mode > (OpusMode)0 && ((opusMode == OpusMode.MODE_CELT_ONLY && prev_mode != OpusMode.MODE_CELT_ONLY && prev_redundancy == 0) || (opusMode != OpusMode.MODE_CELT_ONLY && prev_mode == OpusMode.MODE_CELT_ONLY)))
		{
			num2 = 1;
			if (opusMode == OpusMode.MODE_CELT_ONLY)
			{
				num15 = num9 * channels;
			}
			else
			{
				num14 = num9 * channels;
			}
		}
		short[] array2 = new short[num15];
		if (num2 != 0 && opusMode == OpusMode.MODE_CELT_ONLY)
		{
			array = array2;
			opus_decode_frame(null, 0, 0, array, 0, Inlines.IMIN(num9, num11), 0);
		}
		if (num11 > frame_size)
		{
			return -1;
		}
		frame_size = num11;
		short[] array3 = new short[(opusMode != OpusMode.MODE_CELT_ONLY && num13 == 0) ? (Inlines.IMAX(num8, frame_size) * channels) : 0];
		if (opusMode != OpusMode.MODE_CELT_ONLY)
		{
			int num16 = 0;
			short[] array4;
			if (num13 != 0)
			{
				array4 = pcm;
				num16 = pcm_ptr;
			}
			else
			{
				array4 = array3;
				num16 = 0;
			}
			if (prev_mode == OpusMode.MODE_CELT_ONLY)
			{
				DecodeAPI.silk_InitDecoder(silkDecoder);
			}
			DecControl.payloadSize_ms = Inlines.IMAX(10, 1000 * num11 / Fs);
			if (data != null)
			{
				DecControl.nChannelsInternal = stream_channels;
				if (opusMode == OpusMode.MODE_SILK_ONLY)
				{
					if (bandwidth == OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND)
					{
						DecControl.internalSampleRate = 8000;
					}
					else if (bandwidth == OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND)
					{
						DecControl.internalSampleRate = 12000;
					}
					else if (bandwidth == OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND)
					{
						DecControl.internalSampleRate = 16000;
					}
					else
					{
						DecControl.internalSampleRate = 16000;
					}
				}
				else
				{
					DecControl.internalSampleRate = 16000;
				}
			}
			int num17 = ((data == null) ? 1 : (2 * decode_fec));
			int num18 = 0;
			do
			{
				int newPacketFlag = ((num18 == 0) ? 1 : 0);
				if (DecodeAPI.silk_Decode(silkDecoder, DecControl, num17, newPacketFlag, entropyCoder, array4, num16, out var nSamplesOut) != 0)
				{
					if (num17 == 0)
					{
						return -3;
					}
					nSamplesOut = frame_size;
					Arrays.MemSetWithOffset(array4, (short)0, num16, frame_size * channels);
				}
				num16 += nSamplesOut * channels;
				num18 += nSamplesOut;
			}
			while (num18 < frame_size);
		}
		int startBand = 0;
		if (decode_fec == 0 && opusMode != OpusMode.MODE_CELT_ONLY && data != null && entropyCoder.tell() + 17 + 20 * ((mode == OpusMode.MODE_HYBRID) ? 1 : 0) <= 8 * len)
		{
			num3 = ((opusMode != OpusMode.MODE_HYBRID) ? 1 : entropyCoder.dec_bit_logp(12u));
			if (num3 != 0)
			{
				num5 = entropyCoder.dec_bit_logp(1u);
				num4 = ((opusMode == OpusMode.MODE_HYBRID) ? ((int)(entropyCoder.dec_uint(256u) + 2)) : (len - (entropyCoder.tell() + 7 >> 3)));
				len -= num4;
				if (len * 8 < entropyCoder.tell())
				{
					len = 0;
					num4 = 0;
					num3 = 0;
				}
				entropyCoder.storage = (uint)(entropyCoder.storage - num4);
			}
		}
		if (opusMode != OpusMode.MODE_CELT_ONLY)
		{
			startBand = 17;
		}
		int endBand = 21;
		switch (bandwidth)
		{
		case OpusBandwidth.OPUS_BANDWIDTH_NARROWBAND:
			endBand = 13;
			break;
		case OpusBandwidth.OPUS_BANDWIDTH_MEDIUMBAND:
		case OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND:
			endBand = 17;
			break;
		case OpusBandwidth.OPUS_BANDWIDTH_SUPERWIDEBAND:
			endBand = 19;
			break;
		case OpusBandwidth.OPUS_BANDWIDTH_FULLBAND:
			endBand = 21;
			break;
		}
		celt_Decoder.SetEndBand(endBand);
		celt_Decoder.SetChannels(stream_channels);
		if (num3 != 0)
		{
			num2 = 0;
			num14 = 0;
		}
		short[] array5 = new short[num14];
		if (num2 != 0 && opusMode != OpusMode.MODE_CELT_ONLY)
		{
			array = array5;
			opus_decode_frame(null, 0, 0, array, 0, Inlines.IMIN(num9, num11), 0);
		}
		short[] array6 = new short[(num3 != 0) ? (num9 * channels) : 0];
		if (num3 != 0 && num5 != 0)
		{
			celt_Decoder.SetStartBand(0);
			celt_Decoder.celt_decode_with_ec(data, data_ptr + len, num4, array6, 0, num9, null, 0);
			num6 = celt_Decoder.GetFinalRange();
		}
		celt_Decoder.SetStartBand(startBand);
		if (opusMode != OpusMode.MODE_SILK_ONLY)
		{
			int num19 = Inlines.IMIN(num7, frame_size);
			if (opusMode != prev_mode && prev_mode > (OpusMode)0 && prev_redundancy == 0)
			{
				celt_Decoder.ResetState();
			}
			num = celt_Decoder.celt_decode_with_ec((decode_fec != 0) ? null : data, data_ptr, len, pcm, pcm_ptr, num19, entropyCoder, num13);
		}
		else
		{
			if (num13 == 0)
			{
				for (int i = pcm_ptr; i < frame_size * channels + pcm_ptr; i++)
				{
					pcm[i] = 0;
				}
			}
			if (prev_mode == OpusMode.MODE_HYBRID && (num3 == 0 || num5 == 0 || prev_redundancy == 0))
			{
				celt_Decoder.SetStartBand(0);
				celt_Decoder.celt_decode_with_ec(SILENCE, 0, 2, pcm, pcm_ptr, num10, null, num13);
			}
		}
		if (opusMode != OpusMode.MODE_CELT_ONLY && num13 == 0)
		{
			for (int i = 0; i < frame_size * channels; i++)
			{
				pcm[pcm_ptr + i] = Inlines.SAT16(Inlines.ADD32(pcm[pcm_ptr + i], array3[i]));
			}
		}
		int[] window = celt_Decoder.GetMode().window;
		if (num3 != 0 && num5 == 0)
		{
			celt_Decoder.ResetState();
			celt_Decoder.SetStartBand(0);
			celt_Decoder.celt_decode_with_ec(data, data_ptr + len, num4, array6, 0, num9, null, 0);
			num6 = celt_Decoder.GetFinalRange();
			CodecHelpers.smooth_fade(pcm, pcm_ptr + channels * (frame_size - num10), array6, channels * num10, pcm, pcm_ptr + channels * (frame_size - num10), num10, channels, window, Fs);
		}
		if (num3 != 0 && num5 != 0)
		{
			for (int j = 0; j < channels; j++)
			{
				for (int i = 0; i < num10; i++)
				{
					pcm[channels * i + j + pcm_ptr] = array6[channels * i + j];
				}
			}
			CodecHelpers.smooth_fade(array6, channels * num10, pcm, pcm_ptr + channels * num10, pcm, pcm_ptr + channels * num10, num10, channels, window, Fs);
		}
		if (num2 != 0)
		{
			if (num11 >= num9)
			{
				for (int i = 0; i < channels * num10; i++)
				{
					pcm[i] = array[i];
				}
				CodecHelpers.smooth_fade(array, channels * num10, pcm, pcm_ptr + channels * num10, pcm, pcm_ptr + channels * num10, num10, channels, window, Fs);
			}
			else
			{
				CodecHelpers.smooth_fade(array, 0, pcm, pcm_ptr, pcm, pcm_ptr, num10, channels, window, Fs);
			}
		}
		if (decode_gain != 0)
		{
			int b = Inlines.celt_exp2(Inlines.MULT16_16_P15(21771, decode_gain));
			for (int i = pcm_ptr; i < pcm_ptr + frame_size * channels; i++)
			{
				int x = Inlines.MULT16_32_P16(pcm[i], b);
				pcm[i] = (short)Inlines.SATURATE(x, 32767);
			}
		}
		if (len <= 1)
		{
			rangeFinal = 0u;
		}
		else
		{
			rangeFinal = entropyCoder.rng ^ num6;
		}
		prev_mode = opusMode;
		prev_redundancy = ((num3 != 0 && num5 == 0) ? 1 : 0);
		if (num >= 0)
		{
			return num11;
		}
		return num;
	}

	internal int opus_decode_native(byte[] data, int data_ptr, int len, short[] pcm_out, int pcm_out_ptr, int frame_size, int decode_fec, int self_delimited, out int packet_offset, int soft_clip)
	{
		packet_offset = 0;
		short[] array = new short[48];
		if (decode_fec < 0 || decode_fec > 1)
		{
			return -1;
		}
		if ((decode_fec != 0 || len == 0 || data == null) && frame_size % (Fs / 400) != 0)
		{
			return -1;
		}
		if (len == 0 || data == null)
		{
			int num = 0;
			do
			{
				int num2 = opus_decode_frame(null, 0, 0, pcm_out, pcm_out_ptr + num * channels, frame_size - num, 0);
				if (num2 < 0)
				{
					return num2;
				}
				num += num2;
			}
			while (num < frame_size);
			last_packet_duration = num;
			return num;
		}
		if (len < 0)
		{
			return -1;
		}
		OpusMode encoderMode = OpusPacketInfo.GetEncoderMode(data, data_ptr);
		OpusBandwidth opusBandwidth = OpusPacketInfo.GetBandwidth(data, data_ptr);
		int numSamplesPerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data, data_ptr, Fs);
		int numEncodedChannels = OpusPacketInfo.GetNumEncodedChannels(data, data_ptr);
		int num3 = OpusPacketInfo.opus_packet_parse_impl(data, data_ptr, len, self_delimited, out var _, null, null, 0, array, 0, out var payload_offset, out packet_offset);
		if (num3 < 0)
		{
			return num3;
		}
		data_ptr += payload_offset;
		if (decode_fec != 0)
		{
			int packet_offset2;
			if (frame_size < numSamplesPerFrame || encoderMode == OpusMode.MODE_CELT_ONLY || mode == OpusMode.MODE_CELT_ONLY)
			{
				return opus_decode_native(null, 0, 0, pcm_out, pcm_out_ptr, frame_size, 0, 0, out packet_offset2, soft_clip);
			}
			int num4 = last_packet_duration;
			int num5;
			if (frame_size - numSamplesPerFrame != 0)
			{
				num5 = opus_decode_native(null, 0, 0, pcm_out, pcm_out_ptr, frame_size - numSamplesPerFrame, 0, 0, out packet_offset2, soft_clip);
				if (num5 < 0)
				{
					last_packet_duration = num4;
					return num5;
				}
			}
			mode = encoderMode;
			bandwidth = opusBandwidth;
			this.frame_size = numSamplesPerFrame;
			stream_channels = numEncodedChannels;
			num5 = opus_decode_frame(data, data_ptr, array[0], pcm_out, pcm_out_ptr + channels * (frame_size - numSamplesPerFrame), numSamplesPerFrame, 1);
			if (num5 < 0)
			{
				return num5;
			}
			last_packet_duration = frame_size;
			return frame_size;
		}
		if (num3 * numSamplesPerFrame > frame_size)
		{
			return -2;
		}
		mode = encoderMode;
		bandwidth = opusBandwidth;
		this.frame_size = numSamplesPerFrame;
		stream_channels = numEncodedChannels;
		int num6 = 0;
		for (int i = 0; i < num3; i++)
		{
			int num7 = opus_decode_frame(data, data_ptr, array[i], pcm_out, pcm_out_ptr + num6 * channels, frame_size - num6, 0);
			if (num7 < 0)
			{
				return num7;
			}
			data_ptr += array[i];
			num6 += num7;
		}
		last_packet_duration = num6;
		return num6;
	}

	public int Decode(byte[] in_data, int in_data_offset, int len, short[] out_pcm, int out_pcm_offset, int frame_size, bool decode_fec = false)
	{
		if (frame_size <= 0)
		{
			throw new ArgumentException("Frame size must be > 0");
		}
		try
		{
			int num = opus_decode_native(in_data, in_data_offset, len, out_pcm, out_pcm_offset, frame_size, decode_fec ? 1 : 0, 0, out var _, 0);
			if (num < 0)
			{
				if (num == -1)
				{
					throw new ArgumentException("OPUS_BAD_ARG while decoding");
				}
				throw new OpusException("An error occurred during decoding", num);
			}
			return num;
		}
		catch (ArgumentException ex)
		{
			throw new OpusException("Internal error during decoding: " + ex.Message);
		}
	}

	public int Decode(byte[] in_data, int in_data_offset, int len, float[] out_pcm, int out_pcm_offset, int frame_size, bool decode_fec = false)
	{
		if (frame_size <= 0)
		{
			throw new ArgumentException("Frame size must be > 0");
		}
		if (in_data != null && len > 0 && !decode_fec)
		{
			int numSamples = OpusPacketInfo.GetNumSamples(this, in_data, in_data_offset, len);
			if (numSamples <= 0)
			{
				throw new OpusException("An invalid packet was provided (unable to parse # of samples)");
			}
			frame_size = Inlines.IMIN(frame_size, numSamples);
		}
		short[] array = new short[frame_size * channels];
		try
		{
			int num = opus_decode_native(in_data, in_data_offset, len, array, 0, frame_size, decode_fec ? 1 : 0, 0, out var _, 0);
			if (num < 0)
			{
				if (num == -1)
				{
					throw new ArgumentException("OPUS_BAD_ARG when decoding");
				}
				throw new OpusException("An error occurred during decoding", num);
			}
			if (num > 0)
			{
				for (int i = 0; i < num * channels; i++)
				{
					out_pcm[out_pcm_offset + i] = 3.0517578E-05f * (float)array[i];
				}
			}
			return num;
		}
		catch (ArgumentException ex)
		{
			throw new OpusException("Internal error during decoding: " + ex.Message);
		}
	}

	public void ResetState()
	{
		PartialReset();
		Celt_Decoder.ResetState();
		DecodeAPI.silk_InitDecoder(SilkDecoder);
		stream_channels = channels;
		frame_size = Fs / 400;
	}
}
