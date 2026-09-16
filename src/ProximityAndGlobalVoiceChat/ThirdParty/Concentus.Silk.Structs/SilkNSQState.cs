using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;

namespace Concentus.Silk.Structs;

internal class SilkNSQState
{
	private class NSQ_del_dec_struct
	{
		internal readonly int[] sLPC_Q14 = new int[80 + SilkConstants.NSQ_LPC_BUF_LENGTH];

		internal readonly int[] RandState = new int[32];

		internal readonly int[] Q_Q10 = new int[32];

		internal readonly int[] Xq_Q14 = new int[32];

		internal readonly int[] Pred_Q15 = new int[32];

		internal readonly int[] Shape_Q14 = new int[32];

		internal int[] sAR2_Q14;

		internal int LF_AR_Q14;

		internal int Seed;

		internal int SeedInit;

		internal int RD_Q10;

		internal NSQ_del_dec_struct(int shapingOrder)
		{
			sAR2_Q14 = new int[shapingOrder];
		}

		internal void PartialCopyFrom(NSQ_del_dec_struct other, int q14Offset)
		{
			Buffer.BlockCopy(other.sLPC_Q14, q14Offset * 4, sLPC_Q14, q14Offset * 4, (80 + SilkConstants.NSQ_LPC_BUF_LENGTH - q14Offset) * 4);
			Buffer.BlockCopy(other.RandState, 0, RandState, 0, 128);
			Buffer.BlockCopy(other.Q_Q10, 0, Q_Q10, 0, 128);
			Buffer.BlockCopy(other.Xq_Q14, 0, Xq_Q14, 0, 128);
			Buffer.BlockCopy(other.Pred_Q15, 0, Pred_Q15, 0, 128);
			Buffer.BlockCopy(other.Shape_Q14, 0, Shape_Q14, 0, 128);
			Buffer.BlockCopy(other.sAR2_Q14, 0, sAR2_Q14, 0, sAR2_Q14.Length * 4);
			LF_AR_Q14 = other.LF_AR_Q14;
			Seed = other.Seed;
			SeedInit = other.SeedInit;
			RD_Q10 = other.RD_Q10;
		}

		internal void Assign(NSQ_del_dec_struct other)
		{
			PartialCopyFrom(other, 0);
		}
	}

	private struct NSQ_sample_struct
	{
		internal int Q_Q10;

		internal int RD_Q10;

		internal int xq_Q14;

		internal int LF_AR_Q14;

		internal int sLTP_shp_Q14;

		internal int LPC_exc_Q14;
	}

	internal readonly short[] xq = new short[640];

	internal readonly int[] sLTP_shp_Q14 = new int[640];

	internal readonly int[] sLPC_Q14 = new int[80 + SilkConstants.NSQ_LPC_BUF_LENGTH];

	internal readonly int[] sAR2_Q14 = new int[16];

	internal int sLF_AR_shp_Q14;

	internal int lagPrev;

	internal int sLTP_buf_idx;

	internal int sLTP_shp_buf_idx;

	internal int rand_seed;

	internal int prev_gain_Q16;

	internal int rewhite_flag;

	internal void Reset()
	{
		Arrays.MemSetShort(xq, 0, 640);
		Arrays.MemSetInt(sLTP_shp_Q14, 0, 640);
		Arrays.MemSetInt(sLPC_Q14, 0, 80 + SilkConstants.NSQ_LPC_BUF_LENGTH);
		Arrays.MemSetInt(sAR2_Q14, 0, 16);
		sLF_AR_shp_Q14 = 0;
		lagPrev = 0;
		sLTP_buf_idx = 0;
		sLTP_shp_buf_idx = 0;
		rand_seed = 0;
		prev_gain_Q16 = 0;
		rewhite_flag = 0;
	}

	internal void Assign(SilkNSQState other)
	{
		sLF_AR_shp_Q14 = other.sLF_AR_shp_Q14;
		lagPrev = other.lagPrev;
		sLTP_buf_idx = other.sLTP_buf_idx;
		sLTP_shp_buf_idx = other.sLTP_shp_buf_idx;
		rand_seed = other.rand_seed;
		prev_gain_Q16 = other.prev_gain_Q16;
		rewhite_flag = other.rewhite_flag;
		Array.Copy(other.xq, xq, 640);
		Array.Copy(other.sLTP_shp_Q14, sLTP_shp_Q14, 640);
		Array.Copy(other.sLPC_Q14, sLPC_Q14, 80 + SilkConstants.NSQ_LPC_BUF_LENGTH);
		Array.Copy(other.sAR2_Q14, sAR2_Q14, 16);
	}

