using System.Diagnostics.CodeAnalysis;
using StardewValley;
using StardewValley.Locations;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.World;

/// <summary>A command which moves the player to the next mine level.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class DownMineLevelCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public DownMineLevelCommand()
        : base("world_downminelevel", "Goes down one mine level.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        int level = (Game1.currentLocation as MineShaft)?.mineLevel ?? 0;
        monitor.Log($"OK, warping you to mine level {level + 1}.", LogLevel.Info);
        Game1.enterMine(level + 1);
    }
}
