using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewValley;

namespace StardewModdingAPI.Framework.StateTracking.Snapshots;

/// <summary>A frozen snapshot of the tracked game locations.</summary>
internal class WorldLocationsSnapshot
{
    /*********
    ** Fields
    *********/
    /// <summary>A map of tracked locations.</summary>
    private readonly Dictionary<GameLocation, LocationSnapshot> LocationsDict = new(new ObjectReferenceComparer<GameLocation>());


    /*********
    ** Accessors
    *********/
    /// <summary>Tracks changes to the location list.</summary>
    public SnapshotListDiff<GameLocation> LocationList { get; } = new();

    /// <summary>The tracked locations.</summary>
    public IEnumerable<LocationSnapshot> Locations => this.LocationsDict.Values;


    /*********
    ** Public methods
    *********/
    /// <summary>Update the tracked values.</summary>
    /// <param name="watcher">The watcher to snapshot.</param>
    public void Update(WorldLocationsTracker watcher)
    {
        // update location list
        this.LocationList.Update(watcher.IsLocationListChanged, watcher.Added, watcher.Removed);

        // remove missing locations
        foreach (var key in this.LocationsDict.Keys.Where(key => !watcher.HasLocationTracker(key)).ToArray())
            this.LocationsDict.Remove(key);

        // update locations
        foreach (LocationTracker locationWatcher in watcher.Locations)
        {
            if (!this.LocationsDict.TryGetValue(locationWatcher.Location, out LocationSnapshot? snapshot))
                this.LocationsDict[locationWatcher.Location] = snapshot = new LocationSnapshot(locationWatcher.Location);

            snapshot.Update(locationWatcher);
        }
    }
}
