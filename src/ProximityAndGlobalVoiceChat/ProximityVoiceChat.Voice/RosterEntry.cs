using UnityEngine;

namespace ProximityVoiceChat.Voice;

internal sealed class RosterEntry
{
	internal ulong SteamId;

	internal ZDOID CharacterId;

	internal string Name = "";

	internal Player? Player;

	internal Vector3 LastKnownPosition;

	internal bool HasPosition;
}
