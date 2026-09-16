using ProximityVoiceChat.Voice.Audio;

internal static class LocalClipTests
{
    private sealed class Sink : ILocalClipSink
    {
        internal readonly float[] Clip = new float[48000];
        internal double Start, End;
        internal int Offset, Starts, Stops;
        internal bool Active;
        public void Write(float[] samples, int offset) => Array.Copy(samples,0,Clip,offset,samples.Length);
        public void Clear(int offset) => Array.Clear(Clip,offset,960);
        public void Play(int offset,double start,double end) { Offset=offset;Start=start;End=end;Active=true;Starts++; }
        public void EndAt(double end) => End=end;
        public void Stop() { Active=false; Stops++; }
        internal float Read(double now) => Active && now>=Start && now<End
            ? Clip[(Offset+(int)Math.Floor((now-Start)*48000+1e-6))%48000] : 0;
    }
    internal static void Run(Action<bool,string> check)
    {
        foreach(int rate in new[]{44100,48000,96000})
        foreach(int block in new[]{256,512,1024,2048})
        {
            var sink=new Sink(); var ring=new LocalClipBuffer(sink,rate,block);
            var samples=Enumerable.Repeat(.25f,960).ToArray();
            double nextCapture=0,nextMain=0,first=-1; bool gaps=false; int tick=0;
            for(int ms=0;ms<60000;ms++) {
                double now=ms/1000.0;
                if(now>=nextMain) {
                    while(nextCapture<=now+1e-9) { ring.Push(samples,now); nextCapture+=.02; }
                    ring.Tick(now,false); nextMain+=tick++%3==0?.025:.012;
                }
                float audio=sink.Read(now);
                if(audio!=0 && first<0)first=now;
                if(first>=0 && audio!=.25f)gaps=true;
            }
            check(first>=0 && first<.2 && !gaps && ring.Underruns==0 && ring.Dropped==0 && sink.Starts==1,
                $"Local non-streaming clip: 60s continuous, bounded startup, repeated wraps {rate}/{block}");
            // No main-thread updates: audio must stop in the mixer itself, not
            // replay the one-second storage ring forever.
            double end=sink.End;
            check(sink.Read(end+.01)==0 && sink.Read(end+1.5)==0,"DSP end guards long main-thread stall");
            ring.Tick(end+2,false);
            check(ring.Pending==0 && !ring.Playing && sink.Clip.All(x=>x==0),"Stall recovery clears all expired samples");
            ring.Dispose();
        }
        var target=new Sink(); var b=new LocalClipBuffer(target,48000,512);
        var frame=Enumerable.Repeat(.1f,960).ToArray();
        b.Push(frame,0); b.Tick(0,false); check(target.Starts==0,"Local prefill required");
        b.Tick(.01,true); check(target.Starts==1,"Completed short utterance plays without waiting for full prefill");
        check(target.Read(target.Start+.005)==.1f,"Short utterance content");
        b.Tick(target.End+.001,true); check(b.Underruns==0 && b.Pending==0,"Clean completion drains without underrun");
        for(int i=0;i<3;i++)b.Push(frame,1);
        b.Tick(1,false); b.Tick(2,false);
        check(b.Underruns==1 && !b.Playing,"Underrun stops source");
        b.Push(frame,2); b.Tick(2,false); check(!b.Playing,"Underrun waits for refill");
        b.Push(frame,2);b.Push(frame,2);b.Tick(2,false); check(b.Playing,"Refill restarts local source");
        for(int i=0;i<20;i++)b.Push(frame,2);
        check(b.Pending==10 && b.Dropped>0,"Local backlog limited to 200ms");
        b.Tick(2,false); check(target.Read(target.Start+.001)==.1f,"Overflow preserves newest audio");
        b.Dispose(); b.Push(frame,3);b.Tick(3,true);
        check(!b.Playing && b.Pending==0 && target.Read(3)==0 && target.Clip.All(x=>x==0),"Mute/channel switch/dispose clears local clip");
        var orderedSink=new Sink();var ordered=new LocalClipBuffer(orderedSink,48000,512);
        for(int i=0;i<15;i++) { Array.Fill(frame,(i+1)/100f); ordered.Push(frame,0); }
        ordered.Tick(0,false);
        check(Math.Abs(orderedSink.Read(orderedSink.Start+.001)-.06f)<1e-6 && ordered.Dropped==5,"Overflow drops oldest frames in order");
        ordered.Dispose();
        var wrapSink=new Sink(); var wrap=new LocalClipBuffer(wrapSink,48000,512);
        int produced=0; bool sequenceCorrect=true;
        for(int ms=0;ms<60000;ms++) {
            double now=ms/1000.0;
            if(ms%20==0) { Array.Fill(frame,.1f+produced++*.0001f); wrap.Push(frame,now); }
            wrap.Tick(now,false);
            if(wrapSink.Active && now>=wrapSink.Start && now<wrapSink.End) {
                int expected=(int)Math.Floor((now-wrapSink.Start+1e-9)/.02);
                sequenceCorrect &= Math.Abs(wrapSink.Read(now)-(.1f+expected*.0001f))<1e-6;
            }
        }
        check(sequenceCorrect && produced==3000 && wrapSink.Starts==1,"3000 unique frames retain order through 59 clip wraps");
        wrap.Dispose();
        Console.WriteLine("PASS: local mono clip timeline, 12 one-minute wrap simulations, short speech, DSP stop, refill, overflow and disposal.");
    }
}
