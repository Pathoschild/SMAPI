using System;

namespace StardewModdingAPI.Events;

/// <summary>Events raised when the player provides input using a controller, keyboard, or mouse.</summary>
public interface IInputEvents
{
    /// <summary>Raised after the player presses or releases any buttons on the keyboard, controller, or mouse.</summary>
    event EventHandler<ButtonsChangedEventArgs> ButtonsChanged;

    /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
    event EventHandler<ButtonPressedEventArgs> ButtonPressed;

    /// <summary>Raised after the player releases a button on the keyboard, controller, or mouse.</summary>
    event EventHandler<ButtonReleasedEventArgs> ButtonReleased;

    /// <summary>Raised after the player moves the in-game cursor.</summary>
    event EventHandler<CursorMovedEventArgs> CursorMoved;

    /// <summary>Raised after the player scrolls the mouse wheel.</summary>
    event EventHandler<MouseWheelScrolledEventArgs> MouseWheelScrolled;
}