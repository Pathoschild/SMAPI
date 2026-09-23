using System;
using System.Collections.Generic;
#if SMAPI_FOR_ANDROID
using HarmonyLib;
using StardewModdingAPI.Mobile;
using StardewModdingAPI.Mobile.Facade;
#endif
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Framework.ModLoading.Finders;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_5;
using StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;
using StardewValley;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Enchantments;
using StardewValley.GameData;
using StardewValley.GameData.FishPonds;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.GameData.Movies;
using StardewValley.GameData.SpecialOrders;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Mods;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewValley.Projectiles;
using StardewValley.Quests;
using StardewValley.SpecialOrders;
using StardewValley.SpecialOrders.Objectives;
using StardewValley.SpecialOrders.Rewards;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using StardewValley.WorldMaps;
using xTile.Layers;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Metadata;

/// <summary>Provides CIL instruction handlers which rewrite mods for compatibility, and detect low-level mod issues like incompatible code.</summary>
internal class InstructionMetadata
{
    /*********
    ** Fields
    *********/
    /// <summary>The assembly names to which to heuristically detect broken references.</summary>
    /// <remarks>The current implementation only works correctly with assemblies that should always be present.</remarks>
    private readonly ISet<string> ValidateReferencesToAssemblies = new HashSet<string> { "StardewModdingAPI", "Stardew Valley", "StardewValley", "Netcode" };


