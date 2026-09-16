# Validation - 0.2.2 - 2026-09-16

- Release build: passed, 0 errors, 26 warnings in reconstructed/unused source. Identity 0.2.2 passed; wire protocol 2 and ServerSync minimum 0.2.0 retained.
- 725 audio/protocol assertions passed. Includes existing 36 one-minute output simulations and 12 new one-minute local clip timeline simulations over 44.1/48/96 kHz output clocks and 256/512/1024/2048-frame DSP sizes.
- Local tests cover short utterances, repeated ring wraps, clean completion, starvation/refill, bounded oldest-first overflow, disposal and long stalls. The sink emulates Unity clip storage and DSP-scheduled stopping. 3,000 uniquely valued frames retain order across ring wraps. Simulated local output starts below 200 ms; this excludes capture, transport, jitter and physical hardware latency.
- Compared against the installed 0.2.1 assembly: 43 method bodies in PlaybackBuffer, PlaybackResampler, NormalizedAudioResampler, VoiceAudioOutput, NativeAudio, NativeOpus, NoiseSuppressor and VoiceEncoder are unchanged. Routing now selects the separate mono clip backend only for received Local streams; monitor and Global retain the original path.
- Original 1.0.2 DLL inspection confirmed a non-streamed mono clip, spatial blend 1, spread 35 and custom rolloff. 0.2.2 restores that Unity signal path, without restoring the old codec or pitch corrections.

Pending manual verification: actual Unity local direction and source scheduling; camera rotation, diagonal positions, attenuation/environment effects, short and continuous Local speech, mute/deafen, device change, quick Local/Global transitions and unchanged Global listening. Automated sink tests cannot verify Unity panning or whether a scheduling request is accepted by the mixer on a particular machine. No in-game verification is claimed here.

---

# Validation - 0.2.1 - 2026-09-16

- Release build and plugin identity 0.2.1 passed. Protocol remains 2; ServerSync minimum remains 0.2.0.
- 677 automated assertions passed. New production playback-buffer tests simulate 36 one-minute sessions: 44.1/48/96 kHz, DSP blocks of 256/512/1024/2048 frames, and 1/2/6 channels, with irregular main-thread production. All start below 200 ms in the simulation and have no underruns, overflows or periodic gaps during sustained input.
- Prefill, starvation/refill, oldest-first overflow, stopping, concurrent stop/read/write, device-rate replacement and a no-allocation output callback path passed.
- Capture resampler tests now check signal amplitude as well as frame count. They exposed and verified the fix for normalized floats being rounded to silence by the bundled fixed-point Speex implementation. Playback simulations exercise that same conversion.
- User reproduced the 0.2.0 issue with both Raw microphone and Raw test tone, with roughly one-second onset. This excludes RNNoise/Opus and the microphone as necessary causes. The streamed-clip output was replaced, but the exact old Unity callback pattern was not recorded.

Pending for 0.2.1: actual Unity/Mono launch, continuous Raw tone and microphone listening, startup latency, output-device changes, local spatial/muffle/reverb behavior, global/mute/deafen and two-client tests. These checks require the running game; no perceptual or in-game success is claimed by the simulated tests.

---

# Validation - 0.2.0 - 2026-09-16

Automated checks use production audio/protocol source and shipped Windows x64 native DLLs; Unity playback and Steam networking still require game testing.

- Release build succeeds against installed Valheim/BepInEx.
- 649 game/BepInEx member references and 22 declared Harmony targets resolve.
- Audio tests: 590 assertions pass, including global/local priority, blocked input, compatibility/range routing, channel provenance across partial frames, protocol roundtrip/rejection, sequence wrap, reorder/duplicate/late packets, FEC-or-PLC, end-of-stream, bounded backlog, quiet PTT, raw monitoring and resampling from 16/44.1/96 kHz.
- Native runtime reports libopus 1.6.1. RNNoise's embedded model loads and processes finite samples.
- Synthetic 12 kHz tone decoded RMS: 0.1412 with the new codec, 0.000190 with the original forced SILK/wideband configuration. This demonstrates bandwidth improvement, not speech quality parity with another application.
- Synthetic stationary noise reduction: about 65 dB after warmup. Not a claim about real speech intelligibility.
- RNNoise + one encoder + eight decoders: about 0.55 ms per 20 ms frame on this machine with harmonic input. Not a network or Unity performance benchmark.
- Isolated deployment checks pass: fresh install, replacement, both rollback cases, original-plugin conflict, wrong-profile rejection and preservation of unrelated files.
- Package checks enforce managed identity 0.2.0, exactly the expected files, matching managed build and locked native hashes. Old managed codec is excluded from production.

Manual checks remaining for this version: launch/load in Unity Mono, menu layout, actual microphone processing, raw-versus-processed listening, two-client global/local routing, mute/deafen, environmental effects, reconnect and long-session clock drift. The user's successful 0.1.0 microphone test and original-mod Internet test do not verify this new protocol/audio implementation.

Run `scripts/Test-Baseline.ps1`, `scripts/Build-Package.ps1`, and `scripts/Test-Deployment.ps1` to reproduce automated checks. Follow README for the in-game test.

Deployment completed through Gale's CLI. Read-only database inspection confirms Development has ProximityAndGlobalVoiceChat 0.2.0 enabled. Exactly one active voice plugin was found. Installed managed DLL SHA-256 matches Release: D00CAF0C7150B2FA7BC13B04A18E3DC198F741E0A68BD7A21220A1E00AB18FBD. Both installed native DLL hashes match native/dependencies.lock.json. Valheim was closed during installation; no game launch or multiplayer test is claimed.

0.2.1 installation: Gale Development registration confirmed enabled; exactly one active voice plugin; managed and both native DLL hashes match Release. Package API checks: 652 references and 22 Harmony targets resolve.

0.2.2 deployment: Gale Development confirmed enabled, one active voice plugin, all installed DLL hashes match Release. Package checks resolved 658 game/BepInEx references and 22 Harmony targets.
