using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewModdingAPI.Framework.StateTracking.FieldWatchers;
using StardewValley;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.StateTracking;

/// <summary>Tracks changes to a chest's items.</summary>
internal class ChestTracker : IDisposable
{
    /*********
    ** Fields
    *********/
    /// <summary>The item stack sizes as of the last update.</summary>
    private readonly IDictionary<Item, int> StackSizes;

    /// <summary>Items added since the last update.</summary>
    private readonly HashSet<Item> Added = new(new ObjectReferenceComparer<Item>());

    /// <summary>Items removed since the last update.</summary>
    private readonly HashSet<Item> Removed = new(new ObjectReferenceComparer<Item>());

    /// <summary>The underlying inventory watcher.</summary>
    private readonly ICollectionWatcher<Item> InventoryWatcher;


    /*********
    ** Accessors
    *********/
    /// <summary>The chest being tracked.</summary>
    public Chest Chest { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="chest">The chest being tracked.</param>
    public ChestTracker(string name, Chest chest)
    {
        this.Chest = chest;
        this.InventoryWatcher = WatcherFactory.ForNetList($"{name}.{nameof(chest.items)}", chest.items);

        this.StackSizes = this.Chest.items
            .Where(n => n != null)
            .Distinct()
            .ToDictionary(n => n, n => n.Stack);
    }

    /// <summary>Update the current values if needed.</summary>
    public void Update()
    {
        // update watcher
        this.InventoryWatcher.Update();
        foreach (Item item in this.InventoryWatcher.Added)
            this.Added.Add(item);
        foreach (Item item in this.InventoryWatcher.Removed)
        {
            if (!this.Added.Remove(item)) // item didn't change if it was both added and removed, so remove it from both lists
                this.Removed.Add(item);
        }

        // stop tracking removed stacks
        foreach (Item item in this.Removed)
            this.StackSizes.Remove(item);
    }

    /// <summary>Reset all trackers so their current values are the baseline.</summary>
    public void Reset()
    {
        // update stack sizes
        foreach (Item item in this.StackSizes.Keys.ToArray().Concat(this.Added))
            this.StackSizes[item] = item.Stack;

        // update watcher
        this.InventoryWatcher.Reset();
        this.Added.Clear();
        this.Removed.Clear();
    }

    /// <summary>Get the inventory changes since the last update, if anything changed.</summary>
    /// <param name="changes">The inventory changes, or <c>null</c> if nothing changed.</param>
    /// <returns>Returns whether anything changed.</returns>
    public bool TryGetInventoryChanges([NotNullWhen(true)] out SnapshotItemListDiff? changes)
    {
        return SnapshotItemListDiff.TryGetChanges(added: this.Added, removed: this.Removed, stackSizes: this.StackSizes, out changes);
    }

    /// <summary>Release watchers and resources.</summary>
    public void Dispose()
    {
        this.StackSizes.Clear();
        this.Added.Clear();
        this.Removed.Clear();
        this.InventoryWatcher.Dispose();
    }
}