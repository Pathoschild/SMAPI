using System;

namespace StardewModdingAPI.Events;

/// <summary>Events serving specialized edge cases that shouldn't be used by most mods.</summary>
public interface ISpecializedEvents
{
    /// <summary>Raised when the low-level stage in the game's loading process has changed. This is an advanced event for mods which need to run code at specific points in the loading process. The available stages or when they happen might change without warning in future versions (e.g. due to changes in the game's load process), so mods using this event are more likely to break or have bugs. Most mods should use <see cref="IGameLoopEvents"/> instead.</summary>
    event EventHandler<LoadStageChangedEventArgs> LoadStageChanged;

    /// <summary>Raised before the game state is updated (≈60 times per second), regardless of normal SMAPI validation. This event is not thread-safe and may be invoked while game logic is running asynchronously. Changes to game state in this method may crash the game or corrupt an in-progress save. Do not use this event unless you're fully aware of the context in which your code will be run. Mods using this event will trigger a stability warning in the SMAPI console.</summary>
    event EventHandler<UnvalidatedUpdateTickingEventArgs> UnvalidatedUpdateTicking;

    /// <summary>Raised after the game state is updated (≈60 times per second), regardless of normal SMAPI validation. This event is not thread-safe and may be invoked while game logic is running asynchronously. Changes to game state in this method may crash the game or corrupt an in-progress save. Do not use this event unless you're fully aware of the context in which your code will be run. Mods using this event will trigger a stability warning in the SMAPI console.</summary>
    event EventHandler<UnvalidatedUpdateTickedEventArgs> UnvalidatedUpdateTicked;
}
