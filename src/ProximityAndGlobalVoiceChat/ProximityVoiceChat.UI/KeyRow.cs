using BepInEx.Configuration;
using UnityEngine.UI;

namespace ProximityVoiceChat.UI;

internal sealed class KeyRow
{
	internal Button Button;

	internal Text Label;

	internal ConfigEntry<KeyboardShortcut> Entry;

	internal void Refresh()
	{
		Label.text = (KeyBinder.IsListening(Entry) ? UIBuild.Localize("$pvc_key_press") : KeyBinder.Describe(Entry.Value));
	}
}
