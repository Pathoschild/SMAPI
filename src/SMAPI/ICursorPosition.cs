using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace StardewModdingAPI;

/// <summary>Represents a cursor position in the different coordinate systems.</summary>
public interface ICursorPosition : IEquatable<ICursorPosition>
{
    /*********
    ** Accessors
    *********/
    /// <summary>The pixel position relative to the top-left corner of the in-game map, adjusted for zoom but not UI scaling. See also <see cref="GetScaledAbsolutePixels"/>.</summary>
    Vector2 AbsolutePixels { get; }

    /// <summary>The pixel position relative to the top-left corner of the visible screen, adjusted for zoom but not UI scaling. See also <see cref="GetScaledScreenPixels"/>.</summary>
    Vector2 ScreenPixels { get; }

    /// <summary>The tile position under the cursor relative to the top-left corner of the map.</summary>
    Vector2 Tile { get; }

    /// <summary>The tile position that the game considers under the cursor for purposes of clicking actions. This may be different than <see cref="Tile"/> if that's too far from the player.</summary>
    Vector2 GrabTile { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Get the <see cref="AbsolutePixels"/>, adjusted for UI scaling if needed. This is only different if <see cref="Game1.uiMode"/> is true.</summary>
    Vector2 GetScaledAbsolutePixels();

    /// <summary>Get the <see cref="ScreenPixels"/>, adjusted for UI scaling if needed. This is only different if <see cref="Game1.uiMode"/> is true.</summary>
    Vector2 GetScaledScreenPixels();
}
