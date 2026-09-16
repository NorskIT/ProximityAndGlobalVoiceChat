using System;
namespace ProximityVoiceChat.Voice.Audio;

// This bundled Speex implementation is fixed-point even through its float API.
// Convert normalized Unity PCM explicitly, with reusable buffers (main thread).
internal sealed class NormalizedAudioResampler
{
    private readonly Concentus.Common.SpeexResampler _resampler;
    private readonly short[] _input, _output;
    internal readonly float[] Output;
    internal NormalizedAudioResampler(int inputRate, int outputRate, int maximumInput)
    {
        _resampler = new Concentus.Common.SpeexResampler(1, inputRate, outputRate, 10);
        _input = new short[maximumInput];
        _output = new short[(int)Math.Ceiling(maximumInput * (double)outputRate / inputRate) + 256];
        Output = new float[_output.Length];
    }
    internal int Process(float[] samples, int count)
    {
        for (int i = 0; i < count; i++) _input[i] = (short)Math.Round(Math.Max(-32768, Math.Min(32767, samples[i] * 32768.0)));
        int input = count, output = _output.Length;
        _resampler.Process(0, _input, 0, ref input, _output, 0, ref output);
        if (input != count) throw new InvalidOperationException("Audio resampler did not consume its input.");
        for (int i = 0; i < output; i++) Output[i] = _output[i] / 32768f;
        return output;
    }
}
