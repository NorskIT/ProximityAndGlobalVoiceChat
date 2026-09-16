using System;

namespace ProximityVoiceChat.Voice.Audio;

internal static class AudioFormat
{
	internal const int SampleRate = 48000;

	internal const int Channels = 1;

	internal const int FrameMs = 20;

	internal const int FrameSamples = 960;

	private const float SoftClipKnee = 0.6f;

	private const float SoftClipRange = 0.39999998f;

	internal static bool IsOpusRate(int rate)
	{
		if (rate != 8000 && rate != 12000 && rate != 16000 && rate != 24000)
		{
			return rate == 48000;
		}
		return true;
	}

	internal static float SoftClip(float sample)
	{
		float num = ((sample < 0f) ? (0f - sample) : sample);
		if (num <= 0.6f)
		{
			return sample;
		}
		float num2 = (num - 0.6f) / 0.39999998f;
		float num3 = 0.6f + 0.39999998f * (float)Math.Tanh(num2);
		if (!(sample < 0f))
		{
			return num3;
		}
		return 0f - num3;
	}
}
