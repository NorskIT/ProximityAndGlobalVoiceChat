using Concentus.Common;

namespace Concentus.Celt;

internal static class CeltPitchXCorr
{
	internal static int pitch_xcorr(int[] _x, int[] _y, int[] xcorr, int len, int max_pitch)
	{
		int num = 1;
		int i;
		for (i = 0; i < max_pitch - 3; i += 4)
		{
			int sum = 0;
			int sum2 = 0;
			int sum3 = 0;
			int sum4 = 0;
			Kernels.xcorr_kernel(_x, _y, i, ref sum, ref sum2, ref sum3, ref sum4, len);
			xcorr[i] = sum;
			xcorr[i + 1] = sum2;
			xcorr[i + 2] = sum3;
			xcorr[i + 3] = sum4;
			sum = Inlines.MAX32(sum, sum2);
			sum3 = Inlines.MAX32(sum3, sum4);
			sum = Inlines.MAX32(sum, sum3);
			num = Inlines.MAX32(num, sum);
		}
		for (; i < max_pitch; i++)
		{
			num = Inlines.MAX32(num, xcorr[i] = Kernels.celt_inner_prod(_x, 0, _y, i, len));
		}
		return num;
	}

	internal static int pitch_xcorr(short[] _x, int _x_ptr, short[] _y, int _y_ptr, int[] xcorr, int len, int max_pitch)
	{
		int num = 1;
		int i;
		for (i = 0; i < max_pitch - 3; i += 4)
		{
			int sum = 0;
			int sum2 = 0;
			int sum3 = 0;
			int sum4 = 0;
			Kernels.xcorr_kernel(_x, _x_ptr, _y, _y_ptr + i, ref sum, ref sum2, ref sum3, ref sum4, len);
			xcorr[i] = sum;
			xcorr[i + 1] = sum2;
			xcorr[i + 2] = sum3;
			xcorr[i + 3] = sum4;
			sum = Inlines.MAX32(sum, sum2);
			sum3 = Inlines.MAX32(sum3, sum4);
			sum = Inlines.MAX32(sum, sum3);
			num = Inlines.MAX32(num, sum);
		}
		for (; i < max_pitch; i++)
		{
			num = Inlines.MAX32(num, xcorr[i] = Kernels.celt_inner_prod(_x, _x_ptr, _y, _y_ptr + i, len));
		}
		return num;
	}

	internal static int pitch_xcorr(short[] _x, short[] _y, int[] xcorr, int len, int max_pitch)
	{
		int num = 1;
		int i;
		for (i = 0; i < max_pitch - 3; i += 4)
		{
			int sum = 0;
			int sum2 = 0;
			int sum3 = 0;
			int sum4 = 0;
			Kernels.xcorr_kernel(_x, 0, _y, i, ref sum, ref sum2, ref sum3, ref sum4, len);
			xcorr[i] = sum;
			xcorr[i + 1] = sum2;
			xcorr[i + 2] = sum3;
			xcorr[i + 3] = sum4;
			sum = Inlines.MAX32(sum, sum2);
			sum3 = Inlines.MAX32(sum3, sum4);
			sum = Inlines.MAX32(sum, sum3);
			num = Inlines.MAX32(num, sum);
		}
		for (; i < max_pitch; i++)
		{
			num = Inlines.MAX32(num, xcorr[i] = Kernels.celt_inner_prod(_x, _y, i, len));
		}
		return num;
	}
}
