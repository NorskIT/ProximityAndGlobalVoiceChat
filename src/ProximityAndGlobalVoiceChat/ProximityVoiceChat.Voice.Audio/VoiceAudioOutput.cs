using System;
using UnityEngine;
namespace ProximityVoiceChat.Voice.Audio;

// First filter on a clipless AudioSource: Unity requests actual DSP blocks, not
// speculative streamed-clip read-ahead. Keep this before environmental filters.
internal sealed class VoiceAudioOutput : MonoBehaviour
{
    internal volatile PlaybackBuffer? Buffer;
    internal volatile float Gain = 1f;
    private void OnAudioFilterRead(float[] data, int channels)
    {
        var buffer = Buffer;
        if (buffer == null) Array.Clear(data, 0, data.Length);
        else buffer.Read(data, channels, Gain);
    }
}
