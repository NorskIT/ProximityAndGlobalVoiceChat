using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class FindPitchLags
{
	internal static void silk_find_pitch_lags(SilkChannelEncoder psEnc, SilkEncoderControl psEncCtrl, short[] res, short[] x, int x_ptr)
	{
		int[] array = new int[17];
		short[] rc_Q = new short[16];
		int[] array2 = new int[16];
		short[] array3 = new short[16];
		int num = psEnc.la_pitch + psEnc.frame_length + psEnc.ltp_mem_length;
		int num2 = x_ptr - psEnc.ltp_mem_length;
		short[] array4 = new short[psEnc.pitch_LPC_win_length];
		int num3 = num2 + num - psEnc.pitch_LPC_win_length;
		int num4 = 0;
		ApplySineWindow.silk_apply_sine_window(array4, num4, x, num3, 1, psEnc.la_pitch);
		num4 += psEnc.la_pitch;
		num3 += psEnc.la_pitch;
		Array.Copy(x, num3, array4, num4, psEnc.pitch_LPC_win_length - Inlines.silk_LSHIFT(psEnc.la_pitch, 1));
		num4 += psEnc.pitch_LPC_win_length - Inlines.silk_LSHIFT(psEnc.la_pitch, 1);
		num3 += psEnc.pitch_LPC_win_length - Inlines.silk_LSHIFT(psEnc.la_pitch, 1);
		ApplySineWindow.silk_apply_sine_window(array4, num4, x, num3, 2, psEnc.la_pitch);
		BoxedValueInt boxedValueInt = new BoxedValueInt();
		Autocorrelation.silk_autocorr(array, boxedValueInt, array4, psEnc.pitch_LPC_win_length, psEnc.pitchEstimationLPCOrder + 1);
		_ = boxedValueInt.Val;
		array[0] = Inlines.silk_SMLAWB(array[0], array[0], 66) + 1;
		int a = Schur.silk_schur(rc_Q, array, psEnc.pitchEstimationLPCOrder);
		psEncCtrl.predGain_Q16 = Inlines.silk_DIV32_varQ(array[0], Inlines.silk_max_int(a, 1), 16);
		K2A.silk_k2a(array2, rc_Q, psEnc.pitchEstimationLPCOrder);
		for (int i = 0; i < psEnc.pitchEstimationLPCOrder; i++)
		{
			array3[i] = (short)Inlines.silk_SAT16(Inlines.silk_RSHIFT(array2[i], 12));
		}
		BWExpander.silk_bwexpander(array3, psEnc.pitchEstimationLPCOrder, 64881);
		Filters.silk_LPC_analysis_filter(res, 0, x, num2, array3, 0, num, psEnc.pitchEstimationLPCOrder);
		if (psEnc.indices.signalType != 0 && psEnc.first_frame_after_reset == 0)
		{
			int a2 = 4915;
			a2 = Inlines.silk_SMLABB(a2, -32, psEnc.pitchEstimationLPCOrder);
			a2 = Inlines.silk_SMLAWB(a2, -209714, psEnc.speech_activity_Q8);
			a2 = Inlines.silk_SMLABB(a2, -1228, Inlines.silk_RSHIFT(psEnc.prevSignalType, 1));
			a2 = Inlines.silk_SMLAWB(a2, -1637, psEnc.input_tilt_Q15);
			a2 = Inlines.silk_SAT16(a2);
			BoxedValueShort boxedValueShort = new BoxedValueShort(psEnc.indices.lagIndex);
			BoxedValueSbyte boxedValueSbyte = new BoxedValueSbyte(psEnc.indices.contourIndex);
			BoxedValueInt boxedValueInt2 = new BoxedValueInt(psEnc.LTPCorr_Q15);
			if (PitchAnalysisCore.silk_pitch_analysis_core(res, psEncCtrl.pitchL, boxedValueShort, boxedValueSbyte, boxedValueInt2, psEnc.prevLag, psEnc.pitchEstimationThreshold_Q16, a2, psEnc.fs_kHz, psEnc.pitchEstimationComplexity, psEnc.nb_subfr) == 0)
			{
				psEnc.indices.signalType = 2;
			}
			else
			{
				psEnc.indices.signalType = 1;
			}
			psEnc.indices.lagIndex = boxedValueShort.Val;
			psEnc.indices.contourIndex = boxedValueSbyte.Val;
			psEnc.LTPCorr_Q15 = boxedValueInt2.Val;
		}
		else
		{
			Arrays.MemSetInt(psEncCtrl.pitchL, 0, 4);
			psEnc.indices.lagIndex = 0;
			psEnc.indices.contourIndex = 0;
			psEnc.LTPCorr_Q15 = 0;
		}
	}
}
