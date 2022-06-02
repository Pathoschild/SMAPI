using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events;

/// <inheritdoc cref="IWorldEvents" />
internal class ModWorldEvents : ModEventsBase, IWorldEvents
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public event EventHandler<LocationListChangedEventArgs> LocationListChanged
    {
        add => this.EventManager.LocationListChanged.Add(value, this.Mod);
        remove => this.EventManager.LocationListChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<BuildingListChangedEventArgs> BuildingListChanged
    {
        add => this.EventManager.BuildingListChanged.Add(value, this.Mod);
        remove => this.EventManager.BuildingListChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<DebrisListChangedEventArgs> DebrisListChanged
    {
        add => this.EventManager.DebrisListChanged.Add(value, this.Mod);
        remove => this.EventManager.DebrisListChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<LargeTerrainFeatureListChangedEventArgs> LargeTerrainFeatureListChanged
    {
        add => this.EventManager.LargeTerrainFeatureListChanged.Add(value, this.Mod);
        remove => this.EventManager.LargeTerrainFeatureListChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<NpcListChangedEventArgs> NpcListChanged
    {
        add => this.EventManager.NpcListChanged.Add(value, this.Mod);
        remove => this.EventManager.NpcListChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<ObjectListChangedEventArgs> ObjectListChanged
    {
        add => this.EventManager.ObjectListChanged.Add(value, this.Mod);
        remove => this.EventManager.ObjectListChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<ChestInventoryChangedEventArgs> ChestInventoryChanged
    {
        add => this.EventManager.ChestInventoryChanged.Add(value, this.Mod);
        remove => this.EventManager.ChestInventoryChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<TerrainFeatureListChangedEventArgs> TerrainFeatureListChanged
    {
        add => this.EventManager.TerrainFeatureListChanged.Add(value, this.Mod);
        remove => this.EventManager.TerrainFeatureListChanged.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<FurnitureListChangedEventArgs> FurnitureListChanged
    {
        add => this.EventManager.FurnitureListChanged.Add(value, this.Mod);
        remove => this.EventManager.FurnitureListChanged.Remove(value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    internal ModWorldEvents(IModMetadata mod, EventManager eventManager)
        : base(mod, eventManager) { }
}