	internal void silk_NSQ(SilkChannelEncoder psEncC, SideInfoIndices psIndices, int[] x_Q3, sbyte[] pulses, short[][] PredCoef_Q12, short[] LTPCoef_Q14, short[] AR2_Q13, int[] HarmShapeGain_Q14, int[] Tilt_Q14, int[] LF_shp_Q14, int[] Gains_Q16, int[] pitchL, int Lambda_Q10, int LTP_scale_Q14)
	{
		int num = 0;
		int num2 = 0;
		rand_seed = psIndices.Seed;
		int num3 = lagPrev;
		int offset_Q = Tables.silk_Quantization_Offsets_Q10[psIndices.signalType >> 1][psIndices.quantOffsetType];
		int num4 = ((psIndices.NLSFInterpCoef_Q2 != 4) ? 1 : 0);
		int[] sLTP_Q = new int[psEncC.ltp_mem_length + psEncC.frame_length];
		short[] array = new short[psEncC.ltp_mem_length + psEncC.frame_length];
		int[] x_sc_Q = new int[psEncC.subfr_length];
		sLTP_shp_buf_idx = psEncC.ltp_mem_length;
		sLTP_buf_idx = psEncC.ltp_mem_length;
		int num5 = psEncC.ltp_mem_length;
		for (int i = 0; i < psEncC.nb_subfr; i++)
		{
			int num6 = (i >> 1) | (1 - num4);
			int b_Q14_ptr = i * 5;
			int aR_shp_Q13_ptr = i * 16;
			int num7 = Inlines.silk_RSHIFT(HarmShapeGain_Q14[i], 2);
			num7 |= Inlines.silk_LSHIFT(Inlines.silk_RSHIFT(HarmShapeGain_Q14[i], 1), 16);
			rewhite_flag = 0;
			if (psIndices.signalType == 2)
			{
				num3 = pitchL[i];
				if ((i & (3 - Inlines.silk_LSHIFT(num4, 1))) == 0)
				{
					int num8 = psEncC.ltp_mem_length - num3 - psEncC.predictLPCOrder - 2;
					Filters.silk_LPC_analysis_filter(array, num8, xq, num8 + i * psEncC.subfr_length, PredCoef_Q12[num6], 0, psEncC.ltp_mem_length - num8, psEncC.predictLPCOrder);
					rewhite_flag = 1;
					sLTP_buf_idx = psEncC.ltp_mem_length;
				}
			}
			silk_nsq_scale_states(psEncC, x_Q3, num2, x_sc_Q, array, sLTP_Q, i, LTP_scale_Q14, Gains_Q16, pitchL, psIndices.signalType);
			silk_noise_shape_quantizer(psIndices.signalType, x_sc_Q, pulses, num, xq, num5, sLTP_Q, PredCoef_Q12[num6], LTPCoef_Q14, b_Q14_ptr, AR2_Q13, aR_shp_Q13_ptr, num3, num7, Tilt_Q14[i], LF_shp_Q14[i], Gains_Q16[i], Lambda_Q10, offset_Q, psEncC.subfr_length, psEncC.shapingLPCOrder, psEncC.predictLPCOrder);
			num2 += psEncC.subfr_length;
			num += psEncC.subfr_length;
			num5 += psEncC.subfr_length;
		}
		lagPrev = pitchL[psEncC.nb_subfr - 1];
		Arrays.MemMoveShort(xq, psEncC.frame_length, 0, psEncC.ltp_mem_length);
		Arrays.MemMoveInt(sLTP_shp_Q14, psEncC.frame_length, 0, psEncC.ltp_mem_length);
	}

