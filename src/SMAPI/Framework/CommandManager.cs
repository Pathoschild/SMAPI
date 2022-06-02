using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using StardewModdingAPI.Framework.Commands;

namespace StardewModdingAPI.Framework;

/// <summary>Manages console commands.</summary>
internal class CommandManager
{
    /*********
    ** Fields
    *********/
    /// <summary>The commands registered with SMAPI.</summary>
    private readonly IDictionary<string, Command> Commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Writes messages to the console.</summary>
    private readonly IMonitor Monitor;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitor">Writes messages to the console.</param>
    public CommandManager(IMonitor monitor)
    {
        this.Monitor = monitor;
    }

    /// <summary>Add a console command.</summary>
    /// <param name="mod">The mod adding the command (or <c>null</c> for a SMAPI command).</param>
    /// <param name="name">The command name, which the user must type to trigger it.</param>
    /// <param name="documentation">The human-readable documentation shown when the player runs the built-in 'help' command.</param>
    /// <param name="callback">The method to invoke when the command is triggered. This method is passed the command name and arguments submitted by the user.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> or <paramref name="callback"/> is null or empty.</exception>
    /// <exception cref="FormatException">The <paramref name="name"/> is not a valid format.</exception>
    /// <exception cref="ArgumentException">There's already a command with that name.</exception>
    public CommandManager Add(IModMetadata? mod, string name, string documentation, Action<string, string[]> callback)
    {
        name = this.GetNormalizedName(name)!; // null-checked below

        // validate format
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), "Can't register a command with no name.");
        if (name.Any(char.IsWhiteSpace))
            throw new FormatException($"Can't register the '{name}' command because the name can't contain whitespace.");
        if (callback == null)
            throw new ArgumentNullException(nameof(callback), $"Can't register the '{name}' command because without a callback.");

        // ensure uniqueness
        if (this.Commands.ContainsKey(name))
            throw new ArgumentException(nameof(callback), $"Can't register the '{name}' command because there's already a command with that name.");

        // add command
        this.Commands.Add(name, new Command(mod, name, documentation, callback));
        return this;
    }

    /// <summary>Add a console command.</summary>
    /// <param name="command">the SMAPI console command to add.</param>
    /// <param name="monitor">Writes messages to the console.</param>
    /// <exception cref="ArgumentException">There's already a command with that name.</exception>
    public CommandManager Add(IInternalCommand command, IMonitor monitor)
    {
        return this.Add(null, command.Name, command.Description, (_, args) => command.HandleCommand(args, monitor));
    }

    /// <summary>Get a command by its unique name.</summary>
    /// <param name="name">The command name.</param>
    /// <returns>Returns the matching command, or <c>null</c> if not found.</returns>
    public Command? Get(string? name)
    {
        name = this.GetNormalizedName(name)!;
        if (string.IsNullOrWhiteSpace(name))
            return null;

        this.Commands.TryGetValue(name, out Command? command);
        return command;
    }

    /// <summary>Get all registered commands.</summary>
    public IEnumerable<Command> GetAll()
    {
        return this.Commands
            .Values
            .OrderBy(p => p.Name);
    }

    /// <summary>Try to parse a raw line of user input into an executable command.</summary>
    /// <param name="input">The raw user input.</param>
    /// <param name="name">The parsed command name.</param>
    /// <param name="args">The parsed command arguments.</param>
    /// <param name="command">The command which can handle the input.</param>
    /// <param name="screenId">The screen ID on which to run the command.</param>
    /// <returns>Returns true if the input was successfully parsed and matched to a command; else false.</returns>
    public bool TryParse(string? input, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out string[]? args, [NotNullWhen(true)] out Command? command, out int screenId)
    {
        // ignore if blank
        if (string.IsNullOrWhiteSpace(input))
        {
            name = null;
            args = null;
            command = null;
            screenId = 0;
            return false;
        }

        // parse input
        args = this.ParseArgs(input);
        name = this.GetNormalizedName(args[0])!;
        args = args.Skip(1).ToArray();

        // get screen ID argument
        screenId = 0;
        for (int i = 0; i < args.Length; i++)
        {
            // consume arg & set screen ID
            if (this.TryParseScreenId(args[i], out int rawScreenId, out string? error))
            {
                args = args.Take(i).Concat(args.Skip(i + 1)).ToArray();
                screenId = rawScreenId;
                continue;
            }

            // invalid screen arg
            if (error != null)
            {
                this.Monitor.Log(error, LogLevel.Error);
                command = null;
                return false;
            }
        }

        // get command
        return this.Commands.TryGetValue(name, out command);
    }

    /// <summary>Trigger a command.</summary>
    /// <param name="name">The command name.</param>
    /// <param name="arguments">The command arguments.</param>
    /// <returns>Returns whether a matching command was triggered.</returns>
    public bool Trigger(string? name, string[] arguments)
    {
        // get normalized name
        name = this.GetNormalizedName(name)!;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // get command
        if (this.Commands.TryGetValue(name, out Command? command))
        {
            command.Callback.Invoke(name, arguments);
            return true;
        }

        return false;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Parse a string into command arguments.</summary>
    /// <param name="input">The string to parse.</param>
    private string[] ParseArgs(string input)
    {
        bool inQuotes = false;
        IList<string> args = new List<string>();
        StringBuilder currentArg = new();
        foreach (char ch in input)
        {
            if (ch == '"')
                inQuotes = !inQuotes;
            else if (!inQuotes && char.IsWhiteSpace(ch))
            {
                args.Add(currentArg.ToString());
                currentArg.Clear();
            }
            else
                currentArg.Append(ch);
        }

        args.Add(currentArg.ToString());

        return args.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
    }

    /// <summary>Try to parse a 'screen=X' command argument, which specifies the screen that should receive the command.</summary>
    /// <param name="arg">The raw argument to parse.</param>
    /// <param name="screen">The parsed screen ID, if any.</param>
    /// <param name="error">The error which indicates an invalid screen ID, if applicable.</param>
    /// <returns>Returns whether the screen ID was parsed successfully.</returns>
    private bool TryParseScreenId(string arg, out int screen, out string? error)
    {
        screen = -1;
        error = null;

        // skip non-screen arg
        if (!arg.StartsWith("screen="))
            return false;

        // get screen ID
        string rawScreen = arg.Substring("screen=".Length);
        if (!int.TryParse(rawScreen, out screen))
        {
            error = $"invalid screen ID format: {rawScreen}";
            return false;
        }

        // validate ID
        if (!Context.HasScreenId(screen))
        {
            error = $"there's no active screen with ID {screen}. Active screen IDs: {string.Join(", ", Context.ActiveScreenIds)}.";
            return false;
        }

        return true;
    }

    /// <summary>Get a normalized command name.</summary>
    /// <param name="name">The command name.</param>
    private string? GetNormalizedName(string? name)
    {
        name = name?.Trim().ToLower();
        return !string.IsNullOrWhiteSpace(name)
            ? name
            : null;
    }
}
