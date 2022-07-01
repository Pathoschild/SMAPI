using System;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments when the player scrolls the mouse wheel.</summary>
public class MouseWheelScrolledEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The cursor position.</summary>
    public ICursorPosition Position { get; }

    /// <summary>The old scroll value.</summary>
    public int OldValue { get; }

    /// <summary>The new scroll value.</summary>
    public int NewValue { get; }

    /// <summary>The amount by which the scroll value changed.</summary>
    public int Delta => this.NewValue - this.OldValue;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="position">The cursor position.</param>
    /// <param name="oldValue">The old scroll value.</param>
    /// <param name="newValue">The new scroll value.</param>
    internal MouseWheelScrolledEventArgs(ICursorPosition position, int oldValue, int newValue)
    {
        this.Position = position;
        this.OldValue = oldValue;
        this.NewValue = newValue;
    }
}
