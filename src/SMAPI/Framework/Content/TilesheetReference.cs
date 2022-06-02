using xTile.Dimensions;

namespace StardewModdingAPI.Framework.Content;

/// <summary>Basic metadata about a vanilla tilesheet.</summary>
internal class TilesheetReference
{
    /*********
    ** Accessors
    *********/
    /// <summary>The tilesheet's index in the list.</summary>
    public readonly int Index;

    /// <summary>The tilesheet's unique ID in the map.</summary>
    public readonly string Id;

    /// <summary>The asset path for the tilesheet texture.</summary>
    public readonly string ImageSource;

    /// <summary>The number of tiles in the tilesheet.</summary>
    public readonly Size SheetSize;

    /// <summary>The size of each tile in pixels.</summary>
    public readonly Size TileSize;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="index">The tilesheet's index in the list.</param>
    /// <param name="id">The tilesheet's unique ID in the map.</param>
    /// <param name="imageSource">The asset path for the tilesheet texture.</param>
    /// <param name="sheetSize">The number of tiles in the tilesheet.</param>
    /// <param name="tileSize">The size of each tile in pixels.</param>
    public TilesheetReference(int index, string id, string imageSource, Size sheetSize, Size tileSize)
    {
        this.Index = index;
        this.Id = id;
        this.ImageSource = imageSource;
        this.SheetSize = sheetSize;
        this.TileSize = tileSize;
    }
}
