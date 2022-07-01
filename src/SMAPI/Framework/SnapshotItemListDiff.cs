using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewModdingAPI.Framework;

/// <summary>A snapshot of a tracked item list.</summary>
internal class SnapshotItemListDiff
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether the item list changed.</summary>
    public bool IsChanged { get; }

    /// <summary>The removed values.</summary>
    public Item[] Removed { get; }

    /// <summary>The added values.</summary>
    public Item[] Added { get; }

    /// <summary>The items whose stack sizes changed.</summary>
    public ItemStackSizeChange[] QuantityChanged { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Update the snapshot.</summary>
    /// <param name="added">The added values.</param>
    /// <param name="removed">The removed values.</param>
    /// <param name="sizesChanged">The items whose stack sizes changed.</param>
    public SnapshotItemListDiff(Item[] added, Item[] removed, ItemStackSizeChange[] sizesChanged)
    {
        this.Removed = removed;
        this.Added = added;
        this.QuantityChanged = sizesChanged;

        this.IsChanged = removed.Length > 0 || added.Length > 0 || sizesChanged.Length > 0;
    }

    /// <summary>Get a snapshot diff if anything changed in the given data.</summary>
    /// <param name="added">The added item stacks.</param>
    /// <param name="removed">The removed item stacks.</param>
    /// <param name="stackSizes">The items with their previous stack sizes.</param>
    /// <param name="changes">The inventory changes, or <c>null</c> if nothing changed.</param>
    /// <returns>Returns whether anything changed.</returns>
    public static bool TryGetChanges(ISet<Item> added, ISet<Item> removed, IDictionary<Item, int> stackSizes, [NotNullWhen(true)] out SnapshotItemListDiff? changes)
    {
        KeyValuePair<Item, int>[] sizesChanged = stackSizes.Where(p => p.Key.Stack != p.Value).ToArray();
        if (sizesChanged.Any() || added.Any() || removed.Any())
        {
            changes = new SnapshotItemListDiff(
                added: added.ToArray(),
                removed: removed.ToArray(),
                sizesChanged: sizesChanged.Select(p => new ItemStackSizeChange(p.Key, p.Value, p.Key.Stack)).ToArray()
            );
            return true;
        }

        changes = null;
        return false;
    }
}
