using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace StardewModdingAPI.Framework.Content;

/// <summary>Encapsulates access and changes to image content being read from a data file.</summary>
internal class AssetDataForMap : AssetData<Map>, IAssetDataForMap
{
    /*********
    ** Fields
    *********/
    /// <summary>Simplifies access to private code.</summary>
    private readonly Reflector Reflection;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locale">The content's locale code, if the content is localized.</param>
    /// <param name="assetName">The asset name being read.</param>
    /// <param name="data">The content data being read.</param>
    /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
    /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    public AssetDataForMap(string? locale, IAssetName assetName, Map data, Func<string, string> getNormalizedPath, Action<Map> onDataReplaced, Reflector reflection)
        : base(locale, assetName, data, getNormalizedPath, onDataReplaced)
    {
        this.Reflection = reflection;
    }

    /// <inheritdoc />
    /// <remarks>Derived from <see cref="GameLocation.ApplyMapOverride(Map,string,Rectangle?,Rectangle?)"/> with a few changes:
    /// - can be applied directly to the maps when loading, before the location is created;
    /// - added support for patch modes (overlay, replace by layer, or fully replace);
    /// - added disambiguation if source has a modified version of the same tilesheet, instead of copying tiles into the target tilesheet;
    /// - fixed copying tilesheets (avoid "The specified TileSheet was not created for use with this map" error);
    /// - fixed tilesheets not added at the end (via z_ prefix), which can cause crashes in game code which depends on hardcoded tilesheet indexes;
    /// - fixed issue where different tilesheets are linked by ID.
    /// </remarks>
    public void PatchMap(Map source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMapMode patchMode = PatchMapMode.Overlay)
    {
        Map target = this.Data;

        // get areas
        {
            Rectangle sourceBounds = this.GetMapArea(source);
            Rectangle targetBounds = this.GetMapArea(target);
            sourceArea ??= new Rectangle(0, 0, sourceBounds.Width, sourceBounds.Height);
            targetArea ??= new Rectangle(0, 0, Math.Min(sourceArea.Value.Width, targetBounds.Width), Math.Min(sourceArea.Value.Height, targetBounds.Height));

            // validate
            if (sourceArea.Value.X < 0 || sourceArea.Value.Y < 0 || sourceArea.Value.Right > sourceBounds.Width || sourceArea.Value.Bottom > sourceBounds.Height)
                throw new ArgumentOutOfRangeException(nameof(sourceArea), $"The source area ({sourceArea}) is outside the bounds of the source map ({sourceBounds}).");
            if (targetArea.Value.X < 0 || targetArea.Value.Y < 0 || targetArea.Value.Right > targetBounds.Width || targetArea.Value.Bottom > targetBounds.Height)
                throw new ArgumentOutOfRangeException(nameof(targetArea), $"The target area ({targetArea}) is outside the bounds of the target map ({targetBounds}).");
            if (sourceArea.Value.Width != targetArea.Value.Width || sourceArea.Value.Height != targetArea.Value.Height)
                throw new InvalidOperationException($"The source area ({sourceArea}) and target area ({targetArea}) must be the same size.");
        }

        // apply tilesheets
        IDictionary<TileSheet, TileSheet> tilesheetMap = new Dictionary<TileSheet, TileSheet>();
        foreach (TileSheet sourceSheet in source.TileSheets)
        {
            // copy tilesheets
            TileSheet targetSheet = target.GetTileSheet(sourceSheet.Id);
            if (targetSheet == null || this.NormalizeTilesheetPathForComparison(targetSheet.ImageSource) != this.NormalizeTilesheetPathForComparison(sourceSheet.ImageSource))
            {
                // change ID if needed so new tilesheets are added after vanilla ones (to avoid errors in hardcoded game logic)
                string id = sourceSheet.Id;
                if (!id.StartsWith("z_", StringComparison.OrdinalIgnoreCase))
                    id = $"z_{id}";

                // change ID if it conflicts with an existing tilesheet
                if (target.GetTileSheet(id) != null)
                {
                    int disambiguator = Enumerable.Range(2, int.MaxValue - 1).First(p => target.GetTileSheet($"{id}_{p}") == null);
                    id = $"{id}_{disambiguator}";
                }

                // add tilesheet
                targetSheet = new TileSheet(id, target, sourceSheet.ImageSource, sourceSheet.SheetSize, sourceSheet.TileSize);
                for (int i = 0, tileCount = sourceSheet.TileCount; i < tileCount; ++i)
                    targetSheet.TileIndexProperties[i].CopyFrom(sourceSheet.TileIndexProperties[i]);
                target.AddTileSheet(targetSheet);
            }

            tilesheetMap[sourceSheet] = targetSheet;
        }

        // get target layers
        Dictionary<Layer, Layer> sourceToTargetLayers =
            (
                from sourceLayer in source.Layers
                let targetLayer = target.GetLayer(sourceLayer.Id)
                where targetLayer != null
                select (sourceLayer, targetLayer)
            )
            .ToDictionary(p => p.sourceLayer, p => p.targetLayer);
        HashSet<Layer> orphanedTargetLayers = new(target.Layers.Except(sourceToTargetLayers.Values));

        // apply tiles
        bool replaceAll = patchMode == PatchMapMode.Replace;
        bool replaceByLayer = patchMode == PatchMapMode.ReplaceByLayer;
        for (int x = 0; x < sourceArea.Value.Width; x++)
        {
            for (int y = 0; y < sourceArea.Value.Height; y++)
            {
                // calculate tile positions
                Point sourcePos = new(sourceArea.Value.X + x, sourceArea.Value.Y + y);
                Point targetPos = new(targetArea.Value.X + x, targetArea.Value.Y + y);

                // replace tiles on target-only layers
                if (replaceAll)
                {
                    foreach (Layer targetLayer in orphanedTargetLayers)
                        targetLayer.Tiles[targetPos.X, targetPos.Y] = null;
                }

                // merge layers
                foreach (Layer sourceLayer in source.Layers)
                {
                    // get layer
                    if (!sourceToTargetLayers.TryGetValue(sourceLayer, out Layer? targetLayer))
                    {
                        target.AddLayer(targetLayer = new Layer(sourceLayer.Id, target, target.Layers[0].LayerSize, Layer.m_tileSize));
                        sourceToTargetLayers[sourceLayer] = target.GetLayer(sourceLayer.Id);
                    }

                    // copy layer properties
                    targetLayer.Properties.CopyFrom(sourceLayer.Properties);

                    // create new tile
                    Tile? sourceTile = sourceLayer.Tiles[sourcePos.X, sourcePos.Y];
                    Tile? newTile = null;
                    if (sourceTile != null)
                    {
                        newTile = this.CreateTile(sourceTile, targetLayer, tilesheetMap[sourceTile.TileSheet]);
                        newTile?.Properties.CopyFrom(sourceTile.Properties);
                    }

                    // replace tile
                    if (newTile != null || replaceByLayer || replaceAll)
                        targetLayer.Tiles[targetPos.X, targetPos.Y] = newTile;
                }
            }
        }
    }

