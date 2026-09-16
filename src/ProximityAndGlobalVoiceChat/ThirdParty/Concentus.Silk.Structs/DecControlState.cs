namespace Concentus.Silk.Structs;

internal class DecControlState
{
	internal int nChannelsAPI;

	internal int nChannelsInternal;

	internal int API_sampleRate;

	internal int internalSampleRate;

	internal int payloadSize_ms;

	internal int prevPitchLag;

	internal void Reset()
	{
		nChannelsAPI = 0;
		nChannelsInternal = 0;
		API_sampleRate = 0;
		internalSampleRate = 0;
		payloadSize_ms = 0;
		prevPitchLag = 0;
	}
}
