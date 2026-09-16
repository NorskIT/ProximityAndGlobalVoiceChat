using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class NoiseShapeAnalysis
{
	internal static int warped_gain(int[] coefs_Q24, int lambda_Q16, int order)
	{
		lambda_Q16 = -lambda_Q16;
		int b = coefs_Q24[order - 1];
		for (int num = order - 2; num >= 0; num--)
		{
			b = Inlines.silk_SMLAWB(coefs_Q24[num], b, lambda_Q16);
		}
		b = Inlines.silk_SMLAWB(16777216, b, -lambda_Q16);
		return Inlines.silk_INVERSE32_varQ(b, 40);
	}

	internal static void limit_warped_coefs(int[] coefs_syn_Q24, int[] coefs_ana_Q24, int lambda_Q16, int limit_Q24, int order)
	{
		int num = 0;
		lambda_Q16 = -lambda_Q16;
		for (int num2 = order - 1; num2 > 0; num2--)
		{
			coefs_syn_Q24[num2 - 1] = Inlines.silk_SMLAWB(coefs_syn_Q24[num2 - 1], coefs_syn_Q24[num2], lambda_Q16);
			coefs_ana_Q24[num2 - 1] = Inlines.silk_SMLAWB(coefs_ana_Q24[num2 - 1], coefs_ana_Q24[num2], lambda_Q16);
		}
		lambda_Q16 = -lambda_Q16;
		int a = Inlines.silk_SMLAWB(65536, -lambda_Q16, lambda_Q16);
		int b = Inlines.silk_SMLAWB(16777216, coefs_syn_Q24[0], lambda_Q16);
		int num3 = Inlines.silk_DIV32_varQ(a, b, 24);
		b = Inlines.silk_SMLAWB(16777216, coefs_ana_Q24[0], lambda_Q16);
		int num4 = Inlines.silk_DIV32_varQ(a, b, 24);
		for (int num2 = 0; num2 < order; num2++)
		{
			coefs_syn_Q24[num2] = Inlines.silk_SMULWW(num3, coefs_syn_Q24[num2]);
			coefs_ana_Q24[num2] = Inlines.silk_SMULWW(num4, coefs_ana_Q24[num2]);
		}
		for (int i = 0; i < 10; i++)
		{
			int num5 = -1;
			for (int num2 = 0; num2 < order; num2++)
			{
				int num6 = Inlines.silk_max(Inlines.silk_abs_int32(coefs_syn_Q24[num2]), Inlines.silk_abs_int32(coefs_ana_Q24[num2]));
				if (num6 > num5)
				{
					num5 = num6;
					num = num2;
				}
			}
			if (num5 <= limit_Q24)
			{
				break;
			}
			for (int num2 = 1; num2 < order; num2++)
			{
				coefs_syn_Q24[num2 - 1] = Inlines.silk_SMLAWB(coefs_syn_Q24[num2 - 1], coefs_syn_Q24[num2], lambda_Q16);
				coefs_ana_Q24[num2 - 1] = Inlines.silk_SMLAWB(coefs_ana_Q24[num2 - 1], coefs_ana_Q24[num2], lambda_Q16);
			}
			num3 = Inlines.silk_INVERSE32_varQ(num3, 32);
			num4 = Inlines.silk_INVERSE32_varQ(num4, 32);
			for (int num2 = 0; num2 < order; num2++)
			{
				coefs_syn_Q24[num2] = Inlines.silk_SMULWW(num3, coefs_syn_Q24[num2]);
				coefs_ana_Q24[num2] = Inlines.silk_SMULWW(num4, coefs_ana_Q24[num2]);
			}
			int chirp_Q = 64881 - Inlines.silk_DIV32_varQ(Inlines.silk_SMULWB(num5 - limit_Q24, Inlines.silk_SMLABB(819, 102, i)), Inlines.silk_MUL(num5, num + 1), 22);
			BWExpander.silk_bwexpander_32(coefs_syn_Q24, order, chirp_Q);
			BWExpander.silk_bwexpander_32(coefs_ana_Q24, order, chirp_Q);
			lambda_Q16 = -lambda_Q16;
			for (int num2 = order - 1; num2 > 0; num2--)
			{
				coefs_syn_Q24[num2 - 1] = Inlines.silk_SMLAWB(coefs_syn_Q24[num2 - 1], coefs_syn_Q24[num2], lambda_Q16);
				coefs_ana_Q24[num2 - 1] = Inlines.silk_SMLAWB(coefs_ana_Q24[num2 - 1], coefs_ana_Q24[num2], lambda_Q16);
			}
			lambda_Q16 = -lambda_Q16;
			int a2 = Inlines.silk_SMLAWB(65536, -lambda_Q16, lambda_Q16);
			b = Inlines.silk_SMLAWB(16777216, coefs_syn_Q24[0], lambda_Q16);
			num3 = Inlines.silk_DIV32_varQ(a2, b, 24);
			b = Inlines.silk_SMLAWB(16777216, coefs_ana_Q24[0], lambda_Q16);
			num4 = Inlines.silk_DIV32_varQ(a2, b, 24);
			for (int num2 = 0; num2 < order; num2++)
			{
				coefs_syn_Q24[num2] = Inlines.silk_SMULWW(num3, coefs_syn_Q24[num2]);
				coefs_ana_Q24[num2] = Inlines.silk_SMULWW(num4, coefs_ana_Q24[num2]);
			}
		}
	}

	internal static void silk_noise_shape_analysis(SilkChannelEncoder psEnc, SilkEncoderControl psEncCtrl, short[] pitch_res, int pitch_res_ptr, short[] x, int x_ptr)
	{
		SilkShapeState sShape = psEnc.sShape;
		int shift = 0;
		int[] array = new int[17];
		int[] rc_Q = new int[16];
		int[] array2 = new int[16];
		int[] array3 = new int[16];
		int num = x_ptr - psEnc.la_shape;
		int num2 = psEnc.SNR_dB_Q7;
		psEncCtrl.input_quality_Q14 = Inlines.silk_RSHIFT(psEnc.input_quality_bands_Q15[0] + psEnc.input_quality_bands_Q15[1], 2);
		psEncCtrl.coding_quality_Q14 = Inlines.silk_RSHIFT(Sigmoid.silk_sigm_Q15(Inlines.silk_RSHIFT_ROUND(num2 - 2560, 4)), 1);
		if (psEnc.useCBR == 0)
		{
			int num3 = 256 - psEnc.speech_activity_Q8;
			num3 = Inlines.silk_SMULWB(Inlines.silk_LSHIFT(num3, 8), num3);
			num2 = Inlines.silk_SMLAWB(num2, Inlines.silk_SMULBB(-8, num3), Inlines.silk_SMULWB(16384 + psEncCtrl.input_quality_Q14, psEncCtrl.coding_quality_Q14));
		}
		num2 = ((psEnc.indices.signalType != 2) ? Inlines.silk_SMLAWB(num2, Inlines.silk_SMLAWB(3072, -104858, psEnc.SNR_dB_Q7), 16384 - psEncCtrl.input_quality_Q14) : Inlines.silk_SMLAWB(num2, 512, psEnc.LTPCorr_Q15));
		if (psEnc.indices.signalType == 2)
		{
			psEnc.indices.quantOffsetType = 0;
			psEncCtrl.sparseness_Q8 = 0;
		}
		else
		{
			int num4 = Inlines.silk_LSHIFT(psEnc.fs_kHz, 1);
			int num5 = 0;
			int num6 = 0;
			int num7 = pitch_res_ptr;
			for (int i = 0; i < Inlines.silk_SMULBB(5, psEnc.nb_subfr) / 2; i++)
			{
				SumSqrShift.silk_sum_sqr_shift(out var energy, out shift, pitch_res, num7, num4);
				energy += Inlines.silk_RSHIFT(num4, shift);
				int num8 = Inlines.silk_lin2log(energy);
				if (i > 0)
				{
					num5 += Inlines.silk_abs(num8 - num6);
				}
				num6 = num8;
				num7 += num4;
			}
			psEncCtrl.sparseness_Q8 = Inlines.silk_RSHIFT(Sigmoid.silk_sigm_Q15(Inlines.silk_SMULWB(num5 - 640, 6554)), 7);
			if (psEncCtrl.sparseness_Q8 > 192)
			{
				psEnc.indices.quantOffsetType = 0;
			}
			else
			{
				psEnc.indices.quantOffsetType = 1;
			}
			num2 = Inlines.silk_SMLAWB(num2, 65536, psEncCtrl.sparseness_Q8 - 128);
		}
		int num9 = Inlines.silk_SMULWB(psEncCtrl.predGain_Q16, 66);
		int a2;
		int a = (a2 = Inlines.silk_DIV32_varQ(62259, Inlines.silk_SMLAWW(65536, num9, num9), 16));
		int b = Inlines.silk_SMULWB(65536 - Inlines.silk_SMULBB(3, psEncCtrl.coding_quality_Q14), 655);
		a = Inlines.silk_SUB32(a, b);
		a2 = Inlines.silk_ADD32(a2, b);
		a = Inlines.silk_DIV32_16(Inlines.silk_LSHIFT(a, 14), Inlines.silk_RSHIFT(a2, 2));
		int num10 = ((psEnc.warping_Q16 > 0) ? Inlines.silk_SMLAWB(psEnc.warping_Q16, psEncCtrl.coding_quality_Q14, 2621) : 0);
		short[] array4 = new short[psEnc.shapeWinLength];
		int b2;
		for (int i = 0; i < psEnc.nb_subfr; i++)
		{
			int num11 = psEnc.fs_kHz * 3;
			int num12 = Inlines.silk_RSHIFT(psEnc.shapeWinLength - num11, 1);
			ApplySineWindow.silk_apply_sine_window(array4, 0, x, num, 1, num12);
			int num13 = num12;
			Array.Copy(x, num + num13, array4, num13, num11);
			num13 += num11;
			ApplySineWindow.silk_apply_sine_window(array4, num13, x, num + num13, 2, num12);
			num += psEnc.subfr_length;
			BoxedValueInt boxedValueInt = new BoxedValueInt(shift);
			if (psEnc.warping_Q16 > 0)
			{
				Autocorrelation.silk_warped_autocorrelation(array, boxedValueInt, array4, num10, psEnc.shapeWinLength, psEnc.shapingLPCOrder);
			}
			else
			{
				Autocorrelation.silk_autocorr(array, boxedValueInt, array4, psEnc.shapeWinLength, psEnc.shapingLPCOrder + 1);
			}
			shift = boxedValueInt.Val;
			array[0] = Inlines.silk_ADD32(array[0], Inlines.silk_max_32(Inlines.silk_SMULWB(Inlines.silk_RSHIFT(array[0], 4), 52), 1));
			int energy = Schur.silk_schur64(rc_Q, array, psEnc.shapingLPCOrder);
			K2A.silk_k2a_Q16(array3, rc_Q, psEnc.shapingLPCOrder);
			int num14 = -shift;
			if ((num14 & 1) != 0)
			{
				num14--;
				energy >>= 1;
			}
			int a3 = Inlines.silk_SQRT_APPROX(energy);
			num14 >>= 1;
			psEncCtrl.Gains_Q16[i] = Inlines.silk_LSHIFT_SAT32(a3, 16 - num14);
			if (psEnc.warping_Q16 > 0)
			{
				b2 = warped_gain(array3, num10, psEnc.shapingLPCOrder);
				if (Inlines.silk_SMULWW(Inlines.silk_RSHIFT_ROUND(psEncCtrl.Gains_Q16[i], 1), b2) >= 1073741823)
				{
					psEncCtrl.Gains_Q16[i] = int.MaxValue;
				}
				else
				{
					psEncCtrl.Gains_Q16[i] = Inlines.silk_SMULWW(psEncCtrl.Gains_Q16[i], b2);
				}
			}
			BWExpander.silk_bwexpander_32(array3, psEnc.shapingLPCOrder, a2);
			Array.Copy(array3, array2, psEnc.shapingLPCOrder);
			BWExpander.silk_bwexpander_32(array2, psEnc.shapingLPCOrder, a);
			int a4 = LPCInversePredGain.silk_LPC_inverse_pred_gain_Q24(array3, psEnc.shapingLPCOrder);
			energy = LPCInversePredGain.silk_LPC_inverse_pred_gain_Q24(array2, psEnc.shapingLPCOrder);
			a4 = Inlines.silk_LSHIFT32(Inlines.silk_SMULWB(a4, 22938), 1);
			psEncCtrl.GainsPre_Q14[i] = 4915 + Inlines.silk_DIV32_varQ(a4, energy, 14);
			limit_warped_coefs(array3, array2, num10, 67092088, psEnc.shapingLPCOrder);
			for (int j = 0; j < psEnc.shapingLPCOrder; j++)
			{
				psEncCtrl.AR1_Q13[i * 16 + j] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(array2[j], 11));
				psEncCtrl.AR2_Q13[i * 16 + j] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(array3[j], 11));
			}
		}
		b2 = Inlines.silk_log2lin(-Inlines.silk_SMLAWB(-2048, num2, 10486));
		int b3 = Inlines.silk_log2lin(Inlines.silk_SMLAWB(2048, 256, 10486));
		for (int i = 0; i < psEnc.nb_subfr; i++)
		{
			psEncCtrl.Gains_Q16[i] = Inlines.silk_SMULWW(psEncCtrl.Gains_Q16[i], b2);
			psEncCtrl.Gains_Q16[i] = Inlines.silk_ADD_POS_SAT32(psEncCtrl.Gains_Q16[i], b3);
		}
		b2 = 65536 + Inlines.silk_RSHIFT_ROUND(Inlines.silk_MLA(3355443, psEncCtrl.coding_quality_Q14, 410), 10);
		for (int i = 0; i < psEnc.nb_subfr; i++)
		{
			psEncCtrl.GainsPre_Q14[i] = Inlines.silk_SMULWB(b2, psEncCtrl.GainsPre_Q14[i]);
		}
		num9 = Inlines.silk_MUL(64, Inlines.silk_SMLAWB(4096, 4096, psEnc.input_quality_bands_Q15[0] - 32768));
		num9 = Inlines.silk_RSHIFT(Inlines.silk_MUL(num9, psEnc.speech_activity_Q8), 8);
		int num17;
		if (psEnc.indices.signalType == 2)
		{
			int num15 = Inlines.silk_DIV32_16(3277, psEnc.fs_kHz);
			for (int i = 0; i < psEnc.nb_subfr; i++)
			{
				int num16 = num15 + Inlines.silk_DIV32_16(49152, psEncCtrl.pitchL[i]);
				psEncCtrl.LF_shp_Q14[i] = Inlines.silk_LSHIFT(16384 - num16 - Inlines.silk_SMULWB(num9, num16), 16);
				psEncCtrl.LF_shp_Q14[i] |= (num16 - 16384) & 0xFFFF;
			}
			num17 = -16384 - Inlines.silk_SMULWB(49152, Inlines.silk_SMULWB(5872026, psEnc.speech_activity_Q8));
		}
		else
		{
			int num16 = Inlines.silk_DIV32_16(21299, psEnc.fs_kHz);
			psEncCtrl.LF_shp_Q14[0] = Inlines.silk_LSHIFT(16384 - num16 - Inlines.silk_SMULWB(num9, Inlines.silk_SMULWB(39322, num16)), 16);
			psEncCtrl.LF_shp_Q14[0] |= (num16 - 16384) & 0xFFFF;
			for (int i = 1; i < psEnc.nb_subfr; i++)
			{
				psEncCtrl.LF_shp_Q14[i] = psEncCtrl.LF_shp_Q14[0];
			}
			num17 = -16384;
		}
		int a5 = Inlines.silk_SMULWB(Inlines.silk_SMULWB(131072 - Inlines.silk_LSHIFT(psEncCtrl.coding_quality_Q14, 3), psEnc.LTPCorr_Q15), 6554);
		a5 = Inlines.silk_SMLAWB(a5, 65536 - Inlines.silk_LSHIFT(psEncCtrl.input_quality_Q14, 2), 6554);
		int a6;
		if (psEnc.indices.signalType == 2)
		{
			a6 = Inlines.silk_SMLAWB(19661, 65536 - Inlines.silk_SMULWB(262144 - Inlines.silk_LSHIFT(psEncCtrl.coding_quality_Q14, 4), psEncCtrl.input_quality_Q14), 13107);
			a6 = Inlines.silk_SMULWB(Inlines.silk_LSHIFT(a6, 1), Inlines.silk_SQRT_APPROX(Inlines.silk_LSHIFT(psEnc.LTPCorr_Q15, 15)));
		}
		else
		{
			a6 = 0;
		}
		for (int i = 0; i < 4; i++)
		{
			sShape.HarmBoost_smth_Q16 = Inlines.silk_SMLAWB(sShape.HarmBoost_smth_Q16, a5 - sShape.HarmBoost_smth_Q16, 26214);
			sShape.HarmShapeGain_smth_Q16 = Inlines.silk_SMLAWB(sShape.HarmShapeGain_smth_Q16, a6 - sShape.HarmShapeGain_smth_Q16, 26214);
			sShape.Tilt_smth_Q16 = Inlines.silk_SMLAWB(sShape.Tilt_smth_Q16, num17 - sShape.Tilt_smth_Q16, 26214);
			psEncCtrl.HarmBoost_Q14[i] = Inlines.silk_RSHIFT_ROUND(sShape.HarmBoost_smth_Q16, 2);
			psEncCtrl.HarmShapeGain_Q14[i] = Inlines.silk_RSHIFT_ROUND(sShape.HarmShapeGain_smth_Q16, 2);
			psEncCtrl.Tilt_Q14[i] = Inlines.silk_RSHIFT_ROUND(sShape.Tilt_smth_Q16, 2);
		}
	}
}
