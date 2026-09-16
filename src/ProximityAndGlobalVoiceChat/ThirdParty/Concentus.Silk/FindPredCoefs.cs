using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class FindPredCoefs
{
	internal static void silk_find_pred_coefs(SilkChannelEncoder psEnc, SilkEncoderControl psEncCtrl, short[] res_pitch, short[] x, int x_ptr, int condCoding)
	{
		int[] array = new int[4];
		int[] array2 = new int[4];
		int[] array3 = new int[4];
		short[] array4 = new short[16];
		int[] corr_rshifts = new int[4];
		int num = 33554431;
		for (int i = 0; i < psEnc.nb_subfr; i++)
		{
			num = Inlines.silk_min(num, psEncCtrl.Gains_Q16[i]);
		}
		for (int i = 0; i < psEnc.nb_subfr; i++)
		{
			array[i] = Inlines.silk_DIV32_varQ(num, psEncCtrl.Gains_Q16[i], 14);
			array[i] = Inlines.silk_max(array[i], 363);
			int a = Inlines.silk_SMULWB(array[i], array[i]);
			array3[i] = Inlines.silk_RSHIFT(a, 1);
			array2[i] = Inlines.silk_DIV32(65536, array[i]);
		}
		short[] array5 = new short[psEnc.nb_subfr * psEnc.predictLPCOrder + psEnc.frame_length];
		if (psEnc.indices.signalType == 2)
		{
			int[] array6 = new int[psEnc.nb_subfr * 5 * 5];
			BoxedValueInt boxedValueInt = new BoxedValueInt(psEncCtrl.LTPredCodGain_Q7);
			FindLTP.silk_find_LTP(psEncCtrl.LTPCoef_Q14, array6, boxedValueInt, res_pitch, psEncCtrl.pitchL, array3, psEnc.subfr_length, psEnc.nb_subfr, psEnc.ltp_mem_length, corr_rshifts);
			psEncCtrl.LTPredCodGain_Q7 = boxedValueInt.Val;
			BoxedValueSbyte boxedValueSbyte = new BoxedValueSbyte(psEnc.indices.PERIndex);
			BoxedValueInt boxedValueInt2 = new BoxedValueInt(psEnc.sum_log_gain_Q7);
			QuantizeLTPGains.silk_quant_LTP_gains(psEncCtrl.LTPCoef_Q14, psEnc.indices.LTPIndex, boxedValueSbyte, boxedValueInt2, array6, psEnc.mu_LTP_Q9, psEnc.LTPQuantLowComplexity, psEnc.nb_subfr);
			psEnc.indices.PERIndex = boxedValueSbyte.Val;
			psEnc.sum_log_gain_Q7 = boxedValueInt2.Val;
			LTPScaleControl.silk_LTP_scale_ctrl(psEnc, psEncCtrl, condCoding);
			LTPAnalysisFilter.silk_LTP_analysis_filter(array5, x, x_ptr - psEnc.predictLPCOrder, psEncCtrl.LTPCoef_Q14, psEncCtrl.pitchL, array, psEnc.subfr_length, psEnc.nb_subfr, psEnc.predictLPCOrder);
		}
		else
		{
			int num2 = x_ptr - psEnc.predictLPCOrder;
			int num3 = 0;
			for (int i = 0; i < psEnc.nb_subfr; i++)
			{
				Inlines.silk_scale_copy_vector16(array5, num3, x, num2, array[i], psEnc.subfr_length + psEnc.predictLPCOrder);
				num3 += psEnc.subfr_length + psEnc.predictLPCOrder;
				num2 += psEnc.subfr_length;
			}
			Arrays.MemSetShort(psEncCtrl.LTPCoef_Q14, 0, psEnc.nb_subfr * 5);
			psEncCtrl.LTPredCodGain_Q7 = 0;
			psEnc.sum_log_gain_Q7 = 0;
		}
		int minInvGain_Q;
		if (psEnc.first_frame_after_reset != 0)
		{
			minInvGain_Q = 10737418;
		}
		else
		{
			minInvGain_Q = Inlines.silk_log2lin(Inlines.silk_SMLAWB(2048, psEncCtrl.LTPredCodGain_Q7, 21845));
			minInvGain_Q = Inlines.silk_DIV32_varQ(minInvGain_Q, Inlines.silk_SMULWW(10000, Inlines.silk_SMLAWB(65536, 196608, psEncCtrl.coding_quality_Q14)), 14);
		}
		FindLPC.silk_find_LPC(psEnc, array4, array5, minInvGain_Q);
		NLSF.silk_process_NLSFs(psEnc, psEncCtrl.PredCoef_Q12, array4, psEnc.prev_NLSFq_Q15);
		ResidualEnergy.silk_residual_energy(psEncCtrl.ResNrg, psEncCtrl.ResNrgQ, array5, psEncCtrl.PredCoef_Q12, array2, psEnc.subfr_length, psEnc.nb_subfr, psEnc.predictLPCOrder);
		Array.Copy(array4, psEnc.prev_NLSFq_Q15, 16);
	}
}
