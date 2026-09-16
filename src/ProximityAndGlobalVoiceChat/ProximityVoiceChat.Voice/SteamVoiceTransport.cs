using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;

namespace ProximityVoiceChat.Voice;

internal sealed class SteamVoiceTransport : IDisposable
{
	internal const int Channel = 24052;

	private const int RecvBatch = 32;

	private const int MaxPacketsPerSenderPerSecond = 120;

	private const float RateLimitWindow = 1f;

	private readonly IntPtr[] _recvPtrs = new IntPtr[32];

	private readonly byte[] _recvScratch = new byte[1117];

	private readonly byte[] _sendScratch = new byte[1117];

	private readonly Dictionary<ulong, int> _packetsThisWindow = new Dictionary<ulong, int>();

	private readonly HashSet<ulong> _openSessions = new HashSet<ulong>();

	private readonly Func<ulong, bool> _isKnownPeer;

	private Callback<SteamNetworkingMessagesSessionRequest_t>? _sessionRequest;

	private Callback<SteamNetworkingMessagesSessionFailed_t>? _sessionFailed;

	private IntPtr _sendBuffer;

	private float _windowTimer;

	private bool _broken;

	private ushort _frameSequence;

	private ushort _controlSequence;

	private bool _disposed;

	internal int PacketsSent;

	internal int PacketsReceived;

	internal int PacketsDropped;

	internal ushort FrameSequence => _frameSequence;

	internal bool Broken => _broken;

	internal SteamVoiceTransport(Func<ulong, bool> isKnownPeer)
	{
		_isKnownPeer = isKnownPeer;
		_sendBuffer = Marshal.AllocHGlobal(1117);
		_sessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
		_sessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
	}

