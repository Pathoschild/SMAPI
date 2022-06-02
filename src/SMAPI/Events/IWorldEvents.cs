using System;

namespace StardewModdingAPI.Events;

/// <summary>Events raised when something changes in the world.</summary>
public interface IWorldEvents
{
    /// <summary>Raised after a game location is added or removed.</summary>
    event EventHandler<LocationListChangedEventArgs> LocationListChanged;

    /// <summary>Raised after buildings are added or removed in a location.</summary>
    event EventHandler<BuildingListChangedEventArgs> BuildingListChanged;

    /// <summary>Raised after debris are added or removed in a location.</summary>
    event EventHandler<DebrisListChangedEventArgs> DebrisListChanged;

    /// <summary>Raised after large terrain features (like bushes) are added or removed in a location.</summary>
    event EventHandler<LargeTerrainFeatureListChangedEventArgs> LargeTerrainFeatureListChanged;

    /// <summary>Raised after NPCs are added or removed in a location.</summary>
    event EventHandler<NpcListChangedEventArgs> NpcListChanged;

    /// <summary>Raised after objects are added or removed in a location.</summary>
    event EventHandler<ObjectListChangedEventArgs> ObjectListChanged;

    /// <summary>Raised after items are added or removed from a chest.</summary>
    event EventHandler<ChestInventoryChangedEventArgs> ChestInventoryChanged;

    /// <summary>Raised after terrain features (like floors and trees) are added or removed in a location.</summary>
    event EventHandler<TerrainFeatureListChangedEventArgs> TerrainFeatureListChanged;

    /// <summary>Raised after furniture are added or removed in a location.</summary>
    event EventHandler<FurnitureListChangedEventArgs> FurnitureListChanged;
}
