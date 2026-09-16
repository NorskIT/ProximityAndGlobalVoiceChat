using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ProximityVoiceChat.Voice;
using ProximityVoiceChat.Voice.Audio;
using UIManager;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace ProximityVoiceChat.UI;

internal sealed class VoiceSettingsTab : MonoBehaviour, ISettingsTab
{
	// Explicit equivalents of Valheim's default interface methods for net481.
	public void Terminate() { }
	public void OnBack() { }
	public void OnSharedSettingChanged(string setting, int value) { }

	private Toggle? _noise; private SliderRow? _bitrate; private CycleRow<ProximityVoiceChatPlugin.Quality>? _quality; private CycleRow<ProximityVoiceChatPlugin.MonitorMode>? _monitorMode;
private Text? _device;

	private Text? _status;

	private RectTransform? _inFill;

	private RectTransform? _rmsFill;

	private RectTransform? _outFill;

	private Text? _numbers;

	private Text? _stats;

	private SliderRow? _gain;

	private SliderRow? _gate;

	private SliderRow? _incoming;

	private SliderRow? _monitor;

	private Toggle? _enableVoice;

	private Toggle? _activation;

	private Toggle? _muteSelf;

	private Toggle? _deafen;

	private Toggle? _monitorOn;

	private Toggle? _autoGain;

	private Toggle? _duckMusic;

	private Toggle? _adaptiveJitter;

	private Toggle? _testTone;

	private Toggle? _speakingIndicator;

	private Toggle? _peerIndicators;

	private Toggle? _peerThroughWalls;

	private Toggle? _jawMovement;

	private Toggle? _occlusion;

	private Toggle? _underwater;

	private Toggle? _reverb;

	private SliderRow? _selfHeight;

	private SliderRow? _peerHeight;

	private SliderRow? _peerScale;

	private SliderRow? _jawAngle;

	private SliderRow? _jawSensitivity;

	private CycleRow<ProximityVoiceChatPlugin.Bone>? _jawAxis;

	private readonly List<KeyRow> _keyRows = new List<KeyRow>();

	private const float TextInterval = 0.15f;

	private const int ClipWarningFrames = 50;

	private const int ContentPadding = 24;

	private const float ContentSpacing = 6f;

	private const float ProbeButtonWidth = 220f;

	private const float ProbeButtonHeight = 26f;

	private float _textTimer;

	public event Action<string, int>? SharedSettingChanged;

	public void Initialize()
	{
		try
		{
			Build();
		}
		catch (Exception arg)
		{
			ProximityVoiceChatPlugin.Log.LogError($"Voice settings tab failed to build: {arg}");
		}
	}

