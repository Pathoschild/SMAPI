using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which edits the player's current immunity.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetImmunityCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetImmunityCommand()
        : base("player_setimmunity", "Sets the player's immunity.\n\nUsage: player_setimmunity [value]\n- value: an integer amount.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // validate
        if (!args.Any())
        {
            monitor.Log($"You currently have {Game1.player.immunity} immunity. Specify a value to change it.", LogLevel.Info);
            return;
        }

        // handle
        if (args.TryGetInt(0, "amount", out int amount, min: 0))
        {
            Game1.player.immunity = amount;
            monitor.Log($"OK, you now have {Game1.player.immunity} immunity.", LogLevel.Info);
        }
    }
}
