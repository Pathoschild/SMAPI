using System;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IGameLoopEvents.TimeChanged"/> event.</summary>
public class TimeChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The previous time of day in 24-hour notation (like 1600 for 4pm). The clock time resets when the player sleeps, so 2am (before sleeping) is 2600.</summary>
    public int OldTime { get; }

    /// <summary>The current time of day in 24-hour notation (like 1600 for 4pm). The clock time resets when the player sleeps, so 2am (before sleeping) is 2600.</summary>
    public int NewTime { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="oldTime">The previous time of day in 24-hour notation (like 1600 for 4pm).</param>
    /// <param name="newTime">The current time of day in 24-hour notation (like 1600 for 4pm).</param>
    internal TimeChangedEventArgs(int oldTime, int newTime)
    {
        this.OldTime = oldTime;
        this.NewTime = newTime;
    }
}
