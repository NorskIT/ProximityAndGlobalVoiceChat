using System;
using System.Collections.Generic;
using System.Linq;
namespace ProximityVoiceChat.Voice.Audio;

// Main-thread playout clock: reorder compressed packets before advancing Opus state.
internal sealed class VoiceJitterBuffer : IDisposable
{
    private readonly SortedDictionary<long, byte[]> _packets = new SortedDictionary<long, byte[]>();
    private NativeOpus _decoder = new NativeOpus(false);
    private bool _started, _playing;
    private long _highest, _next, _end = long.MaxValue;
    private double _due, _firstArrival, _scheduledTarget, _lastArrival, _lastTransit, _jitter;
    private int _missing;
    internal int Received, Lost, Late, Duplicates;
    internal double TargetSeconds = 0.06, MaximumSeconds = 0.2;
    internal bool Adaptive = true;
    internal bool Complete => _started && _next >= _end;
    internal int Pending => _packets.Count;
    internal double Target => Math.Max(0.02, Math.Min(MaximumSeconds, TargetSeconds + (Adaptive ? 2 * _jitter : 0)));
    internal void Push(ushort sequence, uint timestamp, byte[] data, int offset, int length, bool end, double now)
    {
        long index = _started ? _highest + VoiceWireFormat.SequenceDelta(sequence, (ushort)_highest) : sequence;
        if (!_started) { _started = true; _next = _highest = index; _firstArrival = now; _scheduledTarget = Target; _due = now + Target; }
        if (_playing && index < _next) { Late++; return; }
        if (!_playing && index < _next) _next = index;
        _highest = Math.Max(index, _highest);
        if (end) { _end = Math.Min(_end, index); return; }
        if (_packets.ContainsKey(index)) { Duplicates++; return; }
        if (_lastArrival > 0)
        {
            double transit = now - timestamp / 1000.0;
            double change = Math.Abs(transit - _lastTransit);
            if (change < 1) _jitter += (change - _jitter) / 16;
            _lastTransit = transit;
        }
        else _lastTransit = now - timestamp / 1000.0;
        _lastArrival = now;
        if (!_playing) { _scheduledTarget = Target; _due = _firstArrival + _scheduledTarget; }
        byte[] copy = new byte[length]; Buffer.BlockCopy(data, offset, copy, 0, length); _packets[index] = copy;
        if (_packets.Count > Math.Max(2, (int)(MaximumSeconds / 0.02)) || _highest - _next > Math.Max(2, (int)(MaximumSeconds / 0.02)))
        {
            // Bounded recovery following a stalled game frame; discard stale speech.
            long start = _highest - Math.Max(1, (int)(Target / 0.02));
            foreach (long key in _packets.Keys.Where(k => k < start).ToArray()) _packets.Remove(key);
            _next = _packets.Keys.First(); _due = now; ResetDecoder();
        }
    }
    internal bool TryDecode(double now, float[] samples)
    {
        if (!_started || now < _due || _next >= _end) return false;
        if (_missing >= 5 && _packets.Count == 0) return false;
        // Resume without decoding a long run of artificial losses after silence/stalls.
        if (_missing >= 5 && _packets.Count > 0 || now - _due > MaximumSeconds)
        {
            if (_packets.Count == 0) return false;
            _next = _packets.Keys.First(); _due = now; _missing = 0; ResetDecoder();
        }
        _playing = true;
        // Grow delay only under pressure, without changing playback pitch. A new
        // talkspurt starts with the base target again, so latency cannot accumulate.
        if (Adaptive && !_packets.ContainsKey(_next) && Target > _scheduledTarget + 0.01)
        {
            double extra = Math.Min(0.02, Target - _scheduledTarget);
            _scheduledTarget += extra; _due += extra; return false;
        }
        if (_packets.TryGetValue(_next, out var packet))
        { _decoder.Decode(packet, packet.Length, samples); _packets.Remove(_next); _missing = 0; Received++; }
        else
        {
            // libopus falls back to PLC if the following packet contains no FEC.
            if (_packets.TryGetValue(_next + 1, out var following)) _decoder.Decode(following, following.Length, samples, true);
            else _decoder.Decode(null, 0, samples);
            Lost++; _missing++;
        }
        _next++; _due += 0.02; return true;
    }
    private void ResetDecoder() { _decoder.Dispose(); _decoder = new NativeOpus(false); }
    internal byte TakeLossPercent()
    { int total = Received + Lost; if (total < 20) return 0; byte value = (byte)(100 * Lost / total); Received = Lost = 0; return value; }
    public void Dispose() { _decoder.Dispose(); _packets.Clear(); }
}
