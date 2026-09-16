using Concentus.Common;

namespace Concentus.Silk.Structs;

internal class SilkEncoder
{
	internal readonly SilkChannelEncoder[] state_Fxx = new SilkChannelEncoder[2];

	internal readonly StereoEncodeState sStereo = new StereoEncodeState();

	internal int nBitsUsedLBRR;

	internal int nBitsExceeded;

	internal int nChannelsAPI;

	internal int nChannelsInternal;

	internal int nPrevChannelsInternal;

	internal int timeSinceSwitchAllowed_ms;

	internal int allowBandwidthSwitch;

	internal int prev_decode_only_middle;

	internal SilkEncoder()
	{
		for (int i = 0; i < 2; i++)
		{
			state_Fxx[i] = new SilkChannelEncoder();
		}
	}

	internal void Reset()
	{
		for (int i = 0; i < 2; i++)
		{
			state_Fxx[i].Reset();
		}
		sStereo.Reset();
		nBitsUsedLBRR = 0;
		nBitsExceeded = 0;
		nChannelsAPI = 0;
		nChannelsInternal = 0;
		nPrevChannelsInternal = 0;
		timeSinceSwitchAllowed_ms = 0;
		allowBandwidthSwitch = 0;
		prev_decode_only_middle = 0;
	}

	internal static int silk_init_encoder(SilkChannelEncoder psEnc)
	{
		psEnc.Reset();
		psEnc.variable_HP_smth1_Q15 = Inlines.silk_LSHIFT(Inlines.silk_lin2log(3932160) - 2048, 8);
		psEnc.variable_HP_smth2_Q15 = psEnc.variable_HP_smth1_Q15;
		psEnc.first_frame_after_reset = 1;
		return 0 + VoiceActivityDetection.silk_VAD_Init(psEnc.sVAD);
	}
}
