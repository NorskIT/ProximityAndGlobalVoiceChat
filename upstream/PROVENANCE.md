# Upstream baseline

- Author: Azumatt. Original mod: ProximityVoiceChat, version 1.0.2.
- Mod page: https://valheim.hexium.gg/mods/Azumatt/ProximityVoiceChat
- Release package: https://cdn.hexium.gg/upload/834/1.0.2.zip
- Original DLL SHA-256: `EE551D85B18A1BD78543810C5280B4A5B0923697AC6040504156058AA737D16D`.
- Source: DLL from Gale's cached, unmodified 1.0.2 package, reconstructed on 2026-09-16 with ILSpyCmd 11.0.0.9375. This is reconstructed source, not the author's original repository/history.
- The original MIT license and all third-party notices supplied in that package are retained under `licenses/`. The package icon is new artwork supplied by NorskIT; its original is preserved in `images/icon-source.png`.

## Reconstruction

Export command (reference directory contains the installed game's Managed DLLs and Development/BepInEx/core DLLs):

```powershell
ilspycmd -p -o export -r reference-directory ProximityVoiceChat.dll
```

Namespace directories for embedded libraries were moved under `ThirdParty/`, preserving their namespaces. Concentus, YamlDotNet, ServerSync, LocalizationManager and UIManager remain compiled into the fork DLL. Embedded English translations and ILRepack.List retain their original resource names; the latter is upstream merge metadata, not a runtime dependency on the original plugin.

## Initial reconstruction changes (0.1.0)

- Fork plugin/config/Harmony/ServerSync identities, version, assembly metadata and startup message; incompatibility marker prevents running alongside the original plugin.
- Build against current local Valheim/Unity/BepInEx, using the game's Mono framework references to match Unity's Span types. Framework target remains net481; game DLLs are not distributed.
- Build-only BepInEx.AssemblyPublicizer 0.4.2 exposes members already used by upstream in assembly_valheim and assembly_guiutils; game installation files remain untouched. See https://github.com/BepInEx/BepInEx.AssemblyPublicizer.
- Explicit empty implementations of ISettingsTab's three default methods; equivalent behavior on the net481 compiler target.
- UIManager console registration uses current constructor defaults, accommodating the new optional hideBehindDevCommands argument.
- C# 12 extension-method syntax, array.Length - 1 instead of index-from-end, and float Math constants instead of ILSpy-generated MathF constants.
- Exclude duplicate nullable attributes supplied by the game's Mono framework and an unreferenced compiler data holder from compilation. ILSpy already expanded its array use.
- No intentional changes to audio behavior, wire protocol, Steam channel or default settings. Original ServerSync configuration identity is not shared with this fork.

Later versions add global voice, native Opus/RNNoise and playback changes. See `DEVELOPMENT.md` and `VALIDATION.md` for the current implementation and checks. These checks do not replace testing within Unity/Valheim.
