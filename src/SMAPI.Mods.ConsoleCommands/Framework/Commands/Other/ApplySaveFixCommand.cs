using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Other;

/// <summary>A command which runs one of the game's save migrations.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class ApplySaveFixCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public ApplySaveFixCommand()
        : base("apply_save_fix", "Apply one of the game's save migrations to the currently loaded save. WARNING: This may corrupt or make permanent changes to your save. DO NOT USE THIS unless you're absolutely sure.\n\nUsage: apply_save_fix list\nList all valid save IDs.\n\nUsage: apply_save_fix <fix ID>\nApply the named save fix.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // get fix ID
        if (!args.TryGet(0, "fix_id", out string? rawFixId, required: false))
        {
            monitor.Log("Invalid usage. Type 'help apply_save_fix' for details.", LogLevel.Error);
            return;
        }
        rawFixId = rawFixId.Trim();


        // list mode
        if (rawFixId == "list")
        {
            monitor.Log("Valid save fix IDs:\n  - " + string.Join("\n  - ", this.GetSaveIds()), LogLevel.Info);
            return;
        }

        // validate fix ID
        if (!Enum.TryParse(rawFixId, ignoreCase: true, out SaveGame.SaveFixes fixId))
        {
            monitor.Log($"Invalid save ID '{rawFixId}'. Type 'help apply_save_fix' for details.", LogLevel.Error);
            return;
        }

        // apply
        monitor.Log("THIS MAY CAUSE PERMANENT CHANGES TO YOUR SAVE FILE. If you're not sure, exit your game without saving to avoid issues.", LogLevel.Warn);
        monitor.Log($"Trying to apply save fix ID: '{fixId}'.", LogLevel.Warn);
        try
        {
            Game1.applySaveFix(fixId);
            monitor.Log("Save fix applied.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            monitor.Log("Applying save fix failed. The save may be in an invalid state; you should exit your game now without saving to avoid issues.", LogLevel.Error);
            monitor.Log($"Technical details: {ex}", LogLevel.Debug);
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the valid save fix IDs.</summary>
    private IEnumerable<string> GetSaveIds()
    {
        foreach (SaveGame.SaveFixes id in Enum.GetValues(typeof(SaveGame.SaveFixes)))
        {
            if (id == SaveGame.SaveFixes.MAX)
                continue;

            yield return id.ToString();
        }
    }
}
