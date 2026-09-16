using System.Collections.Generic;
using Splatform;
using UnityEngine;

namespace ProximityVoiceChat.Voice;

internal sealed class VoiceRoster
{
	private const float RefreshInterval = 1f;

	private const string SteamPlatform = "Steam";

	private readonly Dictionary<ulong, RosterEntry> _bySteamId = new Dictionary<ulong, RosterEntry>();

	private readonly List<RosterEntry> _entries = new List<RosterEntry>();

	private readonly List<ulong> _removed = new List<ulong>();

	private float _refreshTimer;

	internal ulong LocalSteamId { get; private set; }

	internal IReadOnlyList<RosterEntry> Entries => _entries;

	internal static bool BackendSupported
	{
		get
		{
			if (SteamManager.Initialized && PlatformManager.DistributionPlatform != null)
			{
				return PlatformManager.DistributionPlatform.Platform == "Steam";
			}
			return false;
		}
	}

	internal bool IsKnownPeer(ulong steamId)
	{
		return _bySteamId.ContainsKey(steamId);
	}

	internal bool TryGet(ulong steamId, out RosterEntry entry)
	{
		return _bySteamId.TryGetValue(steamId, out entry);
	}

	internal void Reset()
	{
		_bySteamId.Clear();
		_entries.Clear();
		LocalSteamId = 0uL;
	}

	internal void Refresh(float deltaTime, List<ulong>? departed = null)
	{
		_refreshTimer += deltaTime;
		if (_refreshTimer < 1f)
		{
			ResolvePlayers();
			return;
		}
		_refreshTimer = 0f;
		Rebuild(departed);
		ResolvePlayers();
	}

	private void Rebuild(List<ulong>? departed)
	{
		if (ZNet.instance == null || !BackendSupported)
		{
			departed?.AddRange(_bySteamId.Keys);
			Reset();
			return;
		}
		if (LocalSteamId == 0L)
		{
			PlatformUserID platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
			if (platformUserID.m_platform == "Steam" && platformUserID.TryParseAsUInt64(out var result))
			{
				LocalSteamId = result;
			}
		}
		_removed.Clear();
		_removed.AddRange(_bySteamId.Keys);
		List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
		for (int i = 0; i < playerList.Count; i++)
		{
			ZNet.PlayerInfo playerInfo = playerList[i];
			if (!(playerInfo.m_userInfo.m_id.m_platform != "Steam") && playerInfo.m_userInfo.m_id.TryParseAsUInt64(out var result2) && result2 != 0L && result2 != LocalSteamId)
			{
				_removed.Remove(result2);
				if (!_bySteamId.TryGetValue(result2, out RosterEntry value))
				{
					value = new RosterEntry
					{
						SteamId = result2
					};
					_bySteamId[result2] = value;
					_entries.Add(value);
				}
				value.Name = playerInfo.m_name;
				if (value.CharacterId != playerInfo.m_characterID)
				{
					value.CharacterId = playerInfo.m_characterID;
					value.Player = null;
				}
				if (playerInfo.m_publicPosition)
				{
					value.LastKnownPosition = playerInfo.m_position;
					value.HasPosition = true;
				}
			}
		}
		for (int j = 0; j < _removed.Count; j++)
		{
			ulong num = _removed[j];
			if (_bySteamId.TryGetValue(num, out RosterEntry value2))
			{
				_bySteamId.Remove(num);
				_entries.Remove(value2);
				departed?.Add(num);
			}
		}
	}

	private void ResolvePlayers()
	{
		if (ZNetScene.instance == null)
		{
			return;
		}
		for (int i = 0; i < _entries.Count; i++)
		{
			RosterEntry rosterEntry = _entries[i];
			if (rosterEntry.Player != null && !rosterEntry.Player)
			{
				rosterEntry.Player = null;
			}
			if (rosterEntry.Player == null && !rosterEntry.CharacterId.IsNone())
			{
				GameObject gameObject = ZNetScene.instance.FindInstance(rosterEntry.CharacterId);
				if (gameObject != null)
				{
					rosterEntry.Player = gameObject.GetComponent<Player>();
				}
			}
			if (rosterEntry.Player != null && (bool)rosterEntry.Player)
			{
				rosterEntry.LastKnownPosition = rosterEntry.Player.transform.position;
				rosterEntry.HasPosition = true;
			}
		}
	}
}
