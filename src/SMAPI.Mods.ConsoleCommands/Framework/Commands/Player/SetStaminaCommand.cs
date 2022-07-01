using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which edits the player's current stamina.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetStaminaCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetStaminaCommand()
        : base("player_setstamina", "Sets the player's stamina.\n\nUsage: player_setstamina [value]\n- value: an integer amount.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // validate
        if (!args.Any())
        {
            monitor.Log($"You currently have {Game1.player.Stamina} stamina. Specify a value to change it.", LogLevel.Info);
            return;
        }

        // handle
        string amountStr = args[0];
        if (int.TryParse(amountStr, out int amount))
        {
            Game1.player.Stamina = amount;
            monitor.Log($"OK, you now have {Game1.player.Stamina} stamina.", LogLevel.Info);
        }
        else
            this.LogArgumentNotInt(monitor);
    }
}
