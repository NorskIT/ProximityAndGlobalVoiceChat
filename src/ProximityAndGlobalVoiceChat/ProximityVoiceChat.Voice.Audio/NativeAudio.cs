using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
namespace ProximityVoiceChat.Voice.Audio;

internal static class NativeAudio
{
    private static readonly object Sync = new object();
    private static bool _loaded;
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr LoadLibraryW(string path);
    internal static void Ensure()
    {
        lock (Sync)
        {
            if (_loaded) return;
            if (IntPtr.Size != 8 || Environment.OSVersion.Platform != PlatformID.Win32NT) throw new PlatformNotSupportedException("High quality voice requires Windows x64.");
            string directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Native");
            foreach (string name in new[] { "pagvc_opus.dll", "pagvc_rnnoise.dll" })
            {
                string file = Path.Combine(directory, name);
                if (!File.Exists(file) || LoadLibraryW(file) == IntPtr.Zero) throw new InvalidOperationException("Voice library could not load: " + file + " (Windows error " + Marshal.GetLastWin32Error() + "). Reinstall the complete package.");
            }
            _loaded = true;
        }
    }
}
internal sealed class NativeOpus : IDisposable
{
    private IntPtr _handle;
    private readonly bool _encoder;
    private const CallingConvention CC = CallingConvention.Cdecl;
    [DllImport("pagvc_opus", CallingConvention = CC)] private static extern IntPtr opus_encoder_create(int rate, int channels, int application, out int error);
    [DllImport("pagvc_opus", CallingConvention = CC)] private static extern IntPtr opus_decoder_create(int rate, int channels, out int error);
    [DllImport("pagvc_opus", CallingConvention = CC)] private static extern void opus_encoder_destroy(IntPtr state);
    [DllImport("pagvc_opus", CallingConvention = CC)] private static extern void opus_decoder_destroy(IntPtr state);
    [DllImport("pagvc_opus", CallingConvention = CC)] private static extern int opus_encoder_ctl(IntPtr state, int request, int value);
    [DllImport("pagvc_opus", CallingConvention = CC)] private static extern int opus_encode_float(IntPtr state, float[] pcm, int frameSize, byte[] data, int maxBytes);
    [DllImport("pagvc_opus", CallingConvention = CC)] private static extern int opus_decode_float(IntPtr state, byte[]? data, int len, float[] pcm, int frameSize, int fec);
    [DllImport("pagvc_opus", CallingConvention = CC)] private static extern IntPtr opus_get_version_string();
    internal static string Version { get { NativeAudio.Ensure(); return Marshal.PtrToStringAnsi(opus_get_version_string())!; } }
    internal NativeOpus(bool encoder)
    {
        NativeAudio.Ensure(); _encoder = encoder;
        int error;
        _handle = encoder ? opus_encoder_create(48000, 1, 2048, out error) : opus_decoder_create(48000, 1, out error);
        Check(error);
        if (_handle == IntPtr.Zero) throw new InvalidOperationException("Opus allocation failed.");
        if (encoder)
        {
            Set(4010, 10); Set(4004, 1105); Set(4008, -1000); // complexity 10, fullband allowed, automatic mode
            Set(4024, 3001); Set(4002, 96000); Set(4006, 1); // voice, 96k, VBR
            Set(4012, 1); Set(4016, 0); // FEC enabled, DTX off
        }
    }
    private static int Check(int result) { if (result < 0) throw new InvalidOperationException("Opus error " + result); return result; }
    private void Set(int request, int value) => Check(opus_encoder_ctl(_handle, request, value));
    internal void Configure(int bitrate, int loss, bool dtx) { Set(4002, bitrate); Set(4014, Math.Max(0, Math.Min(100, loss))); Set(4016, dtx ? 1 : 0); }
    internal int Encode(float[] samples, byte[] data) => Check(opus_encode_float(_handle, samples, 960, data, data.Length));
    internal int Decode(byte[]? data, int length, float[] samples, bool fec = false) => Check(opus_decode_float(_handle, data, length, samples, 960, fec ? 1 : 0));
    public void Dispose() { if (_handle == IntPtr.Zero) return; if (_encoder) opus_encoder_destroy(_handle); else opus_decoder_destroy(_handle); _handle = IntPtr.Zero; }
}
internal sealed class NoiseSuppressor : IDisposable
{
    private IntPtr _state;
    private readonly float[] _block = new float[480];
    [DllImport("pagvc_rnnoise", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr rnnoise_create(IntPtr model);
    [DllImport("pagvc_rnnoise", CallingConvention = CallingConvention.Cdecl)] private static extern void rnnoise_destroy(IntPtr state);
    [DllImport("pagvc_rnnoise", CallingConvention = CallingConvention.Cdecl)] private static extern float rnnoise_process_frame(IntPtr state, float[] output, float[] input);
    internal NoiseSuppressor() { NativeAudio.Ensure(); _state = rnnoise_create(IntPtr.Zero); if (_state == IntPtr.Zero) throw new InvalidOperationException("RNNoise allocation failed."); }
    internal void Process(float[] frame)
    {
        for (int offset = 0; offset < 960; offset += 480)
        {
            for (int i = 0; i < 480; i++) _block[i] = frame[offset + i] * 32768f;
            rnnoise_process_frame(_state, _block, _block);
            for (int i = 0; i < 480; i++) frame[offset + i] = _block[i] / 32768f;
        }
    }
    public void Dispose() { if (_state != IntPtr.Zero) rnnoise_destroy(_state); _state = IntPtr.Zero; }
}
