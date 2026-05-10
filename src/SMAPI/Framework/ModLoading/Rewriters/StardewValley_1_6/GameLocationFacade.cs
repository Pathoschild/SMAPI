using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Furniture;
using StardewValley.Objects;
using xTile;
using xTile.Dimensions;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="GameLocation"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class GameLocationFacade : GameLocation, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public static int CAROLINES_NECKLACE_ITEM => 191;


    /*********
    ** Public methods
    *********/
    public NetCollection<NPC> getCharacters()
    {
        return base.characters;
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public Building GetContainingBuilding()
    {
        return base.ParentBuilding;
    }

    public virtual int getExtraMillisecondsPerInGameMinuteForThisLocation()
    {
        return base.ExtraMillisecondsPerInGameMinute;
    }

    public Object? getFish(float millisecondsAfterNibble, int bait, int waterDepth, Farmer who, double baitPotency, Vector2 bobberTile, string? location = null)
    {
        return base.getFish(millisecondsAfterNibble, bait.ToString(), waterDepth, who, baitPotency, bobberTile, location) as Object;
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public LightSource? getLightSource(int identifier)
    {
        return base.getLightSource(identifier.ToString());
    }

    public Dictionary<string, string> GetLocationEvents()
    {
        return base.TryGetLocationEvents(out _, out Dictionary<string, string> events)
            ? events
            : new Dictionary<string, string>();
    }

    public Point GetMapPropertyPosition(string key, int default_x, int default_y)
    {
        return base.TryGetMapPropertyAs(key, out Point point)
            ? point
            : new Point(default_x, default_y);
    }

    public int getNumberBuildingsConstructed(string name)
    {
        return base.getNumberBuildingsConstructed(name, includeUnderConstruction: false);
    }

    public Object getObjectAtTile(int x, int y)
    {
        return base.getObjectAtTile(x, y);
    }

    public string GetSeasonForLocation()
    {
        return base.GetSeasonKey();
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public int getTileIndexAt(Location p, string layer)
    {
        return base.getTileIndexAt(p, layer);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public int getTileIndexAt(Point p, string layer)
    {
        return base.getTileIndexAt(p.X, p.Y, layer);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public int getTileIndexAt(int x, int y, string layer)
    {
        return base.getTileIndexAt(x, y, layer);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public bool hasLightSource(int identifier)
    {
        return base.hasLightSource(identifier.ToString());
    }

    public bool isTileLocationOpenIgnoreFrontLayers(Location tile)
    {
        return base.map.RequireLayer("Buildings").Tiles[tile.X, tile.Y] == null && !base.isWaterTile(tile.X, tile.Y);
    }

    public bool isTileLocationTotallyClearAndPlaceable(int x, int y)
    {
        return this.isTileLocationTotallyClearAndPlaceable(new Vector2(x, y));
    }

    public bool isTileLocationTotallyClearAndPlaceable(Vector2 v)
    {
        Vector2 pixel = new((v.X * Game1.tileSize) + Game1.tileSize / 2, (v.Y * Game1.tileSize) + Game1.tileSize / 2);
        foreach (Furniture f in base.furniture)
        {
            if (f.furniture_type.Value != FurnitureType.Rug && !f.isPassable() && f.GetBoundingBox().Contains((int)pixel.X, (int)pixel.Y) && !f.AllowPlacementOnThisTile((int)v.X, (int)v.Y))
                return false;
        }

        return base.isTileOnMap(v) && !this.isTileOccupied(v) && base.isTilePassable(new Location((int)v.X, (int)v.Y), Game1.viewport) && base.isTilePlaceable(v);
    }

    public bool isTileLocationTotallyClearAndPlaceableIgnoreFloors(Vector2 v)
    {
        return base.isTileOnMap(v) && !this.isTileOccupiedIgnoreFloors(v) && base.isTilePassable(new Location((int)v.X, (int)v.Y), Game1.viewport) && base.isTilePlaceable(v);
    }

    public bool isTileOccupied(Vector2 tileLocation, string characterToIgnore = "", bool ignoreAllCharacters = false)
    {
        CollisionMask mask = ignoreAllCharacters ? CollisionMask.All & ~CollisionMask.Characters & ~CollisionMask.Farmers : CollisionMask.All;
        return base.IsTileOccupiedBy(tileLocation, mask);
    }

    public bool isTileOccupiedForPlacement(Vector2 tileLocation, Object? toPlace = null)
    {
        return base.CanItemBePlacedHere(tileLocation, toPlace != null && toPlace.isPassable());
    }

    public bool isTileOccupiedIgnoreFloors(Vector2 tileLocation, string characterToIgnore = "")
    {
        return base.IsTileOccupiedBy(tileLocation, CollisionMask.Buildings | CollisionMask.Furniture | CollisionMask.Objects | CollisionMask.Characters | CollisionMask.TerrainFeatures, ignorePassables: CollisionMask.Flooring);
    }

    public void localSound(string audioName)
    {
        base.localSound(audioName);
    }

    public void localSoundAt(string audioName, Vector2 position)
    {
        base.localSound(audioName, position);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public new void makeHoeDirt(Vector2 tileLocation, bool ignoreChecks = false)
    {
        base.makeHoeDirt(tileLocation, ignoreChecks);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public bool moveObject(int oldX, int oldY, int newX, int newY, string unlessItemId)
    {
        return base.moveContents(oldX, oldY, newX, newY, unlessItemId);
    }

    public void OnStoneDestroyed(int indexOfStone, int x, int y, Farmer who)
    {
        base.OnStoneDestroyed(indexOfStone.ToString(), x, y, who);
    }

    public void playSound(string audioName, SoundContext soundContext = SoundContext.Default)
    {
        base.playSound(audioName, context: soundContext);
    }

    public void playSoundAt(string audioName, Vector2 position, SoundContext soundContext = SoundContext.Default)
    {
        base.playSound(audioName, position, context: soundContext);
    }

    public void playSoundPitched(string audioName, int pitch, SoundContext soundContext = SoundContext.Default)
    {
        base.playSound(audioName, pitch: pitch, context: soundContext);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public void repositionLightSource(int identifier, Vector2 position)
    {
        base.repositionLightSource(identifier.ToString(), position);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public void removeLightSource(int identifier)
    {
        base.removeLightSource(identifier.ToString());
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public void setAnimatedMapTile(int tileX, int tileY, int[] animationTileIndexes, long interval, string layer, string action, int whichTileSheet = 0)
    {
        if (GameLocationFacade.TryGetTileSheetId(this, whichTileSheet, out string? tilesheetId))
            base.setAnimatedMapTile(tileX, tileY, animationTileIndexes, interval, layer, tilesheetId, action);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public void setMapTile(int tileX, int tileY, int index, string layer, string action, int whichTileSheet = 0)
    {
        if (GameLocationFacade.TryGetTileSheetId(this, whichTileSheet, out string? tilesheetId))
            base.setMapTile(tileX, tileY, index, layer, tilesheetId, action);
    }

    /// <remarks>Changed in Stardew Valley 1.6.9.</remarks>
    public void setMapTileIndex(int tileX, int tileY, int index, string layer, int whichTileSheet = 0)
    {
        if (index == -1)
            base.removeMapTile(tileX, tileY, layer);

        else if (GameLocationFacade.TryGetTileSheetId(this, whichTileSheet, out string? tilesheetId))
            base.setMapTile(tileX, tileY, index, layer, tilesheetId);
    }


    /*********
    ** Private methods
    *********/
    private GameLocationFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }

    /// <summary>Get the ID for the tilesheet at the given index in this location's <see cref="Map.TileSheets"/>.</summary>
    /// <param name="location">The location whose tilesheets to search.</param>
    /// <param name="index">The index to check.</param>
    /// <param name="id">The ID of the tilesheet, if found.</param>
    /// <returns>Returns whether the index was within the bounds of the tilesheet list.</returns>
    private static bool TryGetTileSheetId(GameLocation location, int index, [NotNullWhen(true)] out string? id)
    {
        var tilesheets = location.map?.TileSheets;

        if (tilesheets is not null && index >= 0 && index < tilesheets.Count)
        {
            id = tilesheets[index].Id;
            return true;
        }

        id = null;
        return false;
    }
}
