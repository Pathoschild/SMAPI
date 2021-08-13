using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Reflection;
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

namespace StardewModdingAPI.Metadata
{
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

        /// <summary>Whether to enable more aggressive memory optimizations.</summary>
        private readonly bool AggressiveMemoryOptimizations;

        /// <summary>Normalizes an asset key to match the cache key and assert that it's valid.</summary>
        private readonly Func<string, string> AssertAndNormalizeAssetName;

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


        /*********
        ** Public methods
        *********/
        /// <summary>Initialize the core asset data.</summary>
        /// <param name="mainContent">The main content manager through which to reload assets.</param>
        /// <param name="disposableContent">An internal content manager used only for asset propagation.</param>
        /// <param name="monitor">Writes messages to the console.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="aggressiveMemoryOptimizations">Whether to enable more aggressive memory optimizations.</param>
        public CoreAssetPropagator(LocalizedContentManager mainContent, GameContentManagerForAssetPropagation disposableContent, IMonitor monitor, Reflector reflection, bool aggressiveMemoryOptimizations)
        {
            this.MainContentManager = mainContent;
            this.DisposableContentManager = disposableContent;
            this.Monitor = monitor;
            this.Reflection = reflection;
            this.AggressiveMemoryOptimizations = aggressiveMemoryOptimizations;

            this.AssertAndNormalizeAssetName = disposableContent.AssertAndNormalizeAssetName;
        }

