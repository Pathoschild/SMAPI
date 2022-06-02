using System;
using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Other;

/// <summary>A command which logs the keys being pressed for 30 seconds once enabled.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class TestInputCommand : ConsoleCommand
{
    /*********
    ** Fields
    *********/
    /// <summary>The number of seconds for which to log input.</summary>
    private readonly int LogSeconds = 30;

    /// <summary>When the command should stop printing input, or <c>null</c> if currently disabled.</summary>
    private long? ExpiryTicks;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public TestInputCommand()
        : base("test_input", "Prints all input to the console for 30 seconds.", mayNeedUpdate: true, mayNeedInput: true) { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        this.ExpiryTicks = DateTime.UtcNow.Add(TimeSpan.FromSeconds(this.LogSeconds)).Ticks;
        monitor.Log($"OK, logging all player input for {this.LogSeconds} seconds.", LogLevel.Info);
    }

    /// <summary>Perform any logic needed on update tick.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    public override void OnUpdated(IMonitor monitor)
    {
        // handle expiry
        if (this.ExpiryTicks != null && this.ExpiryTicks <= DateTime.UtcNow.Ticks)
        {
            monitor.Log("No longer logging input.", LogLevel.Info);
            this.ExpiryTicks = null;
            return;
        }
    }

    /// <summary>Perform any logic when input is received.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="button">The button that was pressed.</param>
    public override void OnButtonPressed(IMonitor monitor, SButton button)
    {
        if (this.ExpiryTicks != null)
            monitor.Log($"Pressed {button}", LogLevel.Info);
    }
}
