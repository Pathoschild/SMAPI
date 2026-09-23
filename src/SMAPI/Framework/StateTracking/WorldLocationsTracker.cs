using System.Collections.Generic;
using System.Collections.ObjectModel;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewModdingAPI.Framework.StateTracking.FieldWatchers;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace StardewModdingAPI.Framework.StateTracking;

/// <summary>Detects changes to the game's locations.</summary>
internal class WorldLocationsTracker : IWatcher
{
    /*********
    ** Fields
    *********/
    /// <summary>Tracks changes to the location list.</summary>
    private readonly ICollectionWatcher<GameLocation> LocationListWatcher;

    /// <summary>Tracks changes to the list of active mine locations.</summary>
    private readonly ICollectionWatcher<MineShaft> MineLocationListWatcher;

    /// <summary>Tracks changes to the list of active volcano locations.</summary>
    private readonly ICollectionWatcher<GameLocation> VolcanoLocationListWatcher;

    /// <summary>A lookup of the tracked locations.</summary>
    private Dictionary<GameLocation, LocationTracker> LocationDict { get; } = new(new ObjectReferenceComparer<GameLocation>());

    /// <summary>A lookup of registered buildings and their indoor location.</summary>
    private readonly Dictionary<Building, GameLocation?> BuildingIndoors = new(new ObjectReferenceComparer<Building>());

    /// <summary>The backing field for <see cref="Added"/>.</summary>
    private readonly HashSet<GameLocation> AddedImpl = new(new ObjectReferenceComparer<GameLocation>());

    /// <summary>The backing field for <see cref="Removed"/>.</summary>
    private readonly HashSet<GameLocation> RemovedImpl = new(new ObjectReferenceComparer<GameLocation>());

    /// <summary>The pooled list instance for <see cref="GetLocationsWhoseBuildingsChanged"/>.</summary>
    private static readonly List<LocationTracker> PooledLocationsWithBuildingsChanged = [];

    /// <summary>Whether any of the <see cref="Locations"/> have content changes.</summary>
    private bool LocationsHaveChanges;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string Name => nameof(WorldLocationsTracker);

    /// <summary>Whether locations were added or removed since the last reset.</summary>
    public bool IsLocationListChanged { get; private set; }

    /// <inheritdoc />
    public bool IsChanged => this.IsLocationListChanged || this.LocationsHaveChanges;

    /// <summary>The tracked locations.</summary>
    public IReadOnlyCollection<LocationTracker> Locations => this.LocationDict.Values;

    /// <summary>The locations added since the last update.</summary>
    public IReadOnlySet<GameLocation> Added => this.AddedImpl;

    /// <summary>The locations removed since the last update.</summary>
    public IReadOnlySet<GameLocation> Removed => this.RemovedImpl;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locations">The game's list of locations.</param>
    /// <param name="activeMineLocations">The game's list of active mine locations.</param>
    /// <param name="activeVolcanoLocations">The game's list of active volcano locations.</param>
#if SMAPI_FOR_ANDROID
    public WorldLocationsTracker(IList<GameLocation> locations, IList<MineShaft> activeMineLocations, IList<VolcanoDungeon> activeVolcanoLocations)
#else
    public WorldLocationsTracker(ObservableCollection<GameLocation> locations, IList<MineShaft> activeMineLocations, IList<VolcanoDungeon> activeVolcanoLocations)
#endif
    {
#if SMAPI_FOR_ANDROID
        this.LocationListWatcher = WatcherFactory.ForReferenceList($"{this.Name}.{nameof(locations)}", locations);
#else
        this.LocationListWatcher = WatcherFactory.ForObservableCollection($"{this.Name}.{nameof(locations)}", locations);
#endif
        this.MineLocationListWatcher = WatcherFactory.ForReferenceList($"{this.Name}.{nameof(activeMineLocations)}", activeMineLocations);
        this.VolcanoLocationListWatcher = WatcherFactory.ForReferenceList($"{this.Name}.{nameof(activeVolcanoLocations)}", activeVolcanoLocations);
    }

