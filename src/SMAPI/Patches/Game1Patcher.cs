using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Internal.Patching;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace StardewModdingAPI.Patches;

/// <summary>Harmony patches for <see cref="Game1"/> which notify SMAPI for save load stages.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
internal class Game1Patcher : BasePatcher
{
    /*********
    ** Fields
    *********/
    /// <summary>Simplifies access to private code.</summary>
    private static Reflector Reflection = null!; // initialized in constructor

    /// <summary>A callback to invoke when the load stage changes.</summary>
    private static Action<LoadStage> OnStageChanged = null!; // initialized in constructor

    /// <summary>Whether the game is running running the code in <see cref="Game1.loadForNewGame"/>.</summary>
    private static bool IsInLoadForNewGame;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="reflection">Simplifies access to private code.</param>
    /// <param name="onStageChanged">A callback to invoke when the load stage changes.</param>
    public Game1Patcher(Reflector reflection, Action<LoadStage> onStageChanged)
    {
        Game1Patcher.Reflection = reflection;
        Game1Patcher.OnStageChanged = onStageChanged;
    }

    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        // detect CreatedInitialLocations and SaveAddedLocations
        harmony.Patch(
            original: this.RequireMethod<Game1>(nameof(Game1.AddModNPCs)),
            prefix: this.GetHarmonyMethod(nameof(Game1Patcher.Before_AddModNpcs))
        );

        // detect CreatedLocations, and track IsInLoadForNewGame
        harmony.Patch(
            original: this.RequireMethod<Game1>(nameof(Game1.loadForNewGame)),
            prefix: this.GetHarmonyMethod(nameof(Game1Patcher.Before_LoadForNewGame)),
            postfix: this.GetHarmonyMethod(nameof(Game1Patcher.After_LoadForNewGame))
        );

        // detect ReturningToTitle
        harmony.Patch(
            original: this.RequireMethod<Game1>(nameof(Game1.CleanupReturningToTitle)),
            prefix: this.GetHarmonyMethod(nameof(Game1Patcher.Before_CleanupReturningToTitle))
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>The method to call before <see cref="Game1.AddModNPCs"/>.</summary>
    /// <returns>Returns whether to execute the original method.</returns>
    /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
    private static bool Before_AddModNpcs()
    {
        // When this method is called from Game1.loadForNewGame, it happens right after adding the vanilla
        // locations but before initializing them.
        if (Game1Patcher.IsInLoadForNewGame)
        {
            Game1Patcher.OnStageChanged(Game1Patcher.IsCreating()
                ? LoadStage.CreatedInitialLocations
                : LoadStage.SaveAddedLocations
            );
        }

        return true;
    }

    /// <summary>The method to call before <see cref="Game1.CleanupReturningToTitle"/>.</summary>
    /// <returns>Returns whether to execute the original method.</returns>
    /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
    private static bool Before_CleanupReturningToTitle()
    {
        Game1Patcher.OnStageChanged(LoadStage.ReturningToTitle);
        return true;
    }

    /// <summary>The method to call before <see cref="Game1.loadForNewGame"/>.</summary>
    /// <returns>Returns whether to execute the original method.</returns>
    /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
    private static bool Before_LoadForNewGame()
    {
        Game1Patcher.IsInLoadForNewGame = true;
        return true;
    }

    /// <summary>The method to call after <see cref="Game1.loadForNewGame"/>.</summary>
    /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
    private static void After_LoadForNewGame()
    {
        Game1Patcher.IsInLoadForNewGame = false;

        if (Game1Patcher.IsCreating())
            Game1Patcher.OnStageChanged(LoadStage.CreatedLocations);
    }

    /// <summary>Get whether the save file is currently being created.</summary>
    private static bool IsCreating()
    {
        return
            (Game1.currentMinigame is Intro) // creating save with intro
            || (Game1.activeClickableMenu is TitleMenu menu && Game1Patcher.Reflection.GetField<bool>(menu, "transitioningCharacterCreationMenu").GetValue()); // creating save, skipped intro
    }
}