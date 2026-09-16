namespace Concentus.Silk.Structs;

internal class SilkDecoder
{
	internal readonly SilkChannelDecoder[] channel_state = new SilkChannelDecoder[2];

	internal readonly StereoDecodeState sStereo = new StereoDecodeState();

	internal int nChannelsAPI;

	internal int nChannelsInternal;

	internal int prev_decode_only_middle;

	internal SilkDecoder()
	{
		for (int i = 0; i < 2; i++)
		{
			channel_state[i] = new SilkChannelDecoder();
		}
	}

	internal void Reset()
	{
		for (int i = 0; i < 2; i++)
		{
			channel_state[i].Reset();
		}
		sStereo.Reset();
		nChannelsAPI = 0;
		nChannelsInternal = 0;
		prev_decode_only_middle = 0;
	}
}
