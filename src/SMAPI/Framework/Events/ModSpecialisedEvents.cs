using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events;

/// <inheritdoc cref="ISpecializedEvents" />
internal class ModSpecializedEvents : ModEventsBase, ISpecializedEvents
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public event EventHandler<LoadStageChangedEventArgs> LoadStageChanged
    {
        add => this.EventManager.LoadStageChanged.Add(value, this.Mod);
        remove => this.EventManager.LoadStageChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<UnvalidatedUpdateTickingEventArgs> UnvalidatedUpdateTicking
    {
        add => this.EventManager.UnvalidatedUpdateTicking.Add(value, this.Mod);
        remove => this.EventManager.UnvalidatedUpdateTicking.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<UnvalidatedUpdateTickedEventArgs> UnvalidatedUpdateTicked
    {
        add => this.EventManager.UnvalidatedUpdateTicked.Add(value, this.Mod);
        remove => this.EventManager.UnvalidatedUpdateTicked.Remove(value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    internal ModSpecializedEvents(IModMetadata mod, EventManager eventManager)
        : base(mod, eventManager) { }
}
