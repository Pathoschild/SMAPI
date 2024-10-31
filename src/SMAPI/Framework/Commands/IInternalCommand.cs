using System;

namespace StardewModdingAPI.Framework.Commands
{
    /// <summary>A core SMAPI console command.</summary>
    interface IInternalCommand
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The command name, which the user must type to trigger it.</summary>
        string Name { get; }

        /// <summary>The human-readable documentation shown when the player runs the built-in 'help' command.</summary>
        string Description { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Handle the console command when it's entered by the user.</summary>
        /// <param name="args">The command arguments.</param>
        /// <param name="monitor">Writes messages to the console.</param>
        void HandleCommand(string[] args, IMonitor monitor);

        /// <summary>Handle the console command auto-complete when requested by the user..</summary>
        /// <param name="input">The current input.</param>
        /// <param name="monitor">Writes messages to the console.</param>
        string[] HandleAutocomplete(string input, IMonitor monitor)
        {
            // Default implementation for if a command doesn't support it.
            return Array.Empty<string>();
        }
    }
}
