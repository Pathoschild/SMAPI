using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events;

/// <inheritdoc cref="IPlayerEvents" />
internal class ModPlayerEvents : ModEventsBase, IPlayerEvents
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public event EventHandler<InventoryChangedEventArgs> InventoryChanged
    {
        add => this.EventManager.InventoryChanged.Add(value, this.Mod);
        remove => this.EventManager.InventoryChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<LevelChangedEventArgs> LevelChanged
    {
        add => this.EventManager.LevelChanged.Add(value, this.Mod);
        remove => this.EventManager.LevelChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<WarpedEventArgs> Warped
    {
        add => this.EventManager.Warped.Add(value, this.Mod);
        remove => this.EventManager.Warped.Remove(value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    internal ModPlayerEvents(IModMetadata mod, EventManager eventManager)
        : base(mod, eventManager) { }
}
