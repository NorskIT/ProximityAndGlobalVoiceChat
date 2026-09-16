namespace Concentus.Silk.Structs;

internal class NLSFCodebook
{
	internal short nVectors;

	internal short order;

	internal short quantStepSize_Q16;

	internal short invQuantStepSize_Q6;

	internal byte[] CB1_NLSF_Q8;

	internal byte[] CB1_iCDF;

	internal byte[] pred_Q8;

	internal byte[] ec_sel;

	internal byte[] ec_iCDF;

	internal byte[] ec_Rates_Q5;

	internal short[] deltaMin_Q15;

	internal void Reset()
	{
		nVectors = 0;
		order = 0;
		quantStepSize_Q16 = 0;
		invQuantStepSize_Q6 = 0;
		CB1_NLSF_Q8 = null;
		CB1_iCDF = null;
		pred_Q8 = null;
		ec_sel = null;
		ec_iCDF = null;
		ec_Rates_Q5 = null;
		deltaMin_Q15 = null;
	}
}
