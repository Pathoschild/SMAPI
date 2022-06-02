using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData.Movies;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;
using xTile;
using xTile.Tiles;

namespace StardewModdingAPI.Metadata;

/// <summary>Propagates changes to core assets to the game state.</summary>
internal class CoreAssetPropagator
{
    /*********
    ** Fields
    *********/
    /// <summary>The main content manager through which to reload assets.</summary>
    private readonly LocalizedContentManager MainContentManager;

    /// <summary>An internal content manager used only for asset propagation. See remarks on <see cref="GameContentManagerForAssetPropagation"/>.</summary>
    private readonly GameContentManagerForAssetPropagation DisposableContentManager;

    /// <summary>Writes messages to the console.</summary>
    private readonly IMonitor Monitor;

    /// <summary>Simplifies access to private game code.</summary>
    private readonly Reflector Reflection;

    /// <summary>Parse a raw asset name.</summary>
    private readonly Func<string, IAssetName> ParseAssetName;

    /// <summary>Optimized bucket categories for batch reloading assets.</summary>
    private enum AssetBucket
    {
        /// <summary>NPC overworld sprites.</summary>
        Sprite,

        /// <summary>Villager dialogue portraits.</summary>
        Portrait,

        /// <summary>Any other asset.</summary>
        Other
    };

    /// <summary>A cache of world data fetched for the current tick.</summary>
    private readonly TickCacheDictionary<string> WorldCache = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Initialize the core asset data.</summary>
    /// <param name="mainContent">The main content manager through which to reload assets.</param>
    /// <param name="disposableContent">An internal content manager used only for asset propagation.</param>
    /// <param name="monitor">Writes messages to the console.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    /// <param name="parseAssetName">Parse a raw asset name.</param>
    public CoreAssetPropagator(LocalizedContentManager mainContent, GameContentManagerForAssetPropagation disposableContent, IMonitor monitor, Reflector reflection, Func<string, IAssetName> parseAssetName)
    {
        this.MainContentManager = mainContent;
        this.DisposableContentManager = disposableContent;
        this.Monitor = monitor;
        this.Reflection = reflection;
        this.ParseAssetName = parseAssetName;
    }

