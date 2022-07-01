using System.Diagnostics.CodeAnalysis;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which edits the player's name.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetNameCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetNameCommand()
        : base("player_setname", "Sets the player's name.\n\nUsage: player_setname <target> <name>\n- target: what to rename (one of 'player' or 'farm').\n- name: the new name to set.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // parse arguments
        if (!args.TryGet(0, "target", out string? target, oneOf: new[] { "player", "farm" }))
            return;
        args.TryGet(1, "name", out string? name, required: false);

        // handle
        switch (target)
        {
            case "player":
                if (!string.IsNullOrWhiteSpace(name))
                {
                    Game1.player.Name = args[1];
                    monitor.Log($"OK, your name is now {Game1.player.Name}.", LogLevel.Info);
                }
                else
                    monitor.Log($"Your name is currently '{Game1.player.Name}'. Type 'help player_setname' for usage.", LogLevel.Info);
                break;

            case "farm":
                if (!string.IsNullOrWhiteSpace(name))
                {
                    Game1.player.farmName.Value = args[1];
                    monitor.Log($"OK, your farm's name is now {Game1.player.farmName}.", LogLevel.Info);
                }
                else
                    monitor.Log($"Your farm's name is currently '{Game1.player.farmName}'. Type 'help player_setname' for usage.", LogLevel.Info);
                break;
        }
    }
}
