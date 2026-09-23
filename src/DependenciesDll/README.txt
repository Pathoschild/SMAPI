Android game assemblies used to compile SMAPI. These files are gitignored because they come from the Stardew Valley app.

Place these next to this file before building:

  BmFont.dll
  Lidgren.Network.dll
  MonoGame.Framework.dll
  StardewValley.dll
  StardewValley.GameData.dll
  xTile.dll

Extract them from an official Stardew Valley 1.6 Android install (the assemblies inside the APK or split APKs). If you use the AndroidSMAPI repo, its scripts/extract-deps.sh writes the same files into that repo's DependenciesDll folder; copy the six files above here.

The MonoMod.*.dll files in this folder are build references and are kept in git.
