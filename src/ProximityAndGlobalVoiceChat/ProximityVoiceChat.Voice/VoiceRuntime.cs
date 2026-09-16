using System;
using System.Collections.Generic;
using ProximityVoiceChat.Voice.Audio;
using UnityEngine;
namespace ProximityVoiceChat.Voice;

internal sealed class VoiceRuntime : MonoBehaviour
{
    internal const ulong LoopbackId = 1;
    internal static VoiceRuntime? Instance;
    internal static bool LocalTestRequested;
    private readonly VoiceRoster _roster = new VoiceRoster();
    private readonly Dictionary<ulong, PeerLink> _links = new Dictionary<ulong, PeerLink>();
    private readonly List<ulong> _departed = new List<ulong>();
    private readonly PcmRingBuffer _pcm = new PcmRingBuffer(9600);
    private readonly EncodedFrameRing _encoded = new EncodedFrameRing(32);
    private readonly byte[] _hello = new byte[] { 2, 0 };
    private SteamVoiceTransport? _transport;
    private SpatialPlayback? _playback;
    private MicrophoneCapture? _capture;
    private VoiceEncoder? _encoder;
    private CaptureSettings? _settings;
    private string _device = "";
    private int _generation;
    private float _linkTimer, _retryAt;
    private uint _loopStream, _loopEncoded;
    private ushort _loopSequence;
    private int _loopGeneration = -1;
    private bool _testing;
    internal string? AudioError { get; private set; }
    internal VoiceChannel SelectedChannel => _settings?.Channel ?? VoiceChannel.None;
    internal VoiceRoster Roster => _roster;
    internal IReadOnlyDictionary<ulong, PeerLink> Links => _links;
    internal SteamVoiceTransport? Transport => _transport;
    internal MicrophoneCapture? Capture => _capture;
    internal SpatialPlayback? Playback => _playback;
    internal VoiceEncoder? Encoder => _encoder;
    private void Awake() { Instance = this; }
    private void OnDestroy() { Shutdown(); if (Instance == this) Instance = null; }
    private void OnApplicationQuit() => Shutdown();
    internal void Shutdown()
    {
        StopAudio(); MusicDucking.Reset(); _playback?.Dispose(); _playback = null;
        _transport?.Dispose(); _transport = null; _roster.Reset(); _links.Clear(); VoiceMixer.Reset();
    }
    private void StopAudio()
    {
        EndOutgoing(); _encoder?.Dispose(); _encoder = null; _capture?.Dispose(); _capture = null;
        _pcm.Clear(); _encoded.Clear(); _settings = null; _generation++; _device = "";
    }
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool network = ZNet.instance != null && !ZNet.instance.IsDedicated() && VoiceRoster.BackendSupported;
        if (!network)
        {
            if (_transport != null) Shutdown();
            if (!LocalTestRequested) { if (_capture != null || _playback != null) Shutdown(); return; }
        }
        if (_playback == null) _playback = new SpatialPlayback(transform);
        if (network)
        {
            if (_transport == null) _transport = new SteamVoiceTransport(_roster.IsKnownPeer);
            _departed.Clear(); _roster.Refresh(dt, _departed);
            foreach (ulong id in _departed) { _transport.CloseSession(id); _links.Remove(id); _playback.Remove(id); }
            UpdateRanges();
            _transport.Pump(dt, OnPacket);
            _linkTimer += dt;
            if (_linkTimer >= 1) { _linkTimer = 0; SendLinkChecks(); }
        }
        if (!ProximityVoiceChatPlugin.EnableVoice.Value.IsOn()) { if (_capture != null) StopAudio(); _playback.Clear(); MusicDucking.Reset(); return; }
        try { UpdateAudio(dt); }
        catch (Exception ex) { AudioError = ex.Message; StopAudio(); _retryAt = Time.unscaledTime + 5; }
        try { _playback.Tick(dt, _roster); }
        catch (Exception ex) { AudioError = ex.Message; _playback.Clear(); }
        MusicDucking.Tick(dt, _playback.AnyoneSpeaking);
    }
    internal static VoiceChannel ChooseChannel()
    {
        bool blocked = ProximityVoiceChatPlugin.MuteSelf.Value.IsOn() || ProximityVoiceChatPlugin.Deafen.Value.IsOn()
            || Chat.instance != null && Chat.instance.HasFocus();
        return VoiceRouting.Select(blocked, ProximityVoiceChatPlugin.PushToTalkKey.Value.IsKeyHeld(),
            ProximityVoiceChatPlugin.GlobalPushToTalkKey.Value.IsKeyHeld(), ProximityVoiceChatPlugin.UseVoiceActivation.Value.IsOn());
    }
    internal static bool GateOpen() => ChooseChannel() != VoiceChannel.None;
    private void UpdateAudio(float dt)
    {
        if (!ProximityVoiceChatPlugin.EnableVoice.Value.IsOn()) { if (_capture != null) StopAudio(); return; }
        if (_capture != null && _device != ProximityVoiceChatPlugin.InputDevice.Value) StopAudio();
        if (_capture == null)
        {
            if (Time.unscaledTime < _retryAt) return;
            try
            {
                NativeAudio.Ensure();
                _capture = new MicrophoneCapture(_pcm); _device = ProximityVoiceChatPlugin.InputDevice.Value;
                if (!_capture.Start(_device)) { StopAudio(); _retryAt = Time.unscaledTime + 2; return; }
                _encoder = new VoiceEncoder(_pcm, _encoded); _encoder.Start(); AudioError = null;
            }
            catch (Exception ex)
            { AudioError = ex.Message; ProximityVoiceChatPlugin.Log.LogError(ex.Message); StopAudio(); _retryAt = Time.unscaledTime + 5; return; }
        }
        if (_encoder?.Error != null) { AudioError = _encoder.Error; StopAudio(); _retryAt = Time.unscaledTime + 5; return; }
        VoiceChannel channel = ChooseChannel();
        bool testing = ProximityVoiceChatPlugin.LoopbackTest.Value.IsOn();
        bool raw = testing && ProximityVoiceChatPlugin.MonitorInput.Value == ProximityVoiceChatPlugin.MonitorMode.Raw;
        bool activation = channel == VoiceChannel.Local && ProximityVoiceChatPlugin.UseVoiceActivation.Value.IsOn() && !ProximityVoiceChatPlugin.PushToTalkKey.Value.IsKeyHeld();
        bool routingChanged = _settings == null || channel != _settings.Channel || activation != _settings.Activation || testing != _testing || raw != _settings.RawMonitor;
        if (routingChanged)
        { EndOutgoing(); _generation++; _pcm.Clear(); _capture!.DiscardPending(); _playback!.Remove(LoopbackId); }
        _testing = testing;
        int loss = ProximityVoiceChatPlugin.ExpectedPacketLoss.Value;
        if (ProximityVoiceChatPlugin.AdaptiveFec.Value.IsOn())
            foreach (var link in _links.Values) if (VoiceRouting.Receives(channel, link.Compatible, link.InRange)) loss = Math.Max(loss, link.ReportedLossPercent);
        var next = new CaptureSettings(_generation, channel, activation, ProximityVoiceChatPlugin.NoiseSuppression.Value.IsOn(),
            ProximityVoiceChatPlugin.AutoGain.Value.IsOn(), raw, ProximityVoiceChatPlugin.MicGain.Value,
            ProximityVoiceChatPlugin.ActivationThreshold.Value, ProximityVoiceChatPlugin.AutoGainTarget.Value,
            ProximityVoiceChatPlugin.EffectiveBitrate, loss, ProximityVoiceChatPlugin.UseDtx.Value.IsOn());
        if (_settings == null || !Same(_settings, next)) _settings = next;
        _pcm.Settings = _settings;
        _capture!.Poll(dt);
        if (_capture.Dead) { StopAudio(); return; }
        DrainEncodedFrames();
    }
    private static bool Same(CaptureSettings a, CaptureSettings b) => a.Generation == b.Generation && a.Noise == b.Noise && a.AutoGain == b.AutoGain
        && a.Gain == b.Gain && a.Threshold == b.Threshold && a.Target == b.Target && a.Bitrate == b.Bitrate && a.Loss == b.Loss && a.Dtx == b.Dtx;
    private void EndOutgoing()
    {
        if (_transport == null) return;
        foreach (var pair in _links) EndPeer(pair.Key, pair.Value);
    }
    private void EndPeer(ulong id, PeerLink link)
    {
        if (!link.TxActive || _transport == null) return;
        _transport.Send(id, VoicePacketType.Frame, VoiceFlags.EndOfTalkspurt, unchecked((uint)Environment.TickCount), Array.Empty<byte>(), 0, 0,
            link.TxStream, link.TxChannel, link.TxSequence++);
        link.TxActive = false;
    }
    private void DrainEncodedFrames()
    {
        EncodedFrame frame;
        while ((frame = _encoded.BeginRead()) != null)
        {
            try
            {
                if (frame.Generation != _generation || frame.Channel == VoiceChannel.None) continue;
                if (_testing)
                {
                    if (_loopGeneration != frame.Generation || _loopEncoded != frame.StreamId)
                    { _loopStream++; _loopSequence = 0; _loopGeneration = frame.Generation; _loopEncoded = frame.StreamId; }
                    if (frame.RawMonitor) { if ((frame.Flags & VoiceFlags.EndOfTalkspurt) == 0) _playback!.PushRaw(frame.Raw, _loopStream); }
                    else
                    {
                        var header = new VoiceHeader { Type = VoicePacketType.Frame, Flags = frame.Flags, StreamId = _loopStream,
                            Channel = VoiceChannel.Global, Sequence = _loopSequence++, TimestampMs = frame.TimestampMs, PayloadLength = (ushort)frame.Length };
                        _playback!.Push(LoopbackId, in header, frame.Data, 0, frame.Length);
                    }
                    continue; // Both comparison modes are local-only.
                }
                if (_transport == null) continue;
                foreach (var entry in _roster.Entries)
                {
                    var link = GetLink(entry.SteamId);
                    if (!VoiceRouting.Receives(frame.Channel, link.Compatible, link.InRange)) { EndPeer(entry.SteamId, link); continue; }
                    bool end = (frame.Flags & VoiceFlags.EndOfTalkspurt) != 0;
                    if (!link.TxActive || link.TxEncodedStream != frame.StreamId || link.TxGeneration != frame.Generation)
                    {
                        if (end) continue;
                        EndPeer(entry.SteamId, link); link.TxStream++; if (link.TxStream == 0) link.TxStream++;
                        link.TxSequence = 0; link.TxEncodedStream = frame.StreamId; link.TxGeneration = frame.Generation;
                        link.TxChannel = frame.Channel; link.TxActive = true;
                    }
                    _transport.Send(entry.SteamId, VoicePacketType.Frame, frame.Flags, frame.TimestampMs, frame.Data, 0, frame.Length,
                        link.TxStream, frame.Channel, link.TxSequence++);
                    if (end) link.TxActive = false;
                }
            }
            catch (Exception ex) { AudioError = ex.Message; }
            finally { _encoded.CommitRead(); }
        }
    }
    private PeerLink GetLink(ulong id) { if (!_links.TryGetValue(id, out var link)) { link = new PeerLink(); _links[id] = link; } return link; }
    private void UpdateRanges()
    {
        Player local = Player.m_localPlayer;
        foreach (var entry in _roster.Entries)
        {
            var link = GetLink(entry.SteamId);
            link.Distance = local != null && entry.HasPosition ? Vector3.Distance(local.transform.position, entry.LastKnownPosition) : float.PositiveInfinity;
            link.InRange = link.Distance <= ProximityVoiceChatPlugin.MaxRange.Value + (link.InRange ? ProximityVoiceChatPlugin.RangeHysteresis.Value : 0);
            if (link.Compatible && Time.unscaledTime - link.LastHello > 10) { link.Compatible = false; _playback?.Remove(entry.SteamId); }
            if (!link.InRange && link.TxChannel == VoiceChannel.Local) EndPeer(entry.SteamId, link);
            if (!link.InRange && link.RxChannel == VoiceChannel.Local) _playback?.Remove(entry.SteamId);
        }
    }
    private void SendLinkChecks()
    {
        foreach (var entry in _roster.Entries)
        {
            _hello[1] = _playback != null && _playback.Streams.TryGetValue(entry.SteamId, out var stream) ? stream.TakeLossPercent() : (byte)0;
            _transport!.Send(entry.SteamId, VoicePacketType.Hello, VoiceFlags.None, 0, _hello, 0, 2);
        }
    }
    private void OnPacket(ulong sender, in VoiceHeader header, byte[] buffer, int offset, int length)
    {
        var link = GetLink(sender);
        if (header.Type == VoicePacketType.Hello || header.Type == VoicePacketType.Pong)
        {
            link.Compatible = length == 2 && buffer[offset] == 2;
            if (!link.Compatible) return;
            link.LastHello = Time.unscaledTime; if (header.Type == VoicePacketType.Hello) link.ReportedLossPercent = (byte)Math.Min(100, (int)buffer[offset + 1]);
            if (header.Type == VoicePacketType.Hello)
            { _hello[1] = 0; _transport!.Send(sender, VoicePacketType.Pong, VoiceFlags.None, 0, _hello, 0, 2); }
            return;
        }
        if (header.Type != VoicePacketType.Frame || !VoiceRouting.Receives(header.Channel, link.Compatible, link.InRange)) return;
        if (link.RxStream != 0 && unchecked((int)(header.StreamId - link.RxStream)) < 0) return;
        if (link.RxStream == header.StreamId && link.RxChannel != header.Channel) return;
link.RxStream = header.StreamId; link.RxChannel = header.Channel;
        if (length > 0) link.LastFrameTime = Time.unscaledTime;
        try { _playback?.Push(sender, in header, buffer, offset, length); }
        catch (Exception ex) { AudioError = ex.Message; _playback?.Remove(sender); }
    }
}
