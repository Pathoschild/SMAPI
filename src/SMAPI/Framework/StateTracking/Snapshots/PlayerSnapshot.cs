using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewModdingAPI.Framework.StateTracking.Snapshots;

/// <summary>A frozen snapshot of a tracked player.</summary>
internal class PlayerSnapshot
{
    /*********
    ** Fields
    *********/
    /// <summary>An empty item list diff.</summary>
    private readonly SnapshotItemListDiff EmptyItemListDiff = new(Array.Empty<Item>(), Array.Empty<Item>(), Array.Empty<ItemStackSizeChange>());


    /*********
    ** Accessors
    *********/
    /// <summary>The player being tracked.</summary>
    public Farmer Player { get; }

    /// <summary>The player's current location.</summary>
    public SnapshotDiff<GameLocation> Location { get; } = new();

    /// <summary>Tracks changes to the player's skill levels.</summary>
    public IDictionary<SkillType, SnapshotDiff<int>> Skills { get; } =
        Enum
            .GetValues(typeof(SkillType))
            .Cast<SkillType>()
            .ToDictionary(skill => skill, _ => new SnapshotDiff<int>());

    /// <summary>Get a list of inventory changes.</summary>
    public SnapshotItemListDiff Inventory { get; private set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="player">The player being tracked.</param>
    public PlayerSnapshot(Farmer player)
    {
        this.Player = player;
        this.Inventory = this.EmptyItemListDiff;
    }

    /// <summary>Update the tracked values.</summary>
    /// <param name="watcher">The player watcher to snapshot.</param>
    public void Update(PlayerTracker watcher)
    {
        this.Location.Update(watcher.LocationWatcher!);
        foreach ((SkillType skill, var value) in this.Skills)
            value.Update(watcher.SkillWatchers[skill]);

        this.Inventory = watcher.TryGetInventoryChanges(out SnapshotItemListDiff? itemChanges)
            ? itemChanges
            : this.EmptyItemListDiff;
    }
}