    /// <inheritdoc />
    public bool ExtendMap(int minWidth = 0, int minHeight = 0)
    {
        bool resized = false;
        Map map = this.Data;

        // resize layers
        foreach (Layer layer in map.Layers)
        {
            // check if resize needed
            if (layer.LayerWidth >= minWidth && layer.LayerHeight >= minHeight)
                continue;
            resized = true;

            // build new tile matrix
            int width = Math.Max(minWidth, layer.LayerWidth);
            int height = Math.Max(minHeight, layer.LayerHeight);
            Tile[,] tiles = new Tile[width, height];
            for (int x = 0; x < layer.LayerWidth; x++)
            {
                for (int y = 0; y < layer.LayerHeight; y++)
                    tiles[x, y] = layer.Tiles[x, y];
            }

            // update fields
            this.Reflection.GetField<Tile[,]>(layer, "m_tiles").SetValue(tiles);
            this.Reflection.GetField<TileArray>(layer, "m_tileArray").SetValue(new TileArray(layer, tiles));
            this.Reflection.GetField<Size>(layer, "m_layerSize").SetValue(new Size(width, height));
        }

        // resize map
        if (resized)
            this.Reflection.GetMethod(map, "UpdateDisplaySize").Invoke();

        return resized;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Create a new tile for the target map.</summary>
    /// <param name="sourceTile">The source tile to copy.</param>
    /// <param name="targetLayer">The target layer.</param>
    /// <param name="targetSheet">The target tilesheet.</param>
    private Tile? CreateTile(Tile sourceTile, Layer targetLayer, TileSheet targetSheet)
    {
        switch (sourceTile)
        {
            case StaticTile:
                return new StaticTile(targetLayer, targetSheet, sourceTile.BlendMode, sourceTile.TileIndex);

            case AnimatedTile animatedTile:
            {
                StaticTile[] tileFrames = new StaticTile[animatedTile.TileFrames.Length];
                for (int frame = 0; frame < animatedTile.TileFrames.Length; ++frame)
                {
                    StaticTile frameTile = animatedTile.TileFrames[frame];
                    tileFrames[frame] = new StaticTile(targetLayer, targetSheet, frameTile.BlendMode, frameTile.TileIndex);
                }

                return new AnimatedTile(targetLayer, tileFrames, animatedTile.FrameInterval);
            }

            default: // null or unhandled type
                return null;
        }
    }
    /// <summary>Normalize a map tilesheet path for comparison. This value should *not* be used as the actual tilesheet path.</summary>
    /// <param name="path">The path to normalize.</param>
    private string NormalizeTilesheetPathForComparison(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = PathUtilities.NormalizeAssetName(path);
        if (path.StartsWith($"Maps{PathUtilities.PreferredAssetSeparator}", StringComparison.OrdinalIgnoreCase))
            path = path.Substring($"Maps{PathUtilities.PreferredAssetSeparator}".Length);
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            path = path.Substring(0, path.Length - 4);

        return path;
    }

    /// <summary>Get a rectangle which encompasses all layers for a map.</summary>
    /// <param name="map">The map to check.</param>
    private Rectangle GetMapArea(Map map)
    {
        // get max map size
        int maxWidth = 0;
        int maxHeight = 0;
        foreach (Layer layer in map.Layers)
        {
            if (layer.LayerWidth > maxWidth)
                maxWidth = layer.LayerWidth;
            if (layer.LayerHeight > maxHeight)
                maxHeight = layer.LayerHeight;
        }

        return new Rectangle(0, 0, maxWidth, maxHeight);
    }
}