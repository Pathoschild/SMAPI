---
name: android-smapi-build
description: >-
  Build and pack the Android SMAPI zip for the AndroidSMAPI loader.
  Use when building Android Release, running build/android/build.sh, packing SMAPI-Android zip, or missing DependenciesDll game assemblies.
---

# Android SMAPI build

## Prerequisites

- .NET SDK from this repo's `global.json` (10.x), with the Android workload installed. Do not switch this build to the loader's .NET 9 SDK. SMAPI uses C# extension blocks that SDK 9 cannot compile. `build/android/build.sh` sets `DOTNET_EnableWriteXorExecute=0` on macOS so the .NET 10 runtime is not killed during GC.
- JDK 17 and Android SDK API 35.
- Gitignored game assemblies in `src/DependenciesDll/`: `BmFont.dll`, `Lidgren.Network.dll`, `MonoGame.Framework.dll`, `StardewValley.dll`, `StardewValley.GameData.dll`, `xTile.dll`. Copy them from an official install, or from AndroidSMAPI `DependenciesDll/` after `scripts/extract-deps.sh`.

## Commands

```bash
./build/android/build.sh
# or
dotnet build src/SMAPI/SMAPI.csproj -c "Android Release" -p:AndroidBuild=true
dotnet run --project src/PackSMAPIZip/PackSMAPIZip.csproj -c Release --no-launch-profile
```

The script builds `src/SMAPI/bin/ARM64/Android Release/` and writes `SMAPI-Android-<version>-(<buildCode>).zip` under `src/PackSMAPIZip/bin/Release/net9.0/`.

## Loader boundary

Installing the APK, `adb`, logcat, and assembly-store extraction are AndroidSMAPI workflows (`scripts/build.sh` in that repo). This repo only produces the SMAPI zip the loader installs.