    /// <summary>Reload one of the game's core assets (if applicable).</summary>
    /// <param name="assets">The asset keys and types to reload.</param>
    /// <param name="ignoreWorld">Whether the in-game world is fully unloaded (e.g. on the title screen), so there's no need to propagate changes into the world.</param>
    /// <param name="propagatedAssets">A lookup of asset names to whether they've been propagated.</param>
    /// <param name="updatedNpcWarps">Whether the NPC pathfinding cache was reloaded.</param>
    public void Propagate(IDictionary<IAssetName, Type> assets, bool ignoreWorld, out IDictionary<IAssetName, bool> propagatedAssets, out bool updatedNpcWarps)
    {
        // get base name lookup
        propagatedAssets = assets
            .Select(asset => asset.Key.GetBaseAssetName())
            .Distinct()
            .ToDictionary(name => name, _ => false);

        // group into optimized lists
        var buckets = assets.GroupBy(p =>
        {
            if (p.Key.IsDirectlyUnderPath("Characters") || p.Key.IsDirectlyUnderPath("Characters/Monsters"))
                return AssetBucket.Sprite;

            if (p.Key.IsDirectlyUnderPath("Portraits"))
                return AssetBucket.Portrait;

            return AssetBucket.Other;
        });

        // reload assets
        updatedNpcWarps = false;
        foreach (var bucket in buckets)
        {
            switch (bucket.Key)
            {
                case AssetBucket.Sprite:
                    if (!ignoreWorld)
                        this.UpdateNpcSprites(propagatedAssets);
                    break;

                case AssetBucket.Portrait:
                    if (!ignoreWorld)
                        this.UpdateNpcPortraits(propagatedAssets);
                    break;

                default:
                    foreach (var entry in bucket)
                    {
                        bool changed = false;
                        bool curChangedMapWarps = false;
                        try
                        {
                            changed = this.PropagateOther(entry.Key, entry.Value, ignoreWorld, out curChangedMapWarps);
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"An error occurred while propagating asset changes. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                        }

                        propagatedAssets[entry.Key] = changed;
                        updatedNpcWarps = updatedNpcWarps || curChangedMapWarps;
                    }
                    break;
            }
        }

        // reload NPC pathfinding cache if any map changed
        if (updatedNpcWarps)
            NPC.populateRoutesFromLocationToLocationList();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Reload one of the game's core assets (if applicable).</summary>
    /// <param name="assetName">The asset name to reload.</param>
    /// <param name="type">The asset type to reload.</param>
    /// <param name="ignoreWorld">Whether the in-game world is fully unloaded (e.g. on the title screen), so there's no need to propagate changes into the world.</param>
    /// <param name="changedWarps">Whether any map warps were changed as part of this propagation.</param>
    /// <returns>Returns whether an asset was loaded. The return value may be true or false, or a non-null value for true.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "These deliberately match the asset names.")]
    private bool PropagateOther(IAssetName assetName, Type type, bool ignoreWorld, out bool changedWarps)
    {
        var content = this.MainContentManager;
        string key = assetName.BaseName;
        changedWarps = false;

        /****
        ** Special case: current map tilesheet
        ** We only need to do this for the current location, since tilesheets are reloaded when you enter a location.
        ** Just in case, we should still propagate by key even if a tilesheet is matched.
        ****/
        if (!ignoreWorld && Game1.currentLocation?.map?.TileSheets != null)
        {
            foreach (TileSheet tilesheet in Game1.currentLocation.map.TileSheets)
            {
                if (this.IsSameBaseName(assetName, tilesheet.ImageSource))
                    Game1.mapDisplayDevice.LoadTileSheet(tilesheet);
            }
        }

        /****
        ** Propagate map changes
        ****/
        if (type == typeof(Map))
        {
            bool anyChanged = false;

            if (!ignoreWorld)
            {
                foreach (LocationInfo info in this.GetLocationsWithInfo())
                {
                    GameLocation location = info.Location;

                    if (this.IsSameBaseName(assetName, location.mapPath.Value))
                    {
                        static ISet<string> GetWarpSet(GameLocation location)
                        {
                            return new HashSet<string>(
                                location.warps.Select(p => $"{p.X} {p.Y} {p.TargetName} {p.TargetX} {p.TargetY}")
                            );
                        }

                        var oldWarps = GetWarpSet(location);
                        this.UpdateMap(info);
                        var newWarps = GetWarpSet(location);

                        changedWarps = changedWarps || oldWarps.Count != newWarps.Count || oldWarps.Any(p => !newWarps.Contains(p));
                        anyChanged = true;
                    }
                }
            }

            return anyChanged;
        }

        /****
        ** Propagate by key
        ****/
        switch (assetName.BaseName.ToLower().Replace("\\", "/")) // normalized key so we can compare statically
        {
            /****
            ** Animals
            ****/
            case "animals/horse":
                return !ignoreWorld && this.UpdatePetOrHorseSprites<Horse>(assetName);

            /****
            ** Buildings
            ****/
            case "buildings/houses": // Farm
                Farm.houseTextures = this.LoadTexture(key);
                return true;

            case "buildings/houses_paintmask": // Farm
            {
                bool removedFromCache = this.RemoveFromPaintMaskCache(assetName);

                Farm farm = Game1.getFarm();
                farm?.ApplyHousePaint();

                return removedFromCache || farm != null;
            }

            /****
            ** Content\Characters\Farmer
            ****/
            case "characters/farmer/accessories": // Game1.LoadContent
                FarmerRenderer.accessoriesTexture = this.LoadTexture(key);
                return true;

            case "characters/farmer/farmer_base": // Farmer
            case "characters/farmer/farmer_base_bald":
            case "characters/farmer/farmer_girl_base":
            case "characters/farmer/farmer_girl_base_bald":
                return !ignoreWorld && this.UpdatePlayerSprites(assetName);

            case "characters/farmer/hairstyles": // Game1.LoadContent
                FarmerRenderer.hairStylesTexture = this.LoadTexture(key);
                return true;

            case "characters/farmer/hats": // Game1.LoadContent
                FarmerRenderer.hatsTexture = this.LoadTexture(key);
                return true;

            case "characters/farmer/pants": // Game1.LoadContent
                FarmerRenderer.pantsTexture = this.LoadTexture(key);
                return true;

            case "characters/farmer/shirts": // Game1.LoadContent
                FarmerRenderer.shirtsTexture = this.LoadTexture(key);
                return true;

            /****
            ** Content\Data
            ****/
            case "data/achievements": // Game1.LoadContent
                Game1.achievements = content.Load<Dictionary<int, string>>(key);
                return true;

            case "data/bigcraftablesinformation": // Game1.LoadContent
                Game1.bigCraftablesInformation = content.Load<Dictionary<int, string>>(key);
                return true;

            case "data/clothinginformation": // Game1.LoadContent
                Game1.clothingInformation = content.Load<Dictionary<int, string>>(key);
                return true;

            case "data/concessions": // MovieTheater.GetConcessions
                MovieTheater.ClearCachedLocalizedData();
                return true;

            case "data/concessiontastes": // MovieTheater.GetConcessionTasteForCharacter
                this.Reflection
                    .GetField<List<ConcessionTaste>>(typeof(MovieTheater), "_concessionTastes")
                    .SetValue(content.Load<List<ConcessionTaste>>(key));
                return true;

            case "data/cookingrecipes": // CraftingRecipe.InitShared
                CraftingRecipe.cookingRecipes = content.Load<Dictionary<string, string>>(key);
                return true;

            case "data/craftingrecipes": // CraftingRecipe.InitShared
                CraftingRecipe.craftingRecipes = content.Load<Dictionary<string, string>>(key);
                return true;

            case "data/farmanimals": // FarmAnimal constructor
                return !ignoreWorld && this.UpdateFarmAnimalData();

            case "data/hairdata": // Farmer.GetHairStyleMetadataFile
                return this.UpdateHairData();

            case "data/movies": // MovieTheater.GetMovieData
            case "data/moviesreactions": // MovieTheater.GetMovieReactions
                MovieTheater.ClearCachedLocalizedData();
                return true;

            case "data/npcdispositions": // NPC constructor
                return !ignoreWorld && this.UpdateNpcDispositions(content, assetName);

            case "data/npcgifttastes": // Game1.LoadContent
                Game1.NPCGiftTastes = content.Load<Dictionary<string, string>>(key);
                return true;

            case "data/objectcontexttags": // Game1.LoadContent
                Game1.objectContextTags = content.Load<Dictionary<string, string>>(key);
                return true;

            case "data/objectinformation": // Game1.LoadContent
                Game1.objectInformation = content.Load<Dictionary<int, string>>(key);
                return true;

            /****
            ** Content\Fonts
            ****/
            case "fonts/spritefont1": // Game1.LoadContent
                Game1.dialogueFont = content.Load<SpriteFont>(key);
                return true;

            case "fonts/smallfont": // Game1.LoadContent
                Game1.smallFont = content.Load<SpriteFont>(key);
                return true;

            case "fonts/tinyfont": // Game1.LoadContent
                Game1.tinyFont = content.Load<SpriteFont>(key);
                return true;

            case "fonts/tinyfontborder": // Game1.LoadContent
                Game1.tinyFontBorder = content.Load<SpriteFont>(key);
                return true;

            /****
            ** Content\LooseSprites\Lighting
            ****/
            case "loosesprites/lighting/greenlight": // Game1.LoadContent
                Game1.cauldronLight = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/lighting/indoorwindowlight": // Game1.LoadContent
                Game1.indoorWindowLight = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/lighting/lantern": // Game1.LoadContent
                Game1.lantern = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/lighting/sconcelight": // Game1.LoadContent
                Game1.sconceLight = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/lighting/windowlight": // Game1.LoadContent
                Game1.windowLight = content.Load<Texture2D>(key);
                return true;

            /****
            ** Content\LooseSprites
            ****/
            case "loosesprites/birds": // Game1.LoadContent
                Game1.birdsSpriteSheet = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/concessions": // Game1.LoadContent
                Game1.concessionsSpriteSheet = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/controllermaps": // Game1.LoadContent
                Game1.controllerMaps = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/cursors": // Game1.LoadContent
                Game1.mouseCursors = content.Load<Texture2D>(key);
                foreach (DayTimeMoneyBox menu in Game1.onScreenMenus.OfType<DayTimeMoneyBox>())
                {
                    foreach (ClickableTextureComponent button in new[] { menu.questButton, menu.zoomInButton, menu.zoomOutButton })
                        button.texture = Game1.mouseCursors;
                }

                if (!ignoreWorld)
                    this.UpdateDoorSprites(content, assetName);
                return true;

            case "loosesprites/cursors2": // Game1.LoadContent
                Game1.mouseCursors2 = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/daybg": // Game1.LoadContent
                Game1.daybg = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/font_bold": // Game1.LoadContent
                SpriteText.spriteTexture = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/font_colored": // Game1.LoadContent
                SpriteText.coloredTexture = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/giftbox": // Game1.LoadContent
                Game1.giftboxTexture = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/nightbg": // Game1.LoadContent
                Game1.nightbg = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/shadow": // Game1.LoadContent
                Game1.shadowTexture = content.Load<Texture2D>(key);
                return true;

            case "loosesprites/suspensionbridge": // SuspensionBridge constructor
                return !ignoreWorld && this.UpdateSuspensionBridges(content, assetName);

            /****
            ** Content\Maps
            ****/
            case "maps/menutiles": // Game1.LoadContent
                Game1.menuTexture = content.Load<Texture2D>(key);
                return true;

            case "maps/menutilesuncolored": // Game1.LoadContent
                Game1.uncoloredMenuTexture = content.Load<Texture2D>(key);
                return true;

            case "maps/springobjects": // Game1.LoadContent
                Game1.objectSpriteSheet = content.Load<Texture2D>(key);
                return true;

            /****
            ** Content\Minigames
            ****/
            case "minigames/clouds": // TitleMenu
            {
                if (Game1.activeClickableMenu is TitleMenu titleMenu)
                {
                    titleMenu.cloudsTexture = content.Load<Texture2D>(key);
                    return true;
                }
            }
                return false;

            case "minigames/titlebuttons": // TitleMenu
                return this.UpdateTitleButtons(content, assetName);

            /****
            ** Content\Strings
            ****/
            case "strings/stringsfromcsfiles":
                return this.UpdateStringsFromCsFiles(content);

            /****
            ** Content\TileSheets
            ****/
            case "tilesheets/animations": // Game1.LoadContent
                Game1.animations = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/buffsicons": // Game1.LoadContent
                Game1.buffsIcons = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/bushes": // new Bush()
                Bush.texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));
                return true;

            case "tilesheets/chairtiles": // Game1.LoadContent
                return this.UpdateChairTiles(content, assetName, ignoreWorld);

            case "tilesheets/craftables": // Game1.LoadContent
                Game1.bigCraftableSpriteSheet = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/critters": // Critter constructor
                return !ignoreWorld && this.UpdateCritterTextures(assetName);

            case "tilesheets/crops": // Game1.LoadContent
                Game1.cropSpriteSheet = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/debris": // Game1.LoadContent
                Game1.debrisSpriteSheet = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/emotes": // Game1.LoadContent
                Game1.emoteSpriteSheet = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/fruittrees": // FruitTree
                FruitTree.texture = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/furniture": // Game1.LoadContent
                Furniture.furnitureTexture = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/furniturefront": // Game1.LoadContent
                Furniture.furnitureFrontTexture = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/projectiles": // Game1.LoadContent
                Projectile.projectileSheet = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/rain": // Game1.LoadContent
                Game1.rainTexture = content.Load<Texture2D>(key);
                return true;

            case "tilesheets/tools": // Game1.ResetToolSpriteSheet
                Game1.ResetToolSpriteSheet();
                return true;

            case "tilesheets/weapons": // Game1.LoadContent
                Tool.weaponsTexture = content.Load<Texture2D>(key);
                return true;

            /****
            ** Content\TerrainFeatures
            ****/
            case "terrainfeatures/flooring": // from Flooring
                Flooring.floorsTexture = content.Load<Texture2D>(key);
                return true;

            case "terrainfeatures/flooring_winter": // from Flooring
                Flooring.floorsTextureWinter = content.Load<Texture2D>(key);
                return true;

            case "terrainfeatures/grass": // from Grass
                return !ignoreWorld && this.UpdateGrassTextures(content, assetName);

            case "terrainfeatures/hoedirt": // from HoeDirt
                HoeDirt.lightTexture = content.Load<Texture2D>(key);
                return true;

            case "terrainfeatures/hoedirtdark": // from HoeDirt
                HoeDirt.darkTexture = content.Load<Texture2D>(key);
                return true;

            case "terrainfeatures/hoedirtsnow": // from HoeDirt
                HoeDirt.snowTexture = content.Load<Texture2D>(key);
                return true;

            case "terrainfeatures/mushroom_tree": // from Tree
                return !ignoreWorld && this.UpdateTreeTextures(Tree.mushroomTree);

            case "terrainfeatures/tree_palm": // from Tree
                return !ignoreWorld && this.UpdateTreeTextures(Tree.palmTree);

            case "terrainfeatures/tree1_fall": // from Tree
            case "terrainfeatures/tree1_spring": // from Tree
            case "terrainfeatures/tree1_summer": // from Tree
            case "terrainfeatures/tree1_winter": // from Tree
                return !ignoreWorld && this.UpdateTreeTextures(Tree.bushyTree);

            case "terrainfeatures/tree2_fall": // from Tree
            case "terrainfeatures/tree2_spring": // from Tree
            case "terrainfeatures/tree2_summer": // from Tree
            case "terrainfeatures/tree2_winter": // from Tree
                return !ignoreWorld && this.UpdateTreeTextures(Tree.leafyTree);

            case "terrainfeatures/tree3_fall": // from Tree
            case "terrainfeatures/tree3_spring": // from Tree
            case "terrainfeatures/tree3_winter": // from Tree
                return !ignoreWorld && this.UpdateTreeTextures(Tree.pineTree);
        }

        /****
        ** Dynamic assets
        ****/
        if (!ignoreWorld)
        {
            // dynamic textures
            if (assetName.StartsWith("animals/cat"))
                return this.UpdatePetOrHorseSprites<Cat>(assetName);
            if (assetName.StartsWith("animals/dog"))
                return this.UpdatePetOrHorseSprites<Dog>(assetName);
            if (assetName.IsDirectlyUnderPath("Animals"))
                return this.UpdateFarmAnimalSprites(assetName);

            if (assetName.IsDirectlyUnderPath("Buildings"))
                return this.UpdateBuildings(assetName);

            if (assetName.StartsWith("LooseSprites/Fence"))
                return this.UpdateFenceTextures(assetName);

            // dynamic data
            if (assetName.IsDirectlyUnderPath("Characters/Dialogue"))
                return this.UpdateNpcDialogue(assetName);

            if (assetName.IsDirectlyUnderPath("Characters/schedules"))
                return this.UpdateNpcSchedules(assetName);
        }

        return false;
    }


