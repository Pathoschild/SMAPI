using System;

namespace StardewModdingAPI;

/// <summary>Provides an API for managing console commands.</summary>
public interface ICommandHelper : IModLinked
{
    /*********
    ** Public methods
    *********/
    /// <summary>Add a console command.</summary>
    /// <param name="name">The command name, which the user must type to trigger it.</param>
    /// <param name="documentation">The human-readable documentation shown when the player runs the built-in 'help' command.</param>
    /// <param name="callback">The method to invoke when the command is triggered. This method is passed the command name and arguments submitted by the user.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> or <paramref name="callback"/> is null or empty.</exception>
    /// <exception cref="FormatException">The <paramref name="name"/> is not a valid format.</exception>
    /// <exception cref="ArgumentException">There's already a command with that name.</exception>
    ICommandHelper Add(string name, string documentation, Action<string, string[]> callback);

    /// <summary>Trigger a command.</summary>
    /// <param name="name">The command name.</param>
    /// <param name="arguments">The command arguments.</param>
    /// <returns>Returns whether a matching command was triggered.</returns>
    [Obsolete("Use mod-provided APIs to integrate with mods instead. This method will be removed in SMAPI 4.0.0.")]
    bool Trigger(string name, string[] arguments);
}