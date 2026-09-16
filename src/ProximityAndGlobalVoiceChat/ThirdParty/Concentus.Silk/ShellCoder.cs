using Concentus.Common;

namespace Concentus.Silk;

internal static class ShellCoder
{
	internal static void combine_pulses(int[] output, int[] input, int input_ptr, int len)
	{
		for (int i = 0; i < len; i++)
		{
			output[i] = input[input_ptr + 2 * i] + input[input_ptr + 2 * i + 1];
		}
	}

	internal static void combine_pulses(int[] output, int[] input, int len)
	{
		for (int i = 0; i < len; i++)
		{
			output[i] = input[2 * i] + input[2 * i + 1];
		}
	}

	internal static void encode_split(EntropyCoder psRangeEnc, int p_child1, int p, byte[] shell_table)
	{
		if (p > 0)
		{
			psRangeEnc.enc_icdf(p_child1, shell_table, Tables.silk_shell_code_table_offsets[p], 8u);
		}
	}

	internal static void decode_split(short[] p_child1, int child1_ptr, short[] p_child2, int p_child2_ptr, EntropyCoder psRangeDec, int p, byte[] shell_table)
	{
		if (p > 0)
		{
			p_child1[child1_ptr] = (short)psRangeDec.dec_icdf(shell_table, Tables.silk_shell_code_table_offsets[p], 8u);
			p_child2[p_child2_ptr] = (short)(p - p_child1[child1_ptr]);
		}
		else
		{
			p_child1[child1_ptr] = 0;
			p_child2[p_child2_ptr] = 0;
		}
	}

	internal static void silk_shell_encoder(EntropyCoder psRangeEnc, int[] pulses0, int pulses0_ptr)
	{
		int[] array = new int[8];
		int[] array2 = new int[4];
		int[] array3 = new int[2];
		int[] array4 = new int[1];
		combine_pulses(array, pulses0, pulses0_ptr, 8);
		combine_pulses(array2, array, 4);
		combine_pulses(array3, array2, 2);
		combine_pulses(array4, array3, 1);
		encode_split(psRangeEnc, array3[0], array4[0], Tables.silk_shell_code_table3);
		encode_split(psRangeEnc, array2[0], array3[0], Tables.silk_shell_code_table2);
		encode_split(psRangeEnc, array[0], array2[0], Tables.silk_shell_code_table1);
		encode_split(psRangeEnc, pulses0[pulses0_ptr], array[0], Tables.silk_shell_code_table0);
		encode_split(psRangeEnc, pulses0[pulses0_ptr + 2], array[1], Tables.silk_shell_code_table0);
		encode_split(psRangeEnc, array[2], array2[1], Tables.silk_shell_code_table1);
		encode_split(psRangeEnc, pulses0[pulses0_ptr + 4], array[2], Tables.silk_shell_code_table0);
		encode_split(psRangeEnc, pulses0[pulses0_ptr + 6], array[3], Tables.silk_shell_code_table0);
		encode_split(psRangeEnc, array2[2], array3[1], Tables.silk_shell_code_table2);
		encode_split(psRangeEnc, array[4], array2[2], Tables.silk_shell_code_table1);
		encode_split(psRangeEnc, pulses0[pulses0_ptr + 8], array[4], Tables.silk_shell_code_table0);
		encode_split(psRangeEnc, pulses0[pulses0_ptr + 10], array[5], Tables.silk_shell_code_table0);
		encode_split(psRangeEnc, array[6], array2[3], Tables.silk_shell_code_table1);
		encode_split(psRangeEnc, pulses0[pulses0_ptr + 12], array[6], Tables.silk_shell_code_table0);
		encode_split(psRangeEnc, pulses0[pulses0_ptr + 14], array[7], Tables.silk_shell_code_table0);
	}

	internal static void silk_shell_decoder(short[] pulses0, int pulses0_ptr, EntropyCoder psRangeDec, int pulses4)
	{
		short[] array = new short[8];
		short[] array2 = new short[4];
		short[] array3 = new short[2];
		decode_split(array3, 0, array3, 1, psRangeDec, pulses4, Tables.silk_shell_code_table3);
		decode_split(array2, 0, array2, 1, psRangeDec, array3[0], Tables.silk_shell_code_table2);
		decode_split(array, 0, array, 1, psRangeDec, array2[0], Tables.silk_shell_code_table1);
		decode_split(pulses0, pulses0_ptr, pulses0, pulses0_ptr + 1, psRangeDec, array[0], Tables.silk_shell_code_table0);
		decode_split(pulses0, pulses0_ptr + 2, pulses0, pulses0_ptr + 3, psRangeDec, array[1], Tables.silk_shell_code_table0);
		decode_split(array, 2, array, 3, psRangeDec, array2[1], Tables.silk_shell_code_table1);
		decode_split(pulses0, pulses0_ptr + 4, pulses0, pulses0_ptr + 5, psRangeDec, array[2], Tables.silk_shell_code_table0);
		decode_split(pulses0, pulses0_ptr + 6, pulses0, pulses0_ptr + 7, psRangeDec, array[3], Tables.silk_shell_code_table0);
		decode_split(array2, 2, array2, 3, psRangeDec, array3[1], Tables.silk_shell_code_table2);
		decode_split(array, 4, array, 5, psRangeDec, array2[2], Tables.silk_shell_code_table1);
		decode_split(pulses0, pulses0_ptr + 8, pulses0, pulses0_ptr + 9, psRangeDec, array[4], Tables.silk_shell_code_table0);
		decode_split(pulses0, pulses0_ptr + 10, pulses0, pulses0_ptr + 11, psRangeDec, array[5], Tables.silk_shell_code_table0);
		decode_split(array, 6, array, 7, psRangeDec, array2[3], Tables.silk_shell_code_table1);
		decode_split(pulses0, pulses0_ptr + 12, pulses0, pulses0_ptr + 13, psRangeDec, array[6], Tables.silk_shell_code_table0);
		decode_split(pulses0, pulses0_ptr + 14, pulses0, pulses0_ptr + 15, psRangeDec, array[7], Tables.silk_shell_code_table0);
	}
}