        /// <summary>Reload one of the game's core assets (if applicable).</summary>
        /// <param name="assets">The asset keys and types to reload.</param>
        /// <param name="ignoreWorld">Whether the in-game world is fully unloaded (e.g. on the title screen), so there's no need to propagate changes into the world.</param>
        /// <param name="propagatedAssets">A lookup of asset names to whether they've been propagated.</param>
        /// <param name="updatedNpcWarps">Whether the NPC pathfinding cache was reloaded.</param>
        public void Propagate(IDictionary<string, Type> assets, bool ignoreWorld, out IDictionary<string, bool> propagatedAssets, out bool updatedNpcWarps)
        {
            // group into optimized lists
            var buckets = assets.GroupBy(p =>
            {
                if (this.IsInFolder(p.Key, "Characters") || this.IsInFolder(p.Key, "Characters\\Monsters"))
                    return AssetBucket.Sprite;

                if (this.IsInFolder(p.Key, "Portraits"))
                    return AssetBucket.Portrait;

                return AssetBucket.Other;
            });

            // reload assets
            propagatedAssets = assets.ToDictionary(p => p.Key, _ => false, StringComparer.OrdinalIgnoreCase);
            updatedNpcWarps = false;
            foreach (var bucket in buckets)
            {
                switch (bucket.Key)
                {
                    case AssetBucket.Sprite:
                        if (!ignoreWorld)
                            this.ReloadNpcSprites(bucket.Select(p => p.Key), propagatedAssets);
                        break;

                    case AssetBucket.Portrait:
                        if (!ignoreWorld)
                            this.ReloadNpcPortraits(bucket.Select(p => p.Key), propagatedAssets);
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
        /// <param name="key">The asset key to reload.</param>
        /// <param name="type">The asset type to reload.</param>
        /// <param name="ignoreWorld">Whether the in-game world is fully unloaded (e.g. on the title screen), so there's no need to propagate changes into the world.</param>
        /// <param name="changedWarps">Whether any map warps were changed as part of this propagation.</param>
        /// <returns>Returns whether an asset was loaded. The return value may be true or false, or a non-null value for true.</returns>
        [SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "These deliberately match the asset names.")]
        private bool PropagateOther(string key, Type type, bool ignoreWorld, out bool changedWarps)
        {
            var content = this.MainContentManager;
            key = this.AssertAndNormalizeAssetName(key);
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
                    if (this.NormalizeAssetNameIgnoringEmpty(tilesheet.ImageSource) == key)
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
                    foreach (GameLocation location in this.GetLocations())
                    {
                        if (!string.IsNullOrWhiteSpace(location.mapPath.Value) && this.NormalizeAssetNameIgnoringEmpty(location.mapPath.Value) == key)
                        {
                            static ISet<string> GetWarpSet(GameLocation location)
                            {
                                return new HashSet<string>(
                                    location.warps.Select(p => $"{p.X} {p.Y} {p.TargetName} {p.TargetX} {p.TargetY}")
                                );
                            }

                            var oldWarps = GetWarpSet(location);
                            this.ReloadMap(location);
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
            Reflector reflection = this.Reflection;
            switch (key.ToLower().Replace("/", "\\")) // normalized key so we can compare statically
            {
                /****
                ** Animals
                ****/
                case "animals\\horse":
                    return !ignoreWorld && this.ReloadPetOrHorseSprites<Horse>(content, key);

                /****
                ** Buildings
                ****/
                case "buildings\\houses": // Farm
                    Farm.houseTextures = this.LoadAndDisposeIfNeeded(Farm.houseTextures, key);
                    return true;

                case "buildings\\houses_paintmask": // Farm
                    {
                        bool removedFromCache = this.RemoveFromPaintMaskCache(key);

                        Farm farm = Game1.getFarm();
                        farm?.ApplyHousePaint();

                        return removedFromCache || farm != null;
                    }

                /****
                ** Content\Characters\Farmer
                ****/
                case "characters\\farmer\\accessories": // Game1.LoadContent
                    FarmerRenderer.accessoriesTexture = this.LoadAndDisposeIfNeeded(FarmerRenderer.accessoriesTexture, key);
                    return true;

                case "characters\\farmer\\farmer_base": // Farmer
                case "characters\\farmer\\farmer_base_bald":
                case "characters\\farmer\\farmer_girl_base":
                case "characters\\farmer\\farmer_girl_base_bald":
                    return !ignoreWorld && this.ReloadPlayerSprites(key);

                case "characters\\farmer\\hairstyles": // Game1.LoadContent
                    FarmerRenderer.hairStylesTexture = this.LoadAndDisposeIfNeeded(FarmerRenderer.hairStylesTexture, key);
                    return true;

                case "characters\\farmer\\hats": // Game1.LoadContent
                    FarmerRenderer.hatsTexture = this.LoadAndDisposeIfNeeded(FarmerRenderer.hatsTexture, key);
                    return true;

                case "characters\\farmer\\pants": // Game1.LoadContent
                    FarmerRenderer.pantsTexture = this.LoadAndDisposeIfNeeded(FarmerRenderer.pantsTexture, key);
                    return true;

                case "characters\\farmer\\shirts": // Game1.LoadContent
                    FarmerRenderer.shirtsTexture = this.LoadAndDisposeIfNeeded(FarmerRenderer.shirtsTexture, key);
                    return true;

                /****
                ** Content\Data
                ****/
                case "data\\achievements": // Game1.LoadContent
                    Game1.achievements = content.Load<Dictionary<int, string>>(key);
                    return true;

                case "data\\bigcraftablesinformation": // Game1.LoadContent
                    Game1.bigCraftablesInformation = content.Load<Dictionary<int, string>>(key);
                    return true;

                case "data\\clothinginformation": // Game1.LoadContent
                    Game1.clothingInformation = content.Load<Dictionary<int, string>>(key);
                    return true;

                case "data\\concessions": // MovieTheater.GetConcessions
                    MovieTheater.ClearCachedLocalizedData();
                    return true;

                case "data\\concessiontastes": // MovieTheater.GetConcessionTasteForCharacter
                    this.Reflection
                        .GetField<List<ConcessionTaste>>(typeof(MovieTheater), "_concessionTastes")
                        .SetValue(content.Load<List<ConcessionTaste>>(key));
                    return true;

                case "data\\cookingrecipes": // CraftingRecipe.InitShared
                    CraftingRecipe.cookingRecipes = content.Load<Dictionary<string, string>>(key);
                    return true;

                case "data\\craftingrecipes": // CraftingRecipe.InitShared
                    CraftingRecipe.craftingRecipes = content.Load<Dictionary<string, string>>(key);
                    return true;

                case "data\\farmanimals": // FarmAnimal constructor
                    return !ignoreWorld && this.ReloadFarmAnimalData();

                case "data\\hairdata": // Farmer.GetHairStyleMetadataFile
                    return this.ReloadHairData();

                case "data\\movies": // MovieTheater.GetMovieData
                case "data\\moviesreactions": // MovieTheater.GetMovieReactions
                    MovieTheater.ClearCachedLocalizedData();
                    return true;

                case "data\\npcdispositions": // NPC constructor
                    return !ignoreWorld && this.ReloadNpcDispositions(content, key);

                case "data\\npcgifttastes": // Game1.LoadContent
                    Game1.NPCGiftTastes = content.Load<Dictionary<string, string>>(key);
                    return true;

                case "data\\objectcontexttags": // Game1.LoadContent
                    Game1.objectContextTags = content.Load<Dictionary<string, string>>(key);
                    return true;

                case "data\\objectinformation": // Game1.LoadContent
                    Game1.objectInformation = content.Load<Dictionary<int, string>>(key);
                    return true;

                /****
                ** Content\Fonts
                ****/
                case "fonts\\spritefont1": // Game1.LoadContent
                    Game1.dialogueFont = content.Load<SpriteFont>(key);
                    return true;

                case "fonts\\smallfont": // Game1.LoadContent
                    Game1.smallFont = content.Load<SpriteFont>(key);
                    return true;

                case "fonts\\tinyfont": // Game1.LoadContent
                    Game1.tinyFont = content.Load<SpriteFont>(key);
                    return true;

                case "fonts\\tinyfontborder": // Game1.LoadContent
                    Game1.tinyFontBorder = content.Load<SpriteFont>(key);
                    return true;

                /****
                ** Content\LooseSprites\Lighting
                ****/
                case "loosesprites\\lighting\\greenlight": // Game1.LoadContent
                    Game1.cauldronLight = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\lighting\\indoorwindowlight": // Game1.LoadContent
                    Game1.indoorWindowLight = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\lighting\\lantern": // Game1.LoadContent
                    Game1.lantern = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\lighting\\sconcelight": // Game1.LoadContent
                    Game1.sconceLight = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\lighting\\windowlight": // Game1.LoadContent
                    Game1.windowLight = content.Load<Texture2D>(key);
                    return true;

                /****
                ** Content\LooseSprites
                ****/
                case "loosesprites\\birds": // Game1.LoadContent
                    Game1.birdsSpriteSheet = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\concessions": // Game1.LoadContent
                    Game1.concessionsSpriteSheet = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\controllermaps": // Game1.LoadContent
                    Game1.controllerMaps = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\cursors": // Game1.LoadContent
                    Game1.mouseCursors = content.Load<Texture2D>(key);
                    foreach (DayTimeMoneyBox menu in Game1.onScreenMenus.OfType<DayTimeMoneyBox>())
                    {
                        foreach (ClickableTextureComponent button in new[] { menu.questButton, menu.zoomInButton, menu.zoomOutButton })
                            button.texture = Game1.mouseCursors;
                    }

                    if (!ignoreWorld)
                        this.ReloadDoorSprites(content, key);
                    return true;

                case "loosesprites\\cursors2": // Game1.LoadContent
                    Game1.mouseCursors2 = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\daybg": // Game1.LoadContent
                    Game1.daybg = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\font_bold": // Game1.LoadContent
                    SpriteText.spriteTexture = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\font_colored": // Game1.LoadContent
                    SpriteText.coloredTexture = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\giftbox": // Game1.LoadContent
                    Game1.giftboxTexture = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\nightbg": // Game1.LoadContent
                    Game1.nightbg = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\shadow": // Game1.LoadContent
                    Game1.shadowTexture = content.Load<Texture2D>(key);
                    return true;

                case "loosesprites\\suspensionbridge": // SuspensionBridge constructor
                    return !ignoreWorld && this.ReloadSuspensionBridges(content, key);

                /****
                ** Content\Maps
                ****/
                case "maps\\menutiles": // Game1.LoadContent
                    Game1.menuTexture = content.Load<Texture2D>(key);
                    return true;

                case "maps\\menutilesuncolored": // Game1.LoadContent
                    Game1.uncoloredMenuTexture = content.Load<Texture2D>(key);
                    return true;

                case "maps\\springobjects": // Game1.LoadContent
                    Game1.objectSpriteSheet = content.Load<Texture2D>(key);
                    return true;

                case "maps\\walls_and_floors": // Wallpaper
                    Wallpaper.wallpaperTexture = content.Load<Texture2D>(key);
                    return true;

                /****
                ** Content\Minigames
                ****/
                case "minigames\\clouds": // TitleMenu
                    {
                        if (Game1.activeClickableMenu is TitleMenu titleMenu)
                        {
                            titleMenu.cloudsTexture = content.Load<Texture2D>(key);
                            return true;
                        }
                    }
                    return false;

                case "minigames\\titlebuttons": // TitleMenu
                    return this.ReloadTitleButtons(content, key);

                /****
                ** Content\Strings
                ****/
                case "strings\\stringsfromcsfiles":
                    return this.ReloadStringsFromCsFiles(content);

                /****
                ** Content\TileSheets
                ****/
                case "tilesheets\\animations": // Game1.LoadContent
                    Game1.animations = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\buffsicons": // Game1.LoadContent
                    Game1.buffsIcons = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\bushes": // new Bush()
                    Bush.texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));
                    return true;

                case "tilesheets\\chairtiles": // Game1.LoadContent
                    return this.ReloadChairTiles(content, key, ignoreWorld);

                case "tilesheets\\craftables": // Game1.LoadContent
                    Game1.bigCraftableSpriteSheet = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\critters": // Critter constructor
                    return !ignoreWorld && this.ReloadCritterTextures(content, key) > 0;

                case "tilesheets\\crops": // Game1.LoadContent
                    Game1.cropSpriteSheet = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\debris": // Game1.LoadContent
                    Game1.debrisSpriteSheet = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\emotes": // Game1.LoadContent
                    Game1.emoteSpriteSheet = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\fruittrees": // FruitTree
                    FruitTree.texture = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\furniture": // Game1.LoadContent
                    Furniture.furnitureTexture = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\furniturefront": // Game1.LoadContent
                    Furniture.furnitureFrontTexture = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\projectiles": // Game1.LoadContent
                    Projectile.projectileSheet = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\rain": // Game1.LoadContent
                    Game1.rainTexture = content.Load<Texture2D>(key);
                    return true;

                case "tilesheets\\tools": // Game1.ResetToolSpriteSheet
                    Game1.ResetToolSpriteSheet();
                    return true;

                case "tilesheets\\weapons": // Game1.LoadContent
                    Tool.weaponsTexture = content.Load<Texture2D>(key);
                    return true;

                /****
                ** Content\TerrainFeatures
                ****/
                case "terrainfeatures\\flooring": // from Flooring
                    Flooring.floorsTexture = content.Load<Texture2D>(key);
                    return true;

                case "terrainfeatures\\flooring_winter": // from Flooring
                    Flooring.floorsTextureWinter = content.Load<Texture2D>(key);
                    return true;

                case "terrainfeatures\\grass": // from Grass
                    return !ignoreWorld && this.ReloadGrassTextures(content, key);

                case "terrainfeatures\\hoedirt": // from HoeDirt
                    HoeDirt.lightTexture = content.Load<Texture2D>(key);
                    return true;

                case "terrainfeatures\\hoedirtdark": // from HoeDirt
                    HoeDirt.darkTexture = content.Load<Texture2D>(key);
                    return true;

                case "terrainfeatures\\hoedirtsnow": // from HoeDirt
                    HoeDirt.snowTexture = content.Load<Texture2D>(key);
                    return true;

                case "terrainfeatures\\mushroom_tree": // from Tree
                    return !ignoreWorld && this.ReloadTreeTextures(content, key, Tree.mushroomTree);

                case "terrainfeatures\\tree_palm": // from Tree
                    return !ignoreWorld && this.ReloadTreeTextures(content, key, Tree.palmTree);

                case "terrainfeatures\\tree1_fall": // from Tree
                case "terrainfeatures\\tree1_spring": // from Tree
                case "terrainfeatures\\tree1_summer": // from Tree
                case "terrainfeatures\\tree1_winter": // from Tree
                    return !ignoreWorld && this.ReloadTreeTextures(content, key, Tree.bushyTree);

                case "terrainfeatures\\tree2_fall": // from Tree
                case "terrainfeatures\\tree2_spring": // from Tree
                case "terrainfeatures\\tree2_summer": // from Tree
                case "terrainfeatures\\tree2_winter": // from Tree
                    return !ignoreWorld && this.ReloadTreeTextures(content, key, Tree.leafyTree);

                case "terrainfeatures\\tree3_fall": // from Tree
                case "terrainfeatures\\tree3_spring": // from Tree
                case "terrainfeatures\\tree3_winter": // from Tree
                    return !ignoreWorld && this.ReloadTreeTextures(content, key, Tree.pineTree);
            }

            /****
            ** Dynamic assets
            ****/
            if (!ignoreWorld)
            {
                // dynamic textures
                if (this.KeyStartsWith(key, "animals\\cat"))
                    return this.ReloadPetOrHorseSprites<Cat>(content, key);
                if (this.KeyStartsWith(key, "animals\\dog"))
                    return this.ReloadPetOrHorseSprites<Dog>(content, key);
                if (this.IsInFolder(key, "Animals"))
                    return this.ReloadFarmAnimalSprites(content, key);

                if (this.IsInFolder(key, "Buildings"))
                    return this.ReloadBuildings(key);

                if (this.KeyStartsWith(key, "LooseSprites\\Fence"))
                    return this.ReloadFenceTextures(key);

                // dynamic data
                if (this.IsInFolder(key, "Characters\\Dialogue"))
                    return this.ReloadNpcDialogue(key);

                if (this.IsInFolder(key, "Characters\\schedules"))
                    return this.ReloadNpcSchedules(key);
            }

            return false;
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Reload texture methods
        ****/
        /// <summary>Reload buttons on the title screen.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        /// <remarks>Derived from the <see cref="TitleMenu"/> constructor and <see cref="TitleMenu.setUpIcons"/>.</remarks>
        private bool ReloadTitleButtons(LocalizedContentManager content, string key)
        {
            if (Game1.activeClickableMenu is TitleMenu titleMenu)
            {
                Texture2D texture = content.Load<Texture2D>(key);

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

        /// <summary>Reload the sprites for matching pets or horses.</summary>
        /// <typeparam name="TAnimal">The animal type.</typeparam>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadPetOrHorseSprites<TAnimal>(LocalizedContentManager content, string key)
            where TAnimal : NPC
        {
            // find matches
            TAnimal[] animals = this.GetCharacters()
                .OfType<TAnimal>()
                .Where(p => key == this.NormalizeAssetNameIgnoringEmpty(p.Sprite?.Texture?.Name))
                .ToArray();
            if (!animals.Any())
                return false;

            // update sprites
            Texture2D texture = content.Load<Texture2D>(key);
            foreach (TAnimal animal in animals)
                animal.Sprite.spriteTexture = texture;
            return true;
        }

        /// <summary>Reload the sprites for matching farm animals.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        /// <remarks>Derived from <see cref="FarmAnimal.reload"/>.</remarks>
        private bool ReloadFarmAnimalSprites(LocalizedContentManager content, string key)
        {
            // find matches
            FarmAnimal[] animals = this.GetFarmAnimals().ToArray();
            if (!animals.Any())
                return false;

            // update sprites
            Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));
            foreach (FarmAnimal animal in animals)
            {
                // get expected key
                string expectedKey = animal.age.Value < animal.ageWhenMature.Value
                    ? $"Baby{(animal.type.Value == "Duck" ? "White Chicken" : animal.type.Value)}"
                    : animal.type.Value;
                if (animal.showDifferentTextureWhenReadyForHarvest.Value && animal.currentProduce.Value <= 0)
                    expectedKey = $"Sheared{expectedKey}";
                expectedKey = $"Animals\\{expectedKey}";

                // reload asset
                if (expectedKey == key)
                    animal.Sprite.spriteTexture = texture.Value;
            }
            return texture.IsValueCreated;
        }

        /// <summary>Reload building textures.</summary>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadBuildings(string key)
        {
            // get paint mask info
            const string paintMaskSuffix = "_PaintMask";
            bool isPaintMask = key.EndsWith(paintMaskSuffix, StringComparison.OrdinalIgnoreCase);

            // get building type
            string type = Path.GetFileName(key);
            if (isPaintMask)
                type = type.Substring(0, type.Length - paintMaskSuffix.Length);

            // get buildings
            Building[] buildings = this.GetLocations(buildingInteriors: false)
                .OfType<BuildableGameLocation>()
                .SelectMany(p => p.buildings)
                .Where(p => p.buildingType.Value == type)
                .ToArray();

            // remove from paint mask cache
            bool removedFromCache = this.RemoveFromPaintMaskCache(key);

            // reload textures
            if (buildings.Any())
            {
                foreach (Building building in buildings)
                    building.resetTexture();

                return true;
            }

            return removedFromCache;
        }

        /// <summary>Reload map seat textures.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <param name="ignoreWorld">Whether the in-game world is fully unloaded (e.g. on the title screen), so there's no need to propagate changes into the world.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadChairTiles(LocalizedContentManager content, string key, bool ignoreWorld)
        {
            MapSeat.mapChairTexture = content.Load<Texture2D>(key);

            if (!ignoreWorld)
            {
                foreach (var location in this.GetLocations())
                {
                    foreach (MapSeat seat in location.mapSeats.Where(p => p != null))
                    {
                        string curKey = this.NormalizeAssetNameIgnoringEmpty(seat._loadedTextureFile);

                        if (curKey == null || key.Equals(curKey, StringComparison.OrdinalIgnoreCase))
                            seat.overlayTexture = MapSeat.mapChairTexture;
                    }
                }
            }

            return true;
        }

        /// <summary>Reload critter textures.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns the number of reloaded assets.</returns>
        private int ReloadCritterTextures(LocalizedContentManager content, string key)
        {
            // get critters
            Critter[] critters =
                (
                    from location in this.GetLocations()
                    where location.critters != null
                    from Critter critter in location.critters
                    where this.NormalizeAssetNameIgnoringEmpty(critter.sprite?.Texture?.Name) == key
                    select critter
                )
                .ToArray();
            if (!critters.Any())
                return 0;

            // update sprites
            Texture2D texture = content.Load<Texture2D>(key);
            foreach (var entry in critters)
                entry.sprite.spriteTexture = texture;

            return critters.Length;
        }

        /// <summary>Reload the sprites for interior doors.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any doors were affected.</returns>
        private bool ReloadDoorSprites(LocalizedContentManager content, string key)
        {
            Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));

            foreach (GameLocation location in this.GetLocations())
            {
                IEnumerable<InteriorDoor> doors = location.interiorDoors?.Doors;
                if (doors == null)
                    continue;

                foreach (InteriorDoor door in doors)
                {
                    if (door?.Sprite == null)
                        continue;

                    string textureName = this.NormalizeAssetNameIgnoringEmpty(this.Reflection.GetField<string>(door.Sprite, "textureName").GetValue());
                    if (textureName != key)
                        continue;

                    door.Sprite.texture = texture.Value;
                }
            }

            return texture.IsValueCreated;
        }

        /// <summary>Reload the data for matching farm animals.</summary>
        /// <returns>Returns whether any farm animals were affected.</returns>
        /// <remarks>Derived from the <see cref="FarmAnimal"/> constructor.</remarks>
        private bool ReloadFarmAnimalData()
        {
            bool changed = false;
            foreach (FarmAnimal animal in this.GetFarmAnimals())
            {
                animal.reloadData();
                changed = true;
            }

            return changed;
        }

        /// <summary>Reload the sprites for a fence type.</summary>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadFenceTextures(string key)
        {
            // get fence type
            if (!int.TryParse(this.GetSegments(key)[1].Substring("Fence".Length), out int fenceType))
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
            foreach (Fence fence in fences)
                fence.fenceTexture = new Lazy<Texture2D>(fence.loadFenceTexture);
            return true;
        }

        /// <summary>Reload tree textures.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadGrassTextures(LocalizedContentManager content, string key)
        {
            Grass[] grasses =
                (
                    from location in this.GetLocations()
                    from grass in location.terrainFeatures.Values.OfType<Grass>()
                    where this.NormalizeAssetNameIgnoringEmpty(grass.textureName()) == key
                    select grass
                )
                .ToArray();

            if (grasses.Any())
            {
                Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));
                foreach (Grass grass in grasses)
                    grass.texture = texture;
                return true;
            }

