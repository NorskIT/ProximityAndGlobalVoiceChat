using Concentus.Celt;
using Concentus.Common.CPlusPlus;

namespace Concentus.Common;

internal static class Autocorrelation
{
	private const int QC = 10;

	private const int QS = 14;

	internal static void silk_autocorr(int[] results, BoxedValueInt scale, short[] inputData, int inputDataSize, int correlationCount)
	{
		int num = Inlines.silk_min_int(inputDataSize, correlationCount);
		scale.Val = _celt_autocorr(inputData, results, num - 1, inputDataSize);
	}

	internal static int _celt_autocorr(short[] x, int[] ac, int lag, int n)
	{
		int num = n - lag;
		short[] array = new short[n];
		short[] array2 = x;
		int num2 = 0;
		int num3 = 1 + (n << 7);
		if ((n & 1) != 0)
		{
			num3 += Inlines.SHR32(Inlines.MULT16_16(array2[0], array2[0]), 9);
		}
		for (int i = n & 1; i < n; i += 2)
		{
			num3 += Inlines.SHR32(Inlines.MULT16_16(array2[i], array2[i]), 9);
			num3 += Inlines.SHR32(Inlines.MULT16_16(array2[i + 1], array2[i + 1]), 9);
		}
		num2 = Inlines.celt_ilog2(num3) - 30 + 10;
		num2 /= 2;
		if (num2 > 0)
		{
			for (int i = 0; i < n; i++)
			{
				array[i] = (short)Inlines.PSHR32(array2[i], num2);
			}
			array2 = array;
		}
		else
		{
			num2 = 0;
		}
		CeltPitchXCorr.pitch_xcorr(array2, array2, ac, num, lag + 1);
		for (int j = 0; j <= lag; j++)
		{
			int i = j + num;
			int num4 = 0;
			for (; i < n; i++)
			{
				num4 = Inlines.MAC16_16(num4, array2[i], array2[i - j]);
			}
			ac[j] += num4;
		}
		num2 = 2 * num2;
		if (num2 <= 0)
		{
			ac[0] += Inlines.SHL32(1, -num2);
		}
		if (ac[0] < 268435456)
		{
			int num5 = 29 - Inlines.EC_ILOG((uint)ac[0]);
			for (int i = 0; i <= lag; i++)
			{
				ac[i] = Inlines.SHL32(ac[i], num5);
			}
			num2 -= num5;
		}
		else if (ac[0] >= 536870912)
		{
			int num6 = 1;
			if (ac[0] >= 1073741824)
			{
				num6++;
			}
			for (int i = 0; i <= lag; i++)
			{
				ac[i] = Inlines.SHR32(ac[i], num6);
			}
			num2 += num6;
		}
		return num2;
	}

	internal static int _celt_autocorr(int[] x, int[] ac, int[] window, int overlap, int lag, int n)
	{
		int num = n - lag;
		int[] array = new int[n];
		int[] array2;
		if (overlap == 0)
		{
			array2 = x;
		}
		else
		{
			for (int i = 0; i < n; i++)
			{
				array[i] = x[i];
			}
			for (int i = 0; i < overlap; i++)
			{
				array[i] = Inlines.MULT16_16_Q15(x[i], window[i]);
				array[n - i - 1] = Inlines.MULT16_16_Q15(x[n - i - 1], window[i]);
			}
			array2 = array;
		}
		int num2 = 0;
		int num3 = 1 + (n << 7);
		if ((n & 1) != 0)
		{
			num3 += Inlines.SHR32(Inlines.MULT16_16(array2[0], array2[0]), 9);
		}
		for (int i = n & 1; i < n; i += 2)
		{
			num3 += Inlines.SHR32(Inlines.MULT16_16(array2[i], array2[i]), 9);
			num3 += Inlines.SHR32(Inlines.MULT16_16(array2[i + 1], array2[i + 1]), 9);
		}
		num2 = Inlines.celt_ilog2(num3) - 30 + 10;
		num2 /= 2;
		if (num2 > 0)
		{
			for (int i = 0; i < n; i++)
			{
				array[i] = Inlines.PSHR32(array2[i], num2);
			}
			array2 = array;
		}
		else
		{
			num2 = 0;
		}
		CeltPitchXCorr.pitch_xcorr(array2, array2, ac, num, lag + 1);
		for (int j = 0; j <= lag; j++)
		{
			int i = j + num;
			int num4 = 0;
			for (; i < n; i++)
			{
				num4 = Inlines.MAC16_16(num4, array2[i], array2[i - j]);
			}
			ac[j] += num4;
		}
		num2 = 2 * num2;
		if (num2 <= 0)
		{
			ac[0] += Inlines.SHL32(1, -num2);
		}
		if (ac[0] < 268435456)
		{
			int num5 = 29 - Inlines.EC_ILOG((uint)ac[0]);
			for (int i = 0; i <= lag; i++)
			{
				ac[i] = Inlines.SHL32(ac[i], num5);
			}
			num2 -= num5;
		}
		else if (ac[0] >= 536870912)
		{
			int num6 = 1;
			if (ac[0] >= 1073741824)
			{
				num6++;
			}
			for (int i = 0; i <= lag; i++)
			{
				ac[i] = Inlines.SHR32(ac[i], num6);
			}
			num2 += num6;
		}
		return num2;
	}

	internal static void silk_warped_autocorrelation(int[] corr, BoxedValueInt scale, short[] input, int warping_Q16, int length, int order)
	{
		int[] array = new int[17];
		long[] array2 = new long[17];
		for (int i = 0; i < length; i++)
		{
			int num = Inlines.silk_LSHIFT32(input[i], 14);
			for (int j = 0; j < order; j += 2)
			{
				int num2 = Inlines.silk_SMLAWB(array[j], array[j + 1] - num, warping_Q16);
				array[j] = num;
				array2[j] += Inlines.silk_RSHIFT64(Inlines.silk_SMULL(num, array[0]), 18);
				num = Inlines.silk_SMLAWB(array[j + 1], array[j + 2] - num2, warping_Q16);
				array[j + 1] = num2;
				array2[j + 1] += Inlines.silk_RSHIFT64(Inlines.silk_SMULL(num2, array[0]), 18);
			}
			array[order] = num;
			array2[order] += Inlines.silk_RSHIFT64(Inlines.silk_SMULL(num, array[0]), 18);
		}
		int a = Inlines.silk_CLZ64(array2[0]) - 35;
		a = Inlines.silk_LIMIT(a, -22, 20);
		scale.Val = -(10 + a);
		if (a >= 0)
		{
			for (int j = 0; j < order + 1; j++)
			{
				corr[j] = (int)Inlines.silk_LSHIFT64(array2[j], a);
			}
		}
		else
		{
			for (int j = 0; j < order + 1; j++)
			{
				corr[j] = (int)Inlines.silk_RSHIFT64(array2[j], -a);
			}
		}
	}
}
