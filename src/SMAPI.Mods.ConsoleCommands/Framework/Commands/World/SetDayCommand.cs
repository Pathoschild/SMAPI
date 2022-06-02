using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.World;

/// <summary>A command which sets the current day.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetDayCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetDayCommand()
        : base("world_setday", "Sets the day to the specified value.\n\nUsage: world_setday <value>.\n- value: the target day (a number from 1 to 28).") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // no-argument mode
        if (!args.Any())
        {
            monitor.Log($"The current date is {Game1.currentSeason} {Game1.dayOfMonth}. Specify a value to change the day.", LogLevel.Info);
            return;
        }

        // parse arguments
        if (!args.TryGetInt(0, "day", out int day, min: 1, max: 28))
            return;

        // handle
        Game1.dayOfMonth = day;
        Game1.stats.DaysPlayed = (uint)SDate.Now().DaysSinceStart;
        monitor.Log($"OK, the date is now {Game1.currentSeason} {Game1.dayOfMonth}.", LogLevel.Info);
    }
}
