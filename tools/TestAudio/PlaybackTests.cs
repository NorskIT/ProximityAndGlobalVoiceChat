using ProximityVoiceChat.Voice.Audio;

internal static class PlaybackTests
{
    internal static void Run(Action<bool, string> check)
    {
        // Simulate a full minute of independently clocked 20ms capture and DSP
        // consumption. The producer is batched onto irregular game frames.
        foreach (int rate in new[] {44100, 48000, 96000})
        foreach (int block in new[] {256, 512, 1024, 2048})
        foreach (int channels in new[] {1, 2, 6})
        {
            var buffer = new PlaybackBuffer(rate, block);
            var resampler = new PlaybackResampler(rate);
            var frame = Enumerable.Repeat(.2f, 960).ToArray();
            var audio = new float[block * channels];
            double nextMain = 0, nextCapture = .02, nextDsp = 0, firstSound = -1;
            int mainFrame = 0; bool channelMismatch = false, gap = false;
            while (Math.Min(nextMain, nextDsp) < 60)
            {
                if (nextMain <= nextDsp)
                {
                    while (nextCapture <= nextMain + 1e-9)
                    { resampler.Write(frame, buffer); nextCapture += .02; }
                    nextMain += mainFrame++ % 3 == 0 ? .025 : .012;
                }
                else
                {
                    buffer.Read(audio, channels, 1);
                    if (buffer.Rms > .01 && firstSound < 0) firstSound = nextDsp;
                    if (firstSound >= 0 && buffer.Rms < .01) gap = true;
                    for (int i = 0; i < audio.Length; i += channels)
                        for (int c = 1; c < channels; c++) channelMismatch |= audio[i] != audio[i + c];
                    nextDsp += (double)block / rate;
                }
            }
            check(firstSound >= 0 && firstSound < .2, $"DSP starts within 200 ms: {rate}/{block}/{channels}");
            check(!gap && !channelMismatch && buffer.Underruns == 0 && buffer.Dropped == 0,
                $"60s continuous resampled output without gaps: {rate}/{block}/{channels}");
        }
        var b = new PlaybackBuffer(48000,512);
        var data = Enumerable.Repeat(.25f,960).ToArray(); var read = new float[1024];
        b.Write(data,960); b.Read(read,2,1);
        check(read.All(x=>x==0) && b.Buffered==960,"Startup silence must not consume prefill");
        b.Write(data,960); b.Write(data,960);
        b.Read(read,2,1); check(read.All(x=>x==.25f),"Start with complete prefill");
        for(int i=0;i<7;i++) b.Read(read,2,1);
        check(b.Underruns==1 && b.Rms==0,"One underrun per depletion, meter clears");
        b.Write(data,960); b.Read(read,2,1);
        check(b.Buffered==960 && read.All(x=>x==0),"Refill after underrun");
        b.Write(data,960); b.Write(data,960); b.Read(read,2,1);
        check(read.All(x=>x==.25f),"Recovery plays continuously after refill");
        for(int i=0;i<20;i++) b.Write(data,960);
        check(b.Dropped>0 && b.Buffered==9600,"Overflow retains full bounded buffer");
        b.Read(read,2,1); check(read.All(x=>x==.25f),"Overflow does not insert silence");
        b.Stop(); b.Write(data,960); b.Read(read,2,1);
        check(b.Buffered==0 && b.Rms==0 && read.All(x=>x==0),"Stop prevents stale audio and subsequent writes");
        var ordered = new PlaybackBuffer(48000,512);
        var sequence=Enumerable.Range(0,12000).Select(i=>(float)i/20000).ToArray();
        ordered.Write(sequence,sequence.Length); ordered.Read(read,1,1);
        check(ordered.Dropped==2400 && read[0]==sequence[2400] && read[^1]==sequence[3423],"Overflow discards oldest samples in order");
        // A live device change replaces the stopped buffer and resampler; no old
        // rate or buffered audio may cross into the new device generation.
        var fresh=new PlaybackBuffer(44100,1024); var converter=new PlaybackResampler(44100);
        for(int i=0;i<5;i++) converter.Write(data,fresh);
        fresh.Read(read,1,1);
        check(fresh.Rms>.1 && fresh.Underruns==0,"Fresh output after device-rate change");
        var realtime = new PlaybackBuffer(48000,512);
        // Warm the monitor and JIT before measuring the callback path.
        for(int i=0;i<100;i++) { realtime.Write(data,960); realtime.Read(read,2,1); }
        long allocated=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<1000;i++) { realtime.Write(data,960); realtime.Read(read,2,1); }
        check(GC.GetAllocatedBytesForCurrentThread()==allocated,"DSP read/write path allocates no managed memory");
        var shared = new PlaybackBuffer(48000,512);
        var writer = Task.Run(()=> { for(int i=0;i<10000;i++) shared.Write(data,960); });
        var reader = Task.Run(()=> { for(int i=0;i<10000;i++) shared.Read(read,2,1); });
        shared.Stop(); Task.WaitAll(writer,reader);
        check(shared.Buffered==0 && read.All(x=>x==0),"Concurrent stop/read/write leaves no stale audio");
        Console.WriteLine("PASS: 36 one-minute DSP simulations plus prefill, recovery, overflow, stop and device-rate changes.");
    }
}
