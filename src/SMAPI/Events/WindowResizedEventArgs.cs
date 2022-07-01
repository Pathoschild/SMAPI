using System;
using Microsoft.Xna.Framework;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IDisplayEvents.WindowResized"/> event.</summary>
public class WindowResizedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The previous window size.</summary>
    public Point OldSize { get; }

    /// <summary>The current window size.</summary>
    public Point NewSize { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="oldSize">The previous window size.</param>
    /// <param name="newSize">The current window size.</param>
    internal WindowResizedEventArgs(Point oldSize, Point newSize)
    {
        this.OldSize = oldSize;
        this.NewSize = newSize;
    }
}