    /*********
    ** Private methods
    *********/
    /****
    ** Update texture methods
    ****/
    /// <summary>Update buttons on the title screen.</summary>
    /// <param name="content">The content manager through which to update the asset.</param>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    /// <remarks>Derived from the <see cref="TitleMenu"/> constructor and <see cref="TitleMenu.setUpIcons"/>.</remarks>
    private bool UpdateTitleButtons(LocalizedContentManager content, IAssetName assetName)
    {
        if (Game1.activeClickableMenu is TitleMenu titleMenu)
        {
            Texture2D texture = content.Load<Texture2D>(assetName.BaseName);

            titleMenu.titleButtonsTexture = texture;
            titleMenu.backButton.texture = texture;
            titleMenu.aboutButton.texture = texture;
            titleMenu.languageButton.texture = texture;
            foreach (ClickableTextureComponent button in titleMenu.buttons)
                button.texture = texture;
            foreach (TemporaryAnimatedSprite bird in titleMenu.birds)
                bird.texture = texture;

            return true;
        }

        return false;
    }

    /// <summary>Update the sprites for matching pets or horses.</summary>
    /// <typeparam name="TAnimal">The animal type.</typeparam>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private bool UpdatePetOrHorseSprites<TAnimal>(IAssetName assetName)
        where TAnimal : NPC
    {
        // find matches
        TAnimal[] animals = this.GetCharacters()
            .OfType<TAnimal>()
            .Where(p => this.IsSameBaseName(assetName, p.Sprite?.spriteTexture?.Name))
            .ToArray();

        // update sprites
        bool changed = false;
        foreach (TAnimal animal in animals)
            changed |= this.MarkSpriteDirty(animal.Sprite);
        return changed;
    }

