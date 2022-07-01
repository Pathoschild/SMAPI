using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Internal.Patching;

//
// This is part of a three-part fix for PyTK 1.23.0 and earlier. When removing this, search
// 'Platonymous.Toolkit' to find the other part in SMAPI and Content Patcher.
//

namespace StardewModdingAPI.Mods.ErrorHandler.ModPatches;

/// <summary>Harmony patches for the PyTK mod for compatibility with newer SMAPI versions.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "'Platonymous' is part of the mod ID.")]
internal class PyTkPatcher : BasePatcher
{
    /*********
    ** Fields
    *********/
    /// <summary>The PyTK mod metadata, if it's installed.</summary>
    private static IModMetadata? PyTk;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="modRegistry">The mod registry from which to read PyTK metadata.</param>
    public PyTkPatcher(IModRegistry modRegistry)
    {
        IModMetadata? pyTk = (IModMetadata?)modRegistry.Get(@"Platonymous.Toolkit");
        if (pyTk is not null && !pyTk.Manifest.Version.IsNewerThan("1.23.0"))
            PyTkPatcher.PyTk = pyTk;
    }

    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        try
        {
            // get mod info
            IModMetadata? pyTk = PyTkPatcher.PyTk;
            if (pyTk is null)
                return;

            // get patch method
            const string patchMethodName = "PatchImage";
            MethodInfo? patch = AccessTools.Method(pyTk.Mod!.GetType(), patchMethodName);
            if (patch is null)
            {
                monitor.Log("Failed applying compatibility patch for PyTK. Its image scaling feature may not work correctly.", LogLevel.Warn);
                monitor.Log($"Couldn't find patch method '{pyTk.Mod.GetType().FullName}.{patchMethodName}'.");
                return;
            }

            // apply patch
            harmony = new($"{harmony.Id}.compatibility-patches.PyTK");
            harmony.Patch(
                original: AccessTools.Method(typeof(AssetDataForImage), nameof(AssetDataForImage.PatchImage), new[] { typeof(Texture2D), typeof(Rectangle), typeof(Rectangle), typeof(PatchMode) }),
                prefix: new HarmonyMethod(patch)
            );
        }
        catch (Exception ex)
        {
            monitor.Log("Failed applying compatibility patch for PyTK. Its image scaling feature may not work correctly.", LogLevel.Warn);
            monitor.Log(ex.GetLogSummary());
        }
    }
}
