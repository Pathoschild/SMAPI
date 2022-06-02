using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI.Internal.Patching;
using StardewValley;
using xTile;

namespace StardewModdingAPI.Mods.ErrorHandler.Patches;

/// <summary>Harmony patches for <see cref="GameLocation"/> which intercept errors instead of crashing.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
internal class GameLocationPatcher : BasePatcher
{
    /*********
    ** Fields
    *********/
    /// <summary>Writes messages to the console and log file on behalf of the game.</summary>
    private static IMonitor MonitorForGame = null!;

    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
    public GameLocationPatcher(IMonitor monitorForGame)
    {
        GameLocationPatcher.MonitorForGame = monitorForGame;
    }

    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<GameLocation>(nameof(GameLocation.checkEventPrecondition)),
            finalizer: this.GetHarmonyMethod(nameof(GameLocationPatcher.Finalize_CheckEventPrecondition))
        );
        harmony.Patch(
            original: this.RequireMethod<GameLocation>(nameof(GameLocation.updateSeasonalTileSheets)),
            finalizer: this.GetHarmonyMethod(nameof(GameLocationPatcher.Finalize_UpdateSeasonalTileSheets))
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>The method to call when <see cref="GameLocation.checkEventPrecondition"/> throws an exception.</summary>
    /// <param name="__result">The return value of the original method.</param>
    /// <param name="precondition">The precondition to be parsed.</param>
    /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
    /// <returns>Returns the exception to throw, if any.</returns>
    private static Exception? Finalize_CheckEventPrecondition(ref int __result, string precondition, Exception? __exception)
    {
        if (__exception != null)
        {
            __result = -1;
            GameLocationPatcher.MonitorForGame.Log($"Failed parsing event precondition ({precondition}):\n{__exception.InnerException}", LogLevel.Error);
        }

        return null;
    }

    /// <summary>The method to call when <see cref="GameLocation.updateSeasonalTileSheets"/> throws an exception.</summary>
    /// <param name="__instance">The instance being patched.</param>
    /// <param name="map">The map whose tilesheets to update.</param>
    /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
    /// <returns>Returns the exception to throw, if any.</returns>
    private static Exception? Finalize_UpdateSeasonalTileSheets(GameLocation __instance, Map map, Exception? __exception)
    {
        if (__exception != null)
            GameLocationPatcher.MonitorForGame.Log($"Failed updating seasonal tilesheets for location '{__instance.NameOrUniqueName}': \n{__exception}", LogLevel.Error);

        return null;
    }
}