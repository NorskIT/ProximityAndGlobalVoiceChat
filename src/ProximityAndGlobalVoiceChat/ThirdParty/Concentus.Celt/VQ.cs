using Concentus.Common;

namespace Concentus.Celt;

internal static class VQ
{
	private static int[] SPREAD_FACTOR = new int[3] { 15, 10, 5 };

	internal static void exp_rotation1(int[] X, int X_ptr, int len, int stride, int c, int s)
	{
		int num = X_ptr;
		int a = Inlines.NEG16(s);
		for (int i = 0; i < len - stride; i++)
		{
			int b = X[num];
			int b2 = X[num + stride];
			X[num + stride] = Inlines.EXTRACT16(Inlines.PSHR32(Inlines.MAC16_16(Inlines.MULT16_16(c, b2), s, b), 15));
			X[num] = Inlines.EXTRACT16(Inlines.PSHR32(Inlines.MAC16_16(Inlines.MULT16_16(c, b), a, b2), 15));
			num++;
		}
		num = X_ptr + (len - 2 * stride - 1);
		for (int i = len - 2 * stride - 1; i >= 0; i--)
		{
			int b3 = X[num];
			int b4 = X[num + stride];
			X[num + stride] = Inlines.EXTRACT16(Inlines.PSHR32(Inlines.MAC16_16(Inlines.MULT16_16(c, b4), s, b3), 15));
			X[num] = Inlines.EXTRACT16(Inlines.PSHR32(Inlines.MAC16_16(Inlines.MULT16_16(c, b3), a, b4), 15));
			num--;
		}
	}

	internal static void exp_rotation(int[] X, int X_ptr, int len, int dir, int stride, int K, int spread)
	{
		int i = 0;
		if (2 * K >= len || spread == 0)
		{
			return;
		}
		int num = SPREAD_FACTOR[spread - 1];
		int num2 = Inlines.celt_div(Inlines.MULT16_16(32767, len), len + num * K);
		int num3 = Inlines.HALF16(Inlines.MULT16_16_Q15(num2, num2));
		int num4 = Inlines.celt_cos_norm(Inlines.EXTEND32(num3));
		int num5 = Inlines.celt_cos_norm(Inlines.EXTEND32(Inlines.SUB16(32767, num3)));
		if (len >= 8 * stride)
		{
			for (i = 1; (i * i + i) * stride + (stride >> 2) < len; i++)
			{
			}
		}
		len = Inlines.celt_udiv(len, stride);
		for (int j = 0; j < stride; j++)
		{
			if (dir < 0)
			{
				if (i != 0)
				{
					exp_rotation1(X, X_ptr + j * len, len, i, num5, num4);
				}
				exp_rotation1(X, X_ptr + j * len, len, 1, num4, num5);
			}
			else
			{
				exp_rotation1(X, X_ptr + j * len, len, 1, num4, (short)(-num5));
				if (i != 0)
				{
					exp_rotation1(X, X_ptr + j * len, len, i, num5, (short)(-num4));
				}
			}
		}
	}

	internal static void normalise_residual(int[] iy, int[] X, int X_ptr, int N, int Ryy, int gain)
	{
		int num = Inlines.celt_ilog2(Ryy) >> 1;
		int a = Inlines.MULT16_16_P15(Inlines.celt_rsqrt_norm(Inlines.VSHR32(Ryy, 2 * (num - 7))), gain);
		int num2 = 0;
		do
		{
			X[X_ptr + num2] = Inlines.EXTRACT16(Inlines.PSHR32(Inlines.MULT16_16(a, iy[num2]), num + 1));
		}
		while (++num2 < N);
	}

	internal static uint extract_collapse_mask(int[] iy, int N, int B)
	{
		if (B <= 1)
		{
			return 1u;
		}
		int num = Inlines.celt_udiv(N, B);
		uint num2 = 0u;
		int num3 = 0;
		do
		{
			uint num4 = 0u;
			int num5 = 0;
			do
			{
				num4 |= (uint)iy[num3 * num + num5];
			}
			while (++num5 < num);
			num2 |= (uint)(((num4 != 0) ? 1 : 0) << num3);
		}
		while (++num3 < B);
		return num2;
	}

