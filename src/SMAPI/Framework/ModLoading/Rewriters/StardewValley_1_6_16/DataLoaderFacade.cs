using System.Collections.Generic;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.ContentManagement;
using StardewValley.GameData;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Buffs;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Bundles;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Crafting;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Fences;
using StardewValley.GameData.FishPonds;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.GameData.FruitTrees;
using StardewValley.GameData.GarbageCans;
using StardewValley.GameData.GiantCrops;
using StardewValley.GameData.HomeRenovations;
using StardewValley.GameData.LocationContexts;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Machines;
using StardewValley.GameData.MakeoverOutfits;
using StardewValley.GameData.Minecarts;
using StardewValley.GameData.Movies;
using StardewValley.GameData.Museum;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Pants;
using StardewValley.GameData.Pets;
using StardewValley.GameData.Powers;
using StardewValley.GameData.Shirts;
using StardewValley.GameData.Shops;
using StardewValley.GameData.SpecialOrders;
using StardewValley.GameData.Tools;
using StardewValley.GameData.Weapons;
using StardewValley.GameData.Weddings;
using StardewValley.GameData.WildTrees;
using StardewValley.GameData.WorldMaps;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6_16;

/// <summary>Maps Stardew Valley 1.6.15's <see cref="DataLoader"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class DataLoaderFacade : IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    // return type changed too, which is a breaking change we can't easily rewrite
    ///// <inheritdoc cref="DataLoader.Achievements" />
    //public static Dictionary<int, string> Achievements(LocalizedContentManager content)
    //{
    //    return DataLoader.Achievements(content);
    //}

    /// <inheritdoc cref="DataLoader.AdditionalFarms" />
    public static List<ModFarmType> AdditionalFarms(LocalizedContentManager content)
    {
        return DataLoader.AdditionalFarms(content);
    }

    /// <inheritdoc cref="DataLoader.AdditionalLanguages" />
    public static List<ModLanguage> AdditionalLanguages(LocalizedContentManager content)
    {
        return DataLoader.AdditionalLanguages(content);
    }

    /// <inheritdoc cref="DataLoader.AdditionalWallpaperFlooring" />
    public static List<ModWallpaperOrFlooring> AdditionalWallpaperFlooring(LocalizedContentManager content)
    {
        return DataLoader.AdditionalWallpaperFlooring(content);
    }

    /// <inheritdoc cref="DataLoader.AnimationDescriptions" />
    public static Dictionary<string, string> AnimationDescriptions(LocalizedContentManager content)
    {
        return DataLoader.AnimationDescriptions(content);
    }

    /// <inheritdoc cref="DataLoader.AquariumFish" />
    public static Dictionary<string, string> AquariumFish(LocalizedContentManager content)
    {
        return DataLoader.AquariumFish(content);
    }

    /// <inheritdoc cref="DataLoader.AudioChanges" />
    public static Dictionary<string, AudioCueData> AudioChanges(LocalizedContentManager content)
    {
        return DataLoader.AudioChanges(content);
    }

    /// <inheritdoc cref="DataLoader.BigCraftables" />
    public static Dictionary<string, BigCraftableData> BigCraftables(LocalizedContentManager content)
    {
        return DataLoader.BigCraftables(content);
    }

    /// <inheritdoc cref="DataLoader.Buffs" />
    public static Dictionary<string, BuffData> Buffs(LocalizedContentManager content)
    {
        return DataLoader.Buffs(content);
    }

    /// <inheritdoc cref="DataLoader.Buildings" />
    public static Dictionary<string, BuildingData> Buildings(LocalizedContentManager content)
    {
        return DataLoader.Buildings(content);
    }

    /// <inheritdoc cref="DataLoader.Bundles" />
    public static Dictionary<string, string> Bundles(LocalizedContentManager content)
    {
        return DataLoader.Bundles(content);
    }

    /// <inheritdoc cref="DataLoader.ChairTiles" />
    public static Dictionary<string, string> ChairTiles(LocalizedContentManager content)
    {
        return DataLoader.ChairTiles(content);
    }

    /// <inheritdoc cref="DataLoader.Characters" />
    public static Dictionary<string, CharacterData> Characters(LocalizedContentManager content)
    {
        return DataLoader.Characters(content);
    }

    /// <inheritdoc cref="DataLoader.Concessions" />
    public static List<ConcessionItemData> Concessions(LocalizedContentManager content)
    {
        return DataLoader.Concessions(content);
    }

    /// <inheritdoc cref="DataLoader.ConcessionTastes" />
    public static List<ConcessionTaste> ConcessionTastes(LocalizedContentManager content)
    {
        return DataLoader.ConcessionTastes(content);
    }

    /// <inheritdoc cref="DataLoader.CookingRecipes" />
    public static Dictionary<string, string> CookingRecipes(LocalizedContentManager content)
    {
        return DataLoader.CookingRecipes(content);
    }

    /// <inheritdoc cref="DataLoader.CraftingRecipes" />
    public static Dictionary<string, string> CraftingRecipes(LocalizedContentManager content)
    {
        return DataLoader.CraftingRecipes(content);
    }

    /// <inheritdoc cref="DataLoader.Crops" />
    public static Dictionary<string, CropData> Crops(LocalizedContentManager content)
    {
        return DataLoader.Crops(content);
    }

    /// <inheritdoc cref="DataLoader.LostItemsShop" />
    public static List<LostItem> LostItemsShop(LocalizedContentManager content)
    {
        return DataLoader.LostItemsShop(content);
    }

    /// <inheritdoc cref="DataLoader.EngagementDialogue" />
    public static Dictionary<string, string> EngagementDialogue(LocalizedContentManager content)
    {
        return DataLoader.EngagementDialogue(content);
    }

    /// <inheritdoc cref="DataLoader.FarmAnimals" />
    public static Dictionary<string, FarmAnimalData> FarmAnimals(LocalizedContentManager content)
    {
        return DataLoader.FarmAnimals(content);
    }

    /// <inheritdoc cref="DataLoader.Fences" />
    public static Dictionary<string, FenceData> Fences(LocalizedContentManager content)
    {
        return DataLoader.Fences(content);
    }

    /// <inheritdoc cref="DataLoader.Festivals_FestivalDates" />
    public static Dictionary<string, string> Festivals_FestivalDates(LocalizedContentManager content)
    {
        return DataLoader.Festivals_FestivalDates(content);
    }

    /// <inheritdoc cref="DataLoader.Fish" />
    public static Dictionary<string, string> Fish(LocalizedContentManager content)
    {
        return DataLoader.Fish(content);
    }

    /// <inheritdoc cref="DataLoader.FishPondData" />
    public static List<FishPondData> FishPondData(LocalizedContentManager content)
    {
        return DataLoader.FishPondData(content);
    }

    /// <inheritdoc cref="DataLoader.FloorsAndPaths" />
    public static Dictionary<string, FloorPathData> FloorsAndPaths(LocalizedContentManager content)
    {
        return DataLoader.FloorsAndPaths(content);
    }

    /// <inheritdoc cref="DataLoader.FruitTrees" />
    public static Dictionary<string, FruitTreeData> FruitTrees(LocalizedContentManager content)
    {
        return DataLoader.FruitTrees(content);
    }

    /// <inheritdoc cref="DataLoader.GarbageCans" />
    public static GarbageCanData GarbageCans(LocalizedContentManager content)
    {
        return DataLoader.GarbageCans(content);
    }

    /// <inheritdoc cref="DataLoader.GiantCrops" />
    public static Dictionary<string, GiantCropData> GiantCrops(LocalizedContentManager content)
    {
        return DataLoader.GiantCrops(content);
    }

    /// <inheritdoc cref="DataLoader.HairData" />
    public static Dictionary<int, string> HairData(LocalizedContentManager content)
    {
        return DataLoader.HairData(content);
    }

    /// <inheritdoc cref="DataLoader.HomeRenovations" />
    public static Dictionary<string, HomeRenovation> HomeRenovations(LocalizedContentManager content)
    {
        return DataLoader.HomeRenovations(content);
    }

    /// <inheritdoc cref="DataLoader.IncomingPhoneCalls" />
    public static Dictionary<string, IncomingPhoneCallData> IncomingPhoneCalls(LocalizedContentManager content)
    {
        return DataLoader.IncomingPhoneCalls(content);
    }

    /// <inheritdoc cref="DataLoader.JukeboxTracks" />
    public static Dictionary<string, JukeboxTrackData> JukeboxTracks(LocalizedContentManager content)
    {
        return DataLoader.JukeboxTracks(content);
    }

    /// <inheritdoc cref="DataLoader.LocationContexts" />
    public static Dictionary<string, LocationContextData> LocationContexts(LocalizedContentManager content)
    {
        return DataLoader.LocationContexts(content);
    }

    /// <inheritdoc cref="DataLoader.Locations" />
    public static Dictionary<string, LocationData> Locations(LocalizedContentManager content)
    {
        return DataLoader.Locations(content);
    }

    /// <inheritdoc cref="DataLoader.Machines" />
    public static Dictionary<string, MachineData> Machines(LocalizedContentManager content)
    {
        return DataLoader.Machines(content);
    }

    /// <inheritdoc cref="DataLoader.Mail" />
    public static Dictionary<string, string> Mail(LocalizedContentManager content)
    {
        return DataLoader.Mail(content);
    }

    /// <inheritdoc cref="DataLoader.MakeoverOutfits" />
    public static List<MakeoverOutfit> MakeoverOutfits(LocalizedContentManager content)
    {
        return content.Load<List<MakeoverOutfit>>("Data/MakeoverOutfits");
    }

    /// <inheritdoc cref="DataLoader.Mannequins" />
    public static Dictionary<string, MannequinData> Mannequins(LocalizedContentManager content)
    {
        return content.Load<Dictionary<string, MannequinData>>("Data/Mannequins");
    }

    /// <inheritdoc cref="DataLoader.Minecarts" />
    public static Dictionary<string, MinecartNetworkData> Minecarts(LocalizedContentManager content)
    {
        return DataLoader.Minecarts(content);
    }

    /// <inheritdoc cref="DataLoader.MonsterSlayerQuests" />
    public static Dictionary<string, MonsterSlayerQuestData> MonsterSlayerQuests(LocalizedContentManager content)
    {
        return DataLoader.MonsterSlayerQuests(content);
    }

    /// <inheritdoc cref="DataLoader.Movies" />
    public static List<MovieData> Movies(LocalizedContentManager content)
    {
        return DataLoader.Movies(content);
    }

    /// <inheritdoc cref="DataLoader.MoviesReactions" />
    public static List<MovieCharacterReaction> MoviesReactions(LocalizedContentManager content)
    {
        return DataLoader.MoviesReactions(content);
    }

    /// <inheritdoc cref="DataLoader.MuseumRewards" />
    public static Dictionary<string, MuseumRewards> MuseumRewards(LocalizedContentManager content)
    {
        return DataLoader.MuseumRewards(content);
    }

    /// <inheritdoc cref="DataLoader.NpcGiftTastes" />
    public static Dictionary<string, string> NpcGiftTastes(LocalizedContentManager content)
    {
        return DataLoader.NpcGiftTastes(content);
    }

    /// <inheritdoc cref="DataLoader.Objects" />
    public static Dictionary<string, ObjectData> Objects(LocalizedContentManager content)
    {
        return DataLoader.Objects(content);
    }

    /// <inheritdoc cref="DataLoader.PaintData" />
    public static Dictionary<string, string> PaintData(LocalizedContentManager content)
    {
        return DataLoader.PaintData(content);
    }

    /// <inheritdoc cref="DataLoader.Pants" />
    public static Dictionary<string, PantsData> Pants(LocalizedContentManager content)
    {
        return DataLoader.Pants(content);
    }

    /// <inheritdoc cref="DataLoader.PassiveFestivals" />
    public static Dictionary<string, PassiveFestivalData> PassiveFestivals(LocalizedContentManager content)
    {
        return DataLoader.PassiveFestivals(content);
    }

    /// <inheritdoc cref="DataLoader.Pets" />
    public static Dictionary<string, PetData> Pets(LocalizedContentManager content)
    {
        return DataLoader.Pets(content);
    }

    /// <inheritdoc cref="DataLoader.Powers" />
    public static Dictionary<string, PowersData> Powers(LocalizedContentManager content)
    {
        return content.Load<Dictionary<string, PowersData>>("Data/Powers");
    }

    /// <inheritdoc cref="DataLoader.Quests" />
    public static Dictionary<string, string> Quests(LocalizedContentManager content)
    {
        return DataLoader.Quests(content);
    }

    /// <inheritdoc cref="DataLoader.RandomBundles" />
    public static List<RandomBundleData> RandomBundles(LocalizedContentManager content)
    {
        return DataLoader.RandomBundles(content);
    }

    /// <inheritdoc cref="DataLoader.SecretNotes" />
    public static Dictionary<int, string> SecretNotes(LocalizedContentManager content)
    {
        return DataLoader.SecretNotes(content);
    }

    /// <inheritdoc cref="DataLoader.Shirts" />
    public static Dictionary<string, ShirtData> Shirts(LocalizedContentManager content)
    {
        return DataLoader.Shirts(content);
    }

    /// <inheritdoc cref="DataLoader.Shops" />
    public static Dictionary<string, ShopData> Shops(LocalizedContentManager content)
    {
        return DataLoader.Shops(content);
    }

    /// <inheritdoc cref="DataLoader.SpecialOrders" />
    public static Dictionary<string, SpecialOrderData> SpecialOrders(LocalizedContentManager content)
    {
        return DataLoader.SpecialOrders(content);
    }

    /// <inheritdoc cref="DataLoader.TailoringRecipes" />
    public static List<TailorItemRecipe> TailoringRecipes(LocalizedContentManager content)
    {
        return DataLoader.TailoringRecipes(content);
    }

    /// <inheritdoc cref="DataLoader.Tools" />
    public static Dictionary<string, ToolData> Tools(LocalizedContentManager content)
    {
        return DataLoader.Tools(content);
    }

    /// <inheritdoc cref="DataLoader.TriggerActions" />
    public static List<TriggerActionData> TriggerActions(LocalizedContentManager content)
    {
        return DataLoader.TriggerActions(content);
    }

    /// <inheritdoc cref="DataLoader.Trinkets" />
    public static Dictionary<string, TrinketData> Trinkets(LocalizedContentManager content)
    {
        return content.Load<Dictionary<string, TrinketData>>("Data/Trinkets");
    }

    /// <inheritdoc cref="DataLoader.Weapons" />
    public static Dictionary<string, WeaponData> Weapons(LocalizedContentManager content)
    {
        return DataLoader.Weapons(content);
    }

    /// <inheritdoc cref="DataLoader.Weddings" />
    public static WeddingData Weddings(LocalizedContentManager content)
    {
        return DataLoader.Weddings(content);
    }

    /// <inheritdoc cref="DataLoader.WildTrees" />
    public static Dictionary<string, WildTreeData> WildTrees(LocalizedContentManager content)
    {
        return DataLoader.WildTrees(content);
    }

    /// <inheritdoc cref="DataLoader.WorldMap" />
    public static Dictionary<string, WorldMapRegionData> WorldMap(LocalizedContentManager content)
    {
        return DataLoader.WorldMap(content);
    }

    /****
    ** TV assets
    ****/
    /// <inheritdoc cref="DataLoader.Tv_CookingChannel" />
    public static Dictionary<string, string> Tv_CookingChannel(LocalizedContentManager content)
    {
        return DataLoader.Tv_CookingChannel(content);
    }

    /// <inheritdoc cref="DataLoader.Tv_TipChannel" />
    public static Dictionary<string, string> Tv_TipChannel(LocalizedContentManager content)
    {
        return DataLoader.Tv_TipChannel(content);
    }


    /*********
    ** Private methods
    *********/
    private DataLoaderFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
