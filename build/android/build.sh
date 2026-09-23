#!/usr/bin/env bash
# Build the Android SMAPI library and pack SMAPI-Android-*.zip for the loader.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DEPS="$ROOT/src/DependenciesDll"
CONFIG="${1:-Android Release}"

if [[ -x "$HOME/.dotnet/dotnet" ]]; then
  export PATH="$HOME/.dotnet:$PATH"
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

dotnet build "$ROOT/src/SMAPI/SMAPI.csproj" -c "$CONFIG" -p:AndroidBuild=true
dotnet run --project "$ROOT/src/PackSMAPIZip/PackSMAPIZip.csproj" -c Release --no-launch-profile
echo
echo "Zip written under src/PackSMAPIZip/bin/Release/net9.0/"
