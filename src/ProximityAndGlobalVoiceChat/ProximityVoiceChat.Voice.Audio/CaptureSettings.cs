namespace ProximityVoiceChat.Voice.Audio;
// Immutable snapshots travel with captured samples, never inferred at send time.
internal sealed class CaptureSettings
{
    internal readonly int Generation;
    internal readonly VoiceChannel Channel;
    internal readonly bool Activation, Noise, AutoGain, RawMonitor, Dtx;
    internal readonly float Gain, Threshold, Target;
    internal readonly int Bitrate, Loss;
    internal CaptureSettings(int generation, VoiceChannel channel, bool activation, bool noise, bool autoGain, bool rawMonitor,
        float gain, float threshold, float target, int bitrate, int loss, bool dtx)
    { Generation = generation; Channel = channel; Activation = activation; Noise = noise; AutoGain = autoGain; RawMonitor = rawMonitor;
      Gain = gain; Threshold = threshold; Target = target; Bitrate = bitrate; Loss = loss; Dtx = dtx; }
}
