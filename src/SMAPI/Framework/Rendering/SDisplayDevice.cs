using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace StardewModdingAPI.Framework.Rendering;

/// <summary>A map display device which overrides the draw logic to support tile rotation.</summary>
internal class SDisplayDevice : SXnaDisplayDevice
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="contentManager">The content manager through which to load tiles.</param>
    /// <param name="graphicsDevice">The graphics device with which to render tiles.</param>
    public SDisplayDevice(ContentManager contentManager, GraphicsDevice graphicsDevice)
        : base(contentManager, graphicsDevice) { }

    /// <summary>Draw a tile to the screen.</summary>
    /// <param name="tile">The tile to draw.</param>
    /// <param name="location">The tile position to draw.</param>
    /// <param name="layerDepth">The layer depth at which to draw.</param>
    public override void DrawTile(Tile? tile, Location location, float layerDepth)
    {
        // identical to XnaDisplayDevice
        if (tile == null)
            return;
        xTile.Dimensions.Rectangle tileImageBounds = tile.TileSheet.GetTileImageBounds(tile.TileIndex);
        Texture2D tileSheetTexture = this.m_tileSheetTextures[tile.TileSheet];
        if (tileSheetTexture.IsDisposed)
            return;
        this.m_tilePosition.X = location.X;
        this.m_tilePosition.Y = location.Y;
        this.m_sourceRectangle.X = tileImageBounds.X;
        this.m_sourceRectangle.Y = tileImageBounds.Y;
        this.m_sourceRectangle.Width = tileImageBounds.Width;
        this.m_sourceRectangle.Height = tileImageBounds.Height;

        // get rotation and effects
        float rotation = this.GetRotation(tile);
        SpriteEffects effects = this.GetSpriteEffects(tile);
        var origin = new Vector2(tileImageBounds.Width / 2f, tileImageBounds.Height / 2f);
        this.m_tilePosition.X += origin.X * Layer.zoom;
        this.m_tilePosition.Y += origin.X * Layer.zoom;

        // apply
        this.m_spriteBatchAlpha.Draw(tileSheetTexture, this.m_tilePosition, this.m_sourceRectangle, this.m_modulationColour, rotation, origin, Layer.zoom, effects, layerDepth);
    }

    /// <summary>Get the sprite effects to apply for a tile.</summary>
    /// <param name="tile">The tile being drawn.</param>
    private SpriteEffects GetSpriteEffects(Tile tile)
    {
        return tile.Properties.TryGetValue("@Flip", out PropertyValue? propertyValue) && int.TryParse(propertyValue, out int value)
            ? (SpriteEffects)value
            : SpriteEffects.None;
    }

    /// <summary>Get the draw rotation to apply for a tile.</summary>
    /// <param name="tile">The tile being drawn.</param>
    private float GetRotation(Tile tile)
    {
        if (!tile.Properties.TryGetValue("@Rotation", out PropertyValue? propertyValue) || !int.TryParse(propertyValue, out int value))
            return 0;

        value %= 360;
        if (value == 0)
            return 0;

        return (float)(Math.PI / (180.0 / value));
    }
}
