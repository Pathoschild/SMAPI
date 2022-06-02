using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using xTile.Display;
using xTile.Layers;
using xTile.Tiles;
using Rectangle = xTile.Dimensions.Rectangle;

namespace StardewModdingAPI.Framework.Rendering;

/// <summary>A map display device which reimplements the default logic.</summary>
/// <remarks>This is an exact copy of <see cref="XnaDisplayDevice"/>, except that private fields are protected and all methods are virtual.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Field naming deliberately matches " + nameof(XnaDisplayDevice) + " to minimize differences.")]
internal class SXnaDisplayDevice : IDisplayDevice
{
    /*********
    ** Fields
    *********/
    protected readonly ContentManager m_contentManager;
    protected readonly GraphicsDevice m_graphicsDevice;
    protected SpriteBatch m_spriteBatchAlpha;
    protected SpriteBatch m_spriteBatchAdditive;
    protected readonly Dictionary<TileSheet, Texture2D> m_tileSheetTextures;
    protected Vector2 m_tilePosition;
    protected Microsoft.Xna.Framework.Rectangle m_sourceRectangle;
    protected readonly Color m_modulationColour;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="contentManager">The content manager through which to load tiles.</param>
    /// <param name="graphicsDevice">The graphics device with which to render tiles.</param>
    public SXnaDisplayDevice(ContentManager contentManager, GraphicsDevice graphicsDevice)
    {
        this.m_contentManager = contentManager;
        this.m_graphicsDevice = graphicsDevice;
        this.m_spriteBatchAlpha = new SpriteBatch(graphicsDevice);
        this.m_spriteBatchAdditive = new SpriteBatch(graphicsDevice);
        this.m_tileSheetTextures = new Dictionary<TileSheet, Texture2D>();
        this.m_tilePosition = new Vector2();
        this.m_sourceRectangle = new Microsoft.Xna.Framework.Rectangle();
        this.m_modulationColour = Color.White;
    }

    /// <summary>Load a tilesheet texture.</summary>
    /// <param name="tileSheet">The tilesheet instance.</param>
    public virtual void LoadTileSheet(TileSheet tileSheet)
    {
        Texture2D texture2D = this.m_contentManager.Load<Texture2D>(tileSheet.ImageSource);
        this.m_tileSheetTextures[tileSheet] = texture2D;
    }

    /// <summary>Unload a tilesheet texture.</summary>
    /// <param name="tileSheet">The tilesheet instance.</param>
    public virtual void DisposeTileSheet(TileSheet tileSheet)
    {
        this.m_tileSheetTextures.Remove(tileSheet);
    }

    /// <summary>Prepare to render to the screen.</summary>
    /// <param name="b">The sprite batch being rendered.</param>
    public virtual void BeginScene(SpriteBatch b)
    {
        this.m_spriteBatchAlpha = b;
    }

    /// <summary>Set the clipping region.</summary>
    /// <param name="clippingRegion">The clipping region.</param>
    public virtual void SetClippingRegion(Rectangle clippingRegion)
    {
        int backBufferWidth = this.m_graphicsDevice.PresentationParameters.BackBufferWidth;
        int backBufferHeight = this.m_graphicsDevice.PresentationParameters.BackBufferHeight;
        int x = this.Clamp(clippingRegion.X, 0, backBufferWidth);
        int y = this.Clamp(clippingRegion.Y, 0, backBufferHeight);
        int num1 = this.Clamp(clippingRegion.X + clippingRegion.Width, 0, backBufferWidth);
        int num2 = this.Clamp(clippingRegion.Y + clippingRegion.Height, 0, backBufferHeight);
        int width = num1 - x;
        int height = num2 - y;
        this.m_graphicsDevice.Viewport = new Viewport(x, y, width, height);
    }

    /// <summary>Draw a tile to the screen.</summary>
    /// <param name="tile">The tile to draw.</param>
    /// <param name="location">The tile position to draw.</param>
    /// <param name="layerDepth">The layer depth at which to draw.</param>
    public virtual void DrawTile(Tile? tile, Location location, float layerDepth)
    {
        if (tile == null)
            return;
        Rectangle tileImageBounds = tile.TileSheet.GetTileImageBounds(tile.TileIndex);
        Texture2D tileSheetTexture = this.m_tileSheetTextures[tile.TileSheet];
        if (tileSheetTexture.IsDisposed)
            return;
        this.m_tilePosition.X = location.X;
        this.m_tilePosition.Y = location.Y;
        this.m_sourceRectangle.X = tileImageBounds.X;
        this.m_sourceRectangle.Y = tileImageBounds.Y;
        this.m_sourceRectangle.Width = tileImageBounds.Width;
        this.m_sourceRectangle.Height = tileImageBounds.Height;
        this.m_spriteBatchAlpha.Draw(tileSheetTexture, this.m_tilePosition, this.m_sourceRectangle, this.m_modulationColour, 0.0f, Vector2.Zero, Layer.zoom, SpriteEffects.None, layerDepth);
    }

    /// <summary>Finish drawing to the screen.</summary>
    public virtual void EndScene() { }

    /// <summary>Snap a value to the given range.</summary>
    /// <param name="nValue">The value to normalize.</param>
    /// <param name="nMin">The minimum value.</param>
    /// <param name="nMax">The maximum value.</param>
    protected int Clamp(int nValue, int nMin, int nMax)
    {
        return Math.Min(Math.Max(nValue, nMin), nMax);
    }
}
