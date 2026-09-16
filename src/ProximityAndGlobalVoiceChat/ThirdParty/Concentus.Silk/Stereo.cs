using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class Stereo
{
	internal static void silk_stereo_decode_pred(EntropyCoder psRangeDec, int[] pred_Q13)
	{
		int[][] array = Arrays.InitTwoDimensionalArray<int>(2, 3);
		int num = psRangeDec.dec_icdf(Tables.silk_stereo_pred_joint_iCDF, 8u);
		array[0][2] = Inlines.silk_DIV32_16(num, 5);
		array[1][2] = num - 5 * array[0][2];
		for (num = 0; num < 2; num++)
		{
			array[num][0] = psRangeDec.dec_icdf(Tables.silk_uniform3_iCDF, 8u);
			array[num][1] = psRangeDec.dec_icdf(Tables.silk_uniform5_iCDF, 8u);
		}
		for (num = 0; num < 2; num++)
		{
			array[num][0] += 3 * array[num][2];
			int num2 = Tables.silk_stereo_pred_quant_Q13[array[num][0]];
			int b = Inlines.silk_SMULWB(Tables.silk_stereo_pred_quant_Q13[array[num][0] + 1] - num2, 6554);
			pred_Q13[num] = Inlines.silk_SMLABB(num2, b, 2 * array[num][1] + 1);
		}
		pred_Q13[0] -= pred_Q13[1];
	}

	internal static void silk_stereo_decode_mid_only(EntropyCoder psRangeDec, BoxedValueInt decode_only_mid)
	{
		decode_only_mid.Val = psRangeDec.dec_icdf(Tables.silk_stereo_only_code_mid_iCDF, 8u);
	}

	internal static void silk_stereo_encode_pred(EntropyCoder psRangeEnc, sbyte[][] ix)
	{
		int s = 5 * ix[0][2] + ix[1][2];
		psRangeEnc.enc_icdf(s, Tables.silk_stereo_pred_joint_iCDF, 8u);
		for (s = 0; s < 2; s++)
		{
			psRangeEnc.enc_icdf(ix[s][0], Tables.silk_uniform3_iCDF, 8u);
			psRangeEnc.enc_icdf(ix[s][1], Tables.silk_uniform5_iCDF, 8u);
		}
	}

	internal static void silk_stereo_encode_mid_only(EntropyCoder psRangeEnc, sbyte mid_only_flag)
	{
		psRangeEnc.enc_icdf(mid_only_flag, Tables.silk_stereo_only_code_mid_iCDF, 8u);
	}

	internal static int silk_stereo_find_predictor(BoxedValueInt ratio_Q14, short[] x, short[] y, int[] mid_res_amp_Q0, int mid_res_amp_Q0_ptr, int length, int smooth_coef_Q16)
	{
		SumSqrShift.silk_sum_sqr_shift(out var energy, out var shift, x, length);
		SumSqrShift.silk_sum_sqr_shift(out var energy2, out var shift2, y, length);
		int num = Inlines.silk_max_int(shift, shift2);
		num += num & 1;
		energy2 = Inlines.silk_RSHIFT32(energy2, num - shift2);
		energy = Inlines.silk_RSHIFT32(energy, num - shift);
		energy = Inlines.silk_max_int(energy, 1);
		int a = Inlines.silk_inner_prod_aligned_scale(x, y, num, length);
		int a2 = Inlines.silk_DIV32_varQ(a, energy, 13);
		a2 = Inlines.silk_LIMIT(a2, -16384, 16384);
		int num2 = Inlines.silk_SMULWB(a2, a2);
		smooth_coef_Q16 = Inlines.silk_max_int(smooth_coef_Q16, Inlines.silk_abs(num2));
		num = Inlines.silk_RSHIFT(num, 1);
		mid_res_amp_Q0[mid_res_amp_Q0_ptr] = Inlines.silk_SMLAWB(mid_res_amp_Q0[mid_res_amp_Q0_ptr], Inlines.silk_LSHIFT(Inlines.silk_SQRT_APPROX(energy), num) - mid_res_amp_Q0[mid_res_amp_Q0_ptr], smooth_coef_Q16);
		energy2 = Inlines.silk_SUB_LSHIFT32(energy2, Inlines.silk_SMULWB(a, a2), 4);
		energy2 = Inlines.silk_ADD_LSHIFT32(energy2, Inlines.silk_SMULWB(energy, num2), 6);
		mid_res_amp_Q0[mid_res_amp_Q0_ptr + 1] = Inlines.silk_SMLAWB(mid_res_amp_Q0[mid_res_amp_Q0_ptr + 1], Inlines.silk_LSHIFT(Inlines.silk_SQRT_APPROX(energy2), num) - mid_res_amp_Q0[mid_res_amp_Q0_ptr + 1], smooth_coef_Q16);
		ratio_Q14.Val = Inlines.silk_DIV32_varQ(mid_res_amp_Q0[mid_res_amp_Q0_ptr + 1], Inlines.silk_max(mid_res_amp_Q0[mid_res_amp_Q0_ptr], 1), 14);
		ratio_Q14.Val = Inlines.silk_LIMIT(ratio_Q14.Val, 0, 32767);
		return a2;
	}

	internal static void silk_stereo_LR_to_MS(StereoEncodeState state, short[] x1, int x1_ptr, short[] x2, int x2_ptr, sbyte[][] ix, BoxedValueSbyte mid_only_flag, int[] mid_side_rates_bps, int total_rate_bps, int prev_speech_act_Q8, int toMono, int fs_kHz, int frame_length)
	{
		int[] array = new int[2];
		BoxedValueInt boxedValueInt = new BoxedValueInt();
		BoxedValueInt boxedValueInt2 = new BoxedValueInt();
		int num = x1_ptr - 2;
		short[] array2 = new short[frame_length + 2];
		for (int i = 0; i < frame_length + 2; i++)
		{
			int a = x1[x1_ptr + i - 2] + x2[x2_ptr + i - 2];
			int a2 = x1[x1_ptr + i - 2] - x2[x2_ptr + i - 2];
			x1[num + i] = (short)Inlines.silk_RSHIFT_ROUND(a, 1);
			array2[i] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a2, 1));
		}
		Array.Copy(state.sMid, 0, x1, num, 2);
		Array.Copy(state.sSide, array2, 2);
		Array.Copy(x1, num + frame_length, state.sMid, 0, 2);
		Array.Copy(array2, frame_length, state.sSide, 0, 2);
		short[] array3 = new short[frame_length];
		short[] array4 = new short[frame_length];
		for (int i = 0; i < frame_length; i++)
		{
			int a = Inlines.silk_RSHIFT_ROUND(Inlines.silk_ADD_LSHIFT32(x1[num + i] + x1[num + i + 2], x1[num + i + 1], 1), 2);
			array3[i] = (short)a;
			array4[i] = (short)(x1[num + i + 1] - a);
		}
		short[] array5 = new short[frame_length];
		short[] array6 = new short[frame_length];
		for (int i = 0; i < frame_length; i++)
		{
			int a = Inlines.silk_RSHIFT_ROUND(Inlines.silk_ADD_LSHIFT32(array2[i] + array2[i + 2], array2[i + 1], 1), 2);
			array5[i] = (short)a;
			array6[i] = (short)(array2[i + 1] - a);
		}
		int num2 = ((frame_length == 10 * fs_kHz) ? 1 : 0);
		int b = ((num2 != 0) ? 328 : 655);
		b = Inlines.silk_SMULWB(Inlines.silk_SMULBB(prev_speech_act_Q8, prev_speech_act_Q8), b);
		array[0] = silk_stereo_find_predictor(boxedValueInt, array3, array5, state.mid_side_amp_Q0, 0, frame_length, b);
		array[1] = silk_stereo_find_predictor(boxedValueInt2, array4, array6, state.mid_side_amp_Q0, 2, frame_length, b);
		int a3 = Inlines.silk_SMLABB(boxedValueInt2.Val, boxedValueInt.Val, 3);
		a3 = Inlines.silk_min(a3, 65536);
		total_rate_bps -= ((num2 != 0) ? 1200 : 600);
		if (total_rate_bps < 1)
		{
			total_rate_bps = 1;
		}
		int num3 = Inlines.silk_SMLABB(2000, fs_kHz, 900);
		int num4 = Inlines.silk_MUL(3, a3);
		mid_side_rates_bps[0] = Inlines.silk_DIV32_varQ(total_rate_bps, 851968 + num4, 19);
		int a4;
		if (mid_side_rates_bps[0] < num3)
		{
			mid_side_rates_bps[0] = num3;
			mid_side_rates_bps[1] = total_rate_bps - mid_side_rates_bps[0];
			a4 = Inlines.silk_DIV32_varQ(Inlines.silk_LSHIFT(mid_side_rates_bps[1], 1) - num3, Inlines.silk_SMULWB(65536 + num4, num3), 16);
			a4 = Inlines.silk_LIMIT(a4, 0, 16384);
		}
		else
		{
			mid_side_rates_bps[1] = total_rate_bps - mid_side_rates_bps[0];
			a4 = 16384;
		}
		state.smth_width_Q14 = (short)Inlines.silk_SMLAWB(state.smth_width_Q14, a4 - state.smth_width_Q14, b);
		mid_only_flag.Val = 0;
		if (toMono != 0)
		{
			a4 = 0;
			array[0] = 0;
			array[1] = 0;
			silk_stereo_quant_pred(array, ix);
		}
		else if (state.width_prev_Q14 == 0 && (8 * total_rate_bps < 13 * num3 || Inlines.silk_SMULWB(a3, state.smth_width_Q14) < 819))
		{
			array[0] = Inlines.silk_RSHIFT(Inlines.silk_SMULBB(state.smth_width_Q14, array[0]), 14);
			array[1] = Inlines.silk_RSHIFT(Inlines.silk_SMULBB(state.smth_width_Q14, array[1]), 14);
			silk_stereo_quant_pred(array, ix);
			a4 = 0;
			array[0] = 0;
			array[1] = 0;
			mid_side_rates_bps[0] = total_rate_bps;
			mid_side_rates_bps[1] = 0;
			mid_only_flag.Val = 1;
		}
		else if (state.width_prev_Q14 != 0 && (8 * total_rate_bps < 11 * num3 || Inlines.silk_SMULWB(a3, state.smth_width_Q14) < 328))
		{
			array[0] = Inlines.silk_RSHIFT(Inlines.silk_SMULBB(state.smth_width_Q14, array[0]), 14);
			array[1] = Inlines.silk_RSHIFT(Inlines.silk_SMULBB(state.smth_width_Q14, array[1]), 14);
			silk_stereo_quant_pred(array, ix);
			a4 = 0;
			array[0] = 0;
			array[1] = 0;
		}
		else if (state.smth_width_Q14 > 15565)
		{
			silk_stereo_quant_pred(array, ix);
			a4 = 16384;
		}
		else
		{
			array[0] = Inlines.silk_RSHIFT(Inlines.silk_SMULBB(state.smth_width_Q14, array[0]), 14);
			array[1] = Inlines.silk_RSHIFT(Inlines.silk_SMULBB(state.smth_width_Q14, array[1]), 14);
			silk_stereo_quant_pred(array, ix);
			a4 = state.smth_width_Q14;
		}
		if (mid_only_flag.Val == 1)
		{
			state.silent_side_len += (short)(frame_length - 8 * fs_kHz);
			if (state.silent_side_len < 5 * fs_kHz)
			{
				mid_only_flag.Val = 0;
			}
			else
			{
				state.silent_side_len = 10000;
			}
		}
		else
		{
			state.silent_side_len = 0;
		}
		if (mid_only_flag.Val == 0 && mid_side_rates_bps[1] < 1)
		{
			mid_side_rates_bps[1] = 1;
			mid_side_rates_bps[0] = Inlines.silk_max_int(1, total_rate_bps - mid_side_rates_bps[1]);
		}
		int num5 = -state.pred_prev_Q13[0];
		int num6 = -state.pred_prev_Q13[1];
		int num7 = Inlines.silk_LSHIFT(state.width_prev_Q14, 10);
		int b2 = Inlines.silk_DIV32_16(65536, 8 * fs_kHz);
		int num8 = -Inlines.silk_RSHIFT_ROUND(Inlines.silk_SMULBB(array[0] - state.pred_prev_Q13[0], b2), 16);
		int num9 = -Inlines.silk_RSHIFT_ROUND(Inlines.silk_SMULBB(array[1] - state.pred_prev_Q13[1], b2), 16);
		int num10 = Inlines.silk_LSHIFT(Inlines.silk_SMULWB(a4 - state.width_prev_Q14, b2), 10);
		for (int i = 0; i < 8 * fs_kHz; i++)
		{
			num5 += num8;
			num6 += num9;
			num7 += num10;
			int a = Inlines.silk_LSHIFT(Inlines.silk_ADD_LSHIFT(x1[num + i] + x1[num + i + 2], x1[num + i + 1], 1), 9);
			a = Inlines.silk_SMLAWB(Inlines.silk_SMULWB(num7, array2[i + 1]), a, num5);
			a = Inlines.silk_SMLAWB(a, Inlines.silk_LSHIFT(x1[num + i + 1], 11), num6);
			x2[x2_ptr + i - 1] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a, 8));
		}
		num5 = -array[0];
		num6 = -array[1];
		num7 = Inlines.silk_LSHIFT(a4, 10);
		for (int i = 8 * fs_kHz; i < frame_length; i++)
		{
			int a = Inlines.silk_LSHIFT(Inlines.silk_ADD_LSHIFT(x1[num + i] + x1[num + i + 2], x1[num + i + 1], 1), 9);
			a = Inlines.silk_SMLAWB(Inlines.silk_SMULWB(num7, array2[i + 1]), a, num5);
			a = Inlines.silk_SMLAWB(a, Inlines.silk_LSHIFT(x1[num + i + 1], 11), num6);
			x2[x2_ptr + i - 1] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a, 8));
		}
		state.pred_prev_Q13[0] = (short)array[0];
		state.pred_prev_Q13[1] = (short)array[1];
		state.width_prev_Q14 = (short)a4;
	}

	internal static void silk_stereo_MS_to_LR(StereoDecodeState state, short[] x1, int x1_ptr, short[] x2, int x2_ptr, int[] pred_Q13, int fs_kHz, int frame_length)
	{
		Array.Copy(state.sMid, 0, x1, x1_ptr, 2);
		Array.Copy(state.sSide, 0, x2, x2_ptr, 2);
		Array.Copy(x1, x1_ptr + frame_length, state.sMid, 0, 2);
		Array.Copy(x2, x2_ptr + frame_length, state.sSide, 0, 2);
		int num = state.pred_prev_Q13[0];
		int num2 = state.pred_prev_Q13[1];
		int b = Inlines.silk_DIV32_16(65536, 8 * fs_kHz);
		int num3 = Inlines.silk_RSHIFT_ROUND(Inlines.silk_SMULBB(pred_Q13[0] - state.pred_prev_Q13[0], b), 16);
		int num4 = Inlines.silk_RSHIFT_ROUND(Inlines.silk_SMULBB(pred_Q13[1] - state.pred_prev_Q13[1], b), 16);
		for (int i = 0; i < 8 * fs_kHz; i++)
		{
			num += num3;
			num2 += num4;
			int b2 = Inlines.silk_LSHIFT(Inlines.silk_ADD_LSHIFT(x1[x1_ptr + i] + x1[x1_ptr + i + 2], x1[x1_ptr + i + 1], 1), 9);
			b2 = Inlines.silk_SMLAWB(Inlines.silk_LSHIFT(x2[x2_ptr + i + 1], 8), b2, num);
			b2 = Inlines.silk_SMLAWB(b2, Inlines.silk_LSHIFT(x1[x1_ptr + i + 1], 11), num2);
			x2[x2_ptr + i + 1] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(b2, 8));
		}
		num = pred_Q13[0];
		num2 = pred_Q13[1];
		for (int i = 8 * fs_kHz; i < frame_length; i++)
		{
			int b2 = Inlines.silk_LSHIFT(Inlines.silk_ADD_LSHIFT(x1[x1_ptr + i] + x1[x1_ptr + i + 2], x1[x1_ptr + i + 1], 1), 9);
			b2 = Inlines.silk_SMLAWB(Inlines.silk_LSHIFT(x2[x2_ptr + i + 1], 8), b2, num);
			b2 = Inlines.silk_SMLAWB(b2, Inlines.silk_LSHIFT(x1[x1_ptr + i + 1], 11), num2);
			x2[x2_ptr + i + 1] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(b2, 8));
		}
		state.pred_prev_Q13[0] = (short)pred_Q13[0];
		state.pred_prev_Q13[1] = (short)pred_Q13[1];
		for (int i = 0; i < frame_length; i++)
		{
			int b2 = x1[x1_ptr + i + 1] + x2[x2_ptr + i + 1];
			int a = x1[x1_ptr + i + 1] - x2[x2_ptr + i + 1];
			x1[x1_ptr + i + 1] = (short)Inlines.silk_SAT16(b2);
			x2[x2_ptr + i + 1] = (short)Inlines.silk_SAT16(a);
		}
	}

	internal static void silk_stereo_quant_pred(int[] pred_Q13, sbyte[][] ix)
	{
		int num = 0;
		Arrays.MemSetSbyte(ix[0], 0, 3);
		Arrays.MemSetSbyte(ix[1], 0, 3);
		for (int i = 0; i < 2; i++)
		{
			int num2 = int.MaxValue;
			sbyte b = 0;
			while (b < 15)
			{
				int num3 = Tables.silk_stereo_pred_quant_Q13[b];
				int b2 = Inlines.silk_SMULWB(Tables.silk_stereo_pred_quant_Q13[b + 1] - num3, 6554);
				for (sbyte b3 = 0; b3 < 5; b3++)
				{
					int num4 = Inlines.silk_SMLABB(num3, b2, 2 * b3 + 1);
					int num5 = Inlines.silk_abs(pred_Q13[i] - num4);
					if (num5 >= num2)
					{
						goto end_IL_0090;
					}
					num2 = num5;
					num = num4;
					ix[i][0] = b;
					ix[i][1] = b3;
				}
				b++;
				continue;
				end_IL_0090:
				break;
			}
			ix[i][2] = (sbyte)Inlines.silk_DIV32_16(ix[i][0], 3);
			ix[i][0] = (sbyte)(ix[i][0] - (sbyte)(ix[i][2] * 3));
			pred_Q13[i] = num;
		}
		pred_Q13[0] -= pred_Q13[1];
	}
}
