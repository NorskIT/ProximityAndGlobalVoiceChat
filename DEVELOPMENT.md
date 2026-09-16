# ProximityAndGlobalVoiceChat

NorskIT's fork of Azumatt ProximityVoiceChat 1.0.2. Version **0.2.2** restores the original mono/Unity 3D output for received Local voice while keeping Global and monitor playback from 0.2.1. Global push-to-talk and fullband audio were introduced in 0.2.0. Windows x64 Steam clients only. Voice protocol 2 is compatible across 0.2.0, 0.2.1 and 0.2.2; original 1.0.2 and fork 0.1.0 are incompatible with its voice protocol.

## Test from Gale

Select **Development**, confirm **ProximityAndGlobalVoiceChat 0.2.2** is enabled, then **Launch modded**.

1. Open **Settings > Voice > Keys**. **Push to talk locally** preserves your existing binding (B by default). Bind **Push to talk globally** to a different key; it starts unbound. Holding both selects global. Voice activation remains local.
2. Select your microphone. **Voice quality: High** uses 96 kbit/s; Custom allows 32-128. Noise suppression starts enabled. Existing microphone gain, keybinds and volume settings are preserved.
3. Enable **Hear myself while I talk**, hold a talk key, and compare **Monitor input: Raw / Processed**. Raw bypasses processing and encoding. Processed uses the new capture/codec/playback chain. Both are non-positional and local only; while monitoring is enabled, your voice is not sent to anyone. Turn monitoring and the test tone off before multiplayer.
4. With two clients on the same server, test local near/far, global across the map, both keys, rapid switches, mute/deafen, individual mute/volume, and leaving/re-entering local range. Global bypasses distance, walls, water and world reverb. Local retains those effects. Check compatible peer count in Voice settings.
5. Restart and verify bindings/settings persist. Test natural speech, quiet words, sudden loud words and background keyboard/fan noise. Toggle suppression off to judge whether it damages your particular microphone's sound.

The server does not need the mod for voice. Peers are discovered from the complete server player list, including players without shared map positions. Steam transport remains required. One voice plugin per client.

## Local direction fix in 0.2.2

Received Local voice uses a non-streaming 48 kHz mono AudioClip at the speaker's head, with Unity spatial blend 1 and the original spread/distance curve. The game's existing camera AudioListener supplies orientation. Local keeps its wall/water filters and optional reverb. Global and microphone monitoring continue using the clipless DSP generator from 0.2.1. No HRTF, new native libraries or wire changes are introduced.

The mono clip is a one-second storage ring, not a one-second startup delay. Decoded frames fill at least 60 ms (or two DSP blocks, capped at the 200 ms queue limit), then playback is DSP-scheduled with at least 20 ms/two-block lead. Pitch stays fixed at 1. Played slots are cleared, excess backlog drops oldest frames, and each append updates the mixer-scheduled end so a long main-thread stall cannot loop old speech. Underruns refill before restart; a completed short utterance can start without waiting for full prefill. Unity converts the local clip to the device output rate. Device changes rebuild the local output.

Test with two PCs and Hear myself OFF. Have the speaker walk around the listener, including diagonals, while the listener rotates the camera independently of the character. Compare with the original mod's directional behavior. Check distance, walls/water, mute/deafen, a minute of continuous Local speech, and quick Local/Global changes. Global must remain centered and retain its existing sound. Self-monitoring intentionally does not test positional audio. Verify actual Unity playback before calling the directional fix confirmed.

## Playback fix in 0.2.1

A clipless `OnAudioFilterRead` generator replaces the streamed one-second clip. The output buffer prefills at least 60 ms or two DSP blocks, refills after starvation, and discards oldest samples on overflow instead of clearing all queued audio. The normal buffer limit is 200 ms; unusually large hardware DSP blocks require a larger capacity. Spatial audio, voice volume and local environmental filters still use each voice's AudioSource. No pitch adjustment is used.

Raw and Processed use the same output path. Audio is resampled to the device output rate on the main thread. Fixed-point Speex now receives explicitly scaled PCM, correcting a separate silence bug for microphones and outputs not running at 48 kHz. The output callback only reads prepared PCM; it performs no encoding, logging or managed allocation. A device-configuration change stops and replaces the output buffer/resampler on the main thread.