	private void Build()
	{
		RectTransform obj = (RectTransform)UIFactory.ScrollView("VoiceScroll", base.transform, new Vector2(100f, 100f), out var content).transform;
		obj.anchorMin = Vector2.zero;
		obj.anchorMax = Vector2.one;
		obj.offsetMin = Vector2.zero;
		obj.offsetMax = Vector2.zero;
		Transform parent = content;
		VerticalLayoutGroup component = content.GetComponent<VerticalLayoutGroup>();
		if ((object)component != null)
		{
			component.padding = new RectOffset(24, 24, 24, 24);
			component.spacing = 6f;
			component.childControlWidth = true;
			component.childForceExpandWidth = true;
			component.childControlHeight = true;
			component.childForceExpandHeight = false;
		}
		UIBuild.Heading(parent, UIBuild.Localize("$pvc_heading_microphone"));
		_enableVoice = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_enable_voice"), ProximityVoiceChatPlugin.EnableVoice);
		_status = UIBuild.Note(parent, "");
		_device = UIBuild.DevicePicker(parent, UIBuild.Localize("$pvc_input_device"));
		_inFill = UIBuild.Meter(parent, UIBuild.Localize("$pvc_meter_input"));
		_rmsFill = UIBuild.Meter(parent, UIBuild.Localize("$pvc_meter_sent"));
		_outFill = UIBuild.Meter(parent, UIBuild.Localize("$pvc_meter_heard"));
		_numbers = UIBuild.Note(parent, "");
		_gain = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_mic_gain"), ProximityVoiceChatPlugin.MicGain);
		_gate = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_gate_threshold"), ProximityVoiceChatPlugin.ActivationThreshold, "0.000");
		_activation = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_voice_activation"), ProximityVoiceChatPlugin.UseVoiceActivation);
		_autoGain = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_auto_gain"), ProximityVoiceChatPlugin.AutoGain);
		_noise = UIBuild.ToggleFor(parent, "Noise suppression", ProximityVoiceChatPlugin.NoiseSuppression);
_quality = UIBuild.Cycle(parent, "Voice quality", ProximityVoiceChatPlugin.VoiceQuality);
_bitrate = UIBuild.SliderFor(parent, "Custom bitrate (kbit/s)", ProximityVoiceChatPlugin.CustomBitrate, "0");
_muteSelf = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_mute_self"), ProximityVoiceChatPlugin.MuteSelf);
		UIBuild.Heading(parent, UIBuild.Localize("$pvc_heading_listening"));
		_incoming = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_incoming_volume"), ProximityVoiceChatPlugin.VoiceVolume);
		_deafen = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_deafen"), ProximityVoiceChatPlugin.Deafen);
		_duckMusic = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_duck_music"), ProximityVoiceChatPlugin.DuckMusic);
		_adaptiveJitter = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_adaptive_jitter"), ProximityVoiceChatPlugin.AdaptiveJitter);
		_occlusion = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_occlusion"), ProximityVoiceChatPlugin.Occlusion);
		_underwater = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_underwater"), ProximityVoiceChatPlugin.UnderwaterMuffle);
		_reverb = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_reverb"), ProximityVoiceChatPlugin.VoiceReverb);
		UIBuild.Heading(parent, UIBuild.Localize("$pvc_heading_keys"));
		_keyRows.Add(UIBuild.KeyBinding(parent, UIBuild.Localize("$pvc_key_ptt"), ProximityVoiceChatPlugin.PushToTalkKey));
		_keyRows.Add(UIBuild.KeyBinding(parent, "Push to talk globally", ProximityVoiceChatPlugin.GlobalPushToTalkKey));
_keyRows.Add(UIBuild.KeyBinding(parent, UIBuild.Localize("$pvc_key_mute"), ProximityVoiceChatPlugin.MuteSelfKey));
		_keyRows.Add(UIBuild.KeyBinding(parent, UIBuild.Localize("$pvc_key_deafen"), ProximityVoiceChatPlugin.DeafenKey));
		UIBuild.Note(parent, UIBuild.Localize("$pvc_note_binding"));
		UIBuild.Heading(parent, UIBuild.Localize("$pvc_heading_interface"));
		_speakingIndicator = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_speaking_indicator"), ProximityVoiceChatPlugin.ShowSpeakingIndicator);
		_selfHeight = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_indicator_height"), ProximityVoiceChatPlugin.SpeakingIndicatorHeight);
		_peerIndicators = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_peer_indicators"), ProximityVoiceChatPlugin.PeerIndicators);
		_peerHeight = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_peer_indicator_height"), ProximityVoiceChatPlugin.PeerIndicatorHeight);
		_peerScale = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_peer_indicator_scale"), ProximityVoiceChatPlugin.PeerIndicatorScale);
		_peerThroughWalls = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_peer_through_walls"), ProximityVoiceChatPlugin.PeerIndicatorThroughWalls);
		_jawMovement = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_jaw_movement"), ProximityVoiceChatPlugin.JawMovement);
		_jawAngle = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_jaw_angle"), ProximityVoiceChatPlugin.JawMaxAngle, "0");
		_jawSensitivity = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_jaw_sensitivity"), ProximityVoiceChatPlugin.JawSensitivity, "0");
		_jawAxis = UIBuild.Cycle(parent, UIBuild.Localize("$pvc_jaw_axis"), ProximityVoiceChatPlugin.JawAxis);
		UIBuild.Heading(parent, UIBuild.Localize("$pvc_heading_test"));
		_monitor = UIBuild.SliderFor(parent, UIBuild.Localize("$pvc_monitor_volume"), ProximityVoiceChatPlugin.MonitorVolume);
		_monitorOn = UIBuild.RawToggle(parent, UIBuild.Localize("$pvc_hear_myself"), ProximityVoiceChatPlugin.LoopbackTest.Value.IsOn(), delegate(bool on)
		{
			ProximityVoiceChatPlugin.LoopbackTest.Value = (on ? ProximityVoiceChatPlugin.Toggle.On : ProximityVoiceChatPlugin.Toggle.Off);
		});
		_monitorMode = UIBuild.Cycle(parent, "Monitor input", ProximityVoiceChatPlugin.MonitorInput);
UIBuild.Note(parent, "Raw / Processed comparison is local only. While Hear myself is on, your voice is not sent to other players.");
_testTone = UIBuild.ToggleFor(parent, UIBuild.Localize("$pvc_test_tone"), ProximityVoiceChatPlugin.TestTone);
		GameObject gameObject = UIBuild.Row(parent, 28f);
		UIBuild.Flex(UIFactory.Button("Probe", gameObject.transform, UIBuild.Localize("$pvc_probe_button"), new Vector2(220f, 26f), delegate
		{
			VoiceRuntime.Instance?.Capture?.ProbeRaw();
		}));
		_stats = UIBuild.Note(parent, ""); UIBuild.Element(_stats).preferredHeight = 64f;
		UIBuild.Note(parent, UIBuild.Localize("$pvc_note_perplayer"));
		UIBuild.Note(parent, UIBuild.Localize("$pvc_note_menu_mic"));
	}

