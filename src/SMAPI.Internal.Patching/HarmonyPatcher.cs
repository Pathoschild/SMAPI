using System;
using HarmonyLib;

namespace StardewModdingAPI.Internal.Patching;

/// <summary>Simplifies applying <see cref="IPatcher"/> instances to the game.</summary>
internal static class HarmonyPatcher
{
    /*********
    ** Public methods
    *********/
    /// <summary>Apply the given Harmony patchers.</summary>
    /// <param name="id">The mod ID applying the patchers.</param>
    /// <param name="monitor">The monitor with which to log any errors.</param>
    /// <param name="patchers">The patchers to apply.</param>
    public static Harmony Apply(string id, IMonitor monitor, params IPatcher[] patchers)
    {
        Harmony harmony = new(id);

        foreach (IPatcher patcher in patchers)
        {
            try
            {
                patcher.Apply(harmony, monitor);
            }
            catch (Exception ex)
            {
                monitor.Log($"Couldn't apply runtime patch '{patcher.GetType().Name}' to the game. Some SMAPI features may not work correctly. See log file for details.", LogLevel.Error);
                monitor.Log($"Technical details:\n{ex.GetLogSummary()}");
            }
        }

        return harmony;
    }
}
