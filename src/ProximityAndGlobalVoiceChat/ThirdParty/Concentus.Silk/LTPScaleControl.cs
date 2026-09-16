using Concentus.Common;
using Concentus.Silk.Structs;

namespace Concentus.Silk;

internal static class LTPScaleControl
{
	internal static void silk_LTP_scale_ctrl(SilkChannelEncoder psEnc, SilkEncoderControl psEncCtrl, int condCoding)
	{
		if (condCoding == 0)
		{
			int a = psEnc.PacketLoss_perc + psEnc.nFramesPerPacket;
			psEnc.indices.LTP_scaleIndex = (sbyte)Inlines.silk_LIMIT(Inlines.silk_SMULWB(Inlines.silk_SMULBB(a, psEncCtrl.LTPredCodGain_Q7), 51), 0, 2);
		}
		else
		{
			psEnc.indices.LTP_scaleIndex = 0;
		}
		psEncCtrl.LTP_scale_Q14 = Tables.silk_LTPScales_table_Q14[psEnc.indices.LTP_scaleIndex];
	}
}