The old `sent` meter is now called `processed`. `heard` measures PCM actually delivered by the output callback, before downstream Unity mixing/spatial effects and hardware. Monitor diagnostics show output rate, queued milliseconds, DSP block size, underruns, dropped samples and prefill startup time. That startup figure measures first queued output to first callback delivery, not total microphone-to-ear latency.

For the regression check: enable Hear myself and Raw, enable test tone, hold one talk key for 60 seconds, and listen for periodic gaps. Then turn the tone off and compare microphone Raw / Processed. Initial silence without a held key is expected with voice activation off. Confirm normal startup is below roughly 200 ms on your setup. Check both local/global, mute/deafen and two-client spatial behavior. Automated DSP simulations do not replace the Unity listening test.

## Audio changes

The original codec forced SILK-only wideband at 24 kbit/s. This restricted upper frequencies despite capturing at 48 kHz. Version 0.2 uses native Opus 1.6.1, automatic mode/fullband allowed, 48 kHz mono, 20 ms frames, 96 kbit/s VBR and complexity 10. Actual mode can adapt to input and loss; FEC is available when supported by the selected Opus mode, otherwise loss concealment applies.

RNNoise 0.2 runs locally before bounded automatic gain and a peak limiter. Hold-to-talk does not use the voice-activation threshold. Stateful Speex resampling replaces linear resampling for non-48-kHz microphones. Packets are reordered before decoding; playback uses a fixed sample rate rather than changing voice pitch to correct a buffer. Default prefill is 60 ms with a 200 ms compressed-packet backlog limit; microphone, codec, output buffering and hardware add further latency.

This addresses measurable restrictions in the old code. It does not establish perceptual parity with Discord; hardware and real multiplayer listening tests are still necessary. Native Opus uses the portable floating-point implementation; optional Opus neural extensions are not enabled. RNNoise includes its default model.

## Build, test and update

Requirements: Windows x64, .NET SDK 10, installed Valheim and BepInEx in Development. Source references the installed game's Mono assemblies. Override `VALHEIM_INSTALL` and `BEPINEX_PATH` in ignored `Environment.local.props` if needed.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Baseline.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Import-Gale.ps1
```

Close Valheim before import. Gale's CLI registers the local mod and updates the visible list asynchronously. Package output: `artifacts/NorskIT-ProximityAndGlobalVoiceChat-0.2.2.zip`. For a second client use Gale's local-mod import. Disable the original mod first.

Normal builds use hash-verified DLLs in `native/win-x64`. `scripts/Build-Native.ps1` rebuilds from pinned Opus/RNNoise commits using downloaded, hash-verified Zig 0.14.1 and RNNoise model data. It refreshes artifact hashes in `native/dependencies.lock.json` and the packaged notice; review these changes after rebuilding. PE timestamps may change artifact hashes. The package includes the managed plugin plus `Native/pagvc_opus.dll` and `Native/pagvc_rnnoise.dll`, license notices and provenance. Missing native dependencies produce a visible audio error, with no fallback to the old codec.

`Build-Package.ps1` checks installed game API references, Harmony targets, package allowlist and hashes. `Test-Deployment.ps1` tests unmanaged installation/rollback in an isolated profile. `Deploy-Local.ps1` is only for unmanaged Development profiles and refuses to overwrite Gale's installation.

Configuration: `BepInEx/config/NorskIT.ProximityAndGlobalVoiceChat.cfg`. The old `Bitrate` entry is retained but superseded by Voice Quality / Custom Bitrate. Log: `%APPDATA%/com.kesomannen.gale/valheim/profiles/Development/BepInEx/LogOutput.log`.

## Source and attribution

Mod source: `src/ProximityAndGlobalVoiceChat/ProximityVoiceChat*`. Reconstructed upstream libraries: `ThirdParty`. The old managed Concentus codec is compiled only into the regression test executable; production retains its Speex resampler and math helpers. `tools/TestAudio` exercises actual shared audio/protocol source against the shipped native binaries. `tools/VerifyFork` checks metadata and references. Neither tool is installed into the game.

See `VALIDATION.md`, `upstream/PROVENANCE.md`, `native/dependencies.lock.json` and `licenses/`. Original mod copyright (c) 2026 Azumatt, fork changes copyright (c) 2026 NorskIT, MIT licensed. No public release or upstream Git history is claimed.
