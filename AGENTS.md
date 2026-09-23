# SMAPI agent guide

This repository is Pathoschild SMAPI plus the Android port of the modding API. The Android loader (APK, assembly-store extraction, launcher UI) lives in the sibling **AndroidSMAPI** repo. Do not reimplement that loader here.

## What this repo builds

- Desktop SMAPI stays `net6.0` and references `build/0Harmony.dll`. Do not retarget desktop projects to match the loader.
- Android SMAPI is `net9.0-android35.0`, configuration `Android Debug` or `Android Release`, `AndroidBuild=true`, constant `SMAPI_FOR_ANDROID`. Mobile sources are `src/SMAPI/Mobile/**` and are excluded from desktop builds.
- The website and tests target `net10.0`. `global.json` pins the .NET 10 SDK. The loader repo pins a .NET 9 SDK for its own APK; do not copy that pin here.
- Pack the loader zip with `build/android/build.sh`. Output is `SMAPI-Android-*.zip` under `src/PackSMAPIZip/bin/Release/net9.0/`.

## Android dependency stack

Android SMAPI must compile against the same Harmony and MonoMod binaries the loader embeds from `AndroidSMAPI/SharedLibs`:

- Thin `src/DependenciesDll/0Harmony.dll` (Harmony 2.4.2). It references external `MonoMod.Core`.
- `MonoMod.Core.dll` (1.3.1), `MonoMod.Utils.dll` (25.0.9), `MonoMod.Backports.dll`, `MonoMod.Iced.dll`, `MonoMod.ILHelpers.dll`.

Do not reference the `Lib.Harmony` NuGet package for the Android build. Stock Harmony 2.3 fat binaries crash on .NET 9 because `System.Reflection.Emit.LocalBuilder` is abstract. Do not reference `MonoMod.RuntimeDetour.dll`; that assembly is merged into `MonoMod.Core`.

`src/PackSMAPIZip/dependencies.txt` does not ship Harmony or MonoMod. The loader already loads them. Keep it that way so the process does not load a second copy.

Game assemblies in `src/DependenciesDll` (`StardewValley.dll` and the other files listed in that folder's README) are gitignored extracts from an official install. Never modify, repack, or re-sign the game APK.

## Android runtime rules

- No busy-wait loops (`while (!done) {}`) and no `Thread.Sleep` polling. Block with `ManualResetEventSlim`, `SemaphoreSlim`, or `Task`.
- Do not call `.Wait()` or `.Result` on the Android UI thread.
- Marshal work onto the game thread through `AndroidMainThread`. UI and `Activity` calls stay on the main thread.
- Do not store an `Activity` in a static field. Use `WeakReference<Activity>` and drop it when the activity is destroyed. `SMAPIActivityTool` still caches `MainActivity` strongly; do not copy that pattern into new code.
- Do not swallow exceptions with empty `catch` blocks.
- Zip extraction must reject paths that escape the destination directory (`Path.GetFullPath` plus a prefix check).
- Prefer Harmony prefix and postfix patches. Avoid large transpilers on ARM64 Mono.
- `MiniMonoModHotfix` is a .NET Framework field-order workaround. It does not run in the Android build.

## Where to change things

| Change | Where |
|---|---|
| Android game hooks | `src/SMAPI/Mobile/` |
| Android compile references | `src/SMAPI/SMAPI.csproj` (`AndroidBuild`) |
| Harmony / MonoMod binaries | `src/DependenciesDll/`, copied from AndroidSMAPI `SharedLibs/` |
| Zip contents | `src/PackSMAPIZip/dependencies.txt` |
| Loader, APK, assembly store, launcher UI | AndroidSMAPI repo, not this one |
