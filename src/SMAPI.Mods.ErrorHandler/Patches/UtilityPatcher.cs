using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI.Internal.Patching;
using StardewValley;

namespace StardewModdingAPI.Mods.ErrorHandler.Patches;

/// <summary>A Harmony patch for <see cref="Utility"/> methods to log more detailed errors.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
internal class UtilityPatcher : BasePatcher
{
    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<Utility>(nameof(Utility.getItemFromStandardTextDescription)),
            finalizer: this.GetHarmonyMethod(nameof(UtilityPatcher.Finalize_GetItemFromStandardTextDescription))
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>The method to call when <see cref="Utility.getItemFromStandardTextDescription"/> throws an exception.</summary>
    /// <param name="description">The item text description to parse.</param>
    /// <param name="delimiter">The delimiter by which to split the text description.</param>
    /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
    /// <returns>Returns the exception to throw, if any.</returns>
    private static Exception? Finalize_GetItemFromStandardTextDescription(string description, char delimiter, ref Exception? __exception)
    {
        return __exception != null
            ? new FormatException($"Failed to parse item text description \"{description}\" with delimiter \"{delimiter}\".", __exception)
            : null;
    }
}