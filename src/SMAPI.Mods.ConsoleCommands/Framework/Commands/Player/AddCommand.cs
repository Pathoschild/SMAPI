using System;
using System.Linq;
using StardewModdingAPI.Mods.ConsoleCommands.Framework.ItemData;
using StardewValley;
using Object = StardewValley.Object;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which adds an item to the player inventory.</summary>
internal class AddCommand : ConsoleCommand
{
    /*********
    ** Fields
    *********/
    /// <summary>Provides methods for searching and constructing items.</summary>
    private readonly ItemRepository Items = new();

    /// <summary>The type names recognized by this command.</summary>
    private readonly string[] ValidTypes = Enum.GetNames(typeof(ItemType)).Concat(new[] { "Name" }).ToArray();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public AddCommand()
        : base("player_add", AddCommand.GetDescription()) { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // validate
        if (!Context.IsWorldReady)
        {
            monitor.Log("You need to load a save to use this command.", LogLevel.Error);
            return;
        }

        // read arguments
        if (!args.TryGet(0, "item type", out string? type, oneOf: this.ValidTypes))
            return;
        if (!args.TryGetInt(2, "count", out int count, min: 1, required: false))
            count = 1;
        if (!args.TryGetInt(3, "quality", out int quality, min: Object.lowQuality, max: Object.bestQuality, required: false))
            quality = Object.lowQuality;

        // find matching item
        SearchableItem? match = Enum.TryParse(type, true, out ItemType itemType)
            ? this.FindItemByID(monitor, args, itemType)
            : this.FindItemByName(monitor, args);
        if (match == null)
            return;

        // apply count
        match.Item.Stack = count;

        // apply quality
        if (match.Item is Object obj)
            obj.Quality = quality;
        else if (match.Item is Tool tool)
            tool.UpgradeLevel = quality;

        // add to inventory
        Game1.player.addItemByMenuIfNecessary(match.Item);
        monitor.Log($"OK, added {match.Name} ({match.Type} #{match.ID}) to your inventory.", LogLevel.Info);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a matching item by its ID.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="type">The item type.</param>
    private SearchableItem? FindItemByID(IMonitor monitor, ArgumentParser args, ItemType type)
    {
        // read arguments
        if (!args.TryGetInt(1, "item ID", out int id, min: 0))
            return null;

        // find matching item
        SearchableItem? item = this.Items.GetAll().FirstOrDefault(p => p.Type == type && p.ID == id);
        if (item == null)
            monitor.Log($"There's no {type} item with ID {id}.", LogLevel.Error);
        return item;
    }

    /// <summary>Get a matching item by its name.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="args">The command arguments.</param>
    private SearchableItem? FindItemByName(IMonitor monitor, ArgumentParser args)
    {
        // read arguments
        if (!args.TryGet(1, "item name", out string? name))
            return null;

        // find matching items
        SearchableItem[] matches = this.Items.GetAll().Where(p => p.NameContains(name)).ToArray();
        if (!matches.Any())
        {
            monitor.Log($"There's no item with name '{name}'. You can use the 'list_items [name]' command to search for items.", LogLevel.Error);
            return null;
        }

        // handle single exact match
        SearchableItem[] exactMatches = matches.Where(p => p.NameEquivalentTo(name)).ToArray();
        if (exactMatches.Length == 1)
            return exactMatches[0];

        // handle ambiguous results
        string options = this.GetTableString(
            data: matches,
            header: new[] { "type", "name", "command" },
            getRow: item => new[] { item.Type.ToString(), item.DisplayName, $"player_add {item.Type} {item.ID}" }
        );
        monitor.Log($"There's no item with name '{name}'. Do you mean one of these?\n\n{options}", LogLevel.Info);
        return null;
    }

    /// <summary>Get the command description.</summary>
    private static string GetDescription()
    {
        string[] typeValues = Enum.GetNames(typeof(ItemType));
        return "Gives the player an item.\n"
               + "\n"
               + "Usage: player_add <type> <item> [count] [quality]\n"
               + $"- type: the item type (one of {string.Join(", ", typeValues)}).\n"
               + "- item: the item ID (use the 'list_items' command to see a list).\n"
               + "- count (optional): how many of the item to give.\n"
               + $"- quality (optional): one of {Object.lowQuality} (normal), {Object.medQuality} (silver), {Object.highQuality} (gold), or {Object.bestQuality} (iridium).\n"
               + "\n"
               + "Usage: player_add name \"<name>\" [count] [quality]\n"
               + "- name: the item name to search (use the 'list_items' command to see a list). This will add the item immediately if it's an exact match, else show a table of matching items.\n"
               + "- count (optional): how many of the item to give.\n"
               + $"- quality (optional): one of {Object.lowQuality} (normal), {Object.medQuality} (silver), {Object.highQuality} (gold), or {Object.bestQuality} (iridium).\n"
               + "\n"
               + "These examples both add the galaxy sword to your inventory:\n"
               + "  player_add weapon 4\n"
               + "  player_add name \"Galaxy Sword\"";
    }
}
