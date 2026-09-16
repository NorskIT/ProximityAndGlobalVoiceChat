using Concentus.Common;

namespace Concentus.Celt;

internal static class Kernels
{
	internal static void celt_fir(short[] x, int x_ptr, short[] num, short[] y, int y_ptr, int N, int ord, short[] mem)
	{
		short[] array = new short[ord];
		short[] array2 = new short[N + ord];
		int i;
		for (i = 0; i < ord; i++)
		{
			array[i] = num[ord - i - 1];
		}
		for (i = 0; i < ord; i++)
		{
			array2[i] = mem[ord - i - 1];
		}
		for (i = 0; i < N; i++)
		{
			array2[i + ord] = x[x_ptr + i];
		}
		for (i = 0; i < ord; i++)
		{
			mem[i] = x[x_ptr + N - i - 1];
		}
		for (i = 0; i < N - 3; i += 4)
		{
			int sum = 0;
			int sum2 = 0;
			int sum3 = 0;
			int sum4 = 0;
			xcorr_kernel(array, 0, array2, i, ref sum, ref sum2, ref sum3, ref sum4, ord);
			y[y_ptr + i] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i]), Inlines.PSHR32(sum, 12)));
			y[y_ptr + i + 1] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i + 1]), Inlines.PSHR32(sum2, 12)));
			y[y_ptr + i + 2] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i + 2]), Inlines.PSHR32(sum3, 12)));
			y[y_ptr + i + 3] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i + 3]), Inlines.PSHR32(sum4, 12)));
		}
		for (; i < N; i++)
		{
			int num2 = 0;
			for (int j = 0; j < ord; j++)
			{
				num2 = Inlines.MAC16_16(num2, array[j], array2[i + j]);
			}
			y[y_ptr + i] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i]), Inlines.PSHR32(num2, 12)));
		}
	}

	internal static void celt_fir(int[] x, int x_ptr, int[] num, int num_ptr, int[] y, int y_ptr, int N, int ord, int[] mem)
	{
		int[] array = new int[ord];
		int[] array2 = new int[N + ord];
		int i;
		for (i = 0; i < ord; i++)
		{
			array[i] = num[num_ptr + ord - i - 1];
		}
		for (i = 0; i < ord; i++)
		{
			array2[i] = mem[ord - i - 1];
		}
		for (i = 0; i < N; i++)
		{
			array2[i + ord] = x[x_ptr + i];
		}
		for (i = 0; i < ord; i++)
		{
			mem[i] = x[x_ptr + N - i - 1];
		}
		for (i = 0; i < N - 3; i += 4)
		{
			int sum = 0;
			int sum2 = 0;
			int sum3 = 0;
			int sum4 = 0;
			xcorr_kernel(array, array2, i, ref sum, ref sum2, ref sum3, ref sum4, ord);
			y[y_ptr + i] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i]), Inlines.PSHR32(sum, 12)));
			y[y_ptr + i + 1] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i + 1]), Inlines.PSHR32(sum2, 12)));
			y[y_ptr + i + 2] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i + 2]), Inlines.PSHR32(sum3, 12)));
			y[y_ptr + i + 3] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i + 3]), Inlines.PSHR32(sum4, 12)));
		}
		for (; i < N; i++)
		{
			int num2 = 0;
			for (int j = 0; j < ord; j++)
			{
				num2 = Inlines.MAC16_16(num2, array[j], array2[i + j]);
			}
			y[y_ptr + i] = Inlines.SATURATE16(Inlines.ADD32(Inlines.EXTEND32(x[x_ptr + i]), Inlines.PSHR32(num2, 12)));
		}
	}

	internal static void xcorr_kernel(short[] x, int x_ptr, short[] y, int y_ptr, ref int sum0, ref int sum1, ref int sum2, ref int sum3, int len)
	{
		short b = 0;
		short b2 = y[y_ptr++];
		short b3 = y[y_ptr++];
		short b4 = y[y_ptr++];
		int i;
		for (i = 0; i < len - 3; i += 4)
		{
			short a = x[x_ptr++];
			b = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a, b2);
			sum1 = Inlines.MAC16_16(sum1, a, b3);
			sum2 = Inlines.MAC16_16(sum2, a, b4);
			sum3 = Inlines.MAC16_16(sum3, a, b);
			a = x[x_ptr++];
			b2 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a, b3);
			sum1 = Inlines.MAC16_16(sum1, a, b4);
			sum2 = Inlines.MAC16_16(sum2, a, b);
			sum3 = Inlines.MAC16_16(sum3, a, b2);
			a = x[x_ptr++];
			b3 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a, b4);
			sum1 = Inlines.MAC16_16(sum1, a, b);
			sum2 = Inlines.MAC16_16(sum2, a, b2);
			sum3 = Inlines.MAC16_16(sum3, a, b3);
			a = x[x_ptr++];
			b4 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a, b);
			sum1 = Inlines.MAC16_16(sum1, a, b2);
			sum2 = Inlines.MAC16_16(sum2, a, b3);
			sum3 = Inlines.MAC16_16(sum3, a, b4);
		}
		if (i++ < len)
		{
			short a2 = x[x_ptr++];
			b = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a2, b2);
			sum1 = Inlines.MAC16_16(sum1, a2, b3);
			sum2 = Inlines.MAC16_16(sum2, a2, b4);
			sum3 = Inlines.MAC16_16(sum3, a2, b);
		}
		if (i++ < len)
		{
			short a3 = x[x_ptr++];
			b2 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a3, b3);
			sum1 = Inlines.MAC16_16(sum1, a3, b4);
			sum2 = Inlines.MAC16_16(sum2, a3, b);
			sum3 = Inlines.MAC16_16(sum3, a3, b2);
		}
		if (i < len)
		{
			short a4 = x[x_ptr++];
			b3 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a4, b4);
			sum1 = Inlines.MAC16_16(sum1, a4, b);
			sum2 = Inlines.MAC16_16(sum2, a4, b2);
			sum3 = Inlines.MAC16_16(sum3, a4, b3);
		}
	}

	internal static void xcorr_kernel(int[] x, int[] y, int y_ptr, ref int sum0, ref int sum1, ref int sum2, ref int sum3, int len)
	{
		int num = 0;
		int b = 0;
		int b2 = y[y_ptr++];
		int b3 = y[y_ptr++];
		int b4 = y[y_ptr++];
		int i;
		for (i = 0; i < len - 3; i += 4)
		{
			int a = x[num++];
			b = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a, b2);
			sum1 = Inlines.MAC16_16(sum1, a, b3);
			sum2 = Inlines.MAC16_16(sum2, a, b4);
			sum3 = Inlines.MAC16_16(sum3, a, b);
			a = x[num++];
			b2 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a, b3);
			sum1 = Inlines.MAC16_16(sum1, a, b4);
			sum2 = Inlines.MAC16_16(sum2, a, b);
			sum3 = Inlines.MAC16_16(sum3, a, b2);
			a = x[num++];
			b3 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a, b4);
			sum1 = Inlines.MAC16_16(sum1, a, b);
			sum2 = Inlines.MAC16_16(sum2, a, b2);
			sum3 = Inlines.MAC16_16(sum3, a, b3);
			a = x[num++];
			b4 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a, b);
			sum1 = Inlines.MAC16_16(sum1, a, b2);
			sum2 = Inlines.MAC16_16(sum2, a, b3);
			sum3 = Inlines.MAC16_16(sum3, a, b4);
		}
		if (i++ < len)
		{
			int a2 = x[num++];
			b = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a2, b2);
			sum1 = Inlines.MAC16_16(sum1, a2, b3);
			sum2 = Inlines.MAC16_16(sum2, a2, b4);
			sum3 = Inlines.MAC16_16(sum3, a2, b);
		}
		if (i++ < len)
		{
			int a3 = x[num++];
			b2 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a3, b3);
			sum1 = Inlines.MAC16_16(sum1, a3, b4);
			sum2 = Inlines.MAC16_16(sum2, a3, b);
			sum3 = Inlines.MAC16_16(sum3, a3, b2);
		}
		if (i < len)
		{
			int a4 = x[num++];
			b3 = y[y_ptr++];
			sum0 = Inlines.MAC16_16(sum0, a4, b4);
			sum1 = Inlines.MAC16_16(sum1, a4, b);
			sum2 = Inlines.MAC16_16(sum2, a4, b2);
			sum3 = Inlines.MAC16_16(sum3, a4, b3);
		}
	}

	internal static int celt_inner_prod(short[] x, int x_ptr, short[] y, int y_ptr, int N)
	{
		int num = 0;
		for (int i = 0; i < N; i++)
		{
			num = Inlines.MAC16_16(num, x[x_ptr + i], y[y_ptr + i]);
		}
		return num;
	}

	internal static int celt_inner_prod(short[] x, short[] y, int y_ptr, int N)
	{
		int num = 0;
		for (int i = 0; i < N; i++)
		{
			num = Inlines.MAC16_16(num, x[i], y[y_ptr + i]);
		}
		return num;
	}

	internal static int celt_inner_prod(int[] x, int x_ptr, int[] y, int y_ptr, int N)
	{
		int num = 0;
		for (int i = 0; i < N; i++)
		{
			num = Inlines.MAC16_16(num, x[x_ptr + i], y[y_ptr + i]);
		}
		return num;
	}

	internal static void dual_inner_prod(int[] x, int x_ptr, int[] y01, int y01_ptr, int[] y02, int y02_ptr, int N, out int xy1, out int xy2)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < N; i++)
		{
			num = Inlines.MAC16_16(num, x[x_ptr + i], y01[y01_ptr + i]);
			num2 = Inlines.MAC16_16(num2, x[x_ptr + i], y02[y02_ptr + i]);
		}
		xy1 = num;
		xy2 = num2;
	}
}
