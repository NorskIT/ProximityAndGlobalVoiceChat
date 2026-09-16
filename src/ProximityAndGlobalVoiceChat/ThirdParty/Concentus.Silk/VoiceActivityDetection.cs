using Concentus.Common;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class VoiceActivityDetection
{
	private static readonly int[] tiltWeights = new int[4] { 30000, 6000, -12000, -12000 };

	internal static int silk_VAD_Init(SilkVADState psSilk_VAD)
	{
		int result = 0;
		psSilk_VAD.Reset();
		for (int i = 0; i < 4; i++)
		{
			psSilk_VAD.NoiseLevelBias[i] = Inlines.silk_max_32(Inlines.silk_DIV32_16(50, (short)(i + 1)), 1);
		}
		for (int i = 0; i < 4; i++)
		{
			psSilk_VAD.NL[i] = Inlines.silk_MUL(100, psSilk_VAD.NoiseLevelBias[i]);
			psSilk_VAD.inv_NL[i] = Inlines.silk_DIV32(int.MaxValue, psSilk_VAD.NL[i]);
		}
		psSilk_VAD.counter = 15;
		for (int i = 0; i < 4; i++)
		{
			psSilk_VAD.NrgRatioSmth_Q8[i] = 25600;
		}
		return result;
	}

	internal static int silk_VAD_GetSA_Q8(SilkChannelEncoder psEncC, short[] pIn, int pIn_ptr)
	{
		int num = 0;
		int[] array = new int[4];
		int[] array2 = new int[4];
		int[] array3 = new int[4];
		int result = 0;
		SilkVADState sVAD = psEncC.sVAD;
		int num2 = Inlines.silk_RSHIFT(psEncC.frame_length, 1);
		int num3 = Inlines.silk_RSHIFT(psEncC.frame_length, 2);
		int num4 = Inlines.silk_RSHIFT(psEncC.frame_length, 3);
		array3[0] = 0;
		array3[1] = num4 + num3;
		array3[2] = array3[1] + num4;
		array3[3] = array3[2] + num3;
		short[] array4 = new short[array3[3] + num2];
		Filters.silk_ana_filt_bank_1(pIn, pIn_ptr, sVAD.AnaState, array4, array4, array3[3], psEncC.frame_length);
		Filters.silk_ana_filt_bank_1(array4, 0, sVAD.AnaState1, array4, array4, array3[2], num2);
		Filters.silk_ana_filt_bank_1(array4, 0, sVAD.AnaState2, array4, array4, array3[1], num3);
		array4[num4 - 1] = (short)Inlines.silk_RSHIFT(array4[num4 - 1], 1);
		short hPstate = array4[num4 - 1];
		for (int num5 = num4 - 1; num5 > 0; num5--)
		{
			array4[num5 - 1] = (short)Inlines.silk_RSHIFT(array4[num5 - 1], 1);
			array4[num5] -= array4[num5 - 1];
		}
		array4[0] -= sVAD.HPstate;
		sVAD.HPstate = hPstate;
		for (int i = 0; i < 4; i++)
		{
			num4 = Inlines.silk_RSHIFT(psEncC.frame_length, Inlines.silk_min_int(4 - i, 3));
			int num6 = Inlines.silk_RSHIFT(num4, 2);
			int num7 = 0;
			array[i] = sVAD.XnrgSubfr[i];
			for (int j = 0; j < 4; j++)
			{
				num = 0;
				for (int num5 = 0; num5 < num6; num5++)
				{
					int num8 = Inlines.silk_RSHIFT(array4[array3[i] + num5 + num7], 3);
					num = Inlines.silk_SMLABB(num, num8, num8);
				}
				if (j < 3)
				{
					array[i] = Inlines.silk_ADD_POS_SAT32(array[i], num);
				}
				else
				{
					array[i] = Inlines.silk_ADD_POS_SAT32(array[i], Inlines.silk_RSHIFT(num, 1));
				}
				num7 += num6;
			}
			sVAD.XnrgSubfr[i] = num;
		}
		silk_VAD_GetNoiseLevels(array, sVAD);
		num = 0;
		int num9 = 0;
		int num10;
		for (int i = 0; i < 4; i++)
		{
			num10 = array[i] - sVAD.NL[i];
			if (num10 > 0)
			{
				if ((array[i] & 0xFF800000u) == 0L)
				{
					array2[i] = Inlines.silk_DIV32(Inlines.silk_LSHIFT(array[i], 8), sVAD.NL[i] + 1);
				}
				else
				{
					array2[i] = Inlines.silk_DIV32(array[i], Inlines.silk_RSHIFT(sVAD.NL[i], 8) + 1);
				}
				int num11 = Inlines.silk_lin2log(array2[i]) - 1024;
				num = Inlines.silk_SMLABB(num, num11, num11);
				if (num10 < 1048576)
				{
					num11 = Inlines.silk_SMULWB(Inlines.silk_LSHIFT(Inlines.silk_SQRT_APPROX(num10), 6), num11);
				}
				num9 = Inlines.silk_SMLAWB(num9, tiltWeights[i], num11);
			}
			else
			{
				array2[i] = 256;
			}
		}
		num = Inlines.silk_DIV32_16(num, 4);
		int b = (short)(3 * Inlines.silk_SQRT_APPROX(num));
		int num12 = Sigmoid.silk_sigm_Q15(Inlines.silk_SMULWB(45000, b) - 128);
		psEncC.input_tilt_Q15 = Inlines.silk_LSHIFT(Sigmoid.silk_sigm_Q15(num9) - 16384, 1);
		num10 = 0;
		for (int i = 0; i < 4; i++)
		{
			num10 += (i + 1) * Inlines.silk_RSHIFT(array[i] - sVAD.NL[i], 4);
		}
		if (num10 <= 0)
		{
			num12 = Inlines.silk_RSHIFT(num12, 1);
		}
		else if (num10 < 32768)
		{
			num10 = ((psEncC.frame_length != 10 * psEncC.fs_kHz) ? Inlines.silk_LSHIFT_SAT32(num10, 15) : Inlines.silk_LSHIFT_SAT32(num10, 16));
			num10 = Inlines.silk_SQRT_APPROX(num10);
			num12 = Inlines.silk_SMULWB(32768 + num10, num12);
		}
		psEncC.speech_activity_Q8 = Inlines.silk_min_int(Inlines.silk_RSHIFT(num12, 7), 255);
		int num13 = Inlines.silk_SMULWB(4096, Inlines.silk_SMULWB(num12, num12));
		if (psEncC.frame_length == 10 * psEncC.fs_kHz)
		{
			num13 >>= 1;
		}
		for (int i = 0; i < 4; i++)
		{
			sVAD.NrgRatioSmth_Q8[i] = Inlines.silk_SMLAWB(sVAD.NrgRatioSmth_Q8[i], array2[i] - sVAD.NrgRatioSmth_Q8[i], num13);
			int num11 = 3 * (Inlines.silk_lin2log(sVAD.NrgRatioSmth_Q8[i]) - 1024);
			psEncC.input_quality_bands_Q15[i] = Sigmoid.silk_sigm_Q15(Inlines.silk_RSHIFT(num11 - 2048, 4));
		}
		return result;
	}

	internal static void silk_VAD_GetNoiseLevels(int[] pX, SilkVADState psSilk_VAD)
	{
		int b = ((psSilk_VAD.counter < 1000) ? Inlines.silk_DIV32_16(32767, (short)(Inlines.silk_RSHIFT(psSilk_VAD.counter, 4) + 1)) : 0);
		for (int i = 0; i < 4; i++)
		{
			int num = psSilk_VAD.NL[i];
			int num2 = Inlines.silk_ADD_POS_SAT32(pX[i], psSilk_VAD.NoiseLevelBias[i]);
			int num3 = Inlines.silk_DIV32(int.MaxValue, num2);
			int a = ((num2 <= Inlines.silk_LSHIFT(num, 3)) ? ((num2 >= num) ? Inlines.silk_SMULWB(Inlines.silk_SMULWW(num3, num), 2048) : 1024) : 128);
			a = Inlines.silk_max_int(a, b);
			psSilk_VAD.inv_NL[i] = Inlines.silk_SMLAWB(psSilk_VAD.inv_NL[i], num3 - psSilk_VAD.inv_NL[i], a);
			num = Inlines.silk_DIV32(int.MaxValue, psSilk_VAD.inv_NL[i]);
			num = Inlines.silk_min(num, 16777215);
			psSilk_VAD.NL[i] = num;
		}
		psSilk_VAD.counter++;
	}
}
