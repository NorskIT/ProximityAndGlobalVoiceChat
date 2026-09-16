using UnityEngine.Audio;

namespace ProximityVoiceChat.Voice.Audio;

internal static class VoiceMixer
{
	private static AudioMixerGroup? _group;

	private static bool _resolved;

	internal static AudioMixerGroup? Group
	{
		get
		{
			if (_resolved)
			{
				return _group;
			}
			AudioMan instance = AudioMan.instance;
			if (instance == null || instance.m_masterMixer == null)
			{
				return null;
			}
			string[] array = new string[4] { "GUI", "SFX", "Sfx", "Master" };
			foreach (string subPath in array)
			{
				AudioMixerGroup[] array2 = instance.m_masterMixer.FindMatchingGroups(subPath);
				if (array2.Length != 0)
				{
					_group = array2[0];
					break;
				}
			}
			if ((object)_group == null)
			{
				_group = instance.m_ambientMixer;
			}
			_resolved = true;
			ProximityVoiceChatPlugin.Log.LogInfo((_group != null) ? ("Voice routed through mixer group \"" + _group.name + "\"") : "No mixer group found, voice goes straight to the master output");
			return _group;
		}
	}

	internal static void Reset()
	{
		_group = null;
		_resolved = false;
	}
}
