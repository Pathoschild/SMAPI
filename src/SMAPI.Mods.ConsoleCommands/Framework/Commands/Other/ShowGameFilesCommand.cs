using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Other
{
    /// <summary>A command which shows the game files.</summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
    internal class ShowGameFilesCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ShowGameFilesCommand()
            : base("show_game_files", "Opens the game folder.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Constants.GamePath,
                UseShellExecute = true
            });

            monitor.Log($"OK, opening {Constants.GamePath}.", LogLevel.Info);
        }
    }
}
