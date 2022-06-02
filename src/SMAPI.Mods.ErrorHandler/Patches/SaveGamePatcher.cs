using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Internal.Patching;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace StardewModdingAPI.Mods.ErrorHandler.Patches;

/// <summary>Harmony patches for <see cref="SaveGame"/> which prevent some errors due to broken save data.</summary>
/// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
internal class SaveGamePatcher : BasePatcher
{
    /*********
    ** Fields
    *********/
    /// <summary>Writes messages to the console and log file.</summary>
    private static IMonitor Monitor = null!;

    /// <summary>A callback invoked when custom content is removed from the save data to avoid a crash.</summary>
    private static Action OnContentRemoved = null!;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="onContentRemoved">A callback invoked when custom content is removed from the save data to avoid a crash.</param>
    public SaveGamePatcher(IMonitor monitor, Action onContentRemoved)
    {
        SaveGamePatcher.Monitor = monitor;
        SaveGamePatcher.OnContentRemoved = onContentRemoved;
    }

    /// <inheritdoc />
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: this.RequireMethod<SaveGame>(nameof(SaveGame.loadDataToLocations)),
            prefix: this.GetHarmonyMethod(nameof(SaveGamePatcher.Before_LoadDataToLocations))
        );

        harmony.Patch(
            original: this.RequireMethod<SaveGame>(nameof(SaveGame.LoadFarmType)),
            finalizer: this.GetHarmonyMethod(nameof(SaveGamePatcher.Finalize_LoadFarmType))
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>The method to call instead of <see cref="SaveGame.loadDataToLocations"/>.</summary>
    /// <param name="gamelocations">The game locations being loaded.</param>
    /// <returns>Returns whether to execute the original method.</returns>
    private static bool Before_LoadDataToLocations(List<GameLocation> gamelocations)
    {
        // missing locations/NPCs
        IDictionary<string, string> npcs = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");
        if (SaveGamePatcher.RemoveBrokenContent(gamelocations, npcs))
            SaveGamePatcher.OnContentRemoved();

        return true;
    }

    /// <summary>The method to call after <see cref="SaveGame.LoadFarmType"/> throws an exception.</summary>
    /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
    /// <returns>Returns the exception to throw, if any.</returns>
    private static Exception? Finalize_LoadFarmType(Exception? __exception)
    {
        // missing custom farm type
        if (__exception?.Message.Contains("not a valid farm type") == true && !int.TryParse(SaveGame.loaded.whichFarm, out _))
        {
            SaveGamePatcher.Monitor.Log(__exception.GetLogSummary(), LogLevel.Error);
            SaveGamePatcher.Monitor.Log($"Removed invalid custom farm type '{SaveGame.loaded.whichFarm}' to avoid a crash when loading save '{Constants.SaveFolderName}'. (Did you remove a custom farm type mod?)", LogLevel.Warn);

            SaveGame.loaded.whichFarm = Farm.default_layout.ToString();
            SaveGame.LoadFarmType();
            SaveGamePatcher.OnContentRemoved();

            __exception = null;
        }

        return __exception;
    }

    /// <summary>Remove content which no longer exists in the game data.</summary>
    /// <param name="locations">The current game locations.</param>
    /// <param name="npcs">The NPC data.</param>
    private static bool RemoveBrokenContent(IEnumerable<GameLocation> locations, IDictionary<string, string> npcs)
    {
        bool removedAny = false;

        foreach (GameLocation location in locations)
            removedAny |= SaveGamePatcher.RemoveBrokenContent(location, npcs);

        return removedAny;
    }

    /// <summary>Remove content which no longer exists in the game data.</summary>
    /// <param name="location">The current game location.</param>
    /// <param name="npcs">The NPC data.</param>
    private static bool RemoveBrokenContent(GameLocation? location, IDictionary<string, string> npcs)
    {
        bool removedAny = false;
        if (location == null)
            return false;

        // check buildings
        if (location is BuildableGameLocation buildableLocation)
        {
            foreach (Building building in buildableLocation.buildings.ToArray())
            {
                try
                {
                    BluePrint _ = new(building.buildingType.Value);
                }
                catch (ContentLoadException)
                {
                    SaveGamePatcher.Monitor.Log($"Removed invalid building type '{building.buildingType.Value}' in {location.Name} ({building.tileX}, {building.tileY}) to avoid a crash when loading save '{Constants.SaveFolderName}'. (Did you remove a custom building mod?)", LogLevel.Warn);
                    buildableLocation.buildings.Remove(building);
                    removedAny = true;
                    continue;
                }

                SaveGamePatcher.RemoveBrokenContent(building.indoors.Value, npcs);
            }
        }

        // check NPCs
        foreach (NPC npc in location.characters.ToArray())
        {
            if (npc.isVillager() && !npcs.ContainsKey(npc.Name))
            {
                try
                {
                    npc.reloadSprite(); // this won't crash for special villagers like Bouncer
                }
                catch
                {
                    SaveGamePatcher.Monitor.Log($"Removed invalid villager '{npc.Name}' in {location.Name} ({npc.getTileLocation()}) to avoid a crash when loading save '{Constants.SaveFolderName}'. (Did you remove a custom NPC mod?)", LogLevel.Warn);
                    location.characters.Remove(npc);
                    removedAny = true;
                }
            }
        }

        // check objects
        foreach (var pair in location.objects.Pairs.ToArray())
        {
            // SpaceCore can leave null values when removing its custom content
            if (pair.Value == null)
            {
                location.Objects.Remove(pair.Key);
                SaveGamePatcher.Monitor.Log($"Removed invalid null object in {location.Name} ({pair.Key}) to avoid a crash when loading save '{Constants.SaveFolderName}'. (Did you remove a custom item mod?)", LogLevel.Warn);
                removedAny = true;
            }
        }

        return removedAny;
    }
}