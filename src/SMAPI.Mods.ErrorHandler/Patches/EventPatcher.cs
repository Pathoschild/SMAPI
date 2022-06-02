using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI.Internal.Patching;
using StardewValley;

namespace StardewModdingAPI.Mods.ErrorHandler.Patches;

/// <summary>Harmony patches for <see cref="Event"/> which intercept errors to log more details.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
internal class EventPatcher : BasePatcher
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
    public EventPatcher(IMonitor monitorForGame)
    {
        EventPatcher.MonitorForGame = monitorForGame;
    }

    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<Event>(nameof(Event.LogErrorAndHalt)),
            postfix: this.GetHarmonyMethod(nameof(EventPatcher.After_LogErrorAndHalt))
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>The method to call after <see cref="Event.LogErrorAndHalt"/>.</summary>
    /// <param name="e">The exception being logged.</param>
    private static void After_LogErrorAndHalt(Exception e)
    {
        EventPatcher.MonitorForGame.Log(e.ToString(), LogLevel.Error);
    }
}