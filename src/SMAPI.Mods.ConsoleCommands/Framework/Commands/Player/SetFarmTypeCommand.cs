using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using StardewValley;
using StardewValley.GameData;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which changes the player's farm type.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetFarmTypeCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetFarmTypeCommand()
        : base("set_farm_type", "Sets the current player's farm type.\n\nUsage: set_farm_type <farm type>\n- farm type: the farm type to set. Enter `set_farm_type list` for a list of available farm types.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // validate
        if (!Context.IsWorldReady)
        {
            monitor.Log("You must load a save to use this command.", LogLevel.Error);
            return;
        }

        // parse arguments
        if (!args.TryGet(0, "farm type", out string? farmType))
            return;
        bool isVanillaId = int.TryParse(farmType, out int vanillaId) && vanillaId is (>= 0 and < Farm.layout_max);

        // handle argument
        if (farmType == "list")
            this.HandleList(monitor);
        else if (isVanillaId)
            this.HandleVanillaFarmType(vanillaId, monitor);
        else
            this.HandleCustomFarmType(farmType, monitor);
    }


    /*********
    ** Private methods
    *********/
    /****
    ** Handlers
    ****/
    /// <summary>Print a list of available farm types.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    private void HandleList(IMonitor monitor)
    {
        StringBuilder result = new();

        // list vanilla types
        result.AppendLine("The farm type can be one of these vanilla types:");
        foreach (var type in this.GetVanillaFarmTypes())
            result.AppendLine($"   - {type.Key} ({type.Value})");
        result.AppendLine();

        // list custom types
        {
            var customTypes = this.GetCustomFarmTypes();
            if (customTypes.Any())
            {
                result.AppendLine("Or one of these custom farm types:");
                foreach (var type in customTypes.Values.OrderBy(p => p.ID))
                    result.AppendLine($"   - {type.ID} ({this.GetCustomName(type)})");
            }
            else
                result.AppendLine("Or a custom farm type (though none is loaded currently).");
        }

        // print
        monitor.Log(result.ToString(), LogLevel.Info);
    }

    /// <summary>Set a vanilla farm type.</summary>
    /// <param name="type">The farm type.</param>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    private void HandleVanillaFarmType(int type, IMonitor monitor)
    {
        if (Game1.whichFarm == type)
        {
            monitor.Log($"Your current farm is already set to {type} ({this.GetVanillaName(type)}).", LogLevel.Info);
            return;
        }

        this.SetFarmType(type, null);
        this.PrintSuccess(monitor, $"{type} ({this.GetVanillaName(type)}");
    }

    /// <summary>Set a custom farm type.</summary>
    /// <param name="id">The farm type ID.</param>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    private void HandleCustomFarmType(string id, IMonitor monitor)
    {
        if (Game1.whichModFarm?.ID == id)
        {
            monitor.Log($"Your current farm is already set to {id} ({this.GetCustomName(Game1.whichModFarm)}).", LogLevel.Info);
            return;
        }

        if (!this.GetCustomFarmTypes().TryGetValue(id, out ModFarmType? customFarmType))
        {
            monitor.Log($"Invalid farm type '{id}'. Enter `help set_farm_type` for more info.", LogLevel.Error);
            return;
        }

        this.SetFarmType(Farm.mod_layout, customFarmType);
        this.PrintSuccess(monitor, $"{id} ({this.GetCustomName(customFarmType)})");
    }

    /// <summary>Change the farm type.</summary>
    /// <param name="type">The farm type ID.</param>
    /// <param name="customFarmData">The custom farm type data, if applicable.</param>
    private void SetFarmType(int type, ModFarmType? customFarmData)
    {
        // set flags
        Game1.whichFarm = type;
        Game1.whichModFarm = customFarmData;

        // update farm map
        Farm farm = Game1.getFarm();
        farm.mapPath.Value = $@"Maps\{Farm.getMapNameFromTypeInt(Game1.whichFarm)}";
        farm.reloadMap();
        farm.updateWarps();

        // clear spouse area cache to avoid errors
        FieldInfo? cacheField = farm.GetType().GetField("_baseSpouseAreaTiles", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (cacheField == null)
            throw new InvalidOperationException("Failed to access '_baseSpouseAreaTiles' field to clear spouse area cache.");
        if (cacheField.GetValue(farm) is not IDictionary cache)
            throw new InvalidOperationException($"The farm's '_baseSpouseAreaTiles' field didn't match the expected {nameof(IDictionary)} type.");
        cache.Clear();
    }

    private void PrintSuccess(IMonitor monitor, string label)
    {
        StringBuilder result = new();
        result.AppendLine($"Your current farm has been converted to {label}. Saving and reloading is recommended to make sure everything is updated for the change.");
        result.AppendLine();
        result.AppendLine("This doesn't move items that are out of bounds on the new map. If you need to clean up, you can...");
        result.AppendLine("   - temporarily switch back to the previous farm type;");
        result.AppendLine("   - or use a mod like Noclip Mode: https://www.nexusmods.com/stardewvalley/mods/3900 ;");
        result.AppendLine("   - or use the world_clear console command (enter `help world_clear` for details).");

        monitor.Log(result.ToString(), LogLevel.Warn);
    }

    /****
    ** Vanilla farm types
    ****/
    /// <summary>Get the display name for a vanilla farm type.</summary>
    /// <param name="type">The farm type.</param>
    private string GetVanillaName(int type)
    {
        string? translationKey = type switch
        {
            Farm.default_layout => "Character_FarmStandard",
            Farm.riverlands_layout => "Character_FarmFishing",
            Farm.forest_layout => "Character_FarmForaging",
            Farm.mountains_layout => "Character_FarmMining",
            Farm.combat_layout => "Character_FarmCombat",
            Farm.fourCorners_layout => "Character_FarmFourCorners",
            Farm.beach_layout => "Character_FarmBeach",
            _ => null
        };

        return translationKey != null
            ? Game1.content.LoadString(@$"Strings\UI:{translationKey}").Split('_')[0]
            : type.ToString();
    }

    /// <summary>Get the available vanilla farm types by ID.</summary>
    private IDictionary<int, string> GetVanillaFarmTypes()
    {
        IDictionary<int, string> farmTypes = new Dictionary<int, string>();

        foreach (int id in Enumerable.Range(0, Farm.layout_max))
            farmTypes[id] = this.GetVanillaName(id);

        return farmTypes;
    }

    /****
    ** Custom farm types
    ****/
    /// <summary>Get the display name for a custom farm type.</summary>
    /// <param name="farmType">The custom farm type.</param>
    private string? GetCustomName(ModFarmType? farmType)
    {
        if (string.IsNullOrWhiteSpace(farmType?.TooltipStringPath))
            return farmType?.ID;

        return Game1.content.LoadString(farmType.TooltipStringPath)?.Split('_')[0] ?? farmType.ID;
    }

    /// <summary>Get the available custom farm types by ID.</summary>
    private IDictionary<string, ModFarmType> GetCustomFarmTypes()
    {
        IDictionary<string, ModFarmType> farmTypes = new Dictionary<string, ModFarmType>(StringComparer.OrdinalIgnoreCase);

        foreach (ModFarmType farmType in Game1.content.Load<List<ModFarmType>>("Data\\AdditionalFarms"))
        {
            if (string.IsNullOrWhiteSpace(farmType.ID))
                continue;

            farmTypes[farmType.ID] = farmType;
        }

        return farmTypes;
    }
}
