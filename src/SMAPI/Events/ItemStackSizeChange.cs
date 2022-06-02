using StardewValley;

namespace StardewModdingAPI.Events;

/// <summary>An inventory item stack size change.</summary>
public class ItemStackSizeChange
{
    /*********
    ** Accessors
    *********/
    /// <summary>The item whose stack size changed.</summary>
    public Item Item { get; }

    /// <summary>The previous stack size.</summary>
    public int OldSize { get; }

    /// <summary>The new stack size.</summary>
    public int NewSize { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="item">The item whose stack size changed.</param>
    /// <param name="oldSize">The previous stack size.</param>
    /// <param name="newSize">The new stack size.</param>
    public ItemStackSizeChange(Item item, int oldSize, int newSize)
    {
        this.Item = item;
        this.OldSize = oldSize;
        this.NewSize = newSize;
    }
}
