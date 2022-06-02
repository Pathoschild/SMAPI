using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Mods.ConsoleCommands.Framework.ItemData;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which list item types.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class ListItemTypesCommand : ConsoleCommand
{
    /*********
    ** Fields
    *********/
    /// <summary>Provides methods for searching and constructing items.</summary>
    private readonly ItemRepository Items = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public ListItemTypesCommand()
        : base("list_item_types", "Lists item types you can filter in other commands.\n\nUsage: list_item_types") { }

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

        // handle
        ItemType[] matches =
            (
                from item in this.Items.GetAll()
                orderby item.Type.ToString()
                select item.Type
            )
            .Distinct()
            .ToArray();
        string summary = "Searching...\n";
        if (matches.Any())
            monitor.Log(summary + this.GetTableString(matches, new[] { "type" }, val => new[] { val.ToString() }), LogLevel.Info);
        else
            monitor.Log(summary + "No item types found.", LogLevel.Info);
    }
}