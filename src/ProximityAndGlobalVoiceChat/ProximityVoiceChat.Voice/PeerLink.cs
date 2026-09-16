using UnityEngine;

namespace ProximityVoiceChat.Voice;

internal sealed class PeerLink
{
	internal float LastHello;
	internal bool Compatible;
	internal uint TxStream, TxEncodedStream;
	internal int TxGeneration = -1;
	internal ushort TxSequence;
	internal bool TxActive;
	internal VoiceChannel TxChannel, RxChannel;
	internal uint RxStream;
	private const float TransmitHold = 0.3f;

	internal ushort LastSequence;

	internal int Lost;

	internal int Received;

	internal byte ReportedLossPercent;

	internal bool InRange;

	internal float Distance;

	internal float LastFrameTime;

	internal bool Transmitting
	{
		get
		{
			if (LastFrameTime > 0f)
			{
				return Time.unscaledTime - LastFrameTime < 0.3f;
			}
			return false;
		}
	}
}
