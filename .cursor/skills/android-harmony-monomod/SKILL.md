---
name: android-harmony-monomod
description: >-
  Harmony and MonoMod references for the Android SMAPI build on .NET 9 arm64.
  Use when editing Android patches, SMAPI.csproj Android references, DependenciesDll Harmony or MonoMod binaries, or debugging LocalBuilder or detour crashes.
---

# Android Harmony and MonoMod

The Android loader embeds thin Harmony 2.4.2 plus modular MonoMod from AndroidSMAPI `SharedLibs/`. This repo must compile against those same files in `src/DependenciesDll/`.

## Required binaries

| File | Notes |
|---|---|
| `0Harmony.dll` | Harmony 2.4.2, about 300KB. References external `MonoMod.Core`. |
| `MonoMod.Core.dll` | 1.3.1. Includes former `RuntimeDetour`. |
| `MonoMod.Utils.dll` | 25.0.9 |
| `MonoMod.Backports.dll`, `MonoMod.Iced.dll`, `MonoMod.ILHelpers.dll` | Match `SharedLibs/` byte for byte |

Copy updates from `../AndroidSMAPI/SharedLibs/`. Desktop `build/0Harmony.dll` is a different, older build. Leave it alone.

## Do not

- Add `PackageReference` `Lib.Harmony`. Fat Harmony 2.3 throws `Cannot create an instance of System.Reflection.Emit.LocalBuilder because it is an abstract class` on .NET 9.
- Reference `MonoMod.RuntimeDetour.dll`.
- Add `0Harmony.dll` or `MonoMod.*.dll` to `src/PackSMAPIZip/dependencies.txt`.

## Patches

- Keep patches small. Prefer prefix and postfix over transpilers on ARM64 Mono.
- Resolve methods with `AccessTools` rather than hard-coded binding flags.
- `MiniMonoModHotfix.Apply()` is desktop-only (`.NET Framework` field order). Do not call it under `SMAPI_FOR_ANDROID`.
