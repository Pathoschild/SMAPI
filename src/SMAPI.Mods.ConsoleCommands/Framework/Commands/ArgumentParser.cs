using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands;

/// <summary>Provides methods for parsing command-line arguments.</summary>
internal class ArgumentParser : IReadOnlyList<string>
{
    /*********
    ** Fields
    *********/
    /// <summary>The command name for errors.</summary>
    private readonly string CommandName;

    /// <summary>The arguments to parse.</summary>
    private readonly string[] Args;

    /// <summary>Writes messages to the console and log file.</summary>
    private readonly IMonitor Monitor;


    /*********
    ** Accessors
    *********/
    /// <summary>Get the number of arguments.</summary>
    public int Count => this.Args.Length;

    /// <summary>Get the argument at the specified index in the list.</summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    public string this[int index] => this.Args[index];


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="commandName">The command name for errors.</param>
    /// <param name="args">The arguments to parse.</param>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    public ArgumentParser(string commandName, string[] args, IMonitor monitor)
    {
        this.CommandName = commandName;
        this.Args = args;
        this.Monitor = monitor;
    }

    /// <summary>Try to read a string argument.</summary>
    /// <param name="index">The argument index.</param>
    /// <param name="name">The argument name for error messages.</param>
    /// <param name="value">The parsed value.</param>
    /// <param name="required">Whether to show an error if the argument is missing.</param>
    /// <param name="oneOf">Require that the argument match one of the given values (case-insensitive).</param>
    public bool TryGet(int index, string name, [NotNullWhen(true)] out string? value, bool required = true, string[]? oneOf = null)
    {
        value = null;

        // validate
        if (this.Args.Length < index + 1)
        {
            if (required)
                this.LogError($"Argument {index} ({name}) is required.");
            return false;
        }
        if (oneOf?.Any() == true && !oneOf.Contains(this.Args[index], StringComparer.OrdinalIgnoreCase))
        {
            this.LogError($"Argument {index} ({name}) must be one of {string.Join(", ", oneOf)}.");
            return false;
        }

        // get value
        value = this.Args[index];
        return true;
    }

    /// <summary>Try to read an integer argument.</summary>
    /// <param name="index">The argument index.</param>
    /// <param name="name">The argument name for error messages.</param>
    /// <param name="value">The parsed value.</param>
    /// <param name="required">Whether to show an error if the argument is missing.</param>
    /// <param name="min">The minimum value allowed.</param>
    /// <param name="max">The maximum value allowed.</param>
    public bool TryGetInt(int index, string name, out int value, bool required = true, int? min = null, int? max = null)
    {
        value = 0;

        // get argument
        if (!this.TryGet(index, name, out string? raw, required))
            return false;

        // parse
        if (!int.TryParse(raw, out value))
        {
            this.LogIntFormatError(index, name, min, max);
            return false;
        }

        // validate
        if ((min.HasValue && value < min) || (max.HasValue && value > max))
        {
            this.LogIntFormatError(index, name, min, max);
            return false;
        }

        return true;
    }

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)this.Args).GetEnumerator();
    }

    /// <summary>Returns an enumerator that iterates through a collection.</summary>
    /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Log a usage error.</summary>
    /// <param name="message">The message describing the error.</param>
    private void LogError(string message)
    {
        this.Monitor.Log($"{message} Type 'help {this.CommandName}' for usage.", LogLevel.Error);
    }

    /// <summary>Print an error for an invalid int argument.</summary>
    /// <param name="index">The argument index.</param>
    /// <param name="name">The argument name for error messages.</param>
    /// <param name="min">The minimum value allowed.</param>
    /// <param name="max">The maximum value allowed.</param>
    private void LogIntFormatError(int index, string name, int? min, int? max)
    {
        if (min.HasValue && max.HasValue)
            this.LogError($"Argument {index} ({name}) must be an integer between {min} and {max}.");
        else if (min.HasValue)
            this.LogError($"Argument {index} ({name}) must be an integer and at least {min}.");
        else if (max.HasValue)
            this.LogError($"Argument {index} ({name}) must be an integer and at most {max}.");
        else
            this.LogError($"Argument {index} ({name}) must be an integer.");
    }
}
