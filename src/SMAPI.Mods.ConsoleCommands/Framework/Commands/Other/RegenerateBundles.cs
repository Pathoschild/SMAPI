using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Netcode;
using StardewValley;
using StardewValley.Network;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Other;

/// <summary>A command which regenerates the game's bundles.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class RegenerateBundlesCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public RegenerateBundlesCommand()
        : base("regenerate_bundles", $"Regenerate the game's community center bundle data. WARNING: this will reset all bundle progress, and may have unintended effects if you've already completed bundles. DO NOT USE THIS unless you're absolutely sure.\n\nUsage: regenerate_bundles confirm [<type>] [ignore_seed]\nRegenerate all bundles for this save. If the <type> is set to '{string.Join("' or '", Enum.GetNames(typeof(Game1.BundleType)))}', change the bundle type for the save. If an 'ignore_seed' option is included, remixed bundles are re-randomized without using the predetermined save seed.\n\nExample: regenerate_bundles remixed confirm") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // get flags
        var bundleType = Game1.bundleType;
        bool confirmed = false;
        bool useSeed = true;
        foreach (string arg in args)
        {
            if (arg.Equals("confirm", StringComparison.OrdinalIgnoreCase))
                confirmed = true;
            else if (arg.Equals("ignore_seed", StringComparison.OrdinalIgnoreCase))
                useSeed = false;
            else if (Enum.TryParse(arg, ignoreCase: true, out Game1.BundleType type))
                bundleType = type;
            else
            {
                monitor.Log($"Invalid option '{arg}'. Type 'help {command}' for usage.", LogLevel.Error);
                return;
            }
        }

        // require confirmation
        if (!confirmed)
        {
            monitor.Log($"WARNING: this may have unintended consequences (type 'help {command}' for details). Are you sure?", LogLevel.Warn);

            string[] newArgs = args.Concat(new[] { "confirm" }).ToArray();
            monitor.Log($"To confirm, enter this command: '{command} {string.Join(" ", newArgs)}'.", LogLevel.Info);
            return;
        }

        // need a loaded save
        if (!Context.IsWorldReady)
        {
            monitor.Log("You need to load a save to use this command.", LogLevel.Error);
            return;
        }

        // get private fields
        IWorldState state = Game1.netWorldState.Value;
        var bundleData = state.GetType().GetField("_bundleData", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(state) as IDictionary<string, string>
                         ?? throw new InvalidOperationException("Can't access '_bundleData' field on world state.");
        var netBundleData = state.GetType().GetField("netBundleData", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)?.GetValue(state) as NetStringDictionary<string, NetString>
                            ?? throw new InvalidOperationException("Can't access 'netBundleData' field on world state.");

        // clear bundle data
        state.BundleData.Clear();
        state.Bundles.Clear();
        state.BundleRewards.Clear();
        bundleData.Clear();
        netBundleData.Clear();

        // regenerate bundles
        var locale = LocalizedContentManager.CurrentLanguageCode;
        try
        {
            LocalizedContentManager.CurrentLanguageCode = LocalizedContentManager.LanguageCode.en; // the base bundle data needs to be unlocalized (the game will add localized names later)

            Game1.bundleType = bundleType;
            Game1.GenerateBundles(bundleType, use_seed: useSeed);
        }
        finally
        {
            LocalizedContentManager.CurrentLanguageCode = locale;
        }

        monitor.Log("Regenerated bundles and reset bundle progress.", LogLevel.Info);
        monitor.Log("This may have unintended effects if you've already completed any bundles. If you're not sure, exit your game without saving to cancel.", LogLevel.Warn);
    }
}