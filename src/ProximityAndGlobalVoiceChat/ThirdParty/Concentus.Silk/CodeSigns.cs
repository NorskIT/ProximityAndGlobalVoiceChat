using Concentus.Common;

namespace Concentus.Silk;

internal static class CodeSigns
{
	private static int silk_enc_map(int a)
	{
		return Inlines.silk_RSHIFT(a, 15) + 1;
	}

	private static int silk_dec_map(int a)
	{
		return Inlines.silk_LSHIFT(a, 1) - 1;
	}

	internal static void silk_encode_signs(EntropyCoder psRangeEnc, sbyte[] pulses, int length, int signalType, int quantOffsetType, int[] sum_pulses)
	{
		byte[] array = new byte[2];
		byte[] silk_sign_iCDF = Tables.silk_sign_iCDF;
		array[1] = 0;
		int num = 0;
		int num2 = Inlines.silk_SMULBB(7, Inlines.silk_ADD_LSHIFT(quantOffsetType, signalType, 1));
		int num3 = num2;
		length = Inlines.silk_RSHIFT(length + 8, 4);
		for (num2 = 0; num2 < length; num2++)
		{
			int num4 = sum_pulses[num2];
			if (num4 > 0)
			{
				array[0] = silk_sign_iCDF[num3 + Inlines.silk_min(num4 & 0x1F, 6)];
				for (int i = num; i < num + 16; i++)
				{
					if (pulses[i] != 0)
					{
						psRangeEnc.enc_icdf(silk_enc_map(pulses[i]), array, 8u);
					}
				}
			}
			num += 16;
		}
	}

	internal static void silk_decode_signs(EntropyCoder psRangeDec, short[] pulses, int length, int signalType, int quantOffsetType, int[] sum_pulses)
	{
		byte[] array = new byte[2];
		byte[] silk_sign_iCDF = Tables.silk_sign_iCDF;
		array[1] = 0;
		int num = 0;
		int num2 = Inlines.silk_SMULBB(7, Inlines.silk_ADD_LSHIFT(quantOffsetType, signalType, 1));
		int num3 = num2;
		length = Inlines.silk_RSHIFT(length + 8, 4);
		for (num2 = 0; num2 < length; num2++)
		{
			int num4 = sum_pulses[num2];
			if (num4 > 0)
			{
				array[0] = silk_sign_iCDF[num3 + Inlines.silk_min(num4 & 0x1F, 6)];
				for (int i = 0; i < 16; i++)
				{
					if (pulses[num + i] > 0)
					{
						pulses[num + i] *= (short)silk_dec_map(psRangeDec.dec_icdf(array, 8u));
					}
				}
			}
			num += 16;
		}
	}
}
