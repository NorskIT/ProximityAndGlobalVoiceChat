using Concentus.Common;
using Concentus.Common.CPlusPlus;

namespace Concentus.Silk;

internal static class EncodePulses
{
	internal static int combine_and_check(int[] pulses_comb, int pulses_comb_ptr, int[] pulses_in, int pulses_in_ptr, int max_pulses, int len)
	{
		for (int i = 0; i < len; i++)
		{
			int num = 2 * i + pulses_in_ptr;
			int num2 = pulses_in[num] + pulses_in[num + 1];
			if (num2 > max_pulses)
			{
				return 1;
			}
			pulses_comb[pulses_comb_ptr + i] = num2;
		}
		return 0;
	}

	internal static int combine_and_check(int[] pulses_comb, int[] pulses_in, int max_pulses, int len)
	{
		for (int i = 0; i < len; i++)
		{
			int num = pulses_in[2 * i] + pulses_in[2 * i + 1];
			if (num > max_pulses)
			{
				return 1;
			}
			pulses_comb[i] = num;
		}
		return 0;
	}

	internal static void silk_encode_pulses(EntropyCoder psRangeEnc, int signalType, int quantOffsetType, sbyte[] pulses, int frame_length)
	{
		int num = 0;
		int[] array = new int[8];
		Arrays.MemSetInt(array, 0, 8);
		int num2 = Inlines.silk_RSHIFT(frame_length, 4);
		if (num2 * 16 < frame_length)
		{
			num2++;
			Arrays.MemSetWithOffset(pulses, (sbyte)0, frame_length, 16);
		}
		int[] array2 = new int[num2 * 16];
		for (int i = 0; i < num2 * 16; i += 4)
		{
			array2[i] = Inlines.silk_abs(pulses[i]);
			array2[i + 1] = Inlines.silk_abs(pulses[i + 1]);
			array2[i + 2] = Inlines.silk_abs(pulses[i + 2]);
			array2[i + 3] = Inlines.silk_abs(pulses[i + 3]);
		}
		int[] array3 = new int[num2];
		int[] array4 = new int[num2];
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			array4[i] = 0;
			while (combine_and_check(array, 0, array2, num3, Tables.silk_max_pulses_table[0], 8) + combine_and_check(array, array, Tables.silk_max_pulses_table[1], 4) + combine_and_check(array, array, Tables.silk_max_pulses_table[2], 2) + combine_and_check(array3, i, array, 0, Tables.silk_max_pulses_table[3], 1) != 0)
			{
				array4[i]++;
				for (int j = num3; j < num3 + 16; j++)
				{
					array2[j] = Inlines.silk_RSHIFT(array2[j], 1);
				}
			}
			num3 += 16;
		}
		int num4 = int.MaxValue;
		for (int j = 0; j < 9; j++)
		{
			byte[] array5 = Tables.silk_pulses_per_block_BITS_Q5[j];
			int num5 = Tables.silk_rate_levels_BITS_Q5[signalType >> 1][j];
			for (int i = 0; i < num2; i++)
			{
				num5 = ((array4[i] <= 0) ? (num5 + array5[array3[i]]) : (num5 + array5[17]));
			}
			if (num5 < num4)
			{
				num4 = num5;
				num = j;
			}
		}
		psRangeEnc.enc_icdf(num, Tables.silk_rate_levels_iCDF[signalType >> 1], 8u);
		for (int i = 0; i < num2; i++)
		{
			if (array4[i] == 0)
			{
				psRangeEnc.enc_icdf(array3[i], Tables.silk_pulses_per_block_iCDF[num], 8u);
				continue;
			}
			psRangeEnc.enc_icdf(17, Tables.silk_pulses_per_block_iCDF[num], 8u);
			for (int j = 0; j < array4[i] - 1; j++)
			{
				psRangeEnc.enc_icdf(17, Tables.silk_pulses_per_block_iCDF[9], 8u);
			}
			psRangeEnc.enc_icdf(array3[i], Tables.silk_pulses_per_block_iCDF[9], 8u);
		}
		for (int i = 0; i < num2; i++)
		{
			if (array3[i] > 0)
			{
				ShellCoder.silk_shell_encoder(psRangeEnc, array2, i * 16);
			}
		}
		for (int i = 0; i < num2; i++)
		{
			if (array4[i] <= 0)
			{
				continue;
			}
			int num6 = i * 16;
			int num7 = array4[i] - 1;
			for (int j = 0; j < 16; j++)
			{
				int num8 = (sbyte)Inlines.silk_abs(pulses[num6 + j]);
				int s;
				for (int num9 = num7; num9 > 0; num9--)
				{
					s = Inlines.silk_RSHIFT(num8, num9) & 1;
					psRangeEnc.enc_icdf(s, Tables.silk_lsb_iCDF, 8u);
				}
				s = num8 & 1;
				psRangeEnc.enc_icdf(s, Tables.silk_lsb_iCDF, 8u);
			}
		}
		CodeSigns.silk_encode_signs(psRangeEnc, pulses, frame_length, signalType, quantOffsetType, array3);
	}
}
