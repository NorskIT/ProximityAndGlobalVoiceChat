using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using LocalizationManager;
using ProximityVoiceChat.UI;
using ProximityVoiceChat.Voice;
using ServerSync;
using UIManager;
using UnityEngine;

namespace ProximityVoiceChat;

[BepInPlugin("NorskIT.ProximityAndGlobalVoiceChat", "ProximityAndGlobalVoiceChat", "0.2.4")]
[BepInIncompatibility("Azumatt.ProximityVoiceChat")]
public class ProximityVoiceChatPlugin : BaseUnityPlugin
{
	public enum Quality { High, Custom }
    public enum MonitorMode { Processed, Raw }
    public enum Toggle
	{
		On = 1,
		Off = 0
	}

	public enum Bone
	{
		X,
		Y,
		Z
	}

	private class AcceptableShortcuts : AcceptableValueBase
	{
		public AcceptableShortcuts()
			: base(typeof(KeyboardShortcut))
		{
		}

		public override object Clamp(object value)
		{
			return value;
		}

		public override bool IsValid(object value)
		{
			return true;
		}

		public override string ToDescriptionString()
		{
			return "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
		}
	}

	private class ConfigurationManagerAttributes
	{
		[UsedImplicitly]
		public int? Order;

		[UsedImplicitly]
		public bool? Browsable;

		[UsedImplicitly]
		public string? Category;

		[UsedImplicitly]
		public Action<ConfigEntryBase>? CustomDrawer;
	}

	internal const string ModName = "ProximityAndGlobalVoiceChat";

	internal const string ModVersion = "0.2.4";

	internal const string Author = "NorskIT";

	private const string ModGUID = "NorskIT.ProximityAndGlobalVoiceChat";

	private static readonly string ConfigFileName = "NorskIT.ProximityAndGlobalVoiceChat.cfg";

	private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

	private readonly Harmony _harmony = new Harmony("NorskIT.ProximityAndGlobalVoiceChat");

	private static GameObject _runtimeObject = null;

	internal static ProximityVoiceChatPlugin? Instance;

	public static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("ProximityAndGlobalVoiceChat");

	private static readonly ConfigSync ConfigSync = new ConfigSync("NorskIT.ProximityAndGlobalVoiceChat")
	{
		DisplayName = "ProximityAndGlobalVoiceChat",
		CurrentVersion = "0.2.4",
		MinimumRequiredVersion = "0.2.0",
		IsLocked = false
	};

	private static ConfigEntry<Toggle> _serverConfigLocked = null;

	internal static ConfigEntry<KeyboardShortcut> GlobalPushToTalkKey = null;
    internal static ConfigEntry<Quality> VoiceQuality = null;
    internal static ConfigEntry<float> CustomBitrate = null;
    internal static ConfigEntry<Toggle> NoiseSuppression = null;
    internal static ConfigEntry<MonitorMode> MonitorInput = null;
    internal static int EffectiveBitrate => VoiceQuality.Value == Quality.High ? 96000 : (int)CustomBitrate.Value * 1000;
    internal static ConfigEntry<float> MaxRange = null;

	internal static ConfigEntry<float> RangeHysteresis = null;

	internal static ConfigEntry<float> FullVolumeRange = null;

	internal static ConfigEntry<Toggle> EnableVoice = null;

	internal static ConfigEntry<string> InputDevice = null;

	internal static ConfigEntry<Toggle> MuteSelf = null;

	internal static ConfigEntry<KeyboardShortcut> MuteSelfKey = null;

	internal static ConfigEntry<Toggle> UseVoiceActivation = null;

	internal static ConfigEntry<KeyboardShortcut> PushToTalkKey = null;

	internal static ConfigEntry<float> ActivationThreshold = null;

	internal static ConfigEntry<float> MicGain = null;

	internal static ConfigEntry<Toggle> AutoGain = null;

	internal static ConfigEntry<float> AutoGainTarget = null;

	internal static ConfigEntry<int> Bitrate = null;

	internal static ConfigEntry<int> ExpectedPacketLoss = null;

	internal static ConfigEntry<Toggle> UseDtx = null;

