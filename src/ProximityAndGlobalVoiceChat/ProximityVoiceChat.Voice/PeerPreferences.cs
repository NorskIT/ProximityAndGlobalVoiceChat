using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Splatform;
using UnityEngine;
using UserManagement;

namespace ProximityVoiceChat.Voice;

internal static class PeerPreferences
{
	internal const float MaxVolume = 2f;

	private const char RecordSeparator = ';';

	private const char FieldSeparator = ':';

	private const string MutedFlag = "1";

	private const string VolumeFormat = "0.##";

	private const int Fields = 3;

	private static readonly Dictionary<ulong, float> Volumes = new Dictionary<ulong, float>();

	private static readonly HashSet<ulong> Muted = new HashSet<ulong>();

	private static bool _dirty;

	internal static bool IsMutedByUs(ulong steamId)
	{
		return Muted.Contains(steamId);
	}

	internal static bool IsBlockedByGame(ulong steamId)
	{
		PlatformUserID platformUserID = ToUserId(steamId);
		if (!platformUserID.IsValid)
		{
			return false;
		}
		if (!MuteList.Instance.Contains(platformUserID))
		{
			return RelationsManager.IsBlocked(platformUserID);
		}
		return true;
	}

	internal static bool IsSilenced(ulong steamId)
	{
		if (!IsMutedByUs(steamId))
		{
			return IsBlockedByGame(steamId);
		}
		return true;
	}

	internal static void SetMuted(ulong steamId, bool muted)
	{
		if (!(muted ? (!Muted.Add(steamId)) : (!Muted.Remove(steamId))))
		{
			_dirty = true;
		}
	}

	internal static void ToggleMuted(ulong steamId)
	{
		SetMuted(steamId, !IsMutedByUs(steamId));
	}

	internal static float GetVolume(ulong steamId)
	{
		if (!Volumes.TryGetValue(steamId, out var value))
		{
			return 1f;
		}
		return value;
	}

	internal static void SetVolume(ulong steamId, float volume)
	{
		volume = Mathf.Clamp(volume, 0f, 2f);
		if (!Mathf.Approximately(GetVolume(steamId), volume))
		{
			if (Mathf.Approximately(volume, 1f))
			{
				Volumes.Remove(steamId);
			}
			else
			{
				Volumes[steamId] = volume;
			}
			_dirty = true;
		}
	}

	internal static PlatformUserID ToUserId(ulong steamId)
	{
		if (steamId == 0L || PlatformManager.DistributionPlatform == null)
		{
			return PlatformUserID.None;
		}
		return new PlatformUserID(PlatformManager.DistributionPlatform.Platform, steamId);
	}

	internal static bool TryGetSteamId(PlatformUserID user, out ulong steamId)
	{
		steamId = 0uL;
		if (user.IsValid && user.m_platform == "Steam" && user.TryParseAsUInt64(out steamId))
		{
			return steamId != 0;
		}
		return false;
	}

	internal static void Load(string serialized)
	{
		Volumes.Clear();
		Muted.Clear();
		if (string.IsNullOrEmpty(serialized))
		{
			return;
		}
		string[] array = serialized.Split(';');
		foreach (string text in array)
		{
			if (text.Length == 0)
			{
				continue;
			}
			string[] array2 = text.Split(':');
			if (array2.Length >= 3 && ulong.TryParse(array2[0], out var result))
			{
				if (float.TryParse(array2[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && !Mathf.Approximately(result2, 1f))
				{
					Volumes[result] = Mathf.Clamp(result2, 0f, 2f);
				}
				if (array2[2] == "1")
				{
					Muted.Add(result);
				}
			}
		}
		_dirty = false;
	}

	internal static bool TryFlush(out string serialized)
	{
		serialized = "";
		if (!_dirty)
		{
			return false;
		}
		HashSet<ulong> hashSet = new HashSet<ulong>(Muted);
		foreach (ulong key in Volumes.Keys)
		{
			hashSet.Add(key);
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ulong item in hashSet)
		{
			stringBuilder.Append(item).Append(':');
			stringBuilder.Append(GetVolume(item).ToString("0.##", CultureInfo.InvariantCulture)).Append(':');
			stringBuilder.Append(Muted.Contains(item) ? "1" : "0").Append(';');
		}
		serialized = stringBuilder.ToString();
		_dirty = false;
		return true;
	}
}
