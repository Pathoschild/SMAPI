using System;
using System.Collections.Generic;
using StardewValley;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IPlayerEvents.InventoryChanged"/> event.</summary>
public class InventoryChangedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The player whose inventory changed.</summary>
    public Farmer Player { get; }

    /// <summary>The added item stacks.</summary>
    public IEnumerable<Item> Added { get; }

    /// <summary>The removed item stacks.</summary>
    public IEnumerable<Item> Removed { get; }

    /// <summary>The item stacks whose size changed.</summary>
    public IEnumerable<ItemStackSizeChange> QuantityChanged { get; }

    /// <summary>Whether the affected player is the local one.</summary>
    public bool IsLocalPlayer => this.Player.IsLocalPlayer;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="player">The player whose inventory changed.</param>
    /// <param name="added">The added item stacks.</param>
    /// <param name="removed">The removed item stacks.</param>
    /// <param name="quantityChanged">The item stacks whose size changed.</param>
    internal InventoryChangedEventArgs(Farmer player, Item[] added, Item[] removed, ItemStackSizeChange[] quantityChanged)
    {
        this.Player = player;
        this.Added = added;
        this.Removed = removed;
        this.QuantityChanged = quantityChanged;
    }
}
