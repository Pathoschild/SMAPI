using System;

namespace StardewModdingAPI.Framework;

/// <summary>A command that can be submitted through the SMAPI console to interact with SMAPI.</summary>
internal class Command
{
    /*********
    ** Accessor
    *********/
    /// <summary>The mod that registered the command (or <c>null</c> if registered by SMAPI).</summary>
    public IModMetadata? Mod { get; }

    /// <summary>The command name, which the user must type to trigger it.</summary>
    public string Name { get; }

    /// <summary>The human-readable documentation shown when the player runs the built-in 'help' command.</summary>
    public string Documentation { get; }

    /// <summary>The method to invoke when the command is triggered. This method is passed the command name and arguments submitted by the user.</summary>
    public Action<string, string[]> Callback { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod that registered the command (or <c>null</c> if registered by SMAPI).</param>
    /// <param name="name">The command name, which the user must type to trigger it.</param>
    /// <param name="documentation">The human-readable documentation shown when the player runs the built-in 'help' command.</param>
    /// <param name="callback">The method to invoke when the command is triggered. This method is passed the command name and arguments submitted by the user.</param>
    public Command(IModMetadata? mod, string name, string documentation, Action<string, string[]> callback)
    {
        this.Mod = mod;
        this.Name = name;
        this.Documentation = documentation;
        this.Callback = callback;
    }
}