    /*********
    ** Public methods
    *********/
    /// <summary>Get rewriters which detect or fix incompatible CIL instructions in mod assemblies.</summary>
    /// <param name="paranoidMode">Whether to detect paranoid mode issues.</param>
    /// <param name="rewriteMods">Whether to get handlers which rewrite mods for compatibility.</param>
    /// <param name="logTechnicalDetailsForBrokenMods">Whether to include more technical details about broken mods in the TRACE logs. This is mainly useful for creating compatibility rewriters.</param>
    public IEnumerable<IInstructionHandler> GetHandlers(bool paranoidMode, bool rewriteMods, bool logTechnicalDetailsForBrokenMods)
    {
        /****
        ** rewrite CIL to fix incompatible code
        ****/
        // rewrite for crossplatform compatibility
        if (rewriteMods)
        {
            // specific versions
            yield return new ReplaceReferencesRewriter()
                /****
                ** Stardew Valley 1.5
                ****/
                // fields moved
                .MapField("Netcode.NetCollection`1<StardewValley.Objects.Furniture> StardewValley.Locations.DecoratableLocation::furniture", typeof(GameLocation), nameof(GameLocation.furniture))
                .MapField("Netcode.NetCollection`1<StardewValley.TerrainFeatures.ResourceClump> StardewValley.Farm::resourceClumps", typeof(GameLocation), nameof(GameLocation.resourceClumps))
                .MapField("Netcode.NetCollection`1<StardewValley.TerrainFeatures.ResourceClump> StardewValley.Locations.MineShaft::resourceClumps", typeof(GameLocation), nameof(GameLocation.resourceClumps))

                /****
                ** Stardew Valley 1.5.5
                ****/
                // XNA => MonoGame method changes
                .MapFacade<SpriteBatch, SpriteBatchFacade>()

                /****
                ** Stardew Valley 1.6
                ****/
                // moved types (audio)
                .MapType("StardewValley.AudioCategoryWrapper", typeof(AudioCategoryWrapper))
                .MapType("StardewValley.AudioEngineWrapper", typeof(AudioEngineWrapper))
                .MapType("StardewValley.DummyAudioCategory", typeof(DummyAudioCategory))
                .MapType("StardewValley.DummyAudioEngine", typeof(DummyAudioEngine))
                .MapType("StardewValley.IAudioCategory", typeof(IAudioCategory))
                .MapType("StardewValley.IAudioEngine", typeof(IAudioEngine))
                .MapType("StardewValley.Network.NetAudio/SoundContext", typeof(SoundContext))

                // moved types (enchantments)
                .MapType("StardewValley.AmethystEnchantment", typeof(AmethystEnchantment))
                .MapType("StardewValley.AquamarineEnchantment", typeof(AquamarineEnchantment))
                .MapType("StardewValley.ArchaeologistEnchantment", typeof(ArchaeologistEnchantment))
                .MapType("StardewValley.ArtfulEnchantment", typeof(ArtfulEnchantment))
                .MapType("StardewValley.AutoHookEnchantment", typeof(AutoHookEnchantment))
                .MapType("StardewValley.AxeEnchantment", typeof(AxeEnchantment))
                .MapType("StardewValley.BaseEnchantment", typeof(BaseEnchantment))
                .MapType("StardewValley.BaseWeaponEnchantment", typeof(BaseWeaponEnchantment))
                .MapType("StardewValley.BottomlessEnchantment", typeof(BottomlessEnchantment))
                .MapType("StardewValley.BugKillerEnchantment", typeof(BugKillerEnchantment))
                .MapType("StardewValley.CrusaderEnchantment", typeof(CrusaderEnchantment))
                .MapType("StardewValley.DiamondEnchantment", typeof(DiamondEnchantment))
                .MapType("StardewValley.EfficientToolEnchantment", typeof(EfficientToolEnchantment))
                .MapType("StardewValley.EmeraldEnchantment", typeof(EmeraldEnchantment))
                .MapType("StardewValley.FishingRodEnchantment", typeof(FishingRodEnchantment))
                .MapType("StardewValley.GalaxySoulEnchantment", typeof(GalaxySoulEnchantment))
                .MapType("StardewValley.GenerousEnchantment", typeof(GenerousEnchantment))
                .MapType("StardewValley.HaymakerEnchantment", typeof(HaymakerEnchantment))
                .MapType("StardewValley.HoeEnchantment", typeof(HoeEnchantment))
                .MapType("StardewValley.JadeEnchantment", typeof(JadeEnchantment))
                .MapType("StardewValley.MagicEnchantment", typeof(MagicEnchantment))
                .MapType("StardewValley.MasterEnchantment", typeof(MasterEnchantment))
                .MapType("StardewValley.MilkPailEnchantment", typeof(MilkPailEnchantment))
                .MapType("StardewValley.PanEnchantment", typeof(PanEnchantment))
                .MapType("StardewValley.PickaxeEnchantment", typeof(PickaxeEnchantment))
                .MapType("StardewValley.PowerfulEnchantment", typeof(PowerfulEnchantment))
                .MapType("StardewValley.PreservingEnchantment", typeof(PreservingEnchantment))
                .MapType("StardewValley.ReachingToolEnchantment", typeof(ReachingToolEnchantment))
                .MapType("StardewValley.RubyEnchantment", typeof(RubyEnchantment))
                .MapType("StardewValley.ShavingEnchantment", typeof(ShavingEnchantment))
                .MapType("StardewValley.ShearsEnchantment", typeof(ShearsEnchantment))
                .MapType("StardewValley.SwiftToolEnchantment", typeof(SwiftToolEnchantment))
                .MapType("StardewValley.TopazEnchantment", typeof(TopazEnchantment))
                .MapType("StardewValley.VampiricEnchantment", typeof(VampiricEnchantment))
                .MapType("StardewValley.WateringCanEnchantment", typeof(WateringCanEnchantment))

                // moved types (special orders)
                .MapType("StardewValley.SpecialOrder", typeof(SpecialOrder))
                .MapType("StardewValley.SpecialOrder/QuestDuration", typeof(QuestDuration))
                .MapType("StardewValley.SpecialOrder/QuestState", typeof(SpecialOrderStatus))

                .MapType("StardewValley.CollectObjective", typeof(CollectObjective))
                .MapType("StardewValley.DeliverObjective", typeof(DeliverObjective))
                .MapType("StardewValley.DonateObjective", typeof(DonateObjective))
                .MapType("StardewValley.FishObjective", typeof(FishObjective))
                .MapType("StardewValley.GiftObjective", typeof(GiftObjective))
                .MapType("StardewValley.JKScoreObjective", typeof(JKScoreObjective))
                .MapType("StardewValley.OrderObjective", typeof(OrderObjective))
                .MapType("StardewValley.ReachMineFloorObjective", typeof(ReachMineFloorObjective))
                .MapType("StardewValley.ShipObjective", typeof(ShipObjective))
                .MapType("StardewValley.SlayObjective", typeof(SlayObjective))

                .MapType("StardewValley.FriendshipReward", typeof(FriendshipReward))
                .MapType("StardewValley.GemsReward", typeof(GemsReward))
                .MapType("StardewValley.MailReward", typeof(MailReward))
                .MapType("StardewValley.MoneyReward", typeof(MoneyReward))
                .MapType("StardewValley.OrderReward", typeof(OrderReward))
                .MapType("StardewValley.ResetEventReward", typeof(ResetEventReward))

                // moved types (other)
                .MapType("LocationWeather", typeof(LocationWeather))
                .MapType("WaterTiles", typeof(WaterTiles))
                .MapType("StardewValley.Game1/MusicContext", typeof(MusicContext))
                .MapType("StardewValley.ModDataDictionary", typeof(ModDataDictionary))
                .MapType("StardewValley.ModHooks", typeof(ModHooks))
                .MapType("StardewValley.Network.IWorldState", typeof(NetWorldState))
                .MapType("StardewValley.PathFindController", typeof(PathFindController))
                .MapType("StardewValley.SchedulePathDescription", typeof(SchedulePathDescription))

                // deleted delegates
                .MapType("StardewValley.DelayedAction/delayedBehavior", typeof(Action))

                // field renames
                .MapFieldName(typeof(FloorPathData), "ID", nameof(FloorPathData.Id))
                .MapFieldName(typeof(ModFarmType), "ID", nameof(ModFarmType.Id))
                .MapFieldName(typeof(ModLanguage), "ID", nameof(ModLanguage.Id))
                .MapFieldName(typeof(ModWallpaperOrFlooring), "ID", nameof(ModWallpaperOrFlooring.Id))
                .MapFieldName(typeof(MovieData), "ID", nameof(MovieData.Id))
                .MapFieldName(typeof(MovieReaction), "ID", nameof(MovieReaction.Id))
                .MapFieldName(typeof(MovieScene), "ID", nameof(MovieScene.Id))

                // general API changes
                // note: types are mapped before members, regardless of the order listed here
                .MapFacade<AbigailGame, AbigailGameFacade>()
                .MapFacade<AnimalHouse, AnimalHouseFacade>()
                .MapFacade(typeof(ArgUtility).FullName!, typeof(ArgUtilityFacade))
                .MapFacade<BasicProjectile, BasicProjectileFacade>()
                .MapFacade<BedFurniture, BedFurnitureFacade>()
                .MapFacade<BoatTunnel, BoatTunnelFacade>()
                .MapFacade<Boots, BootsFacade>()
                .MapFacade<BreakableContainer, BreakableContainerFacade>()
                .MapFacade<Buff, BuffFacade>()
                .MapFacade<BuffsDisplay, BuffsDisplayFacade>()
                .MapFacade<Bush, BushFacade>()
                .MapFacade<Butterfly, ButterflyFacade>()
                .MapFacade<Building, BuildingFacade>()
                .MapFacade<CarpenterMenu, CarpenterMenuFacade>()
                .MapFacade<Cask, CaskFacade>()
                .MapFacade<Character, CharacterFacade>()
                .MapFacade<Chest, ChestFacade>()
                .MapFacade<Clothing, ClothingFacade>()
                .MapFacade<ColoredObject, ColoredObjectFacade>()
                .MapFacade<CrabPot, CrabPotFacade>()
                .MapFacade<CraftingRecipe, CraftingRecipeFacade>()
                .MapFacade<Crop, CropFacade>()
                .MapFacade<DebuffingProjectile, DebuffingProjectileFacade>()
                .MapFacade<DelayedAction, DelayedActionFacade>()
                .MapFacade<Dialogue, DialogueFacade>()
                .MapFacade<DialogueBox, DialogueBoxFacade>()
                .MapFacade<DiscreteColorPicker, DiscreteColorPickerFacade>()
                .MapFacade<Event, EventFacade>()
                .MapFacade<Farm, FarmFacade>()
                .MapFacade<FarmAnimal, FarmAnimalFacade>()
                .MapFacade<Farmer, FarmerFacade>()
                .MapFacade<FarmerTeam, FarmerTeamFacade>()
                .MapFacade<FarmerRenderer, FarmerRendererFacade>()
                .MapFacade<Fence, FenceFacade>()
                .MapFacade<FishingRod, FishingRodFacade>()
                .MapFacade<FishPondReward, FishPondRewardFacade>()
                .MapFacade<FishTankFurniture, FishTankFurnitureFacade>()
                .MapFacade<Forest, ForestFacade>()
                .MapFacade<Furniture, FurnitureFacade>()
                .MapFacade<FruitTree, FruitTreeFacade>()
                .MapFacade<Game1, Game1Facade>()
                .MapFacade<GameLocation, GameLocationFacade>()
                .MapFacade<GiantCrop, GiantCropFacade>()
                .MapFacade<Hat, HatFacade>()
                .MapFacade<HoeDirt, HoeDirtFacade>()
                .MapFacade<HUDMessage, HudMessageFacade>()
                .MapFacade<IClickableMenu, IClickableMenuFacade>()
                .MapFacade<IslandWest, IslandWestFacade>()
                .MapFacade<Item, ItemFacade>()
                .MapFacade<ItemQueryContext, ItemQueryContextFacade>()
                .MapFacade<JunimoHut, JunimoHutFacade>()
                .MapFacade<LargeTerrainFeature, LargeTerrainFeatureFacade>()
                .MapFacade<Layer, LayerFacade>()
                .MapFacade<LibraryMuseum, LibraryMuseumFacade>()
                .MapFacade<LightSource, LightSourceFacade>()
                .MapFacade<LocalizedContentManager, LocalizedContentManagerFacade>()
                .MapType("StardewValley.Buildings.Mill", typeof(Building))
                .MapFacade<MineShaft, MineShaftFacade>()
                .MapFacade<Multiplayer, MultiplayerFacade>()
                .MapFacade<MeleeWeapon, MeleeWeaponFacade>()
                .MapFacade<NetFields, NetFieldsFacade>()
                .MapFacade<NetWorldState, NetWorldStateFacade>()
                .MapFacade<NPC, NpcFacade>()
                .MapFacade<PathFindController, PathFindControllerFacade>()
                .MapFacade<Projectile, ProjectileFacade>()
                .MapFacade<ProfileMenu, ProfileMenuFacade>()
                .MapFacade<Quest, QuestFacade>()
                .MapFacade<ResourceClump, ResourceClumpFacade>()
                .MapFacade<Ring, RingFacade>()
                .MapFacade<ShippingBin, ShippingBinFacade>()
                .MapFacade<ShopMenu, ShopMenuFacade>()
                .MapFacade<Sign, SignFacade>()
                .MapFacade<Slingshot, SlingshotFacade>()
                .MapFacade<SObject, ObjectFacade>()
                .MapFacade<SoundEffect, SoundEffectFacade>()
                .MapFacade<SpriteText, SpriteTextFacade>()
                .MapFacade<Stats, Stats_160_Facade>()
                .MapFacade<Stats, Stats_1615_Facade>()
                .MapFacade<StorageFurniture, StorageFurnitureFacade>()
                .MapFacade<TemporaryAnimatedSprite, TemporaryAnimatedSpriteFacade>()
                .MapFacade<TerrainFeature, TerrainFeatureFacade>()
                .MapFacade("StardewValley.Tools.ToolFactory", typeof(ToolFactoryFacade))
                .MapFacade<Tree, TreeFacade>()
                .MapFacade<TV, TvFacade>()
                .MapFacade<Utility, UtilityFacade>()
                .MapFacade("Microsoft.Xna.Framework.Graphics.ViewportExtensions", typeof(ViewportExtensionsFacade))
                .MapFacade<Wallpaper, WallpaperFacade>()
                .MapFacade<WateringCan, WateringCanFacade>()
                .MapFacade<WorldDate, WorldDateFacade>()
                .MapFacade(typeof(WorldMapManager).FullName!, typeof(WorldMapManagerFacade))

#if SMAPI_FOR_ANDROID
                .MapFacade<GameMenu, GameMenuFacade>()
                .MapFacade<MenuWithInventory, MenuWithInventoryFacade>()
                .MapFacade<IClickableMenu, IClickableMenuFacadeAndroid>()
                .MapFacade<SaveGame, SaveGameFacade>()
                .MapFacade<ItemGrabMenu, ItemGrabMenuFacade>()
                .MapFacade<InventoryPage, InventoryPageFacade>()
                .MapFacade<Toolbar, ToolbarFacade>()
                .MapFacade<OptionsPage, OptionsPageFacade>()
                .MapFacade<SocialPage, SocialPageFacade>()
                .MapFacade<OptionsDropDown, OptionsDropDownFacade>()
                .MapType(typeof(KeyboardInput).FullName, typeof(KeyboardInput))
                .MapType(typeof(KeyEventArgs).FullName, typeof(KeyEventArgs))
                .MapType("StardewValley.Game1/BundleType", typeof(StardewValley.BundleType))
                .MapType("StardewValley.Game1/MineChestType", typeof(MineChestType))
#endif

                // BuildableGameLocation merged into GameLocation
                .MapFacade("StardewValley.Locations.BuildableGameLocation", typeof(BuildableGameLocationFacade))
                .MapField("Netcode.NetCollection`1<StardewValley.Buildings.Building> StardewValley.Locations.BuildableGameLocation::buildings", typeof(GameLocation), nameof(GameLocation.buildings))

                // OverlaidDictionary enumerators changed
                // note: types are mapped before members, regardless of the order listed here
                .MapType("StardewValley.Network.OverlaidDictionary/KeysCollection", typeof(OverlaidDictionaryFacade.KeysCollection))
                .MapType("StardewValley.Network.OverlaidDictionary/KeysCollection/Enumerator", typeof(OverlaidDictionaryFacade.KeysCollection.Enumerator))
                .MapType("StardewValley.Network.OverlaidDictionary/PairsCollection", typeof(OverlaidDictionaryFacade.PairsCollection))
                .MapType("StardewValley.Network.OverlaidDictionary/PairsCollection/Enumerator", typeof(OverlaidDictionaryFacade.PairsCollection.Enumerator))
                .MapType("StardewValley.Network.OverlaidDictionary/ValuesCollection", typeof(OverlaidDictionaryFacade.ValuesCollection))
                .MapType("StardewValley.Network.OverlaidDictionary/ValuesCollection/Enumerator", typeof(OverlaidDictionaryFacade.ValuesCollection.Enumerator))
                .MapMethod($"{typeof(OverlaidDictionaryFacade).FullName}/{nameof(OverlaidDictionaryFacade.KeysCollection)} StardewValley.Network.OverlaidDictionary::get_Keys()", typeof(OverlaidDictionaryFacade), $"get_{nameof(OverlaidDictionaryFacade.Keys)}")
                .MapMethod($"{typeof(OverlaidDictionaryFacade).FullName}/{nameof(OverlaidDictionaryFacade.PairsCollection)} StardewValley.Network.OverlaidDictionary::get_Pairs()", typeof(OverlaidDictionaryFacade), $"get_{nameof(OverlaidDictionaryFacade.Pairs)}")
                .MapMethod($"{typeof(OverlaidDictionaryFacade).FullName}/{nameof(OverlaidDictionaryFacade.ValuesCollection)} StardewValley.Network.OverlaidDictionary::get_Values()", typeof(OverlaidDictionaryFacade), $"get_{nameof(OverlaidDictionaryFacade.Values)}")

                // implicit NetField conversions removed
                .MapMethod("Netcode.NetFieldBase`2::op_Implicit", typeof(NetFieldBaseFacade<,>), "op_Implicit")
                .MapMethod("System.Int64 Netcode.NetLong::op_Implicit(Netcode.NetLong)", typeof(NetLongFacade), nameof(NetLongFacade.op_Implicit))
                .MapMethod("System.Boolean Netcode.NetBool::op_Implicit(Netcode.NetBool)", typeof(ImplicitConversionOperatorsFacade), nameof(ImplicitConversionOperatorsFacade.NetBool_ToBool))
                .MapMethod("System.Int32 Netcode.NetInt::op_Implicit(Netcode.NetInt)", typeof(ImplicitConversionOperatorsFacade), nameof(ImplicitConversionOperatorsFacade.NetInt_ToInt))
                .MapMethod("System.String Netcode.NetString::op_Implicit(Netcode.NetString)", typeof(ImplicitConversionOperatorsFacade), nameof(ImplicitConversionOperatorsFacade.NetString_ToString))
                .MapMethod("System.Int32 StardewValley.Network.NetDirection::op_Implicit(StardewValley.Network.NetDirection)", typeof(ImplicitConversionOperatorsFacade), nameof(ImplicitConversionOperatorsFacade.NetDirection_ToInt))
                .MapMethod("!0 StardewValley.Network.NetPausableField`3<Microsoft.Xna.Framework.Vector2,Netcode.NetVector2,Netcode.NetVector2>::op_Implicit(StardewValley.Network.NetPausableField`3<!0,!1,!2>)", typeof(NetPausableFieldFacade<Vector2, NetVector2, NetVector2>), nameof(NetPausableFieldFacade<Vector2, NetVector2, NetVector2>.op_Implicit));

            // heuristic rewrites
            yield return new HeuristicFieldRewriter(this.ValidateReferencesToAssemblies);
            yield return new HeuristicMethodRewriter(this.ValidateReferencesToAssemblies);

            // 32-bit to 64-bit in Stardew Valley 1.5.5
            yield return new ArchitectureAssemblyRewriter();

#if SMAPI_FOR_ANDROID
            yield return new MapMethodToStaticMethodRewriter()
                .Add(typeof(OptionsElement), method => method.Name == "draw",
                    typeof(OptionsElementRewriter), method => method.Name == "draw",
                    map => { map.AddPramToSrc(typeof(IClickableMenu)); })
                .AddWithMethodFullName(
                    "System.Int32 StardewValley.Audio.IAudioEngine::GetCategoryIndex(System.String)",
                    StardewAudioMethods.IAudioEngine_GetCategoryIndex_MethodInfo
                )
                .AddWithMethodFullName(
                    "System.Void StardewValley.ISoundBank::AddCue(Microsoft.Xna.Framework.Audio.CueDefinition)",
                    StardewAudioMethods.ISoundBank_AddCue_MethodInfo
                )
                .AddWithMethodFullName(
                    StardewAudioMethods.get_Volume_FullName,
                    StardewAudioMethods.ICue_get_Volume_MethodInfo
                )
                .AddWithMethodFullName(
                    StardewAudioMethods.set_Volume_FullName,
                    StardewAudioMethods.ICue_set_Volume_MethodInfo
                )
                .AddWithMethodFullName(
                    StardewAudioMethods.get_Pitch_FullName,
                    StardewAudioMethods.Get_Pitch_ProxyMethodInfo
                )
                .AddWithMethodFullName(
                    StardewAudioMethods.set_Pitch_FullName,
                    StardewAudioMethods.Set_Pitch_ProxyMethodInfo
                )
                .AddWithMethodFullName(
                    StardewAudioMethods.get_IsPitchBeingControlledByRPC_FullName,
                    StardewAudioMethods.Get_IsPitchBeingControlledByRPC_MethodInfo
                )
                .AddWithMethodFullName(
                    StardewAudioMethods.ISoundBank_Exists_MethodFullName,
                    StardewAudioMethods.ISoundBank_Exists_MethodInfo
                )
                .AddWithTypeFullName(
                    "Force.DeepCloner.DeepClonerExtensions",
                    DeepClonerRewriter.OnRewriterIL
                )
                .AddWithTypeFullName(
                    typeof(GameWindow).FullName,
                    GameWindowRewriter.OnRewriteIL
                )
                .AddWithTypeFullName(
                    "StardewValley.KeyboardInput",
                    KeyboardInputRewriter.OnRewriteIL
                )
                .AddWithMethodFullName(
                    "System.Void StardewValley.KeyEventHandler::.ctor(System.Object,System.IntPtr)",
                    AccessTools.Method(typeof(KeyEventHandlerRewriter), nameof(KeyEventHandlerRewriter.Ctor))
                )
                .AddWithMethodFullName(
                    LetterViewerMenuRewriter.OnPageChange_FullName,
                    LetterViewerMenuRewriter.OnPageChangeProxy_MethodInfo
                )
                .AddWithTypeFullName(
                    typeof(Texture2D).FullName,
                    Texture2DRewriter.RewriterCallback
                );
#endif
        }

        /****
        ** detect mod issues
        ****/
        // Harmony usage
        yield return new HarmonyDetector();

        // broken code
        yield return new ReferenceToInvalidMemberFinder(this.ValidateReferencesToAssemblies, logTechnicalDetailsForBrokenMods);

        // code which may impact game stability
#if SMAPI_FOR_ANDROID
        yield return new FieldFinder(typeof(SaveGame).FullName!, ["serializer", "farmerSerializer", "locationSerializer"], InstructionHandleResult.DetectedSaveSerializer);
#else
        yield return new FieldFinder(typeof(SaveGame).FullName!, [nameof(SaveGame.serializer), nameof(SaveGame.farmerSerializer), nameof(SaveGame.locationSerializer)], InstructionHandleResult.DetectedSaveSerializer);
#endif
        yield return new EventFinder(typeof(ISpecializedEvents).FullName!, [nameof(ISpecializedEvents.UnvalidatedUpdateTicked), nameof(ISpecializedEvents.UnvalidatedUpdateTicking)], InstructionHandleResult.DetectedUnvalidatedUpdateTick);

        // direct console access
        yield return new TypeFinder(typeof(System.Console).FullName!, InstructionHandleResult.DetectedConsoleAccess);

        // paranoid issues
        if (paranoidMode)
        {
            // filesystem access
            yield return new TypeFinder(
                [
                    typeof(System.IO.File).FullName!,
                    typeof(System.IO.FileStream).FullName!,
                    typeof(System.IO.FileInfo).FullName!,
                    typeof(System.IO.Directory).FullName!,
                    typeof(System.IO.DirectoryInfo).FullName!,
                    typeof(System.IO.DriveInfo).FullName!,
                    typeof(System.IO.FileSystemWatcher).FullName!
                ],
                InstructionHandleResult.DetectedFilesystemAccess
            );

            // shell access
            yield return new TypeFinder(typeof(System.Diagnostics.Process).FullName!, InstructionHandleResult.DetectedShellAccess);
        }
    }
}
