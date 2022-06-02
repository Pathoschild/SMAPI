using Microsoft.Xna.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework;

/// <summary>Defines a position on a given map at different reference points.</summary>
internal class CursorPosition : ICursorPosition
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public Vector2 AbsolutePixels { get; }

    /// <inheritdoc />
    public Vector2 ScreenPixels { get; }

    /// <inheritdoc />
    public Vector2 Tile { get; }

    /// <inheritdoc />
    public Vector2 GrabTile { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="absolutePixels">The pixel position relative to the top-left corner of the in-game map, adjusted for zoom but not UI scaling.</param>
    /// <param name="screenPixels">The pixel position relative to the top-left corner of the visible screen, adjusted for zoom but not UI scaling.</param>
    /// <param name="tile">The tile position relative to the top-left corner of the map.</param>
    /// <param name="grabTile">The tile position that the game considers under the cursor for purposes of clicking actions.</param>
    public CursorPosition(Vector2 absolutePixels, Vector2 screenPixels, Vector2 tile, Vector2 grabTile)
    {
        this.AbsolutePixels = absolutePixels;
        this.ScreenPixels = screenPixels;
        this.Tile = tile;
        this.GrabTile = grabTile;
    }

    /// <inheritdoc />
    public bool Equals(ICursorPosition? other)
    {
        return other != null && this.AbsolutePixels == other.AbsolutePixels;
    }

    /// <inheritdoc />
    public Vector2 GetScaledAbsolutePixels()
    {
        return Game1.uiMode
            ? Utility.ModifyCoordinatesForUIScale(this.AbsolutePixels)
            : this.AbsolutePixels;
    }

    /// <inheritdoc />
    public Vector2 GetScaledScreenPixels()
    {
        return Game1.uiMode
            ? Utility.ModifyCoordinatesForUIScale(this.ScreenPixels)
            : this.ScreenPixels;
    }
}
