using System;
using StardewModdingAPI.Framework;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IGameLoopEvents.OneSecondUpdateTicking"/> event.</summary>
public class OneSecondUpdateTickingEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The number of ticks elapsed since the game started, excluding the upcoming tick.</summary>
    public uint Ticks => SCore.TicksElapsed;


    /*********
    ** Public methods
    *********/
    /// <summary>Get whether <see cref="Ticks"/> is a multiple of the given <paramref name="number"/>. This is mainly useful if you want to run logic intermittently (e.g. <code>e.IsMultipleOf(30)</code> for every half-second).</summary>
    /// <param name="number">The factor to check.</param>
    public bool IsMultipleOf(uint number)
    {
        return this.Ticks % number == 0;
    }
}
