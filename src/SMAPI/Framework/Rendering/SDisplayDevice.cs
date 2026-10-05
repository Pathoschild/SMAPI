using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using xTile.Display;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace StardewModdingAPI.Framework.Rendering;

/// <summary>A map display device which overrides the draw logic to support tile rotation.</summary>
internal class SDisplayDevice : XnaDisplayDevice
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="contentManager">The content manager through which to load tiles.</param>
    /// <param name="graphicsDevice">The graphics device with which to render tiles.</param>
    public SDisplayDevice(ContentManager contentManager, GraphicsDevice graphicsDevice)
        : base(contentManager, graphicsDevice) { }

    /// <inheritdoc />
    protected override void DrawImpl(TileSheet sheet, int tileIndex, IPropertyCollection? tileProps, Location location, float layerDepth, Texture2D tileSheetTexture)
    {
        // get rotation and effects
        float rotation = this.GetRotation(tileProps);
        SpriteEffects effects = this.GetSpriteEffects(tileProps);
        var sourceRect = this.m_sourceRectangle;
        var origin = new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f);
        this.m_tilePosition.X += origin.X * Layer.zoom;
        this.m_tilePosition.Y += origin.X * Layer.zoom;

        // apply
        this.m_spriteBatchAlpha.Draw(tileSheetTexture, this.m_tilePosition, sourceRect, this.m_modulationColour, rotation, origin, Layer.zoom, effects, layerDepth);
    }

    /// <summary>Get the sprite effects to apply for a tile.</summary>
    /// <param name="tileProps">The properties of the tile being drawn.</param>
    private SpriteEffects GetSpriteEffects(IPropertyCollection? tileProps)
    {
        if (tileProps == null)
            return SpriteEffects.None;

        return tileProps.TryGetValue("@Flip", out PropertyValue? propertyValue) && int.TryParse(propertyValue, out int value)
            ? (SpriteEffects)value
            : SpriteEffects.None;
    }

    /// <summary>Get the draw rotation to apply for a tile.</summary>
    /// <param name="tileProps">The properties of the tile being drawn.</param>
    private float GetRotation(IPropertyCollection? tileProps)
    {
        if (tileProps == null || !tileProps.TryGetValue("@Rotation", out PropertyValue? propertyValue) || !int.TryParse(propertyValue, out int value))
            return 0;

        value %= 360;
        if (value == 0)
            return 0;

        return (float)(Math.PI / (180.0 / value));
    }
}
