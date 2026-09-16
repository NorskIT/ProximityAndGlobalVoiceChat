using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Enums;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class DecodeAPI
{
	internal static int silk_InitDecoder(SilkDecoder decState)
	{
		decState.Reset();
		int result = SilkError.SILK_NO_ERROR;
		SilkChannelDecoder[] channel_state = decState.channel_state;
		for (int i = 0; i < 2; i++)
		{
			result = channel_state[i].silk_init_decoder();
		}
		decState.sStereo.Reset();
		decState.prev_decode_only_middle = 0;
		return result;
	}

	internal static int silk_Decode(SilkDecoder psDec, DecControlState decControl, int lostFlag, int newPacketFlag, EntropyCoder psRangeDec, short[] samplesOut, int samplesOut_ptr, out int nSamplesOut)
	{
		int num = 0;
		int num2 = SilkError.SILK_NO_ERROR;
		BoxedValueInt boxedValueInt = new BoxedValueInt();
		int[] array = new int[2];
		int[] array2 = new int[2];
		SilkChannelDecoder[] channel_state = psDec.channel_state;
		nSamplesOut = 0;
		if (newPacketFlag != 0)
		{
			for (int i = 0; i < decControl.nChannelsInternal; i++)
			{
				channel_state[i].nFramesDecoded = 0;
			}
		}
		if (decControl.nChannelsInternal > psDec.nChannelsInternal)
		{
			num2 += channel_state[1].silk_init_decoder();
		}
		int num3 = ((decControl.nChannelsInternal == 1 && psDec.nChannelsInternal == 2 && decControl.internalSampleRate == 1000 * channel_state[0].fs_kHz) ? 1 : 0);
		if (channel_state[0].nFramesDecoded == 0)
		{
			for (int i = 0; i < decControl.nChannelsInternal; i++)
			{
				if (decControl.payloadSize_ms == 0)
				{
					channel_state[i].nFramesPerPacket = 1;
					channel_state[i].nb_subfr = 2;
				}
				else if (decControl.payloadSize_ms == 10)
				{
					channel_state[i].nFramesPerPacket = 1;
					channel_state[i].nb_subfr = 2;
				}
				else if (decControl.payloadSize_ms == 20)
				{
					channel_state[i].nFramesPerPacket = 1;
					channel_state[i].nb_subfr = 4;
				}
				else if (decControl.payloadSize_ms == 40)
				{
					channel_state[i].nFramesPerPacket = 2;
					channel_state[i].nb_subfr = 4;
				}
				else
				{
					if (decControl.payloadSize_ms != 60)
					{
						return SilkError.SILK_DEC_INVALID_FRAME_SIZE;
					}
					channel_state[i].nFramesPerPacket = 3;
					channel_state[i].nb_subfr = 4;
				}
				int num4 = (decControl.internalSampleRate >> 10) + 1;
				if (num4 != 8 && num4 != 12 && num4 != 16)
				{
					return SilkError.SILK_DEC_INVALID_SAMPLING_FREQUENCY;
				}
				num2 += channel_state[i].silk_decoder_set_fs(num4, decControl.API_sampleRate);
			}
		}
		if (decControl.nChannelsAPI == 2 && decControl.nChannelsInternal == 2 && (psDec.nChannelsAPI == 1 || psDec.nChannelsInternal == 1))
		{
			Arrays.MemSetShort(psDec.sStereo.pred_prev_Q13, 0, 2);
			Arrays.MemSetShort(psDec.sStereo.sSide, 0, 2);
			channel_state[1].resampler_state.Assign(channel_state[0].resampler_state);
		}
		psDec.nChannelsAPI = decControl.nChannelsAPI;
		psDec.nChannelsInternal = decControl.nChannelsInternal;
		if (decControl.API_sampleRate > 48000 || decControl.API_sampleRate < 8000)
		{
			return SilkError.SILK_DEC_INVALID_SAMPLING_FREQUENCY;
		}
		if (lostFlag != 1 && channel_state[0].nFramesDecoded == 0)
		{
			for (int i = 0; i < decControl.nChannelsInternal; i++)
			{
				for (int j = 0; j < channel_state[i].nFramesPerPacket; j++)
				{
					channel_state[i].VAD_flags[j] = psRangeDec.dec_bit_logp(1u);
				}
				channel_state[i].LBRR_flag = psRangeDec.dec_bit_logp(1u);
			}
			for (int i = 0; i < decControl.nChannelsInternal; i++)
			{
				Arrays.MemSetInt(channel_state[i].LBRR_flags, 0, 3);
				if (channel_state[i].LBRR_flag == 0)
				{
					continue;
				}
				if (channel_state[i].nFramesPerPacket == 1)
				{
					channel_state[i].LBRR_flags[0] = 1;
					continue;
				}
				int a = psRangeDec.dec_icdf(Tables.silk_LBRR_flags_iCDF_ptr[channel_state[i].nFramesPerPacket - 2], 8u) + 1;
				for (int j = 0; j < channel_state[i].nFramesPerPacket; j++)
				{
					channel_state[i].LBRR_flags[j] = Inlines.silk_RSHIFT(a, j) & 1;
				}
			}
			if (lostFlag == 0)
			{
				for (int j = 0; j < channel_state[0].nFramesPerPacket; j++)
				{
					for (int i = 0; i < decControl.nChannelsInternal; i++)
					{
						if (channel_state[i].LBRR_flags[j] == 0)
						{
							continue;
						}
						short[] pulses = new short[320];
						if (decControl.nChannelsInternal == 2 && i == 0)
						{
							Stereo.silk_stereo_decode_pred(psRangeDec, array2);
							if (channel_state[1].LBRR_flags[j] == 0)
							{
								BoxedValueInt boxedValueInt2 = new BoxedValueInt(num);
								Stereo.silk_stereo_decode_mid_only(psRangeDec, boxedValueInt2);
								num = boxedValueInt2.Val;
							}
						}
						DecodeIndices.silk_decode_indices(condCoding: (j > 0 && channel_state[i].LBRR_flags[j - 1] != 0) ? 2 : 0, psDec: channel_state[i], psRangeDec: psRangeDec, FrameIndex: j, decode_LBRR: 1);
						DecodePulses.silk_decode_pulses(psRangeDec, pulses, channel_state[i].indices.signalType, channel_state[i].indices.quantOffsetType, channel_state[i].frame_length);
					}
				}
			}
		}
		if (decControl.nChannelsInternal == 2)
		{
			if (lostFlag == 0 || (lostFlag == 2 && channel_state[0].LBRR_flags[channel_state[0].nFramesDecoded] == 1))
			{
				Stereo.silk_stereo_decode_pred(psRangeDec, array2);
				if ((lostFlag == 0 && channel_state[1].VAD_flags[channel_state[0].nFramesDecoded] == 0) || (lostFlag == 2 && channel_state[1].LBRR_flags[channel_state[0].nFramesDecoded] == 0))
				{
					BoxedValueInt boxedValueInt3 = new BoxedValueInt(num);
					Stereo.silk_stereo_decode_mid_only(psRangeDec, boxedValueInt3);
					num = boxedValueInt3.Val;
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				for (int i = 0; i < 2; i++)
				{
					array2[i] = psDec.sStereo.pred_prev_Q13[i];
				}
			}
		}
		if (decControl.nChannelsInternal == 2 && num == 0 && psDec.prev_decode_only_middle == 1)
		{
			Arrays.MemSetShort(psDec.channel_state[1].outBuf, 0, 480);
			Arrays.MemSetInt(psDec.channel_state[1].sLPC_Q14_buf, 0, 16);
			psDec.channel_state[1].lagPrev = 100;
			psDec.channel_state[1].LastGainIndex = 10;
			psDec.channel_state[1].prevSignalType = 0;
			psDec.channel_state[1].first_frame_after_reset = 1;
		}
		int num5 = ((decControl.internalSampleRate * decControl.nChannelsInternal < decControl.API_sampleRate * decControl.nChannelsAPI) ? 1 : 0);
		short[] array3;
		if (num5 != 0)
		{
			array3 = samplesOut;
			array[0] = samplesOut_ptr;
			array[1] = samplesOut_ptr + channel_state[0].frame_length + 2;
		}
		else
		{
			array3 = new short[decControl.nChannelsInternal * (channel_state[0].frame_length + 2)];
			array[0] = 0;
			array[1] = channel_state[0].frame_length + 2;
		}
		int num6 = ((lostFlag != 0) ? ((psDec.prev_decode_only_middle == 0 || (decControl.nChannelsInternal == 2 && lostFlag == 2 && channel_state[1].LBRR_flags[channel_state[1].nFramesDecoded] == 1)) ? 1 : 0) : ((num == 0) ? 1 : 0));
		for (int i = 0; i < decControl.nChannelsInternal; i++)
		{
			if (i == 0 || num6 != 0)
			{
				int num7 = channel_state[0].nFramesDecoded - i;
				int condCoding = ((num7 > 0) ? ((lostFlag == 2) ? ((channel_state[i].LBRR_flags[num7 - 1] != 0) ? 2 : 0) : ((i > 0 && psDec.prev_decode_only_middle != 0) ? 1 : 2)) : 0);
				num2 += channel_state[i].silk_decode_frame(psRangeDec, array3, array[i] + 2, boxedValueInt, lostFlag, condCoding);
			}
			else
			{
				Arrays.MemSetWithOffset(array3, (short)0, array[i] + 2, boxedValueInt.Val);
			}
			channel_state[i].nFramesDecoded++;
		}
		if (decControl.nChannelsAPI == 2 && decControl.nChannelsInternal == 2)
		{
			Stereo.silk_stereo_MS_to_LR(psDec.sStereo, array3, array[0], array3, array[1], array2, channel_state[0].fs_kHz, boxedValueInt.Val);
		}
		else
		{
			Array.Copy(psDec.sStereo.sMid, 0, array3, array[0], 2);
			Array.Copy(array3, array[0] + boxedValueInt.Val, psDec.sStereo.sMid, 0, 2);
		}
		nSamplesOut = Inlines.silk_DIV32(boxedValueInt.Val * decControl.API_sampleRate, Inlines.silk_SMULBB(channel_state[0].fs_kHz, 1000));
		short[] array4;
		int num8;
		if (decControl.nChannelsAPI == 2)
		{
			array4 = new short[nSamplesOut];
			num8 = 0;
		}
		else
		{
			array4 = samplesOut;
			num8 = samplesOut_ptr;
		}
		if (num5 != 0)
		{
			short[] array5 = new short[decControl.nChannelsInternal * (channel_state[0].frame_length + 2)];
			Array.Copy(samplesOut, samplesOut_ptr, array5, 0, decControl.nChannelsInternal * (channel_state[0].frame_length + 2));
			array3 = array5;
			array[0] = 0;
			array[1] = channel_state[0].frame_length + 2;
		}
		for (int i = 0; i < Inlines.silk_min(decControl.nChannelsAPI, decControl.nChannelsInternal); i++)
		{
			num2 += Resampler.silk_resampler(channel_state[i].resampler_state, array4, num8, array3, array[i] + 1, boxedValueInt.Val);
			if (decControl.nChannelsAPI == 2)
			{
				int num9 = samplesOut_ptr + i;
				for (int j = 0; j < nSamplesOut; j++)
				{
					samplesOut[num9 + 2 * j] = array4[num8 + j];
				}
			}
		}
		if (decControl.nChannelsAPI == 2 && decControl.nChannelsInternal == 1)
		{
			if (num3 != 0)
			{
				num2 += Resampler.silk_resampler(channel_state[1].resampler_state, array4, num8, array3, array[0] + 1, boxedValueInt.Val);
				for (int j = 0; j < nSamplesOut; j++)
				{
					samplesOut[samplesOut_ptr + 1 + 2 * j] = array4[num8 + j];
				}
			}
			else
			{
				for (int j = 0; j < nSamplesOut; j++)
				{
					samplesOut[samplesOut_ptr + 1 + 2 * j] = samplesOut[samplesOut_ptr + 2 * j];
				}
			}
		}
		if (channel_state[0].prevSignalType == 2)
		{
			int[] array6 = new int[3] { 6, 4, 3 };
			decControl.prevPitchLag = channel_state[0].lagPrev * array6[channel_state[0].fs_kHz - 8 >> 2];
		}
		else
		{
			decControl.prevPitchLag = 0;
		}
		if (lostFlag == 1)
		{
			for (int j = 0; j < psDec.nChannelsInternal; j++)
			{
				psDec.channel_state[j].LastGainIndex = 10;
			}
		}
		else
		{
			psDec.prev_decode_only_middle = num;
		}
		return num2;
	}
}
