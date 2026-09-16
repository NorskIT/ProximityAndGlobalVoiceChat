using System;
using Concentus.Common;
using Concentus.Silk.Enums;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class Resampler
{
	private const int USE_silk_resampler_copy = 0;

	private const int USE_silk_resampler_private_up2_HQ_wrapper = 1;

	private const int USE_silk_resampler_private_IIR_FIR = 2;

	private const int USE_silk_resampler_private_down_FIR = 3;

	private const int ORDER_FIR = 4;

	private static int rateID(int R)
	{
		return ((R >> 12) - ((R > 16000) ? 1 : 0) >> ((R > 24000) ? 1 : 0)) - 1;
	}

	internal static int silk_resampler_init(SilkResamplerState S, int Fs_Hz_in, int Fs_Hz_out, int forEnc)
	{
		S.Reset();
		if (forEnc != 0)
		{
			if ((Fs_Hz_in != 8000 && Fs_Hz_in != 12000 && Fs_Hz_in != 16000 && Fs_Hz_in != 24000 && Fs_Hz_in != 48000) || (Fs_Hz_out != 8000 && Fs_Hz_out != 12000 && Fs_Hz_out != 16000))
			{
				return -1;
			}
			S.inputDelay = Tables.delay_matrix_enc[rateID(Fs_Hz_in), rateID(Fs_Hz_out)];
		}
		else
		{
			if ((Fs_Hz_in != 8000 && Fs_Hz_in != 12000 && Fs_Hz_in != 16000) || (Fs_Hz_out != 8000 && Fs_Hz_out != 12000 && Fs_Hz_out != 16000 && Fs_Hz_out != 24000 && Fs_Hz_out != 48000))
			{
				return -1;
			}
			S.inputDelay = Tables.delay_matrix_dec[rateID(Fs_Hz_in), rateID(Fs_Hz_out)];
		}
		S.Fs_in_kHz = Inlines.silk_DIV32_16(Fs_Hz_in, 1000);
		S.Fs_out_kHz = Inlines.silk_DIV32_16(Fs_Hz_out, 1000);
		S.batchSize = S.Fs_in_kHz * 10;
		int num = 0;
		if (Fs_Hz_out > Fs_Hz_in)
		{
			if (Fs_Hz_out == Inlines.silk_MUL(Fs_Hz_in, 2))
			{
				S.resampler_function = 1;
			}
			else
			{
				S.resampler_function = 2;
				num = 1;
			}
		}
		else if (Fs_Hz_out < Fs_Hz_in)
		{
			S.resampler_function = 3;
			if (Inlines.silk_MUL(Fs_Hz_out, 4) == Inlines.silk_MUL(Fs_Hz_in, 3))
			{
				S.FIR_Fracs = 3;
				S.FIR_Order = 18;
				S.Coefs = Tables.silk_Resampler_3_4_COEFS;
			}
			else if (Inlines.silk_MUL(Fs_Hz_out, 3) == Inlines.silk_MUL(Fs_Hz_in, 2))
			{
				S.FIR_Fracs = 2;
				S.FIR_Order = 18;
				S.Coefs = Tables.silk_Resampler_2_3_COEFS;
			}
			else if (Inlines.silk_MUL(Fs_Hz_out, 2) == Fs_Hz_in)
			{
				S.FIR_Fracs = 1;
				S.FIR_Order = 24;
				S.Coefs = Tables.silk_Resampler_1_2_COEFS;
			}
			else if (Inlines.silk_MUL(Fs_Hz_out, 3) == Fs_Hz_in)
			{
				S.FIR_Fracs = 1;
				S.FIR_Order = 36;
				S.Coefs = Tables.silk_Resampler_1_3_COEFS;
			}
			else if (Inlines.silk_MUL(Fs_Hz_out, 4) == Fs_Hz_in)
			{
				S.FIR_Fracs = 1;
				S.FIR_Order = 36;
				S.Coefs = Tables.silk_Resampler_1_4_COEFS;
			}
			else
			{
				if (Inlines.silk_MUL(Fs_Hz_out, 6) != Fs_Hz_in)
				{
					return -1;
				}
				S.FIR_Fracs = 1;
				S.FIR_Order = 36;
				S.Coefs = Tables.silk_Resampler_1_6_COEFS;
			}
		}
		else
		{
			S.resampler_function = 0;
		}
		S.invRatio_Q16 = Inlines.silk_LSHIFT32(Inlines.silk_DIV32(Inlines.silk_LSHIFT32(Fs_Hz_in, 14 + num), Fs_Hz_out), 2);
		while (Inlines.silk_SMULWW(S.invRatio_Q16, Fs_Hz_out) < Inlines.silk_LSHIFT32(Fs_Hz_in, num))
		{
			S.invRatio_Q16++;
		}
		return 0;
	}

	internal static int silk_resampler(SilkResamplerState S, short[] output, int output_ptr, short[] input, int input_ptr, int inLen)
	{
		int num = S.Fs_in_kHz - S.inputDelay;
		short[] delayBuf = S.delayBuf;
		Array.Copy(input, input_ptr, delayBuf, S.inputDelay, num);
		switch (S.resampler_function)
		{
		case 1:
			silk_resampler_private_up2_HQ(S.sIIR, output, output_ptr, delayBuf, 0, S.Fs_in_kHz);
			silk_resampler_private_up2_HQ(S.sIIR, output, output_ptr + S.Fs_out_kHz, input, input_ptr + num, inLen - S.Fs_in_kHz);
			break;
		case 2:
			silk_resampler_private_IIR_FIR(S, output, output_ptr, delayBuf, 0, S.Fs_in_kHz);
			silk_resampler_private_IIR_FIR(S, output, output_ptr + S.Fs_out_kHz, input, input_ptr + num, inLen - S.Fs_in_kHz);
			break;
		case 3:
			silk_resampler_private_down_FIR(S, output, output_ptr, delayBuf, 0, S.Fs_in_kHz);
			silk_resampler_private_down_FIR(S, output, output_ptr + S.Fs_out_kHz, input, input_ptr + num, inLen - S.Fs_in_kHz);
			break;
		default:
			Array.Copy(delayBuf, 0, output, output_ptr, S.Fs_in_kHz);
			Array.Copy(input, input_ptr + num, output, output_ptr + S.Fs_out_kHz, inLen - S.Fs_in_kHz);
			break;
		}
		Array.Copy(input, input_ptr + inLen - S.inputDelay, delayBuf, 0, S.inputDelay);
		return SilkError.SILK_NO_ERROR;
	}

	internal static void silk_resampler_down2(int[] S, short[] output, short[] input, int inLen)
	{
		int num = Inlines.silk_RSHIFT32(inLen, 1);
		for (int i = 0; i < num; i++)
		{
			int a = Inlines.silk_LSHIFT(input[2 * i], 10);
			int num2 = Inlines.silk_SUB32(a, S[0]);
			int b = Inlines.silk_SMLAWB(num2, num2, -25727);
			int a2 = Inlines.silk_ADD32(S[0], b);
			S[0] = Inlines.silk_ADD32(a, b);
			a = Inlines.silk_LSHIFT(input[2 * i + 1], 10);
			b = Inlines.silk_SMULWB(Inlines.silk_SUB32(a, S[1]), 9872);
			a2 = Inlines.silk_ADD32(a2, S[1]);
			a2 = Inlines.silk_ADD32(a2, b);
			S[1] = Inlines.silk_ADD32(a, b);
			output[i] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a2, 11));
		}
	}

	internal static void silk_resampler_down2_3(int[] S, short[] output, short[] input, int inLen)
	{
		int[] array = new int[484];
		int num = 0;
		int num2 = 0;
		Array.Copy(S, 0, array, 0, 4);
		int num3;
		while (true)
		{
			num3 = Inlines.silk_min(inLen, 480);
			silk_resampler_private_AR2(S, 4, array, 4, input, num, Tables.silk_Resampler_2_3_COEFS_LQ, num3);
			int num4 = 0;
			for (int num5 = num3; num5 > 2; num5 -= 3)
			{
				int a = Inlines.silk_SMULWB(array[num4], Tables.silk_Resampler_2_3_COEFS_LQ[2]);
				a = Inlines.silk_SMLAWB(a, array[num4 + 1], Tables.silk_Resampler_2_3_COEFS_LQ[3]);
				a = Inlines.silk_SMLAWB(a, array[num4 + 2], Tables.silk_Resampler_2_3_COEFS_LQ[5]);
				a = Inlines.silk_SMLAWB(a, array[num4 + 3], Tables.silk_Resampler_2_3_COEFS_LQ[4]);
				output[num2++] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a, 6));
				a = Inlines.silk_SMULWB(array[num4 + 1], Tables.silk_Resampler_2_3_COEFS_LQ[4]);
				a = Inlines.silk_SMLAWB(a, array[num4 + 2], Tables.silk_Resampler_2_3_COEFS_LQ[5]);
				a = Inlines.silk_SMLAWB(a, array[num4 + 3], Tables.silk_Resampler_2_3_COEFS_LQ[3]);
				a = Inlines.silk_SMLAWB(a, array[num4 + 4], Tables.silk_Resampler_2_3_COEFS_LQ[2]);
				output[num2++] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a, 6));
				num4 += 3;
			}
			num += num3;
			inLen -= num3;
			if (inLen <= 0)
			{
				break;
			}
			Array.Copy(array, num3, array, 0, 4);
		}
		Array.Copy(array, num3, S, 0, 4);
	}

	internal static void silk_resampler_private_AR2(int[] S, int S_ptr, int[] out_Q8, int out_Q8_ptr, short[] input, int input_ptr, short[] A_Q14, int len)
	{
		for (int i = 0; i < len; i++)
		{
			int num = Inlines.silk_LSHIFT(out_Q8[out_Q8_ptr + i] = Inlines.silk_ADD_LSHIFT32(S[S_ptr], input[input_ptr + i], 8), 2);
			S[S_ptr] = Inlines.silk_SMLAWB(S[S_ptr + 1], num, A_Q14[0]);
			S[S_ptr + 1] = Inlines.silk_SMULWB(num, A_Q14[1]);
		}
	}

	internal static int silk_resampler_private_down_FIR_INTERPOL(short[] output, int output_ptr, int[] buf, short[] FIR_Coefs, int FIR_Coefs_ptr, int FIR_Order, int FIR_Fracs, int max_index_Q16, int index_increment_Q16)
	{
		switch (FIR_Order)
		{
		case 18:
		{
			for (int i = 0; i < max_index_Q16; i += index_increment_Q16)
			{
				int num = Inlines.silk_RSHIFT(i, 16);
				int num2 = Inlines.silk_SMULWB(i & 0xFFFF, FIR_Fracs);
				int num3 = FIR_Coefs_ptr + 9 * num2;
				int a = Inlines.silk_SMULWB(buf[num], FIR_Coefs[num3]);
				a = Inlines.silk_SMLAWB(a, buf[num + 1], FIR_Coefs[num3 + 1]);
				a = Inlines.silk_SMLAWB(a, buf[num + 2], FIR_Coefs[num3 + 2]);
				a = Inlines.silk_SMLAWB(a, buf[num + 3], FIR_Coefs[num3 + 3]);
				a = Inlines.silk_SMLAWB(a, buf[num + 4], FIR_Coefs[num3 + 4]);
				a = Inlines.silk_SMLAWB(a, buf[num + 5], FIR_Coefs[num3 + 5]);
				a = Inlines.silk_SMLAWB(a, buf[num + 6], FIR_Coefs[num3 + 6]);
				a = Inlines.silk_SMLAWB(a, buf[num + 7], FIR_Coefs[num3 + 7]);
				a = Inlines.silk_SMLAWB(a, buf[num + 8], FIR_Coefs[num3 + 8]);
				num3 = FIR_Coefs_ptr + 9 * (FIR_Fracs - 1 - num2);
				a = Inlines.silk_SMLAWB(a, buf[num + 17], FIR_Coefs[num3]);
				a = Inlines.silk_SMLAWB(a, buf[num + 16], FIR_Coefs[num3 + 1]);
				a = Inlines.silk_SMLAWB(a, buf[num + 15], FIR_Coefs[num3 + 2]);
				a = Inlines.silk_SMLAWB(a, buf[num + 14], FIR_Coefs[num3 + 3]);
				a = Inlines.silk_SMLAWB(a, buf[num + 13], FIR_Coefs[num3 + 4]);
				a = Inlines.silk_SMLAWB(a, buf[num + 12], FIR_Coefs[num3 + 5]);
				a = Inlines.silk_SMLAWB(a, buf[num + 11], FIR_Coefs[num3 + 6]);
				a = Inlines.silk_SMLAWB(a, buf[num + 10], FIR_Coefs[num3 + 7]);
				a = Inlines.silk_SMLAWB(a, buf[num + 9], FIR_Coefs[num3 + 8]);
				output[output_ptr++] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a, 6));
			}
			break;
		}
		case 24:
		{
			for (int i = 0; i < max_index_Q16; i += index_increment_Q16)
			{
				int num = Inlines.silk_RSHIFT(i, 16);
				int a = Inlines.silk_SMULWB(Inlines.silk_ADD32(buf[num], buf[num + 23]), FIR_Coefs[FIR_Coefs_ptr]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 1], buf[num + 22]), FIR_Coefs[FIR_Coefs_ptr + 1]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 2], buf[num + 21]), FIR_Coefs[FIR_Coefs_ptr + 2]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 3], buf[num + 20]), FIR_Coefs[FIR_Coefs_ptr + 3]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 4], buf[num + 19]), FIR_Coefs[FIR_Coefs_ptr + 4]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 5], buf[num + 18]), FIR_Coefs[FIR_Coefs_ptr + 5]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 6], buf[num + 17]), FIR_Coefs[FIR_Coefs_ptr + 6]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 7], buf[num + 16]), FIR_Coefs[FIR_Coefs_ptr + 7]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 8], buf[num + 15]), FIR_Coefs[FIR_Coefs_ptr + 8]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 9], buf[num + 14]), FIR_Coefs[FIR_Coefs_ptr + 9]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 10], buf[num + 13]), FIR_Coefs[FIR_Coefs_ptr + 10]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 11], buf[num + 12]), FIR_Coefs[FIR_Coefs_ptr + 11]);
				output[output_ptr++] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a, 6));
			}
			break;
		}
		case 36:
		{
			for (int i = 0; i < max_index_Q16; i += index_increment_Q16)
			{
				int num = Inlines.silk_RSHIFT(i, 16);
				int a = Inlines.silk_SMULWB(Inlines.silk_ADD32(buf[num], buf[num + 35]), FIR_Coefs[FIR_Coefs_ptr]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 1], buf[num + 34]), FIR_Coefs[FIR_Coefs_ptr + 1]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 2], buf[num + 33]), FIR_Coefs[FIR_Coefs_ptr + 2]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 3], buf[num + 32]), FIR_Coefs[FIR_Coefs_ptr + 3]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 4], buf[num + 31]), FIR_Coefs[FIR_Coefs_ptr + 4]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 5], buf[num + 30]), FIR_Coefs[FIR_Coefs_ptr + 5]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 6], buf[num + 29]), FIR_Coefs[FIR_Coefs_ptr + 6]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 7], buf[num + 28]), FIR_Coefs[FIR_Coefs_ptr + 7]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 8], buf[num + 27]), FIR_Coefs[FIR_Coefs_ptr + 8]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 9], buf[num + 26]), FIR_Coefs[FIR_Coefs_ptr + 9]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 10], buf[num + 25]), FIR_Coefs[FIR_Coefs_ptr + 10]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 11], buf[num + 24]), FIR_Coefs[FIR_Coefs_ptr + 11]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 12], buf[num + 23]), FIR_Coefs[FIR_Coefs_ptr + 12]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 13], buf[num + 22]), FIR_Coefs[FIR_Coefs_ptr + 13]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 14], buf[num + 21]), FIR_Coefs[FIR_Coefs_ptr + 14]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 15], buf[num + 20]), FIR_Coefs[FIR_Coefs_ptr + 15]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 16], buf[num + 19]), FIR_Coefs[FIR_Coefs_ptr + 16]);
				a = Inlines.silk_SMLAWB(a, Inlines.silk_ADD32(buf[num + 17], buf[num + 18]), FIR_Coefs[FIR_Coefs_ptr + 17]);
				output[output_ptr++] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a, 6));
			}
			break;
		}
		}
		return output_ptr;
	}

	internal static void silk_resampler_private_down_FIR(SilkResamplerState S, short[] output, int output_ptr, short[] input, int input_ptr, int inLen)
	{
		int[] array = new int[S.batchSize + S.FIR_Order];
		Array.Copy(S.sFIR_i32, array, S.FIR_Order);
		int invRatio_Q = S.invRatio_Q16;
		int num;
		while (true)
		{
			num = Inlines.silk_min(inLen, S.batchSize);
			silk_resampler_private_AR2(S.sIIR, 0, array, S.FIR_Order, input, input_ptr, S.Coefs, num);
			int max_index_Q = Inlines.silk_LSHIFT32(num, 16);
			output_ptr = silk_resampler_private_down_FIR_INTERPOL(output, output_ptr, array, S.Coefs, 2, S.FIR_Order, S.FIR_Fracs, max_index_Q, invRatio_Q);
			input_ptr += num;
			inLen -= num;
			if (inLen <= 1)
			{
				break;
			}
			Array.Copy(array, num, array, 0, S.FIR_Order);
		}
		Array.Copy(array, num, S.sFIR_i32, 0, S.FIR_Order);
	}

	internal static int silk_resampler_private_IIR_FIR_INTERPOL(short[] output, int output_ptr, short[] buf, int max_index_Q16, int index_increment_Q16)
	{
		for (int i = 0; i < max_index_Q16; i += index_increment_Q16)
		{
			int num = Inlines.silk_SMULWB(i & 0xFFFF, 12);
			int num2 = i >> 16;
			int a = Inlines.silk_SMULBB(buf[num2], Tables.silk_resampler_frac_FIR_12[num, 0]);
			a = Inlines.silk_SMLABB(a, buf[num2 + 1], Tables.silk_resampler_frac_FIR_12[num, 1]);
			a = Inlines.silk_SMLABB(a, buf[num2 + 2], Tables.silk_resampler_frac_FIR_12[num, 2]);
			a = Inlines.silk_SMLABB(a, buf[num2 + 3], Tables.silk_resampler_frac_FIR_12[num, 3]);
			a = Inlines.silk_SMLABB(a, buf[num2 + 4], Tables.silk_resampler_frac_FIR_12[11 - num, 3]);
			a = Inlines.silk_SMLABB(a, buf[num2 + 5], Tables.silk_resampler_frac_FIR_12[11 - num, 2]);
			a = Inlines.silk_SMLABB(a, buf[num2 + 6], Tables.silk_resampler_frac_FIR_12[11 - num, 1]);
			a = Inlines.silk_SMLABB(a, buf[num2 + 7], Tables.silk_resampler_frac_FIR_12[11 - num, 0]);
			output[output_ptr++] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a, 15));
		}
		return output_ptr;
	}

	internal static void silk_resampler_private_IIR_FIR(SilkResamplerState S, short[] output, int output_ptr, short[] input, int input_ptr, int inLen)
	{
		short[] array = new short[2 * S.batchSize + 8];
		Array.Copy(S.sFIR_i16, 0, array, 0, 8);
		int invRatio_Q = S.invRatio_Q16;
		int num;
		while (true)
		{
			num = Inlines.silk_min(inLen, S.batchSize);
			silk_resampler_private_up2_HQ(S.sIIR, array, 8, input, input_ptr, num);
			int max_index_Q = Inlines.silk_LSHIFT32(num, 17);
			output_ptr = silk_resampler_private_IIR_FIR_INTERPOL(output, output_ptr, array, max_index_Q, invRatio_Q);
			input_ptr += num;
			inLen -= num;
			if (inLen <= 0)
			{
				break;
			}
			Array.Copy(array, num << 1, array, 0, 8);
		}
		Array.Copy(array, num << 1, S.sFIR_i16, 0, 8);
	}

	internal static void silk_resampler_private_up2_HQ(int[] S, short[] output, int output_ptr, short[] input, int input_ptr, int len)
	{
		for (int i = 0; i < len; i++)
		{
			int a = Inlines.silk_LSHIFT(input[input_ptr + i], 10);
			int b = Inlines.silk_SMULWB(Inlines.silk_SUB32(a, S[0]), Tables.silk_resampler_up2_hq_0[0]);
			int a2 = Inlines.silk_ADD32(S[0], b);
			S[0] = Inlines.silk_ADD32(a, b);
			b = Inlines.silk_SMULWB(Inlines.silk_SUB32(a2, S[1]), Tables.silk_resampler_up2_hq_0[1]);
			int a3 = Inlines.silk_ADD32(S[1], b);
			S[1] = Inlines.silk_ADD32(a2, b);
			int num = Inlines.silk_SUB32(a3, S[2]);
			b = Inlines.silk_SMLAWB(num, num, Tables.silk_resampler_up2_hq_0[2]);
			a2 = Inlines.silk_ADD32(S[2], b);
			S[2] = Inlines.silk_ADD32(a3, b);
			output[output_ptr + 2 * i] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a2, 10));
			b = Inlines.silk_SMULWB(Inlines.silk_SUB32(a, S[3]), Tables.silk_resampler_up2_hq_1[0]);
			a2 = Inlines.silk_ADD32(S[3], b);
			S[3] = Inlines.silk_ADD32(a, b);
			b = Inlines.silk_SMULWB(Inlines.silk_SUB32(a2, S[4]), Tables.silk_resampler_up2_hq_1[1]);
			a3 = Inlines.silk_ADD32(S[4], b);
			S[4] = Inlines.silk_ADD32(a2, b);
			int num2 = Inlines.silk_SUB32(a3, S[5]);
			b = Inlines.silk_SMLAWB(num2, num2, Tables.silk_resampler_up2_hq_1[2]);
			a2 = Inlines.silk_ADD32(S[5], b);
			S[5] = Inlines.silk_ADD32(a3, b);
			output[output_ptr + 2 * i + 1] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT_ROUND(a2, 10));
		}
	}
}
