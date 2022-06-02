using System;
using StardewModdingAPI.Framework.Deprecations;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <summary>Provides an API for managing console commands.</summary>
internal class CommandHelper : BaseHelper, ICommandHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>Manages console commands.</summary>
    private readonly CommandManager CommandManager;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="commandManager">Manages console commands.</param>
    public CommandHelper(IModMetadata mod, CommandManager commandManager)
        : base(mod)
    {
        this.CommandManager = commandManager;
    }

    /// <inheritdoc />
    public ICommandHelper Add(string name, string documentation, Action<string, string[]> callback)
    {
        this.CommandManager.Add(this.Mod, name, documentation, callback);
        return this;
    }

    /// <inheritdoc />
    [Obsolete("Use mod-provided APIs to integrate with mods instead. This method will be removed in SMAPI 4.0.0.")]
    public bool Trigger(string name, string[] arguments)
    {
        SCore.DeprecationManager.Warn(
            source: this.Mod,
            nounPhrase: $"{nameof(IModHelper)}.{nameof(IModHelper.ConsoleCommands)}.{nameof(ICommandHelper.Trigger)}",
            version: "3.8.1",
            severity: DeprecationLevel.Notice
        );

        return this.CommandManager.Trigger(name, arguments);
    }
}