using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Objects;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for a <see cref="IWorldEvents.ChestInventoryChanged"/> event.</summary>
public class ChestInventoryChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The chest whose inventory changed.</summary>
    public Chest Chest { get; }

    /// <summary>The location containing the chest.</summary>
    public GameLocation Location { get; }

    /// <summary>The added item stacks.</summary>
    public IEnumerable<Item> Added { get; }

    /// <summary>The removed item stacks.</summary>
    public IEnumerable<Item> Removed { get; }

    /// <summary>The item stacks whose size changed.</summary>
    public IEnumerable<ItemStackSizeChange> QuantityChanged { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="chest">The chest whose inventory changed.</param>
    /// <param name="location">The location containing the chest.</param>
    /// <param name="added">The added item stacks.</param>
    /// <param name="removed">The removed item stacks.</param>
    /// <param name="quantityChanged">The item stacks whose size changed.</param>
    internal ChestInventoryChangedEventArgs(Chest chest, GameLocation location, Item[] added, Item[] removed, ItemStackSizeChange[] quantityChanged)
    {
        this.Location = location;
        this.Chest = chest;
        this.Added = added;
        this.Removed = removed;
        this.QuantityChanged = quantityChanged;
    }
}
