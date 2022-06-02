using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which edits the player's current health.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetHealthCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetHealthCommand()
        : base("player_sethealth", "Sets the player's health.\n\nUsage: player_sethealth [value]\n- value: an integer amount.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // no-argument mode
        if (!args.Any())
        {
            monitor.Log($"You currently have {Game1.player.health} health. Specify a value to change it.", LogLevel.Info);
            return;
        }

        // handle
        string amountStr = args[0];
        if (int.TryParse(amountStr, out int amount))
        {
            Game1.player.health = amount;
            monitor.Log($"OK, you now have {Game1.player.health} health.", LogLevel.Info);
        }
        else
            this.LogArgumentNotInt(monitor);
    }
}
