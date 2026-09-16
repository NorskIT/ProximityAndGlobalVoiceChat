using Concentus.Common;

namespace Concentus.Silk;

internal static class SumSqrShift
{
	internal static void silk_sum_sqr_shift(out int energy, out int shift, short[] x, int x_ptr, int len)
	{
		int num = 0;
		int num2 = 0;
		len--;
		int i;
		for (i = 0; i < len; i += 2)
		{
			num = Inlines.silk_SMLABB_ovflw(num, x[x_ptr + i], x[x_ptr + i]);
			num = Inlines.silk_SMLABB_ovflw(num, x[x_ptr + i + 1], x[x_ptr + i + 1]);
			if (num < 0)
			{
				num = (int)Inlines.silk_RSHIFT_uint((uint)num, 2);
				num2 = 2;
				i += 2;
				break;
			}
		}
		for (; i < len; i += 2)
		{
			int a = Inlines.silk_SMULBB(x[x_ptr + i], x[x_ptr + i]);
			a = Inlines.silk_SMLABB_ovflw(a, x[x_ptr + i + 1], x[x_ptr + i + 1]);
			num = (int)Inlines.silk_ADD_RSHIFT_uint((uint)num, (uint)a, num2);
			if (num < 0)
			{
				num = (int)Inlines.silk_RSHIFT_uint((uint)num, 2);
				num2 += 2;
			}
		}
		if (i == len)
		{
			int a = Inlines.silk_SMULBB(x[x_ptr + i], x[x_ptr + i]);
			num = (int)Inlines.silk_ADD_RSHIFT_uint((uint)num, (uint)a, num2);
		}
		if ((num & 0xC0000000u) != 0L)
		{
			num = (int)Inlines.silk_RSHIFT_uint((uint)num, 2);
			num2 += 2;
		}
		shift = num2;
		energy = num;
	}

	internal static void silk_sum_sqr_shift(out int energy, out int shift, short[] x, int len)
	{
		int num = 0;
		int num2 = 0;
		len--;
		int i;
		for (i = 0; i < len; i += 2)
		{
			num = Inlines.silk_SMLABB_ovflw(num, x[i], x[i]);
			num = Inlines.silk_SMLABB_ovflw(num, x[i + 1], x[i + 1]);
			if (num < 0)
			{
				num = (int)Inlines.silk_RSHIFT_uint((uint)num, 2);
				num2 = 2;
				i += 2;
				break;
			}
		}
		for (; i < len; i += 2)
		{
			int a = Inlines.silk_SMULBB(x[i], x[i]);
			a = Inlines.silk_SMLABB_ovflw(a, x[i + 1], x[i + 1]);
			num = (int)Inlines.silk_ADD_RSHIFT_uint((uint)num, (uint)a, num2);
			if (num < 0)
			{
				num = (int)Inlines.silk_RSHIFT_uint((uint)num, 2);
				num2 += 2;
			}
		}
		if (i == len)
		{
			int a = Inlines.silk_SMULBB(x[i], x[i]);
			num = (int)Inlines.silk_ADD_RSHIFT_uint((uint)num, (uint)a, num2);
		}
		if ((num & 0xC0000000u) != 0L)
		{
			num = (int)Inlines.silk_RSHIFT_uint((uint)num, 2);
			num2 += 2;
		}
		shift = num2;
		energy = num;
	}
}
