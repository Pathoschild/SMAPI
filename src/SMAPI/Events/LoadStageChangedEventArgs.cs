using System;
using StardewModdingAPI.Enums;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="ISpecializedEvents.LoadStageChanged"/> event.</summary>
public class LoadStageChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The previous load stage.</summary>
    public LoadStage OldStage { get; }

    /// <summary>The new load stage.</summary>
    public LoadStage NewStage { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="old">The previous load stage.</param>
    /// <param name="current">The new load stage.</param>
    public LoadStageChangedEventArgs(LoadStage old, LoadStage current)
    {
        this.OldStage = old;
        this.NewStage = current;
    }
}
