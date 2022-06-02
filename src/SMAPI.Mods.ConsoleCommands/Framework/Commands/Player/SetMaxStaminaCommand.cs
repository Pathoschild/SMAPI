using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which edits the player's maximum stamina.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetMaxStaminaCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetMaxStaminaCommand()
        : base("player_setmaxstamina", "Sets the player's max stamina.\n\nUsage: player_setmaxstamina [value]\n- value: an integer amount.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // validate
        if (!args.Any())
        {
            monitor.Log($"You currently have {Game1.player.MaxStamina} max stamina. Specify a value to change it.", LogLevel.Info);
            return;
        }

        // handle
        if (args.TryGetInt(0, "amount", out int amount, min: 1))
        {
            Game1.player.MaxStamina = amount;
            monitor.Log($"OK, you now have {Game1.player.MaxStamina} max stamina.", LogLevel.Info);
        }
    }
}