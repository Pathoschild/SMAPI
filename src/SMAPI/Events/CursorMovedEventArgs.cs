using System;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments when the in-game cursor is moved.</summary>
public class CursorMovedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The previous cursor position.</summary>
    public ICursorPosition OldPosition { get; }

    /// <summary>The current cursor position.</summary>
    public ICursorPosition NewPosition { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="oldPosition">The previous cursor position.</param>
    /// <param name="newPosition">The new cursor position.</param>
    internal CursorMovedEventArgs(ICursorPosition oldPosition, ICursorPosition newPosition)
    {
        this.OldPosition = oldPosition;
        this.NewPosition = newPosition;
    }
}
