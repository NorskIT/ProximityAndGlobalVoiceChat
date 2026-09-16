using System.Diagnostics;
using ProximityVoiceChat.Voice;
using ProximityVoiceChat.Voice.Audio;

int assertions = 0;
void Check(bool ok, string why) { assertions++; if (!ok) throw new Exception(why); }
Console.WriteLine("Native codec: " + NativeOpus.Version);
foreach (bool blocked in new[]{false,true}) foreach(bool local in new[]{false,true}) foreach(bool global in new[]{false,true}) foreach(bool activation in new[]{false,true})
    Check(VoiceRouting.Select(blocked,local,global,activation) == (blocked ? VoiceChannel.None : global ? VoiceChannel.Global : local || activation ? VoiceChannel.Local : VoiceChannel.None), "Channel priority");
Check(VoiceRouting.Receives(VoiceChannel.Global,true,false), "Global distance independence");
Check(!VoiceRouting.Receives(VoiceChannel.Local,true,false) && !VoiceRouting.Receives(VoiceChannel.Global,false,true), "Range/compatibility boundaries");
var packet = new byte[20];
VoiceWireFormat.WriteHeader(packet,VoicePacketType.Frame,VoiceFlags.None,65535,123,3,42,VoiceChannel.Global);
Check(VoiceWireFormat.TryReadHeader(packet,20,out var header) && header.StreamId==42 && header.Channel==VoiceChannel.Global && header.Sequence==65535, "Wire roundtrip");
Check(!VoiceWireFormat.TryReadHeader(packet,19,out _),"Truncated packet");
packet[1]=1; Check(!VoiceWireFormat.TryReadHeader(packet,20,out _),"Reject old protocol"); packet[1]=2;
packet[16]=3; Check(!VoiceWireFormat.TryReadHeader(packet,20,out _),"Reject unknown channel");
Check(VoiceWireFormat.SequenceDelta(0,65535)==1,"Sequence wrap");
CaptureSettings Settings(int generation, VoiceChannel channel, bool raw=false) => new(generation,channel,false,false,false,raw,1,0.1f,0.1f,96000,5,false);
var pcm = new PcmRingBuffer(9600); float[] input = new float[960], output = new float[960];
pcm.Settings=Settings(1,VoiceChannel.Local); pcm.Write(input,0,480);
pcm.Settings=Settings(2,VoiceChannel.Global); pcm.Write(input,0,960);
Check(pcm.TryReadFrame(output,out var snapshot) && snapshot!.Generation==2 && snapshot.Channel==VoiceChannel.Global,"Never mix channels across a partial frame");
using var encoder = new NativeOpus(true); using var decoder = new NativeOpus(false); using var noise = new NoiseSuppressor();
encoder.Configure(96000,5,false);
var compressed = new byte[1100]; byte[] last = Array.Empty<byte>(); double energy=0;
for(int f=0;f<100;f++) {
    for(int i=0;i<960;i++) input[i]=(float)(0.2*Math.Sin(2*Math.PI*12000*(i+f*960)/48000));
    int n=encoder.Encode(input,compressed); last=compressed[..n];
    Check(decoder.Decode(last,n,output)==960 && output.All(float.IsFinite),"Fullband encode/decode");
    if(f>20) energy+=output.Sum(x=>(double)x*x)/960;
}
Check(Math.Sqrt(energy/79)>0.08,"12 kHz tone survives codec (not telephone bandwidth)");
Console.WriteLine($"12 kHz decoded RMS: {Math.Sqrt(energy/79):F4}");
var legacy = new Concentus.Structs.OpusEncoder(48000,1,Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP) {
    Bitrate=24000, Complexity=10, SignalType=Concentus.Enums.OpusSignal.OPUS_SIGNAL_VOICE,
    ForceMode=Concentus.Enums.OpusMode.MODE_SILK_ONLY, Bandwidth=Concentus.Enums.OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND,
    MaxBandwidth=Concentus.Enums.OpusBandwidth.OPUS_BANDWIDTH_WIDEBAND, UseVBR=true, UseInbandFEC=true, PacketLossPercent=5
};
var legacyDecoder = new Concentus.Structs.OpusDecoder(48000,1); double oldEnergy=0;
for(int f=0;f<100;f++) {
    int n=legacy.Encode(input,0,960,compressed,0,1100); legacyDecoder.Decode(compressed,0,n,output,0,960,false);
    if(f>20) oldEnergy+=output.Sum(x=>(double)x*x)/960;
}
Check(oldEnergy < energy*.01,"Fullband improvement over original forced wideband SILK");
Console.WriteLine($"Original codec 12 kHz decoded RMS: {Math.Sqrt(oldEnergy/79):F6}");
foreach(int rate in new[]{16000,44100,96000}) {
    var resampler=new NormalizedAudioResampler(rate,48000,rate/100); int total=0; double level=0;
    var source=new float[rate/100];
    for(int f=0;f<100;f++) {
        for(int i=0;i<source.Length;i++) source[i]=(float)(.2*Math.Sin(2*Math.PI*1000*(i+f*source.Length)/rate));
        int m=resampler.Process(source,source.Length); var destination=resampler.Output; if(f>10) level+=destination.Take(m).Sum(x=>(double)x*x);
        Check(destination.Take(m).All(float.IsFinite),"Resampler consumes complete chunks"); total+=m;
    }
    Check(Math.Abs(total-48000)<5,"Resampler preserves duration across chunk boundaries");
    Check(Math.Sqrt(level/(89*480))>.13, "Resampler preserves normalized microphone amplitude");
}
using(var jitter=new VoiceJitterBuffer { Adaptive=false }) {
    jitter.Push(65534,0,last,0,last.Length,false,1);
    jitter.Push(0,40,last,0,last.Length,false,1.01);
    jitter.Push(65535,20,last,0,last.Length,false,1.02);
    jitter.Push(65535,20,last,0,last.Length,false,1.03);
    Check(!jitter.TryDecode(1.04,output),"Prefill delay");
    Check(jitter.TryDecode(1.061,output) && jitter.TryDecode(1.081,output) && jitter.TryDecode(1.101,output),"Reorder across wrap");
    Check(jitter.Lost==0 && jitter.Duplicates==1,"No false packet loss from reordering");
    jitter.Push(65534,0,last,0,last.Length,false,1.11); Check(jitter.Late==1,"Drop late packets");
    jitter.Push(2,80,last,0,last.Length,false,1.12);
    Check(jitter.TryDecode(1.121,output) && jitter.Lost==1 && output.All(float.IsFinite),"FEC or PLC for one lost frame");
    jitter.Push(3,100,last,0,0,true,1.13);
    Check(jitter.TryDecode(1.141,output) && !jitter.TryDecode(1.17,output),"End stops synthetic losses");
}
using(var jitter=new VoiceJitterBuffer()) {
    for(ushort n=0;n<100;n++) jitter.Push(n,(uint)(n*20),last,0,last.Length,false,1+n*.02);
    Check(jitter.Pending<=10 && jitter.TryDecode(3.1,output),"Bounded backlog after stalled game thread");
}
var random=new Random(1); double before=0, after=0;
for(int f=0;f<150;f++) {
    for(int i=0;i<960;i++) input[i]=(float)((random.NextDouble()-.5)*.03);
    if(f>50) before+=input.Sum(x=>(double)x*x);
    noise.Process(input); Check(input.All(float.IsFinite),"RNNoise finite output");
    if(f>50) after+=input.Sum(x=>(double)x*x);
}
Check(after<before,"RNNoise suppresses stationary noise");
Console.WriteLine($"Stationary noise reduction: {10*Math.Log10(before/after):F1} dB");
var ring=new EncodedFrameRing(32); pcm.Clear(); pcm.Settings=Settings(3,VoiceChannel.Global);
using(var worker=new VoiceEncoder(pcm,ring)) {
    worker.Start(); Array.Fill(input,0.0001f); pcm.Write(input,0,960);
    var timer=Stopwatch.StartNew(); EncodedFrame? frame;
    while((frame=ring.BeginRead())==null && timer.ElapsedMilliseconds<3000) Thread.Sleep(5);
    Check(worker.Error==null && frame!=null && frame.Channel==VoiceChannel.Global && frame.Length>0,"Quiet PTT bypasses activation gate");
    ring.CommitRead();
    pcm.Settings=Settings(4,VoiceChannel.Local,true); pcm.Write(input,0,960); timer.Restart();
    while((frame=ring.BeginRead())==null && timer.ElapsedMilliseconds<3000) Thread.Sleep(5);
    Check(frame!=null && frame.RawMonitor && frame.Length==0 && frame.Raw[0]==input[0],"Raw monitor preserves samples without encoding"); ring.CommitRead();
}
var decoders=Enumerable.Range(0,8).Select(_=>new NativeOpus(false)).ToArray();
var watch=Stopwatch.StartNew();
for(int f=0;f<100;f++) {
    for(int i=0;i<960;i++) input[i]=(float)(.2*Math.Sin(2*Math.PI*150*(i+f*960)/48000)+.06*Math.Sin(2*Math.PI*1800*(i+f*960)/48000));
    noise.Process(input); int n=encoder.Encode(input,compressed); foreach(var d in decoders) d.Decode(compressed,n,output);
}
watch.Stop(); foreach(var d in decoders)d.Dispose();
Console.WriteLine($"Encode + RNNoise + 8 decoders: {watch.Elapsed.TotalMilliseconds/100:F2} ms per 20 ms frame (this machine)");
Check(watch.Elapsed.TotalMilliseconds/100<20,"Real-time CPU budget");
PlaybackTests.Run(Check);
LocalClipTests.Run(Check);
Console.WriteLine($"PASS: {assertions} audio/protocol assertions.");