    /// <summary>Update the sprites for matching farm animals.</summary>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    /// <remarks>Derived from <see cref="FarmAnimal.reload"/>.</remarks>
    private bool UpdateFarmAnimalSprites(IAssetName assetName)
    {
        // find matches
        FarmAnimal[] animals = this.GetFarmAnimals().ToArray();
        if (!animals.Any())
            return false;

        // update sprites
        bool changed = true;
        foreach (FarmAnimal animal in animals)
        {
            // get expected key
            string expectedKey = animal.age.Value < animal.ageWhenMature.Value
                ? $"Baby{(animal.type.Value == "Duck" ? "White Chicken" : animal.type.Value)}"
                : animal.type.Value;
            if (animal.showDifferentTextureWhenReadyForHarvest.Value && animal.currentProduce.Value <= 0)
                expectedKey = $"Sheared{expectedKey}";
            expectedKey = $"Animals/{expectedKey}";

            // reload asset
            if (this.IsSameBaseName(assetName, expectedKey))
                changed |= this.MarkSpriteDirty(animal.Sprite);
        }
        return changed;
    }

    /// <summary>Update building textures.</summary>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private bool UpdateBuildings(IAssetName assetName)
    {
        // get paint mask info
        const string paintMaskSuffix = "_PaintMask";
        bool isPaintMask = assetName.BaseName.EndsWith(paintMaskSuffix, StringComparison.OrdinalIgnoreCase);

        // get building type
        string type = Path.GetFileName(assetName.BaseName);
        if (isPaintMask)
            type = type[..^paintMaskSuffix.Length];

        // get buildings
        Building[] buildings = this.GetLocations(buildingInteriors: false)
            .OfType<BuildableGameLocation>()
            .SelectMany(p => p.buildings)
            .Where(p => p.buildingType.Value == type)
            .ToArray();

        // remove from paint mask cache
        bool removedFromCache = this.RemoveFromPaintMaskCache(assetName);

        // reload textures
        if (buildings.Any())
        {
            foreach (Building building in buildings)
                building.resetTexture();

            return true;
        }

        return removedFromCache;
    }

