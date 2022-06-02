using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for a <see cref="IWorldEvents.TerrainFeatureListChanged"/> event.</summary>
public class TerrainFeatureListChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The location which changed.</summary>
    public GameLocation Location { get; }

    /// <summary>The terrain features added to the location.</summary>
    public IEnumerable<KeyValuePair<Vector2, TerrainFeature>> Added { get; }

    /// <summary>The terrain features removed from the location.</summary>
    public IEnumerable<KeyValuePair<Vector2, TerrainFeature>> Removed { get; }

    /// <summary>Whether this is the location containing the local player.</summary>
    public bool IsCurrentLocation => object.ReferenceEquals(this.Location, Game1.player?.currentLocation);


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="location">The location which changed.</param>
    /// <param name="added">The terrain features added to the location.</param>
    /// <param name="removed">The terrain features removed from the location.</param>
    internal TerrainFeatureListChangedEventArgs(GameLocation location, IEnumerable<KeyValuePair<Vector2, TerrainFeature>> added, IEnumerable<KeyValuePair<Vector2, TerrainFeature>> removed)
    {
        this.Location = location;
        this.Added = added.ToArray();
        this.Removed = removed.ToArray();
    }
}
