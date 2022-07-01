using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Internal.Patching;
using StardewValley.Menus;

namespace StardewModdingAPI.Patches;

/// <summary>Harmony patches for <see cref="TitleMenu"/> which notify SMAPI when a new character was created.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
internal class TitleMenuPatcher : BasePatcher
{
    /*********
    ** Fields
    *********/
    /// <summary>A callback to invoke when the load stage changes.</summary>
    private static Action<LoadStage> OnStageChanged = null!; // initialized in constructor


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="onStageChanged">A callback to invoke when the load stage changes.</param>
    public TitleMenuPatcher(Action<LoadStage> onStageChanged)
    {
        TitleMenuPatcher.OnStageChanged = onStageChanged;
    }

    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<TitleMenu>(nameof(TitleMenu.createdNewCharacter)),
            prefix: this.GetHarmonyMethod(nameof(TitleMenuPatcher.Before_CreatedNewCharacter))
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>The method to call before <see cref="TitleMenu.createdNewCharacter"/>.</summary>
    /// <returns>Returns whether to execute the original method.</returns>
    /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
    private static bool Before_CreatedNewCharacter()
    {
        TitleMenuPatcher.OnStageChanged(LoadStage.CreatedBasicInfo);
        return true;
    }
}