    /// <inheritdoc />
    public void Update()
    {
        this.LocationsHaveChanges = false;
        this.IsLocationListChanged = false;

        // update watchers
        this.LocationListWatcher.Update();
        this.MineLocationListWatcher.Update();
        this.VolcanoLocationListWatcher.Update();

        // update location content watchers
        foreach (LocationTracker watcher in this.Locations)
        {
            watcher.Update();
            if (watcher.IsChanged)
                this.LocationsHaveChanges = true;
        }

        // detect added/removed locations
        if (this.LocationListWatcher.IsChanged)
        {
            this.Remove(this.LocationListWatcher.Removed);
            this.Add(this.LocationListWatcher.Added);
        }
        if (this.MineLocationListWatcher.IsChanged)
        {
            this.Remove(this.MineLocationListWatcher.Removed);
            this.Add(this.MineLocationListWatcher.Added);
        }
        if (this.VolcanoLocationListWatcher.IsChanged)
        {
            this.Remove(this.VolcanoLocationListWatcher.Removed);
            this.Add(this.VolcanoLocationListWatcher.Added);
        }

        // detect building changed
        foreach (LocationTracker watcher in this.GetLocationsWhoseBuildingsChanged())
        {
            this.Remove(watcher.BuildingsWatcher.Removed);
            this.Add(watcher.BuildingsWatcher.Added);
        }

        // detect building interiors changed (e.g. construction completed)
        foreach ((Building building, GameLocation? oldIndoors) in this.BuildingIndoors)
        {
            GameLocation? newIndoors = building.indoors.Value;
            if (object.ReferenceEquals(oldIndoors, newIndoors))
                continue;

            this.Remove(oldIndoors);
            this.Add(newIndoors);

            this.BuildingIndoors[building] = newIndoors;
        }
    }

    /// <summary>Set the current location list as the baseline.</summary>
    public void ResetLocationList()
    {
        this.RemovedImpl.Clear();
        this.AddedImpl.Clear();

        this.LocationListWatcher.Reset();
        this.MineLocationListWatcher.Reset();
        this.VolcanoLocationListWatcher.Reset();

        this.IsLocationListChanged = false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        this.LocationsHaveChanges = false;

        this.ResetLocationList();

        foreach (LocationTracker watcher in this.Locations)
            watcher.Reset();
    }

    /// <summary>Get whether the given location is tracked.</summary>
    /// <param name="location">The location to check.</param>
    public bool HasLocationTracker(GameLocation location)
    {
        return this.LocationDict.ContainsKey(location);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.LocationListWatcher.Dispose();
        this.MineLocationListWatcher.Dispose();
        this.VolcanoLocationListWatcher.Dispose();

        foreach (LocationTracker watcher in this.Locations)
            watcher.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /****
    ** Enumerable wrappers
    ****/
    /// <summary>Add the given buildings.</summary>
    /// <param name="buildings">The buildings to add.</param>
    public void Add(IEnumerable<Building> buildings)
    {
        foreach (Building building in buildings)
            this.Add(building);
    }

    /// <summary>Add the given locations.</summary>
    /// <param name="locations">The locations to add.</param>
    public void Add(IEnumerable<GameLocation> locations)
    {
        foreach (GameLocation location in locations)
            this.Add(location);
    }

    /// <summary>Remove the given buildings.</summary>
    /// <param name="buildings">The buildings to remove.</param>
    public void Remove(IEnumerable<Building> buildings)
    {
        foreach (Building building in buildings)
            this.Remove(building);
    }

    /// <summary>Remove the given locations.</summary>
    /// <param name="locations">The locations to remove.</param>
    public void Remove(IEnumerable<GameLocation> locations)
    {
        foreach (GameLocation location in locations)
            this.Remove(location);
    }

    /****
    ** Main add/remove logic
    ****/
    /// <summary>Add the given building.</summary>
    /// <param name="building">The building to add.</param>
    public void Add(Building? building)
    {
        if (building == null)
            return;

        GameLocation? indoors = building.indoors.Value;
        this.BuildingIndoors[building] = indoors;
        this.Add(indoors);
    }

    /// <summary>Add the given location.</summary>
    /// <param name="location">The location to add.</param>
    public void Add(GameLocation? location)
    {
        if (location == null)
            return;

        // remove old location if needed
        this.Remove(location);

        // add location
        this.AddedImpl.Add(location);
        this.LocationDict[location] = new LocationTracker(location);

        // add buildings
        this.Add(location.buildings);

        this.IsLocationListChanged = true;
    }

    /// <summary>Remove the given building.</summary>
    /// <param name="building">The building to remove.</param>
    public void Remove(Building? building)
    {
        if (building == null)
            return;

        this.BuildingIndoors.Remove(building);
        this.Remove(building.indoors.Value);
    }

    /// <summary>Remove the given location.</summary>
    /// <param name="location">The location to remove.</param>
    public void Remove(GameLocation? location)
    {
        if (location == null)
            return;

        if (this.LocationDict.TryGetValue(location, out LocationTracker? watcher))
        {
            // track change
            this.RemovedImpl.Add(location);

            // remove
            this.LocationDict.Remove(location);
            watcher.Dispose();
            this.Remove(location.buildings);

            this.IsLocationListChanged = true;
        }
    }

    /****
    ** Helpers
    ****/
    /// <summary>Get the locations whose building list changed, if any.</summary>
    private List<LocationTracker> GetLocationsWhoseBuildingsChanged()
    {
        List<LocationTracker> list = WorldLocationsTracker.PooledLocationsWithBuildingsChanged;
        if (list.Count > 0)
            list.Clear();

        foreach (LocationTracker watcher in this.LocationDict.Values)
        {
            if (watcher.IsChanged)
                list.Add(watcher);
        }

        return list;
    }
}