	internal static ConfigEntry<Toggle> AdaptiveFec = null;

	internal static ConfigEntry<float> VoiceVolume = null;

	internal static ConfigEntry<Toggle> Deafen = null;

	internal static ConfigEntry<KeyboardShortcut> DeafenKey = null;

	internal static ConfigEntry<float> MonitorVolume = null;

	internal static ConfigEntry<int> JitterTargetMs = null;

	internal static ConfigEntry<int> JitterMaxMs = null;

	internal static ConfigEntry<Toggle> AdaptiveJitter = null;

	internal static ConfigEntry<Toggle> DuckMusic = null;

	internal static ConfigEntry<float> DuckMusicAmount = null;

	internal static ConfigEntry<Toggle> Occlusion = null;

	internal static ConfigEntry<float> OccludedCutoff = null;

	internal static ConfigEntry<float> OccludedVolume = null;

	internal static ConfigEntry<Toggle> UnderwaterMuffle = null;

	internal static ConfigEntry<float> UnderwaterCutoff = null;

	internal static ConfigEntry<Toggle> VoiceReverb = null;

	internal static ConfigEntry<float> ReverbMix = null;

	internal static ConfigEntry<Toggle> ShowSpeakingIndicator = null;

	internal static ConfigEntry<float> SpeakingIndicatorHeight = null;

	internal static ConfigEntry<Toggle> PeerIndicators = null;

	internal static ConfigEntry<float> PeerIndicatorHeight = null;

	internal static ConfigEntry<float> PeerIndicatorScale = null;

	internal static ConfigEntry<Toggle> PeerIndicatorThroughWalls = null;

	internal static ConfigEntry<Toggle> JawMovement = null;

	internal static ConfigEntry<float> JawMaxAngle = null;

	internal static ConfigEntry<float> JawSensitivity = null;

	internal static ConfigEntry<Bone> JawAxis = null;

	internal static ConfigEntry<Toggle> HotkeyMessages = null;

	internal static ConfigEntry<Toggle> TestTone = null;

	internal static ConfigEntry<Toggle> LoopbackTest = null;

	internal static ConfigEntry<string> PerPlayerSettings = null;

