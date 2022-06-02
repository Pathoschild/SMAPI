using System;
using System.Diagnostics.CodeAnalysis;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.World;

/// <summary>A command which moves the player to the given mine level.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetMineLevelCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetMineLevelCommand()
        : base("world_setminelevel", "Sets the mine level?\n\nUsage: world_setminelevel <value>\n- value: The target level (a number starting at 1).") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // parse arguments
        if (!args.TryGetInt(0, "mine level", out int level, min: 1))
            return;

        // handle
        level = Math.Max(1, level);
        monitor.Log($"OK, warping you to mine level {level}.", LogLevel.Info);
        Game1.enterMine(level);
    }
}