    /// <summary>Update map seat textures.</summary>
    /// <param name="content">The content manager through which to reload the asset.</param>
    /// <param name="assetName">The asset name to update.</param>
    /// <param name="ignoreWorld">Whether the in-game world is fully unloaded (e.g. on the title screen), so there's no need to propagate changes into the world.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private bool UpdateChairTiles(LocalizedContentManager content, IAssetName assetName, bool ignoreWorld)
    {
        MapSeat.mapChairTexture = content.Load<Texture2D>(assetName.BaseName);

        if (!ignoreWorld)
        {
            foreach (GameLocation location in this.GetLocations())
            {
                foreach (MapSeat seat in location.mapSeats.Where(p => p != null))
                {
                    if (this.IsSameBaseName(assetName, seat._loadedTextureFile))
                        seat._loadedTextureFile = null;
                }
            }
        }

        return true;
    }

    /// <summary>Update critter textures.</summary>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private bool UpdateCritterTextures(IAssetName assetName)
    {
        // get critters
        Critter[] critters =
            (
                from location in this.GetLocations()
                where location.critters != null
                from Critter critter in location.critters
                where this.IsSameBaseName(assetName, critter.sprite?.spriteTexture?.Name)
                select critter
            )
            .ToArray();

        // update sprites
        bool changed = false;
        foreach (Critter entry in critters)
            changed |= this.MarkSpriteDirty(entry.sprite);
        return changed;
    }

    /// <summary>Update the sprites for interior doors.</summary>
    /// <param name="content">The content manager through which to reload the asset.</param>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private void UpdateDoorSprites(LocalizedContentManager content, IAssetName assetName)
    {
        Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(assetName.BaseName));