	internal static uint alg_quant(int[] X, int X_ptr, int N, int K, int spread, int B, EntropyCoder enc)
	{
		int[] array = new int[N];
		int[] array2 = new int[N];
		int[] array3 = new int[N];
		exp_rotation(X, X_ptr, N, 1, B, K, spread);
		int num = 0;
		int num2 = 0;
		do
		{
			int num3 = X_ptr + num2;
			array3[num2] = ((X[num3] > 0) ? 1 : (-1));
			X[num3] = Inlines.ABS16(X[num3]);
			array2[num2] = 0;
			array[num2] = 0;
		}
		while (++num2 < N);
		int num5;
		int num4 = (num5 = 0);
		int num6 = K;
		if (K > N >> 1)
		{
			num2 = 0;
			do
			{
				num += X[X_ptr + num2];
			}
			while (++num2 < N);
			if (num <= K)
			{
				X[X_ptr] = 16384;
				num2 = X_ptr + 1;
				do
				{
					X[num2] = 0;
				}
				while (++num2 < N + X_ptr);
				num = 16384;
			}
			int b = Inlines.EXTRACT16(Inlines.MULT16_32_Q16(K - 1, Inlines.celt_rcp(num)));
			num2 = 0;
			do
			{
				array2[num2] = Inlines.MULT16_16_Q15(X[X_ptr + num2], b);
				array[num2] = array2[num2];
				num5 = Inlines.MAC16_16(num5, array[num2], array[num2]);
				num4 = Inlines.MAC16_16(num4, X[X_ptr + num2], array[num2]);
				array[num2] *= 2;
				num6 -= array2[num2];
			}
			while (++num2 < N);
		}
		if (num6 > N + 3)
		{
			int num7 = num6;
			num5 = Inlines.MAC16_16(num5, num7, num7);
			num5 = Inlines.MAC16_16(num5, num7, array[0]);
			array2[0] += num6;
			num6 = 0;
		}
		int num8 = 1;
		for (int i = 0; i < num6; i++)
		{
			int b2 = -32767;
			int a = 0;
			int shift = 1 + Inlines.celt_ilog2(K - num6 + i + 1);
			int num9 = 0;
			num5 = Inlines.ADD16(num5, 1);
			num2 = 0;
			do
			{
				int num10 = Inlines.EXTRACT16(Inlines.SHR32(Inlines.ADD32(num4, Inlines.EXTEND32(X[X_ptr + num2])), shift));
				int num11 = Inlines.ADD16(num5, array[num2]);
				num10 = Inlines.MULT16_16_Q15(num10, num10);
				if (Inlines.MULT16_16(a, num10) > Inlines.MULT16_16(num11, b2))
				{
					a = num11;
					b2 = num10;
					num9 = num2;
				}
			}
			while (++num2 < N);
			num4 = Inlines.ADD32(num4, Inlines.EXTEND32(X[X_ptr + num9]));
			num5 = Inlines.ADD16(num5, array[num9]);
			array[num9] += 2 * num8;
			array2[num9]++;
		}
		num2 = 0;
		do
		{
			X[X_ptr + num2] = Inlines.MULT16_16(array3[num2], X[X_ptr + num2]);
			array2[num2] = ((array3[num2] < 0) ? (-array2[num2]) : array2[num2]);
		}
		while (++num2 < N);
		CWRS.encode_pulses(array2, N, K, enc);
		return extract_collapse_mask(array2, N, B);
	}

	internal static uint alg_unquant(int[] X, int X_ptr, int N, int K, int spread, int B, EntropyCoder dec, int gain)
	{
		int[] array = new int[N];
		int ryy = CWRS.decode_pulses(array, N, K, dec);
		normalise_residual(array, X, X_ptr, N, ryy, gain);
		exp_rotation(X, X_ptr, N, -1, B, K, spread);
		return extract_collapse_mask(array, N, B);
	}

	internal static void renormalise_vector(int[] X, int X_ptr, int N, int gain)
	{
		int num = 1 + Kernels.celt_inner_prod(X, X_ptr, X, X_ptr, N);
		int num2 = Inlines.celt_ilog2(num) >> 1;
		int a = Inlines.MULT16_16_P15(Inlines.celt_rsqrt_norm(Inlines.VSHR32(num, 2 * (num2 - 7))), gain);
		int num3 = X_ptr;
		for (int i = 0; i < N; i++)
		{
			X[num3] = Inlines.EXTRACT16(Inlines.PSHR32(Inlines.MULT16_16(a, X[num3]), num2 + 1));
			num3++;
		}
	}

	internal static int stereo_itheta(int[] X, int X_ptr, int[] Y, int Y_ptr, int stereo, int N)
	{
		int num2;
		int num = (num2 = 1);
		if (stereo != 0)
		{
			for (int i = 0; i < N; i++)
			{
				int num3 = Inlines.ADD16(Inlines.SHR16(X[X_ptr + i], 1), Inlines.SHR16(Y[Y_ptr + i], 1));
				int num4 = Inlines.SUB16(Inlines.SHR16(X[X_ptr + i], 1), Inlines.SHR16(Y[Y_ptr + i], 1));
				num = Inlines.MAC16_16(num, num3, num3);
				num2 = Inlines.MAC16_16(num2, num4, num4);
			}
		}
		else
		{
			num += Kernels.celt_inner_prod(X, X_ptr, X, X_ptr, N);
			num2 += Kernels.celt_inner_prod(Y, Y_ptr, Y, Y_ptr, N);
		}
		int x = Inlines.celt_sqrt(num);
		int y = Inlines.celt_sqrt(num2);
		return Inlines.MULT16_16_Q15(20861, Inlines.celt_atan2p(y, x));
	}
}
