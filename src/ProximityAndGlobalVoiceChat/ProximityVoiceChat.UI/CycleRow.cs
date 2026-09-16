using System;
using BepInEx.Configuration;
using UnityEngine.UI;

namespace ProximityVoiceChat.UI;

internal sealed class CycleRow<T> where T : struct, Enum
{
	internal Button Button;

	internal Text Label;

	internal ConfigEntry<T> Entry;

	internal void Step()
	{
		T[] array = (T[])Enum.GetValues(typeof(T));
		int num = Array.IndexOf(array, Entry.Value);
		Entry.Value = array[(num + 1 + array.Length) % array.Length];
		Refresh();
	}

	internal void Refresh()
	{
		Label.text = Entry.Value.ToString();
	}
}
