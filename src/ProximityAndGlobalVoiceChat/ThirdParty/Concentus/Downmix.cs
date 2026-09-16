using Concentus.Common;

namespace Concentus;

internal static class Downmix
{
	public delegate void downmix_func<T>(T[] _x, int x_ptr, int[] sub, int sub_ptr, int subframe, int offset, int c1, int c2, int C);

	internal static void downmix_float(float[] x, int x_ptr, int[] sub, int sub_ptr, int subframe, int offset, int c1, int c2, int C)
	{
		int num = c1 + x_ptr;
		for (int i = 0; i < subframe; i++)
		{
			sub[sub_ptr + i] = Inlines.FLOAT2INT16(x[(i + offset) * C + num]);
		}
		if (c2 > -1)
		{
			int num2 = c2 + x_ptr;
			for (int i = 0; i < subframe; i++)
			{
				sub[sub_ptr + i] += Inlines.FLOAT2INT16(x[(i + offset) * C + num2]);
			}
		}
		else if (c2 == -2)
		{
			for (int j = 1; j < C; j++)
			{
				int num3 = j + x_ptr;
				for (int i = 0; i < subframe; i++)
				{
					sub[sub_ptr + i] += Inlines.FLOAT2INT16(x[(i + offset) * C + num3]);
				}
			}
		}
		int num4 = 4096;
		num4 = ((C != -2) ? (num4 / 2) : (num4 / C));
		for (int i = 0; i < subframe; i++)
		{
			sub[sub_ptr + i] *= num4;
		}
	}

	internal static void downmix_int(short[] x, int x_ptr, int[] sub, int sub_ptr, int subframe, int offset, int c1, int c2, int C)
	{
		for (int i = 0; i < subframe; i++)
		{
			sub[i + sub_ptr] = x[(i + offset) * C + c1];
		}
		if (c2 > -1)
		{
			for (int i = 0; i < subframe; i++)
			{
				sub[i + sub_ptr] += x[(i + offset) * C + c2];
			}
		}
		else if (c2 == -2)
		{
			for (int j = 1; j < C; j++)
			{
				for (int i = 0; i < subframe; i++)
				{
					sub[i + sub_ptr] += x[(i + offset) * C + j];
				}
			}
		}
		int num = 4096;
		num = ((C != -2) ? (num / 2) : (num / C));
		for (int i = 0; i < subframe; i++)
		{
			sub[i + sub_ptr] *= num;
		}
	}
}