	public void OnTabOpen(Button backButton, Button okButton)
	{
	}

	private void OnEnable()
	{
		VoiceRuntime.LocalTestRequested = true;
	}

	private void OnDisable()
	{
		VoiceRuntime.LocalTestRequested = false;
		KeyBinder.Cancel();
	}

	public void OnOkAsync(OkActionCompletedHandler okActionCompletedCallback)
	{
		ProximityVoiceChatPlugin.SaveConfig();
		okActionCompletedCallback?.Invoke();
	}

	private void Update()
	{
		KeyBinder.Tick();
		for (int i = 0; i < _keyRows.Count; i++)
		{
			_keyRows[i].Refresh();
		}
		VoiceRuntime instance = VoiceRuntime.Instance;
		MicrophoneCapture microphoneCapture = instance?.Capture;
		VoiceEncoder voiceEncoder = instance?.Encoder;
		float num = 0f;
		if (instance?.Playback != null && instance.Playback.Streams.TryGetValue(1uL, out PeerVoiceStream value))
		{
			num = value.OutputRms;
		}
		SetFill(_inFill, microphoneCapture?.PeakLevel ?? 0f);
		SetFill(_rmsFill, voiceEncoder?.LastRms ?? 0f);
		SetFill(_outFill, num);
		_textTimer += Time.unscaledDeltaTime;
		if (_textTimer >= 0.15f)
		{
			_textTimer = 0f;
			RefreshText(microphoneCapture, voiceEncoder, instance, num);
		}
		_gain?.Set(ProximityVoiceChatPlugin.MicGain.Value);
		_gate?.Set(ProximityVoiceChatPlugin.ActivationThreshold.Value);
		_incoming?.Set(ProximityVoiceChatPlugin.VoiceVolume.Value);
		_monitor?.Set(ProximityVoiceChatPlugin.MonitorVolume.Value);
		_selfHeight?.Set(ProximityVoiceChatPlugin.SpeakingIndicatorHeight.Value);
		_peerHeight?.Set(ProximityVoiceChatPlugin.PeerIndicatorHeight.Value);
		_peerScale?.Set(ProximityVoiceChatPlugin.PeerIndicatorScale.Value);
		_jawAngle?.Set(ProximityVoiceChatPlugin.JawMaxAngle.Value);
		_jawSensitivity?.Set(ProximityVoiceChatPlugin.JawSensitivity.Value);
		_jawAxis?.Refresh(); _quality?.Refresh(); _monitorMode?.Refresh(); _bitrate?.Set(ProximityVoiceChatPlugin.CustomBitrate.Value); SetToggle(_noise, ProximityVoiceChatPlugin.NoiseSuppression);
		SetToggle(_enableVoice, ProximityVoiceChatPlugin.EnableVoice);
		SetToggle(_activation, ProximityVoiceChatPlugin.UseVoiceActivation);
		SetToggle(_muteSelf, ProximityVoiceChatPlugin.MuteSelf);
		SetToggle(_deafen, ProximityVoiceChatPlugin.Deafen);
		SetToggle(_monitorOn, ProximityVoiceChatPlugin.LoopbackTest);
		SetToggle(_autoGain, ProximityVoiceChatPlugin.AutoGain);
		SetToggle(_duckMusic, ProximityVoiceChatPlugin.DuckMusic);
		SetToggle(_adaptiveJitter, ProximityVoiceChatPlugin.AdaptiveJitter);
		SetToggle(_testTone, ProximityVoiceChatPlugin.TestTone);
		SetToggle(_speakingIndicator, ProximityVoiceChatPlugin.ShowSpeakingIndicator);
		SetToggle(_peerIndicators, ProximityVoiceChatPlugin.PeerIndicators);
		SetToggle(_peerThroughWalls, ProximityVoiceChatPlugin.PeerIndicatorThroughWalls);
		SetToggle(_jawMovement, ProximityVoiceChatPlugin.JawMovement);
		SetToggle(_occlusion, ProximityVoiceChatPlugin.Occlusion);
		SetToggle(_underwater, ProximityVoiceChatPlugin.UnderwaterMuffle);
		SetToggle(_reverb, ProximityVoiceChatPlugin.VoiceReverb);
	}

