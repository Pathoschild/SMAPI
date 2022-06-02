using System;
using StardewValley;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IPlayerEvents.Warped"/> event.</summary>
public class WarpedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The player who warped to a new location.</summary>
    public Farmer Player { get; }

    /// <summary>The player's previous location.</summary>
    public GameLocation OldLocation { get; }

    /// <summary>The player's current location.</summary>
    public GameLocation NewLocation { get; }

    /// <summary>Whether the affected player is the local one.</summary>
    public bool IsLocalPlayer => this.Player.IsLocalPlayer;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="player">The player who warped to a new location.</param>
    /// <param name="oldLocation">The player's previous location.</param>
    /// <param name="newLocation">The player's current location.</param>
    internal WarpedEventArgs(Farmer player, GameLocation oldLocation, GameLocation newLocation)
    {
        this.Player = player;
        this.NewLocation = newLocation;
        this.OldLocation = oldLocation;
    }
}