	private void silk_noise_shape_quantizer(int signalType, int[] x_sc_Q10, sbyte[] pulses, int pulses_ptr, short[] xq, int xq_ptr, int[] sLTP_Q15, short[] a_Q12, short[] b_Q14, int b_Q14_ptr, short[] AR_shp_Q13, int AR_shp_Q13_ptr, int lag, int HarmShapeFIRPacked_Q14, int Tilt_Q14, int LF_shp_Q14, int Gain_Q16, int Lambda_Q10, int offset_Q10, int length, int shapingLPCOrder, int predictLPCOrder)
	{
		int num = sLTP_shp_buf_idx - lag + 1;
		int num2 = sLTP_buf_idx - lag + 2;
		int b = Inlines.silk_RSHIFT(Gain_Q16, 6);
		int num3 = SilkConstants.NSQ_LPC_BUF_LENGTH - 1;
		for (int i = 0; i < length; i++)
		{
			rand_seed = Inlines.silk_RAND(rand_seed);
			int a = Inlines.silk_RSHIFT(predictLPCOrder, 1);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3], a_Q12[0]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 1], a_Q12[1]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 2], a_Q12[2]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 3], a_Q12[3]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 4], a_Q12[4]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 5], a_Q12[5]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 6], a_Q12[6]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 7], a_Q12[7]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 8], a_Q12[8]);
			a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 9], a_Q12[9]);
			if (predictLPCOrder == 16)
			{
				a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 10], a_Q12[10]);
				a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 11], a_Q12[11]);
				a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 12], a_Q12[12]);
				a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 13], a_Q12[13]);
				a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 14], a_Q12[14]);
				a = Inlines.silk_SMLAWB(a, sLPC_Q14[num3 - 15], a_Q12[15]);
			}
			int a2;
			if (signalType == 2)
			{
				a2 = 2;
				a2 = Inlines.silk_SMLAWB(a2, sLTP_Q15[num2], b_Q14[b_Q14_ptr]);
				a2 = Inlines.silk_SMLAWB(a2, sLTP_Q15[num2 - 1], b_Q14[b_Q14_ptr + 1]);
				a2 = Inlines.silk_SMLAWB(a2, sLTP_Q15[num2 - 2], b_Q14[b_Q14_ptr + 2]);
				a2 = Inlines.silk_SMLAWB(a2, sLTP_Q15[num2 - 3], b_Q14[b_Q14_ptr + 3]);
				a2 = Inlines.silk_SMLAWB(a2, sLTP_Q15[num2 - 4], b_Q14[b_Q14_ptr + 4]);
				num2++;
			}
			else
			{
				a2 = 0;
			}
			int num4 = sLPC_Q14[num3];
			int num5 = sAR2_Q14[0];
			sAR2_Q14[0] = num4;
			int a3 = Inlines.silk_RSHIFT(shapingLPCOrder, 1);
			a3 = Inlines.silk_SMLAWB(a3, num4, AR_shp_Q13[AR_shp_Q13_ptr]);
			for (int j = 2; j < shapingLPCOrder; j += 2)
			{
				num4 = sAR2_Q14[j - 1];
				sAR2_Q14[j - 1] = num5;
				a3 = Inlines.silk_SMLAWB(a3, num5, AR_shp_Q13[AR_shp_Q13_ptr + j - 1]);
				num5 = sAR2_Q14[j];
				sAR2_Q14[j] = num4;
				a3 = Inlines.silk_SMLAWB(a3, num4, AR_shp_Q13[AR_shp_Q13_ptr + j]);
			}
			sAR2_Q14[shapingLPCOrder - 1] = num5;
			a3 = Inlines.silk_SMLAWB(a3, num5, AR_shp_Q13[AR_shp_Q13_ptr + shapingLPCOrder - 1]);
			a3 = Inlines.silk_LSHIFT32(a3, 1);
			a3 = Inlines.silk_SMLAWB(a3, sLF_AR_shp_Q14, Tilt_Q14);
			int a4 = Inlines.silk_SMULWB(sLTP_shp_Q14[sLTP_shp_buf_idx - 1], LF_shp_Q14);
			a4 = Inlines.silk_SMLAWT(a4, sLF_AR_shp_Q14, LF_shp_Q14);
			num5 = Inlines.silk_SUB32(Inlines.silk_LSHIFT32(a, 2), a3);
			num5 = Inlines.silk_SUB32(num5, a4);
			if (lag > 0)
			{
				int a5 = Inlines.silk_SMULWB(Inlines.silk_ADD32(sLTP_shp_Q14[num], sLTP_shp_Q14[num - 2]), HarmShapeFIRPacked_Q14);
				a5 = Inlines.silk_SMLAWT(a5, sLTP_shp_Q14[num - 1], HarmShapeFIRPacked_Q14);
				a5 = Inlines.silk_LSHIFT(a5, 1);
				num++;
				num4 = Inlines.silk_SUB32(a2, a5);
				num5 = Inlines.silk_ADD_LSHIFT32(num4, num5, 1);
				num5 = Inlines.silk_RSHIFT_ROUND(num5, 3);
			}
			else
			{
				num5 = Inlines.silk_RSHIFT_ROUND(num5, 2);
			}
			int num6 = Inlines.silk_SUB32(x_sc_Q10[i], num5);
			if (rand_seed < 0)
			{
				num6 = -num6;
			}
			num6 = Inlines.silk_LIMIT_32(num6, -31744, 30720);
			int a6 = Inlines.silk_SUB32(num6, offset_Q10);
			int num7 = Inlines.silk_RSHIFT(a6, 10);
			int num8;
			int a7;
			int a8;
			if (num7 > 0)
			{
				a6 = Inlines.silk_SUB32(Inlines.silk_LSHIFT(num7, 10), 80);
				a6 = Inlines.silk_ADD32(a6, offset_Q10);
				num8 = Inlines.silk_ADD32(a6, 1024);
				a7 = Inlines.silk_SMULBB(a6, Lambda_Q10);
				a8 = Inlines.silk_SMULBB(num8, Lambda_Q10);
			}
			else
			{
				switch (num7)
				{
				case 0:
					a6 = offset_Q10;
					num8 = Inlines.silk_ADD32(a6, 944);
					a7 = Inlines.silk_SMULBB(a6, Lambda_Q10);
					a8 = Inlines.silk_SMULBB(num8, Lambda_Q10);
					break;
				case -1:
					num8 = offset_Q10;
					a6 = Inlines.silk_SUB32(num8, 944);
					a7 = Inlines.silk_SMULBB(-a6, Lambda_Q10);
					a8 = Inlines.silk_SMULBB(num8, Lambda_Q10);
					break;
				default:
					a6 = Inlines.silk_ADD32(Inlines.silk_LSHIFT(num7, 10), 80);
					a6 = Inlines.silk_ADD32(a6, offset_Q10);
					num8 = Inlines.silk_ADD32(a6, 1024);
					a7 = Inlines.silk_SMULBB(-a6, Lambda_Q10);
					a8 = Inlines.silk_SMULBB(-num8, Lambda_Q10);
					break;
				}
			}
			int num9 = Inlines.silk_SUB32(num6, a6);
			a7 = Inlines.silk_SMLABB(a7, num9, num9);
			num9 = Inlines.silk_SUB32(num6, num8);
			a8 = Inlines.silk_SMLABB(a8, num9, num9);
			if (a8 < a7)
			{
				a6 = num8;
			}
			pulses[pulses_ptr + i] = (sbyte)Inlines.silk_RSHIFT_ROUND(a6, 10);
			int num10 = Inlines.silk_LSHIFT(a6, 4);
			if (rand_seed < 0)
			{
				num10 = -num10;
			}
			int a9 = Inlines.silk_ADD_LSHIFT32(num10, a2, 1);
			int num11 = Inlines.silk_ADD_LSHIFT32(a9, a, 4);
			xq[xq_ptr + i] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(Inlines.silk_SMULWW(num11, b), 8));
			num3++;
			sLPC_Q14[num3] = num11;
			int a10 = (sLF_AR_shp_Q14 = Inlines.silk_SUB_LSHIFT32(num11, a3, 2));
			sLTP_shp_Q14[sLTP_shp_buf_idx] = Inlines.silk_SUB_LSHIFT32(a10, a4, 2);
			sLTP_Q15[sLTP_buf_idx] = Inlines.silk_LSHIFT(a9, 1);
			sLTP_shp_buf_idx++;
			sLTP_buf_idx++;
			rand_seed = Inlines.silk_ADD32_ovflw(rand_seed, pulses[pulses_ptr + i]);
		}
		Array.Copy(sLPC_Q14, length, sLPC_Q14, 0, SilkConstants.NSQ_LPC_BUF_LENGTH);
	}

	private void silk_nsq_scale_states(SilkChannelEncoder psEncC, int[] x_Q3, int x_Q3_ptr, int[] x_sc_Q10, short[] sLTP, int[] sLTP_Q15, int subfr, int LTP_scale_Q14, int[] Gains_Q16, int[] pitchL, int signal_type)
	{
		int num = pitchL[subfr];
		int num2 = Inlines.silk_INVERSE32_varQ(Inlines.silk_max(Gains_Q16[subfr], 1), 47);
		int num3 = ((Gains_Q16[subfr] == prev_gain_Q16) ? 65536 : Inlines.silk_DIV32_varQ(prev_gain_Q16, Gains_Q16[subfr], 16));
		int b = Inlines.silk_RSHIFT_ROUND(num2, 8);
		for (int i = 0; i < psEncC.subfr_length; i++)
		{
			x_sc_Q10[i] = Inlines.silk_SMULWW(x_Q3[x_Q3_ptr + i], b);
		}
		prev_gain_Q16 = Gains_Q16[subfr];
		if (rewhite_flag != 0)
		{
			if (subfr == 0)
			{
				num2 = Inlines.silk_LSHIFT(Inlines.silk_SMULWB(num2, LTP_scale_Q14), 2);
			}
			for (int i = sLTP_buf_idx - num - 2; i < sLTP_buf_idx; i++)
			{
				sLTP_Q15[i] = Inlines.silk_SMULWB(num2, sLTP[i]);
			}
		}
		if (num3 == 65536)
		{
			return;
		}
		for (int i = sLTP_shp_buf_idx - psEncC.ltp_mem_length; i < sLTP_shp_buf_idx; i++)
		{
			sLTP_shp_Q14[i] = Inlines.silk_SMULWW(num3, sLTP_shp_Q14[i]);
		}
		if (signal_type == 2 && rewhite_flag == 0)
		{
			for (int i = sLTP_buf_idx - num - 2; i < sLTP_buf_idx; i++)
			{
				sLTP_Q15[i] = Inlines.silk_SMULWW(num3, sLTP_Q15[i]);
			}
		}
		sLF_AR_shp_Q14 = Inlines.silk_SMULWW(num3, sLF_AR_shp_Q14);
		for (int i = 0; i < SilkConstants.NSQ_LPC_BUF_LENGTH; i++)
		{
			sLPC_Q14[i] = Inlines.silk_SMULWW(num3, sLPC_Q14[i]);
		}
		for (int i = 0; i < 16; i++)
		{
			sAR2_Q14[i] = Inlines.silk_SMULWW(num3, sAR2_Q14[i]);
		}
	}

	internal void silk_NSQ_del_dec(SilkChannelEncoder psEncC, SideInfoIndices psIndices, int[] x_Q3, sbyte[] pulses, short[][] PredCoef_Q12, short[] LTPCoef_Q14, short[] AR2_Q13, int[] HarmShapeGain_Q14, int[] Tilt_Q14, int[] LF_shp_Q14, int[] Gains_Q16, int[] pitchL, int Lambda_Q10, int LTP_scale_Q14)
	{
		int num = 0;
		int num2 = 0;
		int num3 = lagPrev;
		NSQ_del_dec_struct[] array = new NSQ_del_dec_struct[psEncC.nStatesDelayedDecision];
		for (int i = 0; i < psEncC.nStatesDelayedDecision; i++)
		{
			array[i] = new NSQ_del_dec_struct(psEncC.shapingLPCOrder);
		}
		NSQ_del_dec_struct nSQ_del_dec_struct;
		for (int j = 0; j < psEncC.nStatesDelayedDecision; j++)
		{
			nSQ_del_dec_struct = array[j];
			nSQ_del_dec_struct.Seed = (j + psIndices.Seed) & 3;
			nSQ_del_dec_struct.SeedInit = nSQ_del_dec_struct.Seed;
			nSQ_del_dec_struct.RD_Q10 = 0;
			nSQ_del_dec_struct.LF_AR_Q14 = sLF_AR_shp_Q14;
			nSQ_del_dec_struct.Shape_Q14[0] = sLTP_shp_Q14[psEncC.ltp_mem_length - 1];
			Array.Copy(sLPC_Q14, nSQ_del_dec_struct.sLPC_Q14, SilkConstants.NSQ_LPC_BUF_LENGTH);
			Array.Copy(sAR2_Q14, nSQ_del_dec_struct.sAR2_Q14, psEncC.shapingLPCOrder);
		}
		int offset_Q = Tables.silk_Quantization_Offsets_Q10[psIndices.signalType >> 1][psIndices.quantOffsetType];
		int num4 = 0;
		int num5 = Inlines.silk_min_int(32, psEncC.subfr_length);
		if (psIndices.signalType == 2)
		{
			for (int j = 0; j < psEncC.nb_subfr; j++)
			{
				num5 = Inlines.silk_min_int(num5, pitchL[j] - 2 - 1);
			}
		}
		else if (num3 > 0)
		{
			num5 = Inlines.silk_min_int(num5, num3 - 2 - 1);
		}
		int num6 = ((psIndices.NLSFInterpCoef_Q2 != 4) ? 1 : 0);
		int[] sLTP_Q = new int[psEncC.ltp_mem_length + psEncC.frame_length];
		short[] array2 = new short[psEncC.ltp_mem_length + psEncC.frame_length];
		int[] array3 = new int[psEncC.subfr_length];
		int[] delayedGain_Q = new int[32];
		int num7 = psEncC.ltp_mem_length;
		sLTP_shp_buf_idx = psEncC.ltp_mem_length;
		sLTP_buf_idx = psEncC.ltp_mem_length;
		int num8 = 0;
		int rD_Q;
		int num11;
		int num12;
		for (int j = 0; j < psEncC.nb_subfr; j++)
		{
			int num9 = (j >> 1) | (1 - num6);
			int num10 = Inlines.silk_RSHIFT(HarmShapeGain_Q14[j], 2);
			num10 |= Inlines.silk_LSHIFT(Inlines.silk_RSHIFT(HarmShapeGain_Q14[j], 1), 16);
			rewhite_flag = 0;
			if (psIndices.signalType == 2)
			{
				num3 = pitchL[j];
				if ((j & (3 - Inlines.silk_LSHIFT(num6, 1))) == 0)
				{
					if (j == 2)
					{
						rD_Q = array[0].RD_Q10;
						num11 = 0;
						for (int k = 1; k < psEncC.nStatesDelayedDecision; k++)
						{
							if (array[k].RD_Q10 < rD_Q)
							{
								rD_Q = array[k].RD_Q10;
								num11 = k;
							}
						}
						for (int k = 0; k < psEncC.nStatesDelayedDecision; k++)
						{
							if (k != num11)
							{
								array[k].RD_Q10 += 134217727;
							}
						}
						nSQ_del_dec_struct = array[num11];
						num12 = num4 + num5;
						for (int k = 0; k < num5; k++)
						{
							num12 = (num12 - 1) & 0x1F;
							pulses[num + k - num5] = (sbyte)Inlines.silk_RSHIFT_ROUND(nSQ_del_dec_struct.Q_Q10[num12], 10);
							xq[num7 + k - num5] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(Inlines.silk_SMULWW(nSQ_del_dec_struct.Xq_Q14[num12], Gains_Q16[1]), 14));
							sLTP_shp_Q14[sLTP_shp_buf_idx - num5 + k] = nSQ_del_dec_struct.Shape_Q14[num12];
						}
						num8 = 0;
					}
					int num13 = psEncC.ltp_mem_length - num3 - psEncC.predictLPCOrder - 2;
					Filters.silk_LPC_analysis_filter(array2, num13, xq, num13 + j * psEncC.subfr_length, PredCoef_Q12[num9], 0, psEncC.ltp_mem_length - num13, psEncC.predictLPCOrder);
					sLTP_buf_idx = psEncC.ltp_mem_length;
					rewhite_flag = 1;
				}
			}
			silk_nsq_del_dec_scale_states(psEncC, array, x_Q3, num2, array3, array2, sLTP_Q, j, psEncC.nStatesDelayedDecision, LTP_scale_Q14, Gains_Q16, pitchL, psIndices.signalType, num5);
			BoxedValueInt boxedValueInt = new BoxedValueInt(num4);
			silk_noise_shape_quantizer_del_dec(array, psIndices.signalType, array3, pulses, num, xq, num7, sLTP_Q, delayedGain_Q, PredCoef_Q12[num9], LTPCoef_Q14, j * 5, AR2_Q13, j * 16, num3, num10, Tilt_Q14[j], LF_shp_Q14[j], Gains_Q16[j], Lambda_Q10, offset_Q, psEncC.subfr_length, num8++, psEncC.shapingLPCOrder, psEncC.predictLPCOrder, psEncC.warping_Q16, psEncC.nStatesDelayedDecision, boxedValueInt, num5);
			num4 = boxedValueInt.Val;
			num2 += psEncC.subfr_length;
			num += psEncC.subfr_length;
			num7 += psEncC.subfr_length;
		}
		rD_Q = array[0].RD_Q10;
		num11 = 0;
		for (int j = 1; j < psEncC.nStatesDelayedDecision; j++)
		{
			if (array[j].RD_Q10 < rD_Q)
			{
				rD_Q = array[j].RD_Q10;
				num11 = j;
			}
		}
		nSQ_del_dec_struct = array[num11];
		psIndices.Seed = (sbyte)nSQ_del_dec_struct.SeedInit;
		num12 = num4 + num5;
		int b = Inlines.silk_RSHIFT32(Gains_Q16[psEncC.nb_subfr - 1], 6);
		for (int k = 0; k < num5; k++)
		{
			num12 = (num12 - 1) & 0x1F;
			pulses[num + k - num5] = (sbyte)Inlines.silk_RSHIFT_ROUND(nSQ_del_dec_struct.Q_Q10[num12], 10);
			xq[num7 + k - num5] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(Inlines.silk_SMULWW(nSQ_del_dec_struct.Xq_Q14[num12], b), 8));
			sLTP_shp_Q14[sLTP_shp_buf_idx - num5 + k] = nSQ_del_dec_struct.Shape_Q14[num12];
		}
		Array.Copy(nSQ_del_dec_struct.sLPC_Q14, psEncC.subfr_length, sLPC_Q14, 0, SilkConstants.NSQ_LPC_BUF_LENGTH);
		Array.Copy(nSQ_del_dec_struct.sAR2_Q14, 0, sAR2_Q14, 0, psEncC.shapingLPCOrder);
		sLF_AR_shp_Q14 = nSQ_del_dec_struct.LF_AR_Q14;
		lagPrev = pitchL[psEncC.nb_subfr - 1];
		Arrays.MemMoveShort(xq, psEncC.frame_length, 0, psEncC.ltp_mem_length);
		Arrays.MemMoveInt(sLTP_shp_Q14, psEncC.frame_length, 0, psEncC.ltp_mem_length);
	}

	private void silk_noise_shape_quantizer_del_dec(NSQ_del_dec_struct[] psDelDec, int signalType, int[] x_Q10, sbyte[] pulses, int pulses_ptr, short[] xq, int xq_ptr, int[] sLTP_Q15, int[] delayedGain_Q10, short[] a_Q12, short[] b_Q14, int b_Q14_ptr, short[] AR_shp_Q13, int AR_shp_Q13_ptr, int lag, int HarmShapeFIRPacked_Q14, int Tilt_Q14, int LF_shp_Q14, int Gain_Q16, int Lambda_Q10, int offset_Q10, int length, int subfr, int shapingLPCOrder, int predictLPCOrder, int warping_Q16, int nStatesDelayedDecision, BoxedValueInt smpl_buf_idx, int decisionDelay)
	{
		NSQ_sample_struct[] array = new NSQ_sample_struct[2 * nStatesDelayedDecision];
		for (int i = 0; i < 2 * nStatesDelayedDecision; i++)
		{
			array[i] = default(NSQ_sample_struct);
		}
		int num = sLTP_shp_buf_idx - lag + 1;
		int num2 = sLTP_buf_idx - lag + 2;
		int num3 = Inlines.silk_RSHIFT(Gain_Q16, 6);
		for (int j = 0; j < length; j++)
		{
			int a;
			if (signalType == 2)
			{
				a = 2;
				a = Inlines.silk_SMLAWB(a, sLTP_Q15[num2], b_Q14[b_Q14_ptr]);
				a = Inlines.silk_SMLAWB(a, sLTP_Q15[num2 - 1], b_Q14[b_Q14_ptr + 1]);
				a = Inlines.silk_SMLAWB(a, sLTP_Q15[num2 - 2], b_Q14[b_Q14_ptr + 2]);
				a = Inlines.silk_SMLAWB(a, sLTP_Q15[num2 - 3], b_Q14[b_Q14_ptr + 3]);
				a = Inlines.silk_SMLAWB(a, sLTP_Q15[num2 - 4], b_Q14[b_Q14_ptr + 4]);
				a = Inlines.silk_LSHIFT(a, 1);
				num2++;
			}
			else
			{
				a = 0;
			}
			int a2;
			if (lag > 0)
			{
				a2 = Inlines.silk_SMULWB(Inlines.silk_ADD32(sLTP_shp_Q14[num], sLTP_shp_Q14[num - 2]), HarmShapeFIRPacked_Q14);
				a2 = Inlines.silk_SMLAWT(a2, sLTP_shp_Q14[num - 1], HarmShapeFIRPacked_Q14);
				a2 = Inlines.silk_SUB_LSHIFT32(a, a2, 2);
				num++;
			}
			else
			{
				a2 = 0;
			}
			NSQ_del_dec_struct nSQ_del_dec_struct;
			for (int k = 0; k < nStatesDelayedDecision; k++)
			{
				nSQ_del_dec_struct = psDelDec[k];
				int[] array2 = nSQ_del_dec_struct.sAR2_Q14;
				int num4 = 2 * k;
				int num5 = num4 + 1;
				nSQ_del_dec_struct.Seed = Inlines.silk_RAND(nSQ_del_dec_struct.Seed);
				int num6 = SilkConstants.NSQ_LPC_BUF_LENGTH - 1 + j;
				int a3 = Inlines.silk_RSHIFT(predictLPCOrder, 1);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6], a_Q12[0]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 1], a_Q12[1]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 2], a_Q12[2]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 3], a_Q12[3]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 4], a_Q12[4]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 5], a_Q12[5]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 6], a_Q12[6]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 7], a_Q12[7]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 8], a_Q12[8]);
				a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 9], a_Q12[9]);
				if (predictLPCOrder == 16)
				{
					a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 10], a_Q12[10]);
					a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 11], a_Q12[11]);
					a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 12], a_Q12[12]);
					a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 13], a_Q12[13]);
					a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 14], a_Q12[14]);
					a3 = Inlines.silk_SMLAWB(a3, nSQ_del_dec_struct.sLPC_Q14[num6 - 15], a_Q12[15]);
				}
				a3 = Inlines.silk_LSHIFT(a3, 4);
				int num7 = Inlines.silk_SMLAWB(nSQ_del_dec_struct.sLPC_Q14[num6], array2[0], warping_Q16);
				int num8 = Inlines.silk_SMLAWB(array2[0], array2[1] - num7, warping_Q16);
				array2[0] = num7;
				int a4 = Inlines.silk_RSHIFT(shapingLPCOrder, 1);
				a4 = Inlines.silk_SMLAWB(a4, num7, AR_shp_Q13[AR_shp_Q13_ptr]);
				for (int l = 2; l < shapingLPCOrder; l += 2)
				{
					num7 = Inlines.silk_SMLAWB(array2[l - 1], array2[l] - num8, warping_Q16);
					array2[l - 1] = num8;
					a4 = Inlines.silk_SMLAWB(a4, num8, AR_shp_Q13[AR_shp_Q13_ptr + l - 1]);
					num8 = Inlines.silk_SMLAWB(array2[l], array2[l + 1] - num7, warping_Q16);
					array2[l] = num7;
					a4 = Inlines.silk_SMLAWB(a4, num7, AR_shp_Q13[AR_shp_Q13_ptr + l]);
				}
				array2[shapingLPCOrder - 1] = num8;
				a4 = Inlines.silk_SMLAWB(a4, num8, AR_shp_Q13[AR_shp_Q13_ptr + shapingLPCOrder - 1]);
				a4 = Inlines.silk_LSHIFT(a4, 1);
				a4 = Inlines.silk_SMLAWB(a4, nSQ_del_dec_struct.LF_AR_Q14, Tilt_Q14);
				a4 = Inlines.silk_LSHIFT(a4, 2);
				int a5 = Inlines.silk_SMULWB(nSQ_del_dec_struct.Shape_Q14[smpl_buf_idx.Val], LF_shp_Q14);
				a5 = Inlines.silk_SMLAWT(a5, nSQ_del_dec_struct.LF_AR_Q14, LF_shp_Q14);
				a5 = Inlines.silk_LSHIFT(a5, 2);
				num8 = Inlines.silk_ADD32(a4, a5);
				num7 = Inlines.silk_ADD32(a2, a3);
				num8 = Inlines.silk_SUB32(num7, num8);
				num8 = Inlines.silk_RSHIFT_ROUND(num8, 4);
				int num9 = Inlines.silk_SUB32(x_Q10[j], num8);
				if (nSQ_del_dec_struct.Seed < 0)
				{
					num9 = -num9;
				}
				num9 = Inlines.silk_LIMIT_32(num9, -31744, 30720);
				int a6 = Inlines.silk_SUB32(num9, offset_Q10);
				int num10 = Inlines.silk_RSHIFT(a6, 10);
				int num11;
				int a7;
				int a8;
				if (num10 > 0)
				{
					a6 = Inlines.silk_SUB32(Inlines.silk_LSHIFT(num10, 10), 80);
					a6 = Inlines.silk_ADD32(a6, offset_Q10);
					num11 = Inlines.silk_ADD32(a6, 1024);
					a7 = Inlines.silk_SMULBB(a6, Lambda_Q10);
					a8 = Inlines.silk_SMULBB(num11, Lambda_Q10);
				}
				else
				{
					switch (num10)
					{
					case 0:
						a6 = offset_Q10;
						num11 = Inlines.silk_ADD32(a6, 944);
						a7 = Inlines.silk_SMULBB(a6, Lambda_Q10);
						a8 = Inlines.silk_SMULBB(num11, Lambda_Q10);
						break;
					case -1:
						num11 = offset_Q10;
						a6 = Inlines.silk_SUB32(num11, 944);
						a7 = Inlines.silk_SMULBB(-a6, Lambda_Q10);
						a8 = Inlines.silk_SMULBB(num11, Lambda_Q10);
						break;
					default:
						a6 = Inlines.silk_ADD32(Inlines.silk_LSHIFT(num10, 10), 80);
						a6 = Inlines.silk_ADD32(a6, offset_Q10);
						num11 = Inlines.silk_ADD32(a6, 1024);
						a7 = Inlines.silk_SMULBB(-a6, Lambda_Q10);
						a8 = Inlines.silk_SMULBB(-num11, Lambda_Q10);
						break;
					}
				}
				int num12 = Inlines.silk_SUB32(num9, a6);
				a7 = Inlines.silk_RSHIFT(Inlines.silk_SMLABB(a7, num12, num12), 10);
				num12 = Inlines.silk_SUB32(num9, num11);
				a8 = Inlines.silk_RSHIFT(Inlines.silk_SMLABB(a8, num12, num12), 10);
				if (a7 < a8)
				{
					array[num4].RD_Q10 = Inlines.silk_ADD32(nSQ_del_dec_struct.RD_Q10, a7);
					array[num5].RD_Q10 = Inlines.silk_ADD32(nSQ_del_dec_struct.RD_Q10, a8);
					array[num4].Q_Q10 = a6;
					array[num5].Q_Q10 = num11;
				}
				else
				{
					array[num4].RD_Q10 = Inlines.silk_ADD32(nSQ_del_dec_struct.RD_Q10, a8);
					array[num5].RD_Q10 = Inlines.silk_ADD32(nSQ_del_dec_struct.RD_Q10, a7);
					array[num4].Q_Q10 = num11;
					array[num5].Q_Q10 = a6;
				}
				int num13 = Inlines.silk_LSHIFT32(array[num4].Q_Q10, 4);
				if (nSQ_del_dec_struct.Seed < 0)
				{
					num13 = -num13;
				}
				int num14 = Inlines.silk_ADD32(num13, a);
				int num15 = Inlines.silk_ADD32(num14, a3);
				int num16 = Inlines.silk_SUB32(num15, a4);
				array[num4].sLTP_shp_Q14 = Inlines.silk_SUB32(num16, a5);
				array[num4].LF_AR_Q14 = num16;
				array[num4].LPC_exc_Q14 = num14;
				array[num4].xq_Q14 = num15;
				num13 = Inlines.silk_LSHIFT32(array[num5].Q_Q10, 4);
				if (nSQ_del_dec_struct.Seed < 0)
				{
					num13 = -num13;
				}
				num14 = Inlines.silk_ADD32(num13, a);
				num15 = Inlines.silk_ADD32(num14, a3);
				num16 = Inlines.silk_SUB32(num15, a4);
				array[num5].sLTP_shp_Q14 = Inlines.silk_SUB32(num16, a5);
				array[num5].LF_AR_Q14 = num16;
				array[num5].LPC_exc_Q14 = num14;
				array[num5].xq_Q14 = num15;
			}
			smpl_buf_idx.Val = (smpl_buf_idx.Val - 1) & 0x1F;
			int num17 = (smpl_buf_idx.Val + decisionDelay) & 0x1F;
			int rD_Q = array[0].RD_Q10;
			int num18 = 0;
			for (int k = 1; k < nStatesDelayedDecision; k++)
			{
				if (array[k * 2].RD_Q10 < rD_Q)
				{
					rD_Q = array[k * 2].RD_Q10;
					num18 = k;
				}
			}
			int num19 = psDelDec[num18].RandState[num17];
			for (int k = 0; k < nStatesDelayedDecision; k++)
			{
				if (psDelDec[k].RandState[num17] != num19)
				{
					int num20 = k * 2;
					array[num20].RD_Q10 = Inlines.silk_ADD32(array[num20].RD_Q10, 134217727);
					array[num20 + 1].RD_Q10 = Inlines.silk_ADD32(array[num20 + 1].RD_Q10, 134217727);
				}
			}
			int rD_Q2 = array[0].RD_Q10;
			rD_Q = array[1].RD_Q10;
			int num21 = 0;
			int num22 = 0;
			for (int k = 1; k < nStatesDelayedDecision; k++)
			{
				int num23 = k * 2;
				if (array[num23].RD_Q10 > rD_Q2)
				{
					rD_Q2 = array[num23].RD_Q10;
					num21 = k;
				}
				if (array[num23 + 1].RD_Q10 < rD_Q)
				{
					rD_Q = array[num23 + 1].RD_Q10;
					num22 = k;
				}
			}
			if (rD_Q < rD_Q2)
			{
				psDelDec[num21].PartialCopyFrom(psDelDec[num22], j);
				array[num21 * 2] = array[num22 * 2 + 1];
			}
			nSQ_del_dec_struct = psDelDec[num18];
			if (subfr > 0 || j >= decisionDelay)
			{
				pulses[pulses_ptr + j - decisionDelay] = (sbyte)Inlines.silk_RSHIFT_ROUND(nSQ_del_dec_struct.Q_Q10[num17], 10);
				xq[xq_ptr + j - decisionDelay] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(Inlines.silk_SMULWW(nSQ_del_dec_struct.Xq_Q14[num17], delayedGain_Q10[num17]), 8));
				sLTP_shp_Q14[sLTP_shp_buf_idx - decisionDelay] = nSQ_del_dec_struct.Shape_Q14[num17];
				sLTP_Q15[sLTP_buf_idx - decisionDelay] = nSQ_del_dec_struct.Pred_Q15[num17];
			}
			sLTP_shp_buf_idx++;
			sLTP_buf_idx++;
			for (int k = 0; k < nStatesDelayedDecision; k++)
			{
				nSQ_del_dec_struct = psDelDec[k];
				int num4 = k * 2;
				nSQ_del_dec_struct.LF_AR_Q14 = array[num4].LF_AR_Q14;
				nSQ_del_dec_struct.sLPC_Q14[SilkConstants.NSQ_LPC_BUF_LENGTH + j] = array[num4].xq_Q14;
				nSQ_del_dec_struct.Xq_Q14[smpl_buf_idx.Val] = array[num4].xq_Q14;
				nSQ_del_dec_struct.Q_Q10[smpl_buf_idx.Val] = array[num4].Q_Q10;
				nSQ_del_dec_struct.Pred_Q15[smpl_buf_idx.Val] = Inlines.silk_LSHIFT32(array[num4].LPC_exc_Q14, 1);
				nSQ_del_dec_struct.Shape_Q14[smpl_buf_idx.Val] = array[num4].sLTP_shp_Q14;
				nSQ_del_dec_struct.Seed = Inlines.silk_ADD32_ovflw(nSQ_del_dec_struct.Seed, Inlines.silk_RSHIFT_ROUND(array[num4].Q_Q10, 10));
				nSQ_del_dec_struct.RandState[smpl_buf_idx.Val] = nSQ_del_dec_struct.Seed;
				nSQ_del_dec_struct.RD_Q10 = array[num4].RD_Q10;
			}
			delayedGain_Q10[smpl_buf_idx.Val] = num3;
		}
		for (int k = 0; k < nStatesDelayedDecision; k++)
		{
			NSQ_del_dec_struct nSQ_del_dec_struct = psDelDec[k];
			Buffer.BlockCopy(nSQ_del_dec_struct.sLPC_Q14, length * 4, nSQ_del_dec_struct.sLPC_Q14, 0, SilkConstants.NSQ_LPC_BUF_LENGTH * 4);
		}
	}

	private void silk_nsq_del_dec_scale_states(SilkChannelEncoder psEncC, NSQ_del_dec_struct[] psDelDec, int[] x_Q3, int x_Q3_ptr, int[] x_sc_Q10, short[] sLTP, int[] sLTP_Q15, int subfr, int nStatesDelayedDecision, int LTP_scale_Q14, int[] Gains_Q16, int[] pitchL, int signal_type, int decisionDelay)
	{
		int num = pitchL[subfr];
		int num2 = Inlines.silk_INVERSE32_varQ(Inlines.silk_max(Gains_Q16[subfr], 1), 47);
		int num3 = ((Gains_Q16[subfr] == prev_gain_Q16) ? 65536 : Inlines.silk_DIV32_varQ(prev_gain_Q16, Gains_Q16[subfr], 16));
		int b = Inlines.silk_RSHIFT_ROUND(num2, 8);
		for (int i = 0; i < psEncC.subfr_length; i++)
		{
			x_sc_Q10[i] = Inlines.silk_SMULWW(x_Q3[x_Q3_ptr + i], b);
		}
		prev_gain_Q16 = Gains_Q16[subfr];
		if (rewhite_flag != 0)
		{
			if (subfr == 0)
			{
				num2 = Inlines.silk_LSHIFT(Inlines.silk_SMULWB(num2, LTP_scale_Q14), 2);
			}
			for (int i = sLTP_buf_idx - num - 2; i < sLTP_buf_idx; i++)
			{
				sLTP_Q15[i] = Inlines.silk_SMULWB(num2, sLTP[i]);
			}
		}
		if (num3 == 65536)
		{
			return;
		}
		for (int i = sLTP_shp_buf_idx - psEncC.ltp_mem_length; i < sLTP_shp_buf_idx; i++)
		{
			sLTP_shp_Q14[i] = Inlines.silk_SMULWW(num3, sLTP_shp_Q14[i]);
		}
		if (signal_type == 2 && rewhite_flag == 0)
		{
			for (int i = sLTP_buf_idx - num - 2; i < sLTP_buf_idx - decisionDelay; i++)
			{
				sLTP_Q15[i] = Inlines.silk_SMULWW(num3, sLTP_Q15[i]);
			}
		}
		for (int j = 0; j < nStatesDelayedDecision; j++)
		{
			NSQ_del_dec_struct nSQ_del_dec_struct = psDelDec[j];
			nSQ_del_dec_struct.LF_AR_Q14 = Inlines.silk_SMULWW(num3, nSQ_del_dec_struct.LF_AR_Q14);
			for (int i = 0; i < SilkConstants.NSQ_LPC_BUF_LENGTH; i++)
			{
				nSQ_del_dec_struct.sLPC_Q14[i] = Inlines.silk_SMULWW(num3, nSQ_del_dec_struct.sLPC_Q14[i]);
			}
			for (int i = 0; i < psEncC.shapingLPCOrder; i++)
			{
				nSQ_del_dec_struct.sAR2_Q14[i] = Inlines.silk_SMULWW(num3, nSQ_del_dec_struct.sAR2_Q14[i]);
			}
			for (int i = 0; i < 32; i++)
			{
				nSQ_del_dec_struct.Pred_Q15[i] = Inlines.silk_SMULWW(num3, nSQ_del_dec_struct.Pred_Q15[i]);
				nSQ_del_dec_struct.Shape_Q14[i] = Inlines.silk_SMULWW(num3, nSQ_del_dec_struct.Shape_Q14[i]);
			}
		}
	}
}
