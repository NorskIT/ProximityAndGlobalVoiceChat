using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class FindLPC
{
	internal static void silk_find_LPC(SilkChannelEncoder psEncC, short[] NLSF_Q15, short[] x, int minInvGain_Q30)
	{
		int[] a_Q = new int[16];
		BoxedValueInt boxedValueInt = new BoxedValueInt();
		BoxedValueInt boxedValueInt2 = new BoxedValueInt();
		int[] a_Q2 = new int[16];
		short[] array = new short[16];
		short[] array2 = new short[16];
		int num = psEncC.subfr_length + psEncC.predictLPCOrder;
		psEncC.indices.NLSFInterpCoef_Q2 = 4;
		BurgModified.silk_burg_modified(boxedValueInt, boxedValueInt2, a_Q, x, 0, minInvGain_Q30, num, psEncC.nb_subfr, psEncC.predictLPCOrder);
		int num2 = boxedValueInt.Val;
		int num3 = boxedValueInt2.Val;
		if (psEncC.useInterpolatedNLSFs != 0 && psEncC.first_frame_after_reset == 0 && psEncC.nb_subfr == 4)
		{
			BurgModified.silk_burg_modified(boxedValueInt, boxedValueInt2, a_Q2, x, 2 * num, minInvGain_Q30, num, 2, psEncC.predictLPCOrder);
			int val = boxedValueInt.Val;
			int val2 = boxedValueInt2.Val;
			int num4 = val2 - num3;
			if (num4 >= 0)
			{
				if (num4 < 32)
				{
					num2 -= Inlines.silk_RSHIFT(val, num4);
				}
			}
			else
			{
				num2 = Inlines.silk_RSHIFT(num2, -num4) - val;
				num3 = val2;
			}
			NLSF.silk_A2NLSF(NLSF_Q15, a_Q2, psEncC.predictLPCOrder);
			short[] array3 = new short[2 * num];
			for (int num5 = 3; num5 >= 0; num5--)
			{
				Inlines.silk_interpolate(array2, psEncC.prev_NLSFq_Q15, NLSF_Q15, num5, psEncC.predictLPCOrder);
				NLSF.silk_NLSF2A(array, array2, psEncC.predictLPCOrder);
				Filters.silk_LPC_analysis_filter(array3, 0, x, 0, array, 0, 2 * num, psEncC.predictLPCOrder);
				SumSqrShift.silk_sum_sqr_shift(out var energy, out var shift, array3, psEncC.predictLPCOrder, num - psEncC.predictLPCOrder);
				SumSqrShift.silk_sum_sqr_shift(out var energy2, out var shift2, array3, psEncC.predictLPCOrder + num, num - psEncC.predictLPCOrder);
				num4 = shift - shift2;
				int num6;
				if (num4 >= 0)
				{
					energy2 = Inlines.silk_RSHIFT(energy2, num4);
					num6 = -shift;
				}
				else
				{
					energy = Inlines.silk_RSHIFT(energy, -num4);
					num6 = -shift2;
				}
				int num7 = Inlines.silk_ADD32(energy, energy2);
				num4 = num6 - num3;
				int num8 = ((num4 >= 0) ? ((Inlines.silk_RSHIFT(num7, num4) < num2) ? 1 : 0) : ((-num4 < 32) ? ((num7 < Inlines.silk_RSHIFT(num2, -num4)) ? 1 : 0) : 0));
				if (num8 == 1)
				{
					num2 = num7;
					num3 = num6;
					psEncC.indices.NLSFInterpCoef_Q2 = (sbyte)num5;
				}
			}
		}
		if (psEncC.indices.NLSFInterpCoef_Q2 == 4)
		{
			NLSF.silk_A2NLSF(NLSF_Q15, a_Q, psEncC.predictLPCOrder);
		}
	}
}
