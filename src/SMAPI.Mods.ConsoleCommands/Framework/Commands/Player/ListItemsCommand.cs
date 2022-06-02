using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Mods.ConsoleCommands.Framework.ItemData;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which list items available to spawn.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class ListItemsCommand : ConsoleCommand
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
    public ListItemsCommand()
        : base("list_items", "Lists and searches items in the game data.\n\nUsage: list_items [search]\n- search (optional): an arbitrary search string to filter by.") { }

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
        SearchableItem[] matches =
            (
                from item in this.GetItems(args.ToArray())
                orderby item.Type.ToString(), item.Name
                select item
            )
            .ToArray();
        string summary = "Searching...\n";
        if (matches.Any())
            monitor.Log(summary + this.GetTableString(matches, new[] { "type", "name", "id" }, val => new[] { val.Type.ToString(), val.Name, val.ID.ToString() }), LogLevel.Info);
        else
            monitor.Log(summary + "No items found", LogLevel.Info);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get all items which can be searched and added to the player's inventory through the console.</summary>
    /// <param name="searchWords">The search string to find.</param>
    private IEnumerable<SearchableItem> GetItems(string[] searchWords)
    {
        // normalize search term
        searchWords = searchWords.Where(word => !string.IsNullOrWhiteSpace(word)).ToArray();
        bool getAll = !searchWords.Any();

        // find matches
        return (
            from item in this.Items.GetAll()
            let term = $"{item.ID}|{item.Type}|{item.Name}|{item.DisplayName}"
            where getAll || searchWords.All(word => term.IndexOf(word, StringComparison.CurrentCultureIgnoreCase) != -1)
            select item
        );
    }
}