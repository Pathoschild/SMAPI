namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands;

/// <summary>A console command to register.</summary>
internal interface IConsoleCommand
{
    /*********
    ** Accessors
    *********/
    /// <summary>The command name the user must type.</summary>
    string Name { get; }

    /// <summary>The command description.</summary>
    string Description { get; }

    /// <summary>Whether the command may need to perform logic when the game updates. This value shouldn't change.</summary>
    bool MayNeedUpdate { get; }

    /// <summary>Whether the command may need to perform logic when the player presses a button. This value shouldn't change.</summary>
    bool MayNeedInput { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    void Handle(IMonitor monitor, string command, ArgumentParser args);

    /// <summary>Perform any logic needed on update tick.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    void OnUpdated(IMonitor monitor);

    /// <summary>Perform any logic when input is received.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="button">The button that was pressed.</param>
    void OnButtonPressed(IMonitor monitor, SButton button);
}