	private void RefreshText(MicrophoneCapture? capture, VoiceEncoder? encoder, VoiceRuntime? runtime, float heard)
	{
		if (_device != null)
		{
			_device.text = UIBuild.CurrentDeviceName();
		}
		if (_status != null)
		{
			_status.text = ((capture == null) ? UIBuild.Localize("$pvc_no_microphone") : UIBuild.Localize("$pvc_status_device", capture.Device ?? "", capture.DeviceRate, capture.Channels));
		}
		if (_status != null && runtime != null) _status.text += runtime.AudioError != null ? " | Audio error: " + runtime.AudioError : " | Channel: " + runtime.SelectedChannel;
if (_stats != null)
		{
			SteamVoiceTransport steamVoiceTransport = runtime?.Transport;
			_stats.text = ((steamVoiceTransport == null) ? UIBuild.Localize("$pvc_not_connected") : UIBuild.Localize("$pvc_stats", steamVoiceTransport.PacketsSent, steamVoiceTransport.PacketsReceived, steamVoiceTransport.PacketsDropped));
		}
		if (_stats != null && runtime != null) { int compatible = 0; foreach (var link in runtime.Links.Values) if (link.Compatible) compatible++; _stats.text += " | Compatible voice peers: " + compatible + "/" + runtime.Links.Count + " (others: unavailable or incompatible)"; }
if (_stats != null && runtime?.Playback != null && runtime.Playback.Streams.TryGetValue(VoiceRuntime.LoopbackId, out var monitorStream)) _stats.text += "\n" + monitorStream.Diagnostics;
if (!(_numbers == null))
		{
			string text = ((encoder != null && encoder.FramesSinceClip < 50) ? ("   <color=#ff8080>" + UIBuild.Localize("$pvc_too_loud") + "</color>") : "");
			_numbers.text = UIBuild.Localize("$pvc_numbers", (capture?.PeakLevel ?? 0f).ToString("0.000"), (encoder?.LastRms ?? 0f).ToString("0.000"), heard.ToString("0.000"), (-60f).ToString("0")) + text;
		}
	}

	private static void SetFill(RectTransform? fill, float value)
	{
		if (fill != null)
		{
			fill.anchorMax = new Vector2(UIBuild.MeterScale(value), 1f);
		}
	}

	private static void SetToggle(Toggle? toggle, ConfigEntry<ProximityVoiceChatPlugin.Toggle> entry)
	{
		if (toggle != null)
		{
			toggle.SetIsOnWithoutNotify(entry.Value.IsOn());
		}
	}
}
