using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Objects;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for a <see cref="IWorldEvents.FurnitureListChanged"/> event.</summary>
public class FurnitureListChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The location which changed.</summary>
    public GameLocation Location { get; }

    /// <summary>The furniture added to the location.</summary>
    public IEnumerable<Furniture> Added { get; }

    /// <summary>The furniture removed from the location.</summary>
    public IEnumerable<Furniture> Removed { get; }

    /// <summary>Whether this is the location containing the local player.</summary>
    public bool IsCurrentLocation => object.ReferenceEquals(this.Location, Game1.player?.currentLocation);


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="location">The location which changed.</param>
    /// <param name="added">The furniture added to the location.</param>
    /// <param name="removed">The furniture removed from the location.</param>
    internal FurnitureListChangedEventArgs(GameLocation location, IEnumerable<Furniture> added, IEnumerable<Furniture> removed)
    {
        this.Location = location;
        this.Added = added.ToArray();
        this.Removed = removed.ToArray();
    }
}
