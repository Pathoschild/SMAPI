using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events;

/// <inheritdoc cref="IGameLoopEvents" />
internal class ModGameLoopEvents : ModEventsBase, IGameLoopEvents
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public event EventHandler<GameLaunchedEventArgs> GameLaunched
    {
        add => this.EventManager.GameLaunched.Add(value, this.Mod);
        remove => this.EventManager.GameLaunched.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<UpdateTickingEventArgs> UpdateTicking
    {
        add => this.EventManager.UpdateTicking.Add(value, this.Mod);
        remove => this.EventManager.UpdateTicking.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<UpdateTickedEventArgs> UpdateTicked
    {
        add => this.EventManager.UpdateTicked.Add(value, this.Mod);
        remove => this.EventManager.UpdateTicked.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<OneSecondUpdateTickingEventArgs> OneSecondUpdateTicking
    {
        add => this.EventManager.OneSecondUpdateTicking.Add(value, this.Mod);
        remove => this.EventManager.OneSecondUpdateTicking.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<OneSecondUpdateTickedEventArgs> OneSecondUpdateTicked
    {
        add => this.EventManager.OneSecondUpdateTicked.Add(value, this.Mod);
        remove => this.EventManager.OneSecondUpdateTicked.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<SaveCreatingEventArgs> SaveCreating
    {
        add => this.EventManager.SaveCreating.Add(value, this.Mod);
        remove => this.EventManager.SaveCreating.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<SaveCreatedEventArgs> SaveCreated
    {
        add => this.EventManager.SaveCreated.Add(value, this.Mod);
        remove => this.EventManager.SaveCreated.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<SavingEventArgs> Saving
    {
        add => this.EventManager.Saving.Add(value, this.Mod);
        remove => this.EventManager.Saving.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<SavedEventArgs> Saved
    {
        add => this.EventManager.Saved.Add(value, this.Mod);
        remove => this.EventManager.Saved.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<SaveLoadedEventArgs> SaveLoaded
    {
        add => this.EventManager.SaveLoaded.Add(value, this.Mod);
        remove => this.EventManager.SaveLoaded.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<DayStartedEventArgs> DayStarted
    {
        add => this.EventManager.DayStarted.Add(value, this.Mod);
        remove => this.EventManager.DayStarted.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<DayEndingEventArgs> DayEnding
    {
        add => this.EventManager.DayEnding.Add(value, this.Mod);
        remove => this.EventManager.DayEnding.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<TimeChangedEventArgs> TimeChanged
    {
        add => this.EventManager.TimeChanged.Add(value, this.Mod);
        remove => this.EventManager.TimeChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<ReturnedToTitleEventArgs> ReturnedToTitle
    {
        add => this.EventManager.ReturnedToTitle.Add(value, this.Mod);
        remove => this.EventManager.ReturnedToTitle.Remove(value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    internal ModGameLoopEvents(IModMetadata mod, EventManager eventManager)
        : base(mod, eventManager) { }
}
