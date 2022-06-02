using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for a <see cref="IWorldEvents.LargeTerrainFeatureListChanged"/> event.</summary>
public class LargeTerrainFeatureListChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The location which changed.</summary>
    public GameLocation Location { get; }

    /// <summary>The large terrain features added to the location.</summary>
    public IEnumerable<LargeTerrainFeature> Added { get; }

    /// <summary>The large terrain features removed from the location.</summary>
    public IEnumerable<LargeTerrainFeature> Removed { get; }

    /// <summary>Whether this is the location containing the local player.</summary>
    public bool IsCurrentLocation => object.ReferenceEquals(this.Location, Game1.player?.currentLocation);


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="location">The location which changed.</param>
    /// <param name="added">The large terrain features added to the location.</param>
    /// <param name="removed">The large terrain features removed from the location.</param>
    internal LargeTerrainFeatureListChangedEventArgs(GameLocation location, IEnumerable<LargeTerrainFeature> added, IEnumerable<LargeTerrainFeature> removed)
    {
        this.Location = location;
        this.Added = added.ToArray();
        this.Removed = removed.ToArray();
    }
}
