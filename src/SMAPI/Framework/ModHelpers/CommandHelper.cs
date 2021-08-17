using System;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides an API for managing console commands.</summary>
    internal class CommandHelper : BaseHelper, ICommandHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod using this instance.</summary>
        private readonly IModMetadata Mod;

        /// <summary>Manages console commands.</summary>
        private readonly CommandManager CommandManager;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod using this instance.</param>
        /// <param name="commandManager">Manages console commands.</param>
        public CommandHelper(IModMetadata mod, CommandManager commandManager)
            : base(mod?.Manifest?.UniqueID ?? "SMAPI")
        {
            this.Mod = mod;
            this.CommandManager = commandManager;
        }

        /// <inheritdoc />
        public ICommandHelper Add(string name, string documentation, Action<string, string[]> callback)
        {
            this.CommandManager.Add(this.Mod, name, documentation, callback);
            return this;
        }
        
        /// <inheritdoc />
        public ICommandHelper Add(string name, string documentation, Action<string, string[]> callback, Func<string, string[]> autoCompleteHandler)
        {
            this.CommandManager.Add(this.Mod, name, documentation, callback, autoCompleteHandler: autoCompleteHandler);
            return this;
        }

        /// <inheritdoc />
        [Obsolete]
        public bool Trigger(string name, string[] arguments)
        {
            SCore.DeprecationManager.Warn(
                source: SCore.DeprecationManager.GetSourceName(this.ModID),
                nounPhrase: $"{nameof(IModHelper)}.{nameof(IModHelper.ConsoleCommands)}.{nameof(ICommandHelper.Trigger)}",
                version: "3.8.1",
                severity: DeprecationLevel.Notice
            );

            return this.CommandManager.Trigger(name, arguments);
        }
    }
}