            return false;
        }

        /// <summary>Reload hair style metadata.</summary>
        /// <returns>Returns whether any assets were reloaded.</returns>
        /// <remarks>Derived from the <see cref="Farmer.GetHairStyleMetadataFile"/> and <see cref="Farmer.GetHairStyleMetadata"/>.</remarks>
        private bool ReloadHairData()
        {
            if (Farmer.hairStyleMetadataFile == null)
                return false;

            Farmer.hairStyleMetadataFile = null;
            Farmer.allHairStyleIndices = null;
            Farmer.hairStyleMetadata.Clear();

            return true;
        }

        /// <summary>Reload the map for a location.</summary>
        /// <param name="location">The location whose map to reload.</param>
        private void ReloadMap(GameLocation location)
        {
            Vector2? playerPos = Game1.player?.Position;

            if (this.AggressiveMemoryOptimizations)
                location.map.DisposeTileSheets(Game1.mapDisplayDevice);

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

            // reset player position
            // The game may move the player as part of the map changes, even if they're not in that
            // location. That's not needed in this case, and it can have weird effects like players
            // warping onto the wrong tile (or even off-screen) if a patch changes the farmhouse
            // map on location change.
            if (playerPos.HasValue)
                Game1.player.Position = playerPos.Value;
        }

        /// <summary>Reload the disposition data for matching NPCs.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any NPCs were affected.</returns>
        private bool ReloadNpcDispositions(LocalizedContentManager content, string key)
        {
            IDictionary<string, string> data = content.Load<Dictionary<string, string>>(key);
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

        /// <summary>Reload the sprites for matching NPCs.</summary>
        /// <param name="keys">The asset keys to reload.</param>
        /// <param name="propagated">The asset keys which have been propagated.</param>
        private void ReloadNpcSprites(IEnumerable<string> keys, IDictionary<string, bool> propagated)
        {
            // get NPCs
            HashSet<string> lookup = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
            var characters =
                (
                    from npc in this.GetCharacters()
                    let key = this.NormalizeAssetNameIgnoringEmpty(npc.Sprite?.Texture?.Name)
                    where key != null && lookup.Contains(key)
                    select new { Npc = npc, Key = key }
                )
                .ToArray();
            if (!characters.Any())
                return;

            // update sprite
            foreach (var target in characters)
            {
                target.Npc.Sprite.spriteTexture = this.LoadAndDisposeIfNeeded(target.Npc.Sprite.spriteTexture, target.Key);
                propagated[target.Key] = true;
            }
        }

        /// <summary>Reload the portraits for matching NPCs.</summary>
        /// <param name="keys">The asset key to reload.</param>
        /// <param name="propagated">The asset keys which have been propagated.</param>
        private void ReloadNpcPortraits(IEnumerable<string> keys, IDictionary<string, bool> propagated)
        {
            // get NPCs
            HashSet<string> lookup = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
            var characters =
                (
                    from npc in this.GetCharacters()
                    where npc.isVillager()

                    let key = this.NormalizeAssetNameIgnoringEmpty(npc.Portrait?.Name)
                    where key != null && lookup.Contains(key)
                    select new { Npc = npc, Key = key }
                )
                .ToList();

            // special case: Gil is a private NPC field on the AdventureGuild class (only used for the portrait)
            {
                string gilKey = this.NormalizeAssetNameIgnoringEmpty("Portraits/Gil");
                if (lookup.Contains(gilKey))
                {
                    GameLocation adventureGuild = Game1.getLocationFromName("AdventureGuild");
                    if (adventureGuild != null)
                        characters.Add(new { Npc = this.Reflection.GetField<NPC>(adventureGuild, "Gil").GetValue(), Key = gilKey });
                }
            }

            // update portrait
            foreach (var target in characters)
            {
                target.Npc.Portrait = this.LoadAndDisposeIfNeeded(target.Npc.Portrait, target.Key);
                propagated[target.Key] = true;
            }
        }

        /// <summary>Reload the sprites for matching players.</summary>
        /// <param name="key">The asset key to reload.</param>
        private bool ReloadPlayerSprites(string key)
        {
            Farmer[] players =
                (
                    from player in Game1.getOnlineFarmers()
                    where key == this.NormalizeAssetNameIgnoringEmpty(player.getTexture())
                    select player
                )
                .ToArray();

            foreach (Farmer player in players)
            {
                this.Reflection.GetField<Dictionary<string, Dictionary<int, List<int>>>>(typeof(FarmerRenderer), "_recolorOffsets").GetValue().Remove(player.getTexture());
                player.FarmerRenderer.MarkSpriteDirty();
            }

            return players.Any();
        }

        /// <summary>Reload suspension bridge textures.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadSuspensionBridges(LocalizedContentManager content, string key)
        {
            Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));

            foreach (GameLocation location in this.GetLocations(buildingInteriors: false))
            {
                // get suspension bridges field
                var field = this.Reflection.GetField<IEnumerable<SuspensionBridge>>(location, nameof(IslandNorth.suspensionBridges), required: false);
                if (field == null || !typeof(IEnumerable<SuspensionBridge>).IsAssignableFrom(field.FieldInfo.FieldType))
                    continue;

                // update textures
                foreach (SuspensionBridge bridge in field.GetValue())
                    this.Reflection.GetField<Texture2D>(bridge, "_texture").SetValue(texture.Value);
            }

            return texture.IsValueCreated;
        }

        /// <summary>Reload tree textures.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <param name="type">The type to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadTreeTextures(LocalizedContentManager content, string key, int type)
        {
            Tree[] trees = this.GetLocations()
                .SelectMany(p => p.terrainFeatures.Values.OfType<Tree>())
                .Where(tree => tree.treeType.Value == type)
                .ToArray();

            if (trees.Any())
            {
                Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));
                foreach (Tree tree in trees)
                    tree.texture = texture;
                return true;
            }

            return false;
        }

        /****
        ** Reload data methods
        ****/
        /// <summary>Reload the dialogue data for matching NPCs.</summary>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any assets were reloaded.</returns>
        private bool ReloadNpcDialogue(string key)
        {
            // get NPCs
            string name = Path.GetFileName(key);
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

        /// <summary>Reload the schedules for matching NPCs.</summary>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any assets were reloaded.</returns>
        private bool ReloadNpcSchedules(string key)
        {
            // get NPCs
            string name = Path.GetFileName(key);
            NPC[] villagers = this.GetCharacters().Where(npc => npc.Name == name && npc.isVillager()).ToArray();
            if (!villagers.Any())
                return false;

            // update schedule
            foreach (NPC villager in villagers)
            {
                // reload schedule
                this.Reflection.GetField<bool>(villager, "_hasLoadedMasterScheduleData").SetValue(false);
                this.Reflection.GetField<Dictionary<string, string>>(villager, "_masterScheduleData").SetValue(null);
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

        /// <summary>Reload cached translations from the <c>Strings\StringsFromCSFiles</c> asset.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <returns>Returns whether any data was reloaded.</returns>
        /// <remarks>Derived from the <see cref="Game1.TranslateFields"/>.</remarks>
        private bool ReloadStringsFromCsFiles(LocalizedContentManager content)
        {
            Game1.samBandName = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2156");
            Game1.elliottBookName = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.2157");

            string[] dayNames = this.Reflection.GetField<string[]>(typeof(Game1), "_shortDayDisplayName").GetValue();
            dayNames[0] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3042");
            dayNames[1] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3043");
            dayNames[2] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3044");
            dayNames[3] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3045");
            dayNames[4] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3046");
            dayNames[5] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3047");
            dayNames[6] = content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3048");

            return true;
        }

        /****
        ** Helpers
        ****/
        /// <summary>Get all NPCs in the game (excluding farm animals).</summary>
        private IEnumerable<NPC> GetCharacters()
        {
            foreach (NPC character in this.GetLocations().SelectMany(p => p.characters))
                yield return character;

            if (Game1.CurrentEvent?.actors != null)
            {
                foreach (NPC character in Game1.CurrentEvent.actors)
                    yield return character;
            }
        }

        /// <summary>Get all farm animals in the game.</summary>
        private IEnumerable<FarmAnimal> GetFarmAnimals()
        {
            foreach (GameLocation location in this.GetLocations())
            {
                if (location is Farm farm)
                {
                    foreach (FarmAnimal animal in farm.animals.Values)
                        yield return animal;
                }
                else if (location is AnimalHouse animalHouse)
                    foreach (FarmAnimal animal in animalHouse.animals.Values)
                        yield return animal;
            }
        }

        /// <summary>Get all locations in the game.</summary>
        /// <param name="buildingInteriors">Whether to also get the interior locations for constructable buildings.</param>
        private IEnumerable<GameLocation> GetLocations(bool buildingInteriors = true)
        {
            // get available root locations
            IEnumerable<GameLocation> rootLocations = Game1.locations;
            if (SaveGame.loaded?.locations != null)
                rootLocations = rootLocations.Concat(SaveGame.loaded.locations);

            // yield root + child locations
            foreach (GameLocation location in rootLocations)
            {
                yield return location;

                if (buildingInteriors && location is BuildableGameLocation buildableLocation)
                {
                    foreach (Building building in buildableLocation.buildings)
                    {
                        GameLocation indoors = building.indoors.Value;
                        if (indoors != null)
                            yield return indoors;
                    }
                }
            }
        }

        /// <summary>Normalize an asset key to match the cache key and assert that it's valid, but don't raise an error for null or empty values.</summary>
        /// <param name="path">The asset key to normalize.</param>
        private string NormalizeAssetNameIgnoringEmpty(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return this.AssertAndNormalizeAssetName(path);
        }

        /// <summary>Get whether a key starts with a substring after the substring is normalized.</summary>
        /// <param name="key">The key to check.</param>
        /// <param name="rawSubstring">The substring to normalize and find.</param>
        private bool KeyStartsWith(string key, string rawSubstring)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(rawSubstring))
                return false;

            return key.StartsWith(this.NormalizeAssetNameIgnoringEmpty(rawSubstring), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Get whether a normalized asset key is in the given folder.</summary>
        /// <param name="key">The normalized asset key (like <c>Animals/cat</c>).</param>
        /// <param name="folder">The key folder (like <c>Animals</c>); doesn't need to be normalized.</param>
        /// <param name="allowSubfolders">Whether to return true if the key is inside a subfolder of the <paramref name="folder"/>.</param>
        private bool IsInFolder(string key, string folder, bool allowSubfolders = false)
        {
            return
                this.KeyStartsWith(key, $"{folder}\\")
                && (allowSubfolders || this.CountSegments(key) == this.CountSegments(folder) + 1);
        }

        /// <summary>Get the segments in a path (e.g. 'a/b' is 'a' and 'b').</summary>
        /// <param name="path">The path to check.</param>
        private string[] GetSegments(string path)
        {
            return path != null
                ? PathUtilities.GetSegments(path)
                : new string[0];
        }

        /// <summary>Count the number of segments in a path (e.g. 'a/b' is 2).</summary>
        /// <param name="path">The path to check.</param>
        private int CountSegments(string path)
        {
            return this.GetSegments(path).Length;
        }

        /// <summary>Load a texture, and dispose the old one if <see cref="AggressiveMemoryOptimizations"/> is enabled and it's different from the new instance.</summary>
        /// <param name="oldTexture">The previous texture to dispose.</param>
        /// <param name="key">The asset key to load.</param>
        private Texture2D LoadAndDisposeIfNeeded(Texture2D oldTexture, string key)
        {
            // if aggressive memory optimizations are enabled, load the asset from the disposable
            // content manager and dispose the old instance if needed.
            if (this.AggressiveMemoryOptimizations)
            {
                GameContentManagerForAssetPropagation content = this.DisposableContentManager;

                Texture2D newTexture = content.Load<Texture2D>(key);
                if (oldTexture?.IsDisposed == false && !object.ReferenceEquals(oldTexture, newTexture) && content.IsResponsibleFor(oldTexture))
                    oldTexture.Dispose();

                return newTexture;
            }

            // else just (re)load it from the main content manager
            return this.MainContentManager.Load<Texture2D>(key);
        }

        /// <summary>Remove a case-insensitive key from the paint mask cache.</summary>
        /// <param name="key">The paint mask asset key.</param>
        private bool RemoveFromPaintMaskCache(string key)
        {
            // make cache case-insensitive
            // This is needed for cache invalidation since mods may specify keys with a different capitalization
            if (!object.ReferenceEquals(BuildingPainter.paintMaskLookup.Comparer, StringComparer.OrdinalIgnoreCase))
                BuildingPainter.paintMaskLookup = new Dictionary<string, List<List<int>>>(BuildingPainter.paintMaskLookup, StringComparer.OrdinalIgnoreCase);

            // remove key from cache
            return BuildingPainter.paintMaskLookup.Remove(key);
        }
    }
}
