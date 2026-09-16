using Concentus.Common;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class EncodeIndices
{
	internal static void silk_encode_indices(SilkChannelEncoder psEncC, EntropyCoder psRangeEnc, int FrameIndex, int encode_LBRR, int condCoding)
	{
		short[] array = new short[16];
		byte[] pred_Q = new byte[16];
		SideInfoIndices sideInfoIndices = ((encode_LBRR == 0) ? psEncC.indices : psEncC.indices_LBRR[FrameIndex]);
		int num = 2 * sideInfoIndices.signalType + sideInfoIndices.quantOffsetType;
		if (encode_LBRR != 0 || num >= 2)
		{
			psRangeEnc.enc_icdf(num - 2, Tables.silk_type_offset_VAD_iCDF, 8u);
		}
		else
		{
			psRangeEnc.enc_icdf(num, Tables.silk_type_offset_no_VAD_iCDF, 8u);
		}
		if (condCoding == 2)
		{
			psRangeEnc.enc_icdf(sideInfoIndices.GainsIndices[0], Tables.silk_delta_gain_iCDF, 8u);
		}
		else
		{
			psRangeEnc.enc_icdf(Inlines.silk_RSHIFT(sideInfoIndices.GainsIndices[0], 3), Tables.silk_gain_iCDF[sideInfoIndices.signalType], 8u);
			psRangeEnc.enc_icdf(sideInfoIndices.GainsIndices[0] & 7, Tables.silk_uniform8_iCDF, 8u);
		}
		for (int i = 1; i < psEncC.nb_subfr; i++)
		{
			psRangeEnc.enc_icdf(sideInfoIndices.GainsIndices[i], Tables.silk_delta_gain_iCDF, 8u);
		}
		psRangeEnc.enc_icdf(sideInfoIndices.NLSFIndices[0], psEncC.psNLSF_CB.CB1_iCDF, (sideInfoIndices.signalType >> 1) * psEncC.psNLSF_CB.nVectors, 8u);
		NLSF.silk_NLSF_unpack(array, pred_Q, psEncC.psNLSF_CB, sideInfoIndices.NLSFIndices[0]);
		for (int i = 0; i < psEncC.psNLSF_CB.order; i++)
		{
			if (sideInfoIndices.NLSFIndices[i + 1] >= 4)
			{
				psRangeEnc.enc_icdf(8, psEncC.psNLSF_CB.ec_iCDF, array[i], 8u);
				psRangeEnc.enc_icdf(sideInfoIndices.NLSFIndices[i + 1] - 4, Tables.silk_NLSF_EXT_iCDF, 8u);
			}
			else if (sideInfoIndices.NLSFIndices[i + 1] <= -4)
			{
				psRangeEnc.enc_icdf(0, psEncC.psNLSF_CB.ec_iCDF, array[i], 8u);
				psRangeEnc.enc_icdf(-sideInfoIndices.NLSFIndices[i + 1] - 4, Tables.silk_NLSF_EXT_iCDF, 8u);
			}
			else
			{
				psRangeEnc.enc_icdf(sideInfoIndices.NLSFIndices[i + 1] + 4, psEncC.psNLSF_CB.ec_iCDF, array[i], 8u);
			}
		}
		if (psEncC.nb_subfr == 4)
		{
			psRangeEnc.enc_icdf(sideInfoIndices.NLSFInterpCoef_Q2, Tables.silk_NLSF_interpolation_factor_iCDF, 8u);
		}
		if (sideInfoIndices.signalType == 2)
		{
			int num2 = 1;
			if (condCoding == 2 && psEncC.ec_prevSignalType == 2)
			{
				int num3 = sideInfoIndices.lagIndex - psEncC.ec_prevLagIndex;
				if (num3 < -8 || num3 > 11)
				{
					num3 = 0;
				}
				else
				{
					num3 += 9;
					num2 = 0;
				}
				psRangeEnc.enc_icdf(num3, Tables.silk_pitch_delta_iCDF, 8u);
			}
			if (num2 != 0)
			{
				int num4 = Inlines.silk_DIV32_16(sideInfoIndices.lagIndex, Inlines.silk_RSHIFT(psEncC.fs_kHz, 1));
				int s = sideInfoIndices.lagIndex - Inlines.silk_SMULBB(num4, Inlines.silk_RSHIFT(psEncC.fs_kHz, 1));
				psRangeEnc.enc_icdf(num4, Tables.silk_pitch_lag_iCDF, 8u);
				psRangeEnc.enc_icdf(s, psEncC.pitch_lag_low_bits_iCDF, 8u);
			}
			psEncC.ec_prevLagIndex = sideInfoIndices.lagIndex;
			psRangeEnc.enc_icdf(sideInfoIndices.contourIndex, psEncC.pitch_contour_iCDF, 8u);
			psRangeEnc.enc_icdf(sideInfoIndices.PERIndex, Tables.silk_LTP_per_index_iCDF, 8u);
			for (int j = 0; j < psEncC.nb_subfr; j++)
			{
				psRangeEnc.enc_icdf(sideInfoIndices.LTPIndex[j], Tables.silk_LTP_gain_iCDF_ptrs[sideInfoIndices.PERIndex], 8u);
			}
			if (condCoding == 0)
			{
				psRangeEnc.enc_icdf(sideInfoIndices.LTP_scaleIndex, Tables.silk_LTPscale_iCDF, 8u);
			}
		}
		psEncC.ec_prevSignalType = sideInfoIndices.signalType;
		psRangeEnc.enc_icdf(sideInfoIndices.Seed, Tables.silk_uniform4_iCDF, 8u);
	}
}
