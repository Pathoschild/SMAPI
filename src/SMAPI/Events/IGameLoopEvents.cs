using System;

namespace StardewModdingAPI.Events;

/// <summary>Events linked to the game's update loop. The update loop runs roughly ≈60 times/second to run game logic like state changes, action handling, etc. These can be useful, but you should consider more semantic events like <see cref="IInputEvents"/> if possible.</summary>
public interface IGameLoopEvents
{
    /// <summary>Raised after the game is launched, right before the first update tick. This happens once per game session (unrelated to loading saves). All mods are loaded and initialized at this point, so this is a good time to set up mod integrations.</summary>
    event EventHandler<GameLaunchedEventArgs> GameLaunched;

    /// <summary>Raised before the game state is updated (≈60 times per second).</summary>
    event EventHandler<UpdateTickingEventArgs> UpdateTicking;

    /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
    event EventHandler<UpdateTickedEventArgs> UpdateTicked;

    /// <summary>Raised once per second before the game state is updated.</summary>
    event EventHandler<OneSecondUpdateTickingEventArgs> OneSecondUpdateTicking;

    /// <summary>Raised once per second after the game state is updated.</summary>
    event EventHandler<OneSecondUpdateTickedEventArgs> OneSecondUpdateTicked;

    /// <summary>Raised before the game creates a new save file.</summary>
    event EventHandler<SaveCreatingEventArgs> SaveCreating;

    /// <summary>Raised after the game finishes creating the save file.</summary>
    event EventHandler<SaveCreatedEventArgs> SaveCreated;

    /// <summary>Raised before the game begins writing data to the save file (except the initial save creation).</summary>
    event EventHandler<SavingEventArgs> Saving;

    /// <summary>Raised after the game finishes writing data to the save file (except the initial save creation).</summary>
    event EventHandler<SavedEventArgs> Saved;

    /// <summary>Raised after the player loads a save slot and the world is initialized.</summary>
    event EventHandler<SaveLoadedEventArgs> SaveLoaded;

    /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
    event EventHandler<DayStartedEventArgs> DayStarted;

    /// <summary>Raised before the game ends the current day. This happens before it starts setting up the next day and before <see cref="Saving"/>.</summary>
    event EventHandler<DayEndingEventArgs> DayEnding;

    /// <summary>Raised after the in-game clock time changes.</summary>
    event EventHandler<TimeChangedEventArgs> TimeChanged;

    /// <summary>Raised after the game returns to the title screen.</summary>
    event EventHandler<ReturnedToTitleEventArgs> ReturnedToTitle;
}