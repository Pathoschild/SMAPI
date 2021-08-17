using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Framework.Commands
{
    /// <summary>The 'help' SMAPI console command.</summary>
    internal class HelpCommand : IInternalCommand
    {
        /*********
        ** Fields
        *********/
        /// <summary>Manages console commands.</summary>
        private readonly CommandManager CommandManager;


        /*********
        ** Accessors
        *********/
        /// <summary>The command name, which the user must type to trigger it.</summary>
        public string Name { get; } = "help";

        /// <summary>The human-readable documentation shown when the player runs the built-in 'help' command.</summary>
        public string Description { get; } = "Lists command documentation.\n\nUsage: help\nLists all available commands.\n\nUsage: help <cmd>\n- cmd: The name of a command whose documentation to display.";


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="commandManager">Manages console commands.</param>
        public HelpCommand(CommandManager commandManager)
        {
            this.CommandManager = commandManager;
        }

        /// <summary>Handle the console command when it's entered by the user.</summary>
        /// <param name="args">The command arguments.</param>
        /// <param name="monitor">Writes messages to the console.</param>
        public void HandleCommand(string[] args, IMonitor monitor)
        {
            if (args.Any())
            {
                Command result = this.CommandManager.Get(args[0]);
                if (result == null)
                    monitor.Log("There's no command with that name. Type 'help' by itself for more info.", LogLevel.Error);
                else
                    monitor.Log($"{result.Name}: {result.Documentation}{(result.Mod != null ? $"\n(Added by {result.Mod.DisplayName}.)" : "")}", LogLevel.Info);
            }
            else
            {
                string message =
                    "\n\n"
                    + "Need help with a SMAPI or mod issue?\n"
                    + "------------------------------------\n"
                    + "See https://smapi.io/help for the best places to ask.\n\n\n"
                    + "How commands work\n"
                    + "-----------------\n"
                    + "Just enter a command directly to run it, just like you did for this help command. Commands may take optional arguments\n"
                    + "which change what they do; for example, type 'help help' to see help about the help command. When playing in split-screen\n"
                    + "mode, you can add screen=X to send the command to a specific screen instance.\n\n\n"
                    + "Valid commands\n"
                    + "--------------\n"
                    + "The following commands are registered. For more info about a command, type 'help command_name'.\n\n";

                IGrouping<string, string>[] groups = (from command in this.CommandManager.GetAll() orderby command.Mod?.DisplayName, command.Name group command.Name by command.Mod?.DisplayName).ToArray();
                foreach (var group in groups)
                {
                    string modName = group.Key ?? "SMAPI";
                    string[] commandNames = group.ToArray();
                    message += $"{modName}:\n  {string.Join("\n  ", commandNames)}\n\n";
                }

                monitor.Log(message, LogLevel.Info);
            }
        }

        /// <inheritdoc/>
        public string[] HandleAutoCompletion( string currInput )
        {
            // If this is true, then they already entered an argument.
            // This command only accepts one argument, so we are done.
            if ( currInput.Contains( ' ' ) )
                return null;

            var allCommandNames = this.CommandManager.GetAll().Select( ( cmd ) => cmd.Name );

            List<string> ret = new List<string>();
            foreach ( string cmdName in allCommandNames )
            {
                if ( cmdName.StartsWith( currInput ) )
                    ret.Add( cmdName );
            }
            return ret.ToArray();
        }
    }
}
