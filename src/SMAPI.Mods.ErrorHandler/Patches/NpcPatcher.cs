using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Internal.Patching;
using StardewValley;

namespace StardewModdingAPI.Mods.ErrorHandler.Patches;

/// <summary>Harmony patches for <see cref="NPC"/> which intercept crashes due to invalid schedule data.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
internal class NpcPatcher : BasePatcher
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
    public NpcPatcher(IMonitor monitorForGame)
    {
        NpcPatcher.MonitorForGame = monitorForGame;
    }

    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<NPC>($"get_{nameof(NPC.CurrentDialogue)}"),
            finalizer: this.GetHarmonyMethod(nameof(NpcPatcher.Finalize_CurrentDialogue))
        );

        harmony.Patch(
            original: this.RequireMethod<NPC>(nameof(NPC.parseMasterSchedule)),
            finalizer: this.GetHarmonyMethod(nameof(NpcPatcher.Finalize_ParseMasterSchedule))
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>The method to call when <see cref="NPC.CurrentDialogue"/> throws an exception.</summary>
    /// <param name="__instance">The instance being patched.</param>
    /// <param name="__result">The return value of the original method.</param>
    /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
    /// <returns>Returns the exception to throw, if any.</returns>
    private static Exception? Finalize_CurrentDialogue(NPC __instance, ref Stack<Dialogue> __result, Exception? __exception)
    {
        if (__exception == null)
            return null;

        NpcPatcher.MonitorForGame.Log($"Failed loading current dialogue for NPC {__instance.Name}:\n{__exception.GetLogSummary()}", LogLevel.Error);
        __result = new Stack<Dialogue>();

        return null;
    }

    /// <summary>The method to call instead of <see cref="NPC.parseMasterSchedule"/>.</summary>
    /// <param name="rawData">The raw schedule data to parse.</param>
    /// <param name="__instance">The instance being patched.</param>
    /// <param name="__result">The patched method's return value.</param>
    /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
    /// <returns>Returns the exception to throw, if any.</returns>
    private static Exception? Finalize_ParseMasterSchedule(string rawData, NPC __instance, ref Dictionary<int, SchedulePathDescription> __result, Exception? __exception)
    {
        if (__exception != null)
        {
            NpcPatcher.MonitorForGame.Log($"Failed parsing schedule for NPC {__instance.Name}:\n{rawData}\n{__exception.GetLogSummary()}", LogLevel.Error);
            __result = new Dictionary<int, SchedulePathDescription>();
        }

        return null;
    }
}
