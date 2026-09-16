using System;
using Concentus.Common;
using Concentus.Common.CPlusPlus;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class ProcessGains
{
	internal static void silk_process_gains(SilkChannelEncoder psEnc, SilkEncoderControl psEncCtrl, int condCoding)
	{
		SilkShapeState sShape = psEnc.sShape;
		if (psEnc.indices.signalType == 2)
		{
			int c = -Sigmoid.silk_sigm_Q15(Inlines.silk_RSHIFT_ROUND(psEncCtrl.LTPredCodGain_Q7 - 1536, 4));
			for (int i = 0; i < psEnc.nb_subfr; i++)
			{
				psEncCtrl.Gains_Q16[i] = Inlines.silk_SMLAWB(psEncCtrl.Gains_Q16[i], psEncCtrl.Gains_Q16[i], c);
			}
		}
		int b = Inlines.silk_DIV32_16(Inlines.silk_log2lin(Inlines.silk_SMULWB(8894 - psEnc.SNR_dB_Q7, 21627)), psEnc.subfr_length);
		for (int i = 0; i < psEnc.nb_subfr; i++)
		{
			int num = Inlines.silk_SMULWW(psEncCtrl.ResNrg[i], b);
			num = ((psEncCtrl.ResNrgQ[i] > 0) ? Inlines.silk_RSHIFT_ROUND(num, psEncCtrl.ResNrgQ[i]) : ((num < Inlines.silk_RSHIFT(int.MaxValue, -psEncCtrl.ResNrgQ[i])) ? Inlines.silk_LSHIFT(num, -psEncCtrl.ResNrgQ[i]) : int.MaxValue));
			int num2 = psEncCtrl.Gains_Q16[i];
			int num3 = Inlines.silk_ADD_SAT32(num, Inlines.silk_SMMUL(num2, num2));
			if (num3 < 32767)
			{
				num3 = Inlines.silk_SMLAWW(Inlines.silk_LSHIFT(num, 16), num2, num2);
				num2 = Inlines.silk_SQRT_APPROX(num3);
				num2 = Inlines.silk_min(num2, 8388607);
				psEncCtrl.Gains_Q16[i] = Inlines.silk_LSHIFT_SAT32(num2, 8);
			}
			else
			{
				num2 = Inlines.silk_SQRT_APPROX(num3);
				num2 = Inlines.silk_min(num2, 32767);
				psEncCtrl.Gains_Q16[i] = Inlines.silk_LSHIFT_SAT32(num2, 16);
			}
		}
		Array.Copy(psEncCtrl.Gains_Q16, psEncCtrl.GainsUnq_Q16, psEnc.nb_subfr);
		psEncCtrl.lastGainIndexPrev = sShape.LastGainIndex;
		BoxedValueSbyte boxedValueSbyte = new BoxedValueSbyte(sShape.LastGainIndex);
		GainQuantization.silk_gains_quant(psEnc.indices.GainsIndices, psEncCtrl.Gains_Q16, boxedValueSbyte, (condCoding == 2) ? 1 : 0, psEnc.nb_subfr);
		sShape.LastGainIndex = boxedValueSbyte.Val;
		if (psEnc.indices.signalType == 2)
		{
			if (psEncCtrl.LTPredCodGain_Q7 + Inlines.silk_RSHIFT(psEnc.input_tilt_Q15, 8) > 128)
			{
				psEnc.indices.quantOffsetType = 0;
			}
			else
			{
				psEnc.indices.quantOffsetType = 1;
			}
		}
		int b2 = Tables.silk_Quantization_Offsets_Q10[psEnc.indices.signalType >> 1][psEnc.indices.quantOffsetType];
		psEncCtrl.Lambda_Q10 = 1229 + Inlines.silk_SMULBB(-50, psEnc.nStatesDelayedDecision) + Inlines.silk_SMULWB(-52428, psEnc.speech_activity_Q8) + Inlines.silk_SMULWB(-409, psEncCtrl.input_quality_Q14) + Inlines.silk_SMULWB(-818, psEncCtrl.coding_quality_Q14) + Inlines.silk_SMULWB(52429, b2);
	}
}