        foreach (GameLocation location in this.GetLocations())
        {
            IEnumerable<InteriorDoor?>? doors = location.interiorDoors?.Doors;
            if (doors == null)
                continue;

            foreach (InteriorDoor? door in doors)
            {
                if (door?.Sprite == null)
                    continue;

                string? curKey = this.Reflection.GetField<string?>(door.Sprite, "textureName").GetValue();
                if (this.IsSameBaseName(assetName, curKey))
                    door.Sprite.texture = texture.Value;
            }
        }
    }

    /// <summary>Update the sprites for a fence type.</summary>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private bool UpdateFenceTextures(IAssetName assetName)
    {
        // get fence type (e.g. LooseSprites/Fence3 => 3)
        if (!int.TryParse(this.GetSegments(assetName.BaseName)[1]["Fence".Length..], out int fenceType))
            return false;

        // get fences
        Fence[] fences =
            (
                from location in this.GetLocations()
                from fence in location.Objects.Values.OfType<Fence>()
                where
                    fence.whichType.Value == fenceType
                    || (fence.isGate.Value && fenceType == 1) // gates are hardcoded to draw fence type 1
                select fence
            )
            .ToArray();

        // update fence textures
        bool changed = false;
        foreach (Fence fence in fences)
        {
            if (fence.fenceTexture.IsValueCreated)
            {
                fence.fenceTexture = new Lazy<Texture2D>(fence.loadFenceTexture);
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>Update tree textures.</summary>
    /// <param name="content">The content manager through which to reload the asset.</param>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private bool UpdateGrassTextures(LocalizedContentManager content, IAssetName assetName)
    {
        Grass[] grasses =
            (
                from grass in this.GetTerrainFeatures().OfType<Grass>()
                where this.IsSameBaseName(assetName, grass.textureName())
                select grass
            )
            .ToArray();

        bool changed = false;
        foreach (Grass grass in grasses)
        {
            if (grass.texture.IsValueCreated)
            {
                grass.texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(assetName.BaseName));
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>Update the sprites for matching NPCs.</summary>
    /// <param name="propagated">The asset names being propagated.</param>
    private void UpdateNpcSprites(IDictionary<IAssetName, bool> propagated)
    {
        // get NPCs
        var characters =
            (
                from npc in this.GetCharacters()
                let key = this.ParseAssetNameOrNull(npc.Sprite?.spriteTexture?.Name)?.GetBaseAssetName()
                where key != null && propagated.ContainsKey(key)
                select new { Npc = npc, AssetName = key }
            )
            .ToArray();

        // update sprite
        foreach (var target in characters)
        {
            if (this.MarkSpriteDirty(target.Npc.Sprite))
                propagated[target.AssetName] = true;
        }
    }

    /// <summary>Update the portraits for matching NPCs.</summary>
    /// <param name="propagated">The asset names being propagated.</param>
    private void UpdateNpcPortraits(IDictionary<IAssetName, bool> propagated)
    {
        // get NPCs
        var characters =
            (
                from npc in this.GetCharacters()
                where npc.isVillager()

                let key = this.ParseAssetNameOrNull(npc.Portrait?.Name)?.GetBaseAssetName()
                where key != null && propagated.ContainsKey(key)
                select new { Npc = npc, AssetName = key }
            )
            .ToList();

        // special case: Gil is a private NPC field on the AdventureGuild class (only used for the portrait)
        {
            IAssetName gilKey = this.ParseAssetName("Portraits/Gil");
            if (propagated.ContainsKey(gilKey))
            {
                GameLocation adventureGuild = Game1.getLocationFromName("AdventureGuild");
                if (adventureGuild != null)
                {
                    NPC? gil = this.Reflection.GetField<NPC?>(adventureGuild, "Gil").GetValue();
                    if (gil != null)
                        characters.Add(new { Npc = gil, AssetName = gilKey });
                }
            }
        }

        // update portrait
        foreach (var target in characters)
        {
            target.Npc.resetPortrait();
            propagated[target.AssetName] = true;
        }
    }

    /// <summary>Update the sprites for matching players.</summary>
    /// <param name="assetName">The asset name to update.</param>
    private bool UpdatePlayerSprites(IAssetName assetName)
    {
        Farmer[] players =
            (
                from player in Game1.getOnlineFarmers()
                where this.IsSameBaseName(assetName, player.getTexture())
                select player
            )
            .ToArray();

        foreach (Farmer player in players)
        {
            var recolorOffsets = this.Reflection.GetField<Dictionary<string, Dictionary<int, List<int>>>?>(typeof(FarmerRenderer), "_recolorOffsets").GetValue();
            recolorOffsets?.Clear();

            player.FarmerRenderer.MarkSpriteDirty();
        }

        return players.Any();
    }

    /// <summary>Update suspension bridge textures.</summary>
    /// <param name="content">The content manager through which to reload the asset.</param>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private bool UpdateSuspensionBridges(LocalizedContentManager content, IAssetName assetName)
    {
        Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(assetName.BaseName));

        foreach (GameLocation location in this.GetLocations(buildingInteriors: false))
        {
            // get suspension bridges field
            var field = this.Reflection.GetField<IEnumerable<SuspensionBridge>?>(location, nameof(IslandNorth.suspensionBridges), required: false);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse -- field is nullable when required: false
            if (field == null || !typeof(IEnumerable<SuspensionBridge>).IsAssignableFrom(field.FieldInfo.FieldType))
                continue;

            // update textures
            IEnumerable<SuspensionBridge>? bridges = field.GetValue();
            if (bridges != null)
            {
                foreach (SuspensionBridge bridge in bridges)
                    this.Reflection.GetField<Texture2D>(bridge, "_texture").SetValue(texture.Value);
            }
        }

        return texture.IsValueCreated;
    }

    /// <summary>Update tree textures.</summary>
    /// <param name="type">The type to update.</param>
    /// <returns>Returns whether any references were updated.</returns>
    private bool UpdateTreeTextures(int type)
    {
        Tree[] trees = this
            .GetTerrainFeatures()
            .OfType<Tree>()
            .Where(tree => tree.treeType.Value == type)
            .ToArray();

        bool changed = false;
        foreach (Tree tree in trees)
        {
            if (tree.texture.IsValueCreated)
            {
                this.Reflection.GetMethod(tree, "resetTexture").Invoke();
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>Mark an animated sprite's texture dirty, so it's reloaded next time it's rendered.</summary>
    /// <param name="sprite">The animated sprite to change.</param>
    /// <returns>Returns whether the sprite was changed.</returns>
    private bool MarkSpriteDirty(AnimatedSprite sprite)
    {
        if (sprite.loadedTexture is null && sprite.spriteTexture is null)
            return false;

        sprite.loadedTexture = null;
        sprite.spriteTexture = null;
        return true;
    }

    /****
    ** Update data methods
    ****/
    /// <summary>Update the data for matching farm animals.</summary>
    /// <returns>Returns whether any farm animals were updated.</returns>
    /// <remarks>Derived from the <see cref="FarmAnimal"/> constructor.</remarks>
    private bool UpdateFarmAnimalData()
    {
        bool changed = false;
        foreach (FarmAnimal animal in this.GetFarmAnimals())
        {
            animal.reloadData();
            changed = true;
        }

        return changed;
    }

    /// <summary>Update hair style metadata.</summary>
    /// <returns>Returns whether any data was updated.</returns>
    /// <remarks>Derived from the <see cref="Farmer.GetHairStyleMetadataFile"/> and <see cref="Farmer.GetHairStyleMetadata"/>.</remarks>
    private bool UpdateHairData()
    {
        if (Farmer.hairStyleMetadataFile == null)
            return false;

        Farmer.hairStyleMetadataFile = null;
        Farmer.allHairStyleIndices = null;
        Farmer.hairStyleMetadata.Clear();

        return true;
    }

    /// <summary>Update the dialogue data for matching NPCs.</summary>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any NPCs were updated.</returns>
    private bool UpdateNpcDialogue(IAssetName assetName)
    {
        // get NPCs
        string name = Path.GetFileName(assetName.BaseName);
        NPC[] villagers = this.GetCharacters().Where(npc => npc.Name == name && npc.isVillager()).ToArray();
        if (!villagers.Any())
            return false;

        // update dialogue
        // Note that marriage dialogue isn't reloaded after reset, but it doesn't need to be
        // propagated anyway since marriage dialogue keys can't be added/removed and the field
        // doesn't store the text itself.
        foreach (NPC villager in villagers)
        {
            bool shouldSayMarriageDialogue = villager.shouldSayMarriageDialogue.Value;
            MarriageDialogueReference[] marriageDialogue = villager.currentMarriageDialogue.ToArray();

            villager.resetSeasonalDialogue(); // doesn't only affect seasonal dialogue
            villager.resetCurrentDialogue();

            villager.shouldSayMarriageDialogue.Set(shouldSayMarriageDialogue);
            villager.currentMarriageDialogue.Set(marriageDialogue);
        }

        return true;
    }

    /// <summary>Update the disposition data for matching NPCs.</summary>
    /// <param name="content">The content manager through which to reload the asset.</param>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any NPCs were updated.</returns>
    private bool UpdateNpcDispositions(LocalizedContentManager content, IAssetName assetName)
    {
        IDictionary<string, string> data = content.Load<Dictionary<string, string>>(assetName.BaseName);
        bool changed = false;
        foreach (NPC npc in this.GetCharacters())
        {
            if (npc.isVillager() && data.ContainsKey(npc.Name))
            {
                npc.reloadData();
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>Update the schedules for matching NPCs.</summary>
    /// <param name="assetName">The asset name to update.</param>
    /// <returns>Returns whether any NPCs were updated.</returns>
    private bool UpdateNpcSchedules(IAssetName assetName)
    {
        // get NPCs
        string name = Path.GetFileName(assetName.BaseName);
        NPC[] villagers = this.GetCharacters().Where(npc => npc.Name == name && npc.isVillager()).ToArray();
        if (!villagers.Any())
            return false;

        // update schedule
        foreach (NPC villager in villagers)
        {
            // reload schedule
            this.Reflection.GetField<bool>(villager, "_hasLoadedMasterScheduleData").SetValue(false);
            this.Reflection.GetField<Dictionary<string, string>?>(villager, "_masterScheduleData").SetValue(null);
            villager.Schedule = villager.getSchedule(Game1.dayOfMonth);

            // switch to new schedule if needed
            if (villager.Schedule != null)
            {
                int lastScheduleTime = villager.Schedule.Keys.Where(p => p <= Game1.timeOfDay).OrderByDescending(p => p).FirstOrDefault();
                if (lastScheduleTime != 0)
                {
                    villager.queuedSchedulePaths.Clear();
                    villager.lastAttemptedSchedule = 0;
                    villager.checkSchedule(lastScheduleTime);
                }
            }
        }
        return true;
    }

    /// <summary>Update cached translations from the <c>Strings\StringsFromCSFiles</c> asset.</summary>
    /// <param name="content">The content manager through which to reload the asset.</param>
    /// <returns>Returns whether any data was updated.</returns>
    /// <remarks>Derived from the <see cref="Game1.TranslateFields"/>.</remarks>
    private bool UpdateStringsFromCsFiles(LocalizedContentManager content)
    {
        Game1.samBandName = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.2156");
        Game1.elliottBookName = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.2157");

        string[] dayNames = this.Reflection.GetField<string[]>(typeof(Game1), "_shortDayDisplayName").GetValue();
        dayNames[0] = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.3042");
        dayNames[1] = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.3043");
        dayNames[2] = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.3044");
        dayNames[3] = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.3045");
        dayNames[4] = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.3046");
        dayNames[5] = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.3047");
        dayNames[6] = content.LoadString("Strings/StringsFromCSFiles:Game1.cs.3048");

        return true;
    }

    /****
    ** Update map methods
    ****/
    /// <summary>Update the map for a location.</summary>
    /// <param name="locationInfo">The location whose map to update.</param>
    private void UpdateMap(LocationInfo locationInfo)
    {
        GameLocation location = locationInfo.Location;
        Vector2? playerPos = Game1.player?.Position;

        // reload map
        location.interiorDoors.Clear(); // prevent errors when doors try to update tiles which no longer exist
        location.reloadMap();

        // reload interior doors
        location.interiorDoors.Clear();
        location.interiorDoors.ResetSharedState(); // load doors from map properties
        location.interiorDoors.ResetLocalState(); // reapply door tiles

        // reapply map changes (after reloading doors so they apply theirs too)
        location.MakeMapModifications(force: true);

        // update for changes
        location.updateWarps();
        location.updateDoors();
        locationInfo.ParentBuilding?.updateInteriorWarps();

        // reset player position
        // The game may move the player as part of the map changes, even if they're not in that
        // location. That's not needed in this case, and it can have weird effects like players
        // warping onto the wrong tile (or even off-screen) if a patch changes the farmhouse
        // map on location change.
        if (playerPos.HasValue)
            Game1.player!.Position = playerPos.Value;
    }

    /****
    ** Helpers
    ****/
    /// <summary>Get all NPCs in the game (excluding farm animals).</summary>
    private IEnumerable<NPC> GetCharacters()
    {
        return this.WorldCache.GetOrSet(
            nameof(this.GetCharacters),
            () =>
            {
                List<NPC> characters = new();

                foreach (NPC character in this.GetLocations().SelectMany(p => p.characters))
                    characters.Add(character);

                if (Game1.CurrentEvent?.actors != null)
                {
                    foreach (NPC character in Game1.CurrentEvent.actors)
                        characters.Add(character);
                }

                return characters;
            }
        );
    }

    /// <summary>Get all farm animals in the game.</summary>
    private IEnumerable<FarmAnimal> GetFarmAnimals()
    {
        return this.WorldCache.GetOrSet(
            nameof(this.GetFarmAnimals),
            () =>
            {
                List<FarmAnimal> animals = new();

                foreach (GameLocation location in this.GetLocations())
                {
                    if (location is Farm farm)
                    {
                        foreach (FarmAnimal animal in farm.animals.Values)
                            animals.Add(animal);
                    }
                    else if (location is AnimalHouse animalHouse)
                    {
                        foreach (FarmAnimal animal in animalHouse.animals.Values)
                            animals.Add(animal);
                    }
                }

                return animals;
            }
        );
    }

    /// <summary>Get all locations in the game.</summary>
    /// <param name="buildingInteriors">Whether to also get the interior locations for constructable buildings.</param>
    private IEnumerable<GameLocation> GetLocations(bool buildingInteriors = true)
    {
        return this.WorldCache.GetOrSet(
            $"{nameof(this.GetLocations)}_{buildingInteriors}",
            () => this.GetLocationsWithInfo(buildingInteriors).Select(info => info.Location).ToArray()
        );
    }

    /// <summary>Get all locations in the game.</summary>
    /// <param name="buildingInteriors">Whether to also get the interior locations for constructable buildings.</param>
    private IEnumerable<LocationInfo> GetLocationsWithInfo(bool buildingInteriors = true)
    {
        return this.WorldCache.GetOrSet(
            $"{nameof(this.GetLocationsWithInfo)}_{buildingInteriors}",
            () =>
            {
                List<LocationInfo> locations = new();

                // get root locations
                foreach (GameLocation location in Game1.locations)
                    locations.Add(new LocationInfo(location, null));
                if (SaveGame.loaded?.locations != null)
                {
                    foreach (GameLocation location in SaveGame.loaded.locations)
                        locations.Add(new LocationInfo(location, null));
                }

                // get child locations
                if (buildingInteriors)
                {
                    foreach (BuildableGameLocation location in locations.Select(p => p.Location).OfType<BuildableGameLocation>().ToArray())
                    {
                        foreach (Building building in location.buildings)
                        {
                            GameLocation indoors = building.indoors.Value;
                            if (indoors is not null)
                                locations.Add(new LocationInfo(indoors, building));
                        }
                    }
                }

                return locations;
            });
    }

    /// <summary>Get all terrain features in the game.</summary>
    private IEnumerable<TerrainFeature> GetTerrainFeatures()
    {
        return this.WorldCache.GetOrSet(
            $"{nameof(this.GetTerrainFeatures)}",
            () => this.GetLocations().SelectMany(p => p.terrainFeatures.Values).ToArray()
        );
    }

    /// <summary>Get whether two asset names are equivalent if you ignore the locale code.</summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    private bool IsSameBaseName(IAssetName? left, string? right)
    {
        if (left is null || right is null)
            return false;

        IAssetName? parsedB = this.ParseAssetNameOrNull(right);
        return this.IsSameBaseName(left, parsedB);
    }

    /// <summary>Get whether two asset names are equivalent if you ignore the locale code.</summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    private bool IsSameBaseName(IAssetName? left, IAssetName? right)
    {
        if (left is null || right is null)
            return false;

        return left.IsEquivalentTo(right.BaseName, useBaseName: true);
    }

    /// <summary>Normalize an asset key to match the cache key and assert that it's valid, but don't raise an error for null or empty values.</summary>
    /// <param name="path">The asset key to normalize.</param>
    private IAssetName? ParseAssetNameOrNull(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return this.ParseAssetName(path);
    }

    /// <summary>Get the segments in a path (e.g. 'a/b' is 'a' and 'b').</summary>
    /// <param name="path">The path to check.</param>
    private string[] GetSegments(string? path)
    {
        return path != null
            ? PathUtilities.GetSegments(path)
            : Array.Empty<string>();
    }

    /// <summary>Load a texture from the main content manager.</summary>
    /// <param name="key">The asset key to load.</param>
    private Texture2D LoadTexture(string key)
    {
        return this.MainContentManager.Load<Texture2D>(key);
    }

    /// <summary>Remove a case-insensitive key from the paint mask cache.</summary>
    /// <param name="assetName">The paint mask asset name.</param>
    private bool RemoveFromPaintMaskCache(IAssetName assetName)
    {
        // make cache case-insensitive
        // This is needed for cache invalidation since mods may specify keys with a different capitalization
        if (!object.ReferenceEquals(BuildingPainter.paintMaskLookup.Comparer, StringComparer.OrdinalIgnoreCase))
            BuildingPainter.paintMaskLookup = new Dictionary<string, List<List<int>>>(BuildingPainter.paintMaskLookup, StringComparer.OrdinalIgnoreCase);

        // remove key from cache
        return BuildingPainter.paintMaskLookup.Remove(assetName.BaseName);
    }

    /// <summary>Metadata about a location used in asset propagation.</summary>
    /// <param name="Location">The location instance.</param>
    /// <param name="ParentBuilding">The building which contains the location, if any.</param>
    private record LocationInfo(GameLocation Location, Building? ParentBuilding);
}