	private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t data)
	{
		SteamNetworkingIdentity identityRemote = data.m_identityRemote;
		ulong steamID = identityRemote.GetSteamID64();
		if (!_isKnownPeer(steamID))
		{
			ProximityVoiceChatPlugin.Log.LogInfo($"Rejecting voice session from unknown user {steamID}");
			SteamNetworkingMessages.CloseSessionWithUser(ref identityRemote);
		}
		else if (SteamNetworkingMessages.AcceptSessionWithUser(ref identityRemote))
		{
			_openSessions.Add(steamID);
			ProximityVoiceChatPlugin.Log.LogInfo($"Accepted voice session from {steamID}");
		}
	}

	private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t data)
	{
		ulong steamID = data.m_info.m_identityRemote.GetSteamID64();
		_openSessions.Remove(steamID);
		ProximityVoiceChatPlugin.Log.LogWarning($"Voice session with {steamID} failed: {data.m_info.m_eEndReason} {data.m_info.m_szEndDebug}");
	}

	internal bool Send(ulong steamId, VoicePacketType type, VoiceFlags flags, uint timestampMs, byte[] payload, int payloadOffset, int payloadLength, uint streamId = 0, VoiceChannel channel = VoiceChannel.None, ushort? sequence = null)
	{
		if (_disposed || _broken || payloadLength > 1100)
		{
			return false;
		}
		ushort seq = sequence ?? _controlSequence;
		VoiceWireFormat.WriteHeader(_sendScratch, type, flags, seq, timestampMs, (ushort)payloadLength, streamId, channel);
		if (payloadLength > 0)
		{
			Buffer.BlockCopy(payload, payloadOffset, _sendScratch, VoiceWireFormat.HeaderSize, payloadLength);
		}
		int num = VoiceWireFormat.HeaderSize + payloadLength;
		Marshal.Copy(_sendScratch, 0, _sendBuffer, num);
		SteamNetworkingIdentity identityRemote = default(SteamNetworkingIdentity);
		identityRemote.SetSteamID64(steamId);
		EResult eResult;
		try
		{
			eResult = SteamNetworkingMessages.SendMessageToUser(ref identityRemote, _sendBuffer, (uint)num, 37, 24052);
		}
		catch (Exception ex)
		{
			Break("send", ex);
			return false;
		}
		if (eResult != EResult.k_EResultOK)
		{
			return false;
		}
		PacketsSent++;
		return true;
	}

	internal void AdvanceFrameSequence()
	{
		_frameSequence++;
	}

	internal void AdvanceControlSequence()
	{
		_controlSequence++;
	}

	private void Break(string stage, Exception ex)
	{
		if (!_broken)
		{
			_broken = true;
			ProximityVoiceChatPlugin.Log.LogError("SteamNetworkingMessages " + stage + " failed, voice transport is off for this session: " + ex.Message);
		}
	}

	internal void Pump(float deltaTime, VoicePacketHandler handler)
	{
		if (_disposed || _broken)
		{
			return;
		}
		_windowTimer += deltaTime;
		if (_windowTimer >= 1f)
		{
			_windowTimer = 0f;
			_packetsThisWindow.Clear();
		}
		int num;
		do
		{
			try
			{
				num = SteamNetworkingMessages.ReceiveMessagesOnChannel(24052, _recvPtrs, 32);
			}
			catch (Exception ex)
			{
				Break("receive", ex);
				break;
			}
			for (int i = 0; i < num; i++)
			{
				IntPtr intPtr = _recvPtrs[i];
				try
				{
					Dispatch(intPtr, handler);
				}
				finally
				{
					SteamNetworkingMessage_t.Release(intPtr);
				}
			}
		}
		while (num == 32);
	}

	private void Dispatch(IntPtr ptr, VoicePacketHandler handler)
	{
		SteamNetworkingMessage_t steamNetworkingMessage_t = SteamNetworkingMessage_t.FromIntPtr(ptr);
		ulong steamID = steamNetworkingMessage_t.m_identityPeer.GetSteamID64();
		if (steamNetworkingMessage_t.m_cbSize < VoiceWireFormat.HeaderSize || steamNetworkingMessage_t.m_cbSize > 1117)
		{
			PacketsDropped++;
			return;
		}
		if (!_isKnownPeer(steamID))
		{
			PacketsDropped++;
			return;
		}
		_packetsThisWindow.TryGetValue(steamID, out var value);
		if (value >= 120)
		{
			PacketsDropped++;
			return;
		}
		_packetsThisWindow[steamID] = value + 1;
		Marshal.Copy(steamNetworkingMessage_t.m_pData, _recvScratch, 0, steamNetworkingMessage_t.m_cbSize);
		if (!VoiceWireFormat.TryReadHeader(_recvScratch, steamNetworkingMessage_t.m_cbSize, out var header))
		{
			PacketsDropped++;
			return;
		}
		PacketsReceived++;
		_openSessions.Add(steamID);
		handler(steamID, in header, _recvScratch, VoiceWireFormat.HeaderSize, header.PayloadLength);
	}

	internal void CloseSession(ulong steamId)
	{
		if (_broken || !_openSessions.Remove(steamId))
		{
			return;
		}
		SteamNetworkingIdentity identityRemote = default(SteamNetworkingIdentity);
		identityRemote.SetSteamID64(steamId);
		try
		{
			SteamNetworkingMessages.CloseSessionWithUser(ref identityRemote);
		}
		catch (Exception ex)
		{
			Break("close", ex);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		try
		{
			foreach (ulong item in new List<ulong>(_openSessions))
			{
				SteamNetworkingIdentity identityRemote = default(SteamNetworkingIdentity);
				identityRemote.SetSteamID64(item);
				SteamNetworkingMessages.CloseSessionWithUser(ref identityRemote);
			}
		}
		catch (Exception ex)
		{
			ProximityVoiceChatPlugin.Log.LogWarning("Could not close voice sessions cleanly: " + ex.Message);
		}
		_openSessions.Clear();
		_sessionRequest?.Dispose();
		_sessionRequest = null;
		_sessionFailed?.Dispose();
		_sessionFailed = null;
		if (_sendBuffer != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_sendBuffer);
			_sendBuffer = IntPtr.Zero;
		}
	}
}
