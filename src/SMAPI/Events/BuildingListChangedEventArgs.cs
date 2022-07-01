using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Buildings;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for a <see cref="IWorldEvents.BuildingListChanged"/> event.</summary>
public class BuildingListChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The location which changed.</summary>
    public GameLocation Location { get; }

    /// <summary>The buildings added to the location.</summary>
    public IEnumerable<Building> Added { get; }

    /// <summary>The buildings removed from the location.</summary>
    public IEnumerable<Building> Removed { get; }

    /// <summary>Whether this is the location containing the local player.</summary>
    public bool IsCurrentLocation => object.ReferenceEquals(this.Location, Game1.player?.currentLocation);


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="location">The location which changed.</param>
    /// <param name="added">The buildings added to the location.</param>
    /// <param name="removed">The buildings removed from the location.</param>
    internal BuildingListChangedEventArgs(GameLocation location, IEnumerable<Building> added, IEnumerable<Building> removed)
    {
        this.Location = location;
        this.Added = added.ToArray();
        this.Removed = removed.ToArray();
    }
}
