using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events;

/// <inheritdoc cref="IInputEvents" />
internal class ModInputEvents : ModEventsBase, IInputEvents
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public event EventHandler<ButtonsChangedEventArgs> ButtonsChanged
    {
        add => this.EventManager.ButtonsChanged.Add(value, this.Mod);
        remove => this.EventManager.ButtonsChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<ButtonPressedEventArgs> ButtonPressed
    {
        add => this.EventManager.ButtonPressed.Add(value, this.Mod);
        remove => this.EventManager.ButtonPressed.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<ButtonReleasedEventArgs> ButtonReleased
    {
        add => this.EventManager.ButtonReleased.Add(value, this.Mod);
        remove => this.EventManager.ButtonReleased.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<CursorMovedEventArgs> CursorMoved
    {
        add => this.EventManager.CursorMoved.Add(value, this.Mod);
        remove => this.EventManager.CursorMoved.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<MouseWheelScrolledEventArgs> MouseWheelScrolled
    {
        add => this.EventManager.MouseWheelScrolled.Add(value, this.Mod);
        remove => this.EventManager.MouseWheelScrolled.Remove(value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    internal ModInputEvents(IModMetadata mod, EventManager eventManager)
        : base(mod, eventManager) { }
}
