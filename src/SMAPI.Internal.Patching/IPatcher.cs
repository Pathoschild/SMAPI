using HarmonyLib;

namespace StardewModdingAPI.Internal.Patching;

/// <summary>A set of Harmony patches to apply.</summary>
internal interface IPatcher
{
    /*********
    ** Public methods
    *********/
    /// <summary>Apply the Harmony patches for this instance.</summary>
    /// <param name="harmony">The Harmony instance.</param>
    /// <param name="monitor">The monitor with which to log any errors.</param>
    public void Apply(Harmony harmony, IMonitor monitor);
}