#!/usr/bin/env bash
# Build the Android SMAPI library and pack SMAPI-Android-*.zip for the loader.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DEPS="$ROOT/src/DependenciesDll"
CONFIG="${1:-Android Release}"

if [[ -d "$HOME/.dotnet" ]]; then
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$DOTNET_ROOT:$PATH"
fi

# .NET 10 maps JIT memory as write-xor-execute. macOS kills that process with
# SIGKILL (code signature invalid) during GC. The AndroidSMAPI loader avoids it
# by running on the .NET 9 runtime. This repo still needs the .NET 10 SDK.
if [[ "$(uname -s)" == "Darwin" ]]; then
  export DOTNET_EnableWriteXorExecute=0
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI not found. Install the SDK pinned in global.json."
  exit 1
fi

missing=()
for file in BmFont.dll Lidgren.Network.dll MonoGame.Framework.dll StardewValley.dll StardewValley.GameData.dll xTile.dll; do
  if [[ ! -f "$DEPS/$file" ]]; then
    missing+=("$file")
  fi
done

if ((${#missing[@]})); then
  echo "Missing Android game assemblies in src/DependenciesDll:"
  printf '  %s\n' "${missing[@]}"
  echo
  echo "See src/DependenciesDll/README.txt for where they come from."
  exit 1
fi

build_args=(
  build "$ROOT/src/SMAPI/SMAPI.csproj"
  -c "$CONFIG"
  -p:AndroidBuild=true
)

if [[ -z "${ANDROID_HOME:-}" && -d "$HOME/Library/Android/sdk" ]]; then
  build_args+=("-p:AndroidSdkDirectory=$HOME/Library/Android/sdk")
elif [[ -z "${ANDROID_HOME:-}" && -d "$HOME/Android/Sdk" ]]; then
  build_args+=("-p:AndroidSdkDirectory=$HOME/Android/Sdk")
fi

if [[ -z "${JAVA_HOME:-}" && -d "$HOME/.sdkman/candidates/java/current" ]]; then
  build_args+=("-p:JavaSdkDirectory=$HOME/.sdkman/candidates/java/current")
fi

dotnet "${build_args[@]}"

dll="$ROOT/src/SMAPI/bin/ARM64/$CONFIG/StardewModdingAPI.dll"
if [[ ! -f "$dll" ]]; then
  echo "Android SMAPI assembly not found at $dll"
  exit 1
fi

echo "Checking Android startup invariants in $dll"
dotnet run "$ROOT/build/android/VerifyAndroidStartup.cs" -- "$dll"

dotnet run --project "$ROOT/src/PackSMAPIZip/PackSMAPIZip.csproj" -c Release --no-launch-profile
echo
echo "Zip written under src/PackSMAPIZip/"
