using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.World;

/// <summary>A command which sets the current season.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetSeasonCommand : ConsoleCommand
{
    /*********
    ** Fields
    *********/
    /// <summary>The valid season names.</summary>
    private readonly string[] ValidSeasons = { "winter", "spring", "summer", "fall" };


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetSeasonCommand()
        : base("world_setseason", "Sets the season to the specified value.\n\nUsage: world_setseason <season>\n- season: the target season (one of 'spring', 'summer', 'fall', 'winter').") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // no-argument mode
        if (!args.Any())
        {
            monitor.Log($"The current season is {Game1.currentSeason}. Specify a value to change it.", LogLevel.Info);
            return;
        }

        // parse arguments
        if (!args.TryGet(0, "season", out string? season, oneOf: this.ValidSeasons))
            return;

        // handle
        Game1.currentSeason = season.ToLower();
        Game1.setGraphicsForSeason();
        Game1.stats.DaysPlayed = (uint)SDate.Now().DaysSinceStart;
        monitor.Log($"OK, the date is now {Game1.currentSeason} {Game1.dayOfMonth}.", LogLevel.Info);
    }
}
