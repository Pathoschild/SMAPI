using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.World;

/// <summary>A command which freezes the current time.</summary>
internal class FreezeTimeCommand : ConsoleCommand
{
    /*********
    ** Fields
    *********/
    /// <summary>The time of day at which to freeze time.</summary>
    internal static int FrozenTime;

    /// <summary>Whether to freeze time.</summary>
    private bool FreezeTime;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public FreezeTimeCommand()
        : base("world_freezetime", "Freezes or resumes time.\n\nUsage: world_freezetime [value]\n- value: one of 0 (resume), 1 (freeze), or blank (toggle).", mayNeedUpdate: true) { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        if (args.Any())
        {
            // parse arguments
            if (!args.TryGetInt(0, "value", out int value, min: 0, max: 1))
                return;

            // handle
            this.FreezeTime = value == 1;
            FreezeTimeCommand.FrozenTime = Game1.timeOfDay;
            monitor.Log($"OK, time is now {(this.FreezeTime ? "frozen" : "resumed")}.", LogLevel.Info);
        }
        else
        {
            this.FreezeTime = !this.FreezeTime;
            FreezeTimeCommand.FrozenTime = Game1.timeOfDay;
            monitor.Log($"OK, time is now {(this.FreezeTime ? "frozen" : "resumed")}.", LogLevel.Info);
        }
    }

    /// <summary>Perform any logic needed on update tick.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    public override void OnUpdated(IMonitor monitor)
    {
        if (this.FreezeTime && Context.IsWorldReady)
            Game1.timeOfDay = FreezeTimeCommand.FrozenTime;
    }
}