	public void Awake()
	{
		Instance = this;
        Log.LogInfo("ProximityAndGlobalVoiceChat 0.2.4 - local fork of Azumatt ProximityVoiceChat 1.0.2");
		Localizer.Load();
		bool saveOnConfigSet = base.Config.SaveOnConfigSet;
		base.Config.SaveOnConfigSet = false;
		_serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, only server admins can change the synced settings.");
		ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
		MaxRange = config("2 - Proximity", "Max Range", 45f, new ConfigDescription("Max distance you can hear other players", new AcceptableValueRange<float>(10f, 200f)));
		RangeHysteresis = config("2 - Proximity", "Range Buffer", 8f, new ConfigDescription("Extra distance before a player disconnects from voice. Helps prevent audio cutting in and out near max range.", new AcceptableValueRange<float>(0f, 50f)));
		FullVolumeRange = config("2 - Proximity", "Full Volume Range", 4f, new ConfigDescription("Players within this distance are heard at full volume. Beyond it, volume fades with distance.", new AcceptableValueRange<float>(1f, 40f)));
		EnableVoice = config("3 - Microphone", "Enable Voice", Toggle.On, "Turn microphone capture off entirely.", synchronizedSetting: false);
		InputDevice = config("3 - Microphone", "Input Device", "", "Microphone to use. Leave blank to use your system default.", synchronizedSetting: false);
		MuteSelf = config("3 - Microphone", "Mute Self", Toggle.Off, "Stops sending your voice without turning off the microphone.", synchronizedSetting: false);
		MuteSelfKey = config("3 - Microphone", "Mute Self Key", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Toggles your microphone mute. Unbound by default to avoid conflicts with other mods.", new AcceptableShortcuts()), synchronizedSetting: false);
		UseVoiceActivation = config("3 - Microphone", "Use Voice Activation", Toggle.Off, "Automatically transmits when you speak. Turn this off to use push-to-talk.", synchronizedSetting: false);
		PushToTalkKey = config("3 - Microphone", "Push To Talk", new KeyboardShortcut(KeyCode.B), new ConfigDescription("Hold this key to talk when Voice Activation is off.", new AcceptableShortcuts()), synchronizedSetting: false);
		ActivationThreshold = config("3 - Microphone", "Activation Threshold", 0.02f, new ConfigDescription("How loud you need to speak before voice activation starts transmitting. Lower values are more sensitive.", new AcceptableValueRange<float>(0.001f, 0.3f)), synchronizedSetting: false);
		MicGain = config("3 - Microphone", "Microphone Gain", 1f, new ConfigDescription("Boosts or lowers your microphone volume before it is sent. Checked before encoding", new AcceptableValueRange<float>(0.1f, 8f)), synchronizedSetting: false);
		AutoGain = config("3 - Microphone", "Auto Gain", Toggle.On, "Chases a steady speaking level so you do not have to hunt for a gain number. Microphone Gain still multiplies on top.", synchronizedSetting: false);
		AutoGainTarget = config("3 - Microphone", "Auto Gain Target", 0.12f, new ConfigDescription("Target speaking volume used by Automatic Gain. Higher values make your voice louder.", new AcceptableValueRange<float>(0.02f, 0.4f)), synchronizedSetting: false);
		GlobalPushToTalkKey = config("3 - Microphone", "Global Push To Talk", new KeyboardShortcut(KeyCode.None), "Hold to talk to everyone running voice protocol 2 (version 0.2.x) in this server.", synchronizedSetting: false);
        VoiceQuality = config("4 - Codec", "Voice Quality", Quality.High, "High uses 96 kbit/s fullband voice. Custom uses Custom Bitrate.");
        CustomBitrate = config("4 - Codec", "Custom Bitrate kbit", 96f, new ConfigDescription("Custom fullband bitrate in kbit/s.", new AcceptableValueRange<float>(32f, 128f)));
        NoiseSuppression = config("3 - Microphone", "Noise Suppression", Toggle.On, "Reduce background noise locally using RNNoise. Turn off for an unprocessed sound in a quiet room.", synchronizedSetting: false);
        MonitorInput = config("5 - Playback", "Monitor Input", MonitorMode.Processed, "Compare raw microphone with processed/encoded audio locally. Monitor audio is never sent to other players.", synchronizedSetting: false);
        Bitrate = config("4 - Codec", "Bitrate", 24000, new ConfigDescription("Legacy setting retained for existing configurations. Use Voice Quality and Custom Bitrate instead.", new AcceptableValueRange<int>(6000, 64000)));
		ExpectedPacketLoss = config("4 - Codec", "Expected Packet Loss", 5, new ConfigDescription("How much packet loss Opus should expect. Higher values use more error correction.", new AcceptableValueRange<int>(0, 40)));
		UseDtx = config("4 - Codec", "Use DTX", Toggle.Off, "Stops sending audio while you are silent. Saves bandwidth, but very quiet words may get cut off.");
		AdaptiveFec = config("4 - Codec", "Adaptive FEC", Toggle.On, "Adds more error correction when other players report lost audio packets.", synchronizedSetting: false);
		VoiceVolume = config("5 - Playback", "Voice Volume", 1f, new ConfigDescription("Overall volume of voice chat.", new AcceptableValueRange<float>(0f, 3f)), synchronizedSetting: false);
		Deafen = config("5 - Playback", "Deafen", Toggle.Off, "Stops all incoming voice and stops you transmitting.", synchronizedSetting: false);
		DeafenKey = config("5 - Playback", "Deafen Key", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Toggles Deafen. Unbound by default to avoid conflicts with other mods.", new AcceptableShortcuts()), synchronizedSetting: false);
		MonitorVolume = config("5 - Playback", "Monitor Volume", 0.7f, new ConfigDescription("How loudly you hear yourself while Hear Myself While I Talk is enabled.", new AcceptableValueRange<float>(0f, 2f)), synchronizedSetting: false);
		JitterTargetMs = config("5 - Playback", "Jitter Buffer Target", 60, new ConfigDescription("Audio buffered to smooth out network hiccups. Lower means less delay; higher is more stable.", new AcceptableValueRange<int>(20, 300)), synchronizedSetting: false);
		JitterMaxMs = config("5 - Playback", "Jitter Buffer Max", 200, new ConfigDescription("Maximum buffer size allowed when Automatic Voice Buffer is enabled.", new AcceptableValueRange<int>(40, 600)), synchronizedSetting: false);
		AdaptiveJitter = config("5 - Playback", "Adaptive Jitter", Toggle.On, "Automatically adjusts each player's audio buffer based on their connection.", synchronizedSetting: false);
		DuckMusic = config("5 - Playback", "Duck Music", Toggle.On, "Lowers Valheim's music while a nearby player is speaking.", synchronizedSetting: false);
		DuckMusicAmount = config("5 - Playback", "Duck Music Amount", 0.35f, new ConfigDescription("Music volume while someone is speaking. 0 is silent, 1 is unchanged.", new AcceptableValueRange<float>(0f, 1f)), synchronizedSetting: false);
		Occlusion = config("5 - Playback", "Occlusion", Toggle.On, "Makes voices quieter and muffled when something solid is between you.", synchronizedSetting: false);
		OccludedCutoff = config("5 - Playback", "Occluded Cutoff", 900f, new ConfigDescription("How muffled voices sound through walls. Lower values sound more muffled.", new AcceptableValueRange<float>(200f, 8000f)), synchronizedSetting: false);
		OccludedVolume = config("5 - Playback", "Occluded Volume", 0.7f, new ConfigDescription("Volume of voices heard through walls. 1 is full volume.", new AcceptableValueRange<float>(0.1f, 1f)), synchronizedSetting: false);
		UnderwaterMuffle = config("5 - Playback", "Underwater Muffle", Toggle.On, "Muffles voice chat while you are swimming.", synchronizedSetting: false);
		UnderwaterCutoff = config("5 - Playback", "Underwater Cutoff", 500f, new ConfigDescription("How muffled voices sound underwater. Lower values sound more muffled.", new AcceptableValueRange<float>(150f, 4000f)), synchronizedSetting: false);
		VoiceReverb = config("5 - Playback", "Voice Reverb", Toggle.Off, "Let the game's reverb zones colour voices, so caves and halls sound like caves and halls.", synchronizedSetting: false);
		ReverbMix = config("5 - Playback", "Reverb Mix", 0.3f, new ConfigDescription("How much of the game's reverb a voice picks up. A full send drowns speech in echo.", new AcceptableValueRange<float>(0f, 1f)), synchronizedSetting: false);
		ShowSpeakingIndicator = config("6 - UI", "Speaking Indicator", Toggle.On, "Shows a speaker icon above your character while you are transmitting.", synchronizedSetting: false);
		SpeakingIndicatorHeight = config("6 - UI", "Speaking Indicator Height", 0.5f, new ConfigDescription("How high the speaker icon sits above your head.", new AcceptableValueRange<float>(0f, 2f)), synchronizedSetting: false);
		PeerIndicators = config("6 - UI", "Peer Speaking Indicators", Toggle.Off, "Shows a speaker icon above other players while they are talking.", synchronizedSetting: false);
		PeerIndicatorHeight = config("6 - UI", "Peer Indicator Height", 0.9f, new ConfigDescription("How high speaker icons sit above other players.", new AcceptableValueRange<float>(0f, 3f)), synchronizedSetting: false);
		PeerIndicatorScale = config("6 - UI", "Peer Indicator Scale", 1f, new ConfigDescription("Size of the speaker icons shown above other players.", new AcceptableValueRange<float>(0.3f, 3f)), synchronizedSetting: false);
		PeerIndicatorThroughWalls = config("6 - UI", "Peer Indicator Through Walls", Toggle.Off, "Keeps speaking indicators visible even when a wall is between you.", synchronizedSetting: false);
		JawMovement = config("6 - UI", "Jaw Movement", Toggle.On, "Moves player jaws while they are speaking. Yours and others.", synchronizedSetting: false);
		JawMaxAngle = config("6 - UI", "Jaw Max Angle", 14f, new ConfigDescription("Maximum amount the jaw opens while speaking.", new AcceptableValueRange<float>(0f, 40f)), synchronizedSetting: false);
		JawSensitivity = config("6 - UI", "Jaw Sensitivity", 12f, new ConfigDescription("How strongly voice volume moves the jaw. Raise this if mouths barely move.", new AcceptableValueRange<float>(1f, 60f)), synchronizedSetting: false);
		JawAxis = config("6 - UI", "Jaw Axis", Bone.X, "Change this if the head twists instead of the mouth opening. Default is good for Valheim model, but might not be for VRMs", synchronizedSetting: false);
		HotkeyMessages = config("6 - UI", "Hotkey Messages", Toggle.On, "Shows a short message when you mute or deafen yourself with a hotkey.", synchronizedSetting: false);
		TestTone = config("6 - UI", "Test Tone", Toggle.Off, "Sends a test tone instead of microphone audio. Useful for testing without a working microphone.", synchronizedSetting: false);
		LoopbackTest = config("6 - UI", "Loopback Test", Toggle.Off, "Plays your outgoing voice back to you so you can hear what other players hear.", synchronizedSetting: false);
		PerPlayerSettings = config("6 - UI", "Per Player Settings", "", new ConfigDescription("Saved data. Don't write here manually, unless you know what you're doing. Written by the player list. steamid:volume:muted, semicolon separated.", null, new ConfigurationManagerAttributes
		{
			Browsable = false
		}), synchronizedSetting: false);
		TestTone.Value = Toggle.Off;
		LoopbackTest.Value = Toggle.Off;
		PeerPreferences.Load(PerPlayerSettings.Value);
		_harmony.PatchAll(Assembly.GetExecutingAssembly());
		_runtimeObject = new GameObject("ProximityVoiceChat_Runtime");
		UnityEngine.Object.DontDestroyOnLoad(_runtimeObject);
		_runtimeObject.hideFlags = HideFlags.HideAndDontSave;
		_runtimeObject.AddComponent<VoiceRuntime>();
		_runtimeObject.AddComponent<SpeakingIndicator>();
		_runtimeObject.AddComponent<JawAnimator>();
		_runtimeObject.AddComponent<VoiceHotkeys>();
		UIRoot.Init(Log, _harmony);
		SetupWatcher();
		base.Config.Save();
		if (saveOnConfigSet)
		{
			base.Config.SaveOnConfigSet = saveOnConfigSet;
		}
	}

	private void OnDestroy()
	{
		if (_runtimeObject != null)
		{
			UnityEngine.Object.Destroy(_runtimeObject);
		}
		if (PeerPreferences.TryFlush(out string serialized))
		{
			PerPlayerSettings.Value = serialized;
		}
		base.Config.Save();
	}

	internal static void SaveConfig()
	{
		if (PeerPreferences.TryFlush(out string serialized))
		{
			PerPlayerSettings.Value = serialized;
		}
		Instance?.Config.Save();
	}

	private void SetupWatcher()
	{
		FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
		fileSystemWatcher.Changed += ReadConfigValues;
		fileSystemWatcher.Created += ReadConfigValues;
		fileSystemWatcher.Renamed += ReadConfigValues;
		fileSystemWatcher.IncludeSubdirectories = true;
		fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
		fileSystemWatcher.EnableRaisingEvents = true;
	}

	private void ReadConfigValues(object sender, FileSystemEventArgs e)
	{
		if (!File.Exists(ConfigFileFullPath))
		{
			return;
		}
		try
		{
			Log.LogDebug("ReadConfigValues called");
			base.Config.Reload();
		}
		catch
		{
			Log.LogError("There was an issue loading your " + ConfigFileName);
			Log.LogError("Please check your config entries for spelling and format!");
		}
	}

	private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
	{
		ConfigDescription configDescription = new ConfigDescription(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
		ConfigEntry<T> configEntry = base.Config.Bind(group, name, value, configDescription);
		ConfigSync.AddConfigEntry(configEntry).SynchronizedConfig = synchronizedSetting;
		return configEntry;
	}

	private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
	{
		return config(group, name, value, new ConfigDescription(description, null), synchronizedSetting);
	}
}
