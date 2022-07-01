using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace StardewModdingAPI.Framework.Deprecations;

/// <summary>Manages deprecation warnings.</summary>
internal class DeprecationManager
{
    /*********
    ** Fields
    *********/
    /// <summary>The deprecations which have already been logged (as 'mod name::noun phrase::version').</summary>
    private readonly HashSet<string> LoggedDeprecations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Encapsulates monitoring and logging for a given module.</summary>
    private readonly IMonitor Monitor;

    /// <summary>Tracks the installed mods.</summary>
    private readonly ModRegistry ModRegistry;

    /// <summary>The queued deprecation warnings to display.</summary>
    private readonly IList<DeprecationWarning> QueuedWarnings = new List<DeprecationWarning>();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitor">Encapsulates monitoring and logging for a given module.</param>
    /// <param name="modRegistry">Tracks the installed mods.</param>
    public DeprecationManager(IMonitor monitor, ModRegistry modRegistry)
    {
        this.Monitor = monitor;
        this.ModRegistry = modRegistry;
    }

    /// <summary>Get a mod from its unique ID.</summary>
    /// <param name="modId">The mod's unique ID.</param>
    public IModMetadata? GetMod(string modId)
    {
        return this.ModRegistry.Get(modId);
    }

    /// <summary>Log a deprecation warning.</summary>
    /// <param name="source">The mod which used the deprecated code, or <c>null</c> to get it heuristically. Note that getting it heuristically is very slow in some cases, and should be avoided if at all possible.</param>
    /// <param name="nounPhrase">A noun phrase describing what is deprecated.</param>
    /// <param name="version">The SMAPI version which deprecated it.</param>
    /// <param name="severity">How deprecated the code is.</param>
    /// <param name="unlessStackIncludes">A list of stack trace substrings which should suppress deprecation warnings if they appear in the stack trace.</param>
    /// <param name="logStackTrace">Whether to log a stack trace showing where the deprecated code is in the mod.</param>
    public void Warn(IModMetadata? source, string nounPhrase, string version, DeprecationLevel severity, string[]? unlessStackIncludes = null, bool logStackTrace = true)
    {
        // get heuristic source
        // The call stack is usually the most reliable way to get the source if it's unknown. This is *very* slow
        // though, especially before we check whether this is a duplicate warning. The initial cache check uses a
        // quick heuristic method if at all possible to avoid that.
        IModMetadata? heuristicSource = source;
        ImmutableStackTrace? stack = null;
        if (heuristicSource is null)
            Context.HeuristicModsRunningCode.TryPeek(out heuristicSource);
        if (heuristicSource is null)
        {
            stack = ImmutableStackTrace.Get(skipFrames: 1);
            heuristicSource = this.ModRegistry.GetFromStack(stack.GetFrames());
        }

        // skip if already warned
        string cacheKey = $"{heuristicSource?.Manifest.UniqueID ?? "<unknown>"}::{nounPhrase}::{version}";
        if (this.LoggedDeprecations.Contains(cacheKey))
            return;
        this.LoggedDeprecations.Add(cacheKey);

        // get more accurate source
        if (stack is not null)
            source ??= heuristicSource!;
        else
        {
            stack ??= ImmutableStackTrace.Get(skipFrames: 1);
            source ??= this.ModRegistry.GetFromStack(stack.GetFrames());
        }

        // log unless suppressed
        if (!this.ShouldSuppress(stack, unlessStackIncludes))
            this.QueuedWarnings.Add(new DeprecationWarning(source, nounPhrase, version, severity, stack, logStackTrace));
    }

    /// <summary>A placeholder method used to track deprecated code for which a separate warning will be shown.</summary>
    /// <param name="version">The SMAPI version which deprecated it.</param>
    /// <param name="severity">How deprecated the code is.</param>
    public void PlaceholderWarn(string version, DeprecationLevel severity) { }

    /// <summary>Print any queued messages.</summary>
    public void PrintQueued()
    {
        if (!this.QueuedWarnings.Any())
            return;

        foreach (DeprecationWarning warning in this.QueuedWarnings.OrderBy(p => p.ModName).ThenBy(p => p.NounPhrase))
        {
            // build message
            string message = $"{warning.ModName} uses deprecated code ({warning.NounPhrase} is deprecated since SMAPI {warning.Version}).";

            // get log level
            LogLevel level;
            switch (warning.Level)
            {
                case DeprecationLevel.Notice:
                    level = LogLevel.Trace;
                    break;

                case DeprecationLevel.Info:
                    level = LogLevel.Debug;
                    break;

                case DeprecationLevel.PendingRemoval:
                    level = LogLevel.Warn;
                    break;

                default:
                    throw new NotSupportedException($"Unknown deprecation level '{warning.Level}'.");
            }

            // log message
            if (level == LogLevel.Trace)
            {
                if (warning.LogStackTrace)
                    message += $"\n{this.GetSimplifiedStackTrace(warning.StackTrace, warning.Mod)}";
                this.Monitor.Log(message, level);
            }
            else
            {
                this.Monitor.Log(message, level);
                if (warning.LogStackTrace)
                    this.Monitor.Log(this.GetSimplifiedStackTrace(warning.StackTrace, warning.Mod), LogLevel.Debug);
            }
        }

        this.QueuedWarnings.Clear();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get whether a deprecation warning should be suppressed.</summary>
    /// <param name="stack">The stack trace for which it was raised.</param>
    /// <param name="unlessStackIncludes">A list of stack trace substrings which should suppress deprecation warnings if they appear in the stack trace.</param>
    private bool ShouldSuppress(ImmutableStackTrace stack, string[]? unlessStackIncludes)
    {
        if (unlessStackIncludes?.Any() == true)
        {
            string stackTrace = stack.ToString();
            foreach (string method in unlessStackIncludes)
            {
                if (stackTrace.Contains(method))
                    return true;
            }
        }

        return false;
    }

    /// <summary>Get the simplest stack trace which shows where in the mod the deprecated code was called from.</summary>
    /// <param name="stack">The stack trace.</param>
    /// <param name="mod">The mod for which to show a stack trace.</param>
    private string GetSimplifiedStackTrace(ImmutableStackTrace stack, IModMetadata? mod)
    {
        // unknown mod, show entire stack trace
        if (mod == null)
            return stack.ToString();

        // get frame info
        var frames = stack
            .GetFrames()
            .Select(frame => (Frame: frame, Mod: this.ModRegistry.GetFrom(frame)))
            .ToArray();
        var modIds = new HashSet<string>(
            from frame in frames
            let id = frame.Mod?.Manifest.UniqueID
            where id != null
            select id
        );

        // can't filter to the target mod
        if (modIds.Count != 1 || !modIds.Contains(mod.Manifest.UniqueID))
            return stack.ToString();

        // get stack frames for the target mod, plus one for context
        var framesStartingAtMod = frames.SkipWhile(p => p.Mod == null).ToArray();
        var displayFrames = framesStartingAtMod.TakeWhile(p => p.Mod != null).ToArray();
        displayFrames = displayFrames.Concat(framesStartingAtMod.Skip(displayFrames.Length).Take(1)).ToArray();

        // build stack trace
        StringBuilder str = new();
        foreach (var frame in displayFrames)
            str.Append(new StackTrace(frame.Frame));
        return str.ToString().TrimEnd();
    }
}
