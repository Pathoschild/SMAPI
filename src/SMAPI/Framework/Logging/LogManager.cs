using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using StardewModdingAPI.Framework.Commands;
using StardewModdingAPI.Framework.Models;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;

namespace StardewModdingAPI.Framework.Logging
{
    /// <summary>Manages the SMAPI console window and log file.</summary>
    internal class LogManager : IDisposable
    {
        /*********
        ** Fields
        *********/
        /// <summary>The log file to which to write messages.</summary>
        private readonly LogFileManager LogFile;

        /// <summary>Create a monitor instance given the ID and name.</summary>
        private readonly Func<string, string, Monitor> GetMonitorImpl;


        /*********
        ** Accessors
        *********/
        /// <summary>The core logger and monitor for SMAPI.</summary>
        public Monitor Monitor { get; }

        /// <summary>The core logger and monitor on behalf of the game.</summary>
        public Monitor MonitorForGame { get; }


        /*********
        ** Public methods
        *********/
        /****
        ** Initialization
        ****/
        /// <summary>Construct an instance.</summary>
        /// <param name="logPath">The log file path to write.</param>
        /// <param name="colorConfig">The colors to use for text written to the SMAPI console.</param>
        /// <param name="writeToConsole">Whether to output log messages to the console.</param>
        /// <param name="verboseLogging">The log contexts for which to enable verbose logging, which may show a lot more information to simplify troubleshooting.</param>
        /// <param name="isDeveloperMode">Whether to enable full console output for developers.</param>
        /// <param name="getScreenIdForLog">Get the screen ID that should be logged to distinguish between players in split-screen mode, if any.</param>
        public LogManager(string logPath, ColorSchemeConfig colorConfig, bool writeToConsole, HashSet<string> verboseLogging, bool isDeveloperMode, Func<int?> getScreenIdForLog)
        {
            // init log file
            this.LogFile = new LogFileManager(logPath);

            // init monitor
            this.GetMonitorImpl = (id, name) => new Monitor(name, this.LogFile, colorConfig, verboseLogging.Contains("*") || verboseLogging.Contains(id), getScreenIdForLog)
            {
                WriteToConsole = writeToConsole,
                ShowTraceInConsole = isDeveloperMode,
                ShowFullStampInConsole = isDeveloperMode
            };
            this.Monitor = this.GetMonitor("SMAPI", "SMAPI");
            this.MonitorForGame = this.GetMonitor("game", "game");

            // enable Unicode handling on Windows
            // (the terminal defaults to UTF-8 on Linux/macOS)
#if SMAPI_FOR_WINDOWS
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
#endif
        }

        /// <summary>Get a monitor instance derived from SMAPI's current settings.</summary>
        /// <param name="id">The unique ID for the mod context.</param>
        /// <param name="name">The name of the module which will log messages with this instance.</param>
        public Monitor GetMonitor(string id, string name)
        {
            return this.GetMonitorImpl(id, name);
        }

        /// <summary>Set the title of the SMAPI console window.</summary>
        /// <param name="title">The new window title.</param>
        public void SetConsoleTitle(string title)
        {
            Console.Title = title;
        }

        /****
        ** Console input
        ****/
        /// <summary>Run a loop handling console input.</summary>
        [SuppressMessage("ReSharper", "FunctionNeverReturns", Justification = "The thread is aborted when the game exits.")]
        public void RunConsoleInputLoop(CommandManager commandManager, Action reloadTranslations, Action<string> handleInput, Func<bool> continueWhile)
        {
            // prepare console
            this.Monitor.Log("Type 'help' for help, or 'help <cmd>' for a command's usage", LogLevel.Info);
            commandManager
                .Add(new HelpCommand(commandManager), this.Monitor)
                .Add(new HarmonySummaryCommand(), this.Monitor)
                .Add(new ReloadI18nCommand(reloadTranslations), this.Monitor);

            // start handling command line input
            Thread inputThread = new(() =>
            {
                while (true)
                {
                    // get input
                    string? input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    // handle command
                    this.Monitor.LogUserInput(input);
                    handleInput(input);
                }
            });
            inputThread.Start();

            // keep console thread alive while the game is running
            while (continueWhile())
                Thread.Sleep(1000 / 10);
        }

        /// <summary>Show a 'press any key to exit' message, and exit when they press a key.</summary>
        public void PressAnyKeyToExit()
        {
            this.Monitor.Log("Game has ended. Press any key to exit.", LogLevel.Info);
            this.PressAnyKeyToExit(showMessage: false);
        }

        /// <summary>Show a 'press any key to exit' message, and exit when they press a key.</summary>
        /// <param name="showMessage">Whether to print a 'press any key to exit' message to the console.</param>
        public void PressAnyKeyToExit(bool showMessage)
        {
            if (showMessage)
                this.Monitor.Log("Game has ended. Press any key to exit.");
            Thread.Sleep(100);
            Console.ReadKey();
            Environment.Exit(0);
        }

        /****
        ** Crash/update handling
        ****/
        /// <summary>Create a crash marker and duplicate the log into the crash log.</summary>
        public void WriteCrashLog()
        {
            try
            {
                File.WriteAllText(Constants.FatalCrashMarker, string.Empty);
                File.Copy(this.LogFile.Path, Constants.FatalCrashLog, overwrite: true);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"SMAPI failed trying to track the crash details: {ex.GetLogSummary()}", LogLevel.Error);
            }
        }

        /// <summary>Write an update alert marker file.</summary>
        /// <param name="version">The new version found.</param>
        /// <param name="url">The download URL for the update.</param>
        public void WriteUpdateMarker(string version, string url)
        {
            File.WriteAllText(Constants.UpdateMarker, $"{version}|{url}");
        }

        /// <summary>Check whether SMAPI crashed or detected an update during the last session, and display them in the SMAPI console.</summary>
        public void HandleMarkerFiles()
        {
            // show update alert
            if (File.Exists(Constants.UpdateMarker))
            {
                string[] rawUpdateFound = File.ReadAllText(Constants.UpdateMarker).Split('|', 2);
                if (SemanticVersion.TryParse(rawUpdateFound[0], out ISemanticVersion? updateFound))
                {
                    if (Constants.ApiVersion.IsPrerelease() && updateFound.IsNewerThan(Constants.ApiVersion))
                    {
                        string url = rawUpdateFound.Length > 1
                            ? rawUpdateFound[1]
                            : Constants.HomePageUrl;

                        this.Monitor.Log("A new version of SMAPI was detected last time you played.", LogLevel.Error);
                        this.Monitor.Log($"You can update to {updateFound}: {url}.", LogLevel.Error);
                        this.Monitor.Log("Press any key to continue playing anyway. (This only appears when using a SMAPI beta.)", LogLevel.Info);
                        Console.ReadKey();
                    }
                }
                File.Delete(Constants.UpdateMarker);
            }

            // show details if game crashed during last session
            if (File.Exists(Constants.FatalCrashMarker))
            {
                this.Monitor.Log("The game crashed last time you played. If it happens repeatedly, see 'get help' on https://smapi.io.", LogLevel.Error);
                this.Monitor.Log("If you ask for help, make sure to share your SMAPI log: https://smapi.io/log.", LogLevel.Error);
                this.Monitor.Log("Press any key to delete the crash data and continue playing.", LogLevel.Info);
                Console.ReadKey();
                File.Delete(Constants.FatalCrashLog);
                File.Delete(Constants.FatalCrashMarker);
            }
        }

        /// <summary>Log a fatal exception which prevents SMAPI from launching.</summary>
        /// <param name="exception">The exception details.</param>
        public void LogFatalLaunchError(Exception exception)
        {
            this.MonitorForGame.Log($"The game failed to launch: {exception.GetLogSummary()}", LogLevel.Error);
        }

        /****
        ** General log output
        ****/
        /// <summary>Log the initial header with general SMAPI and system details.</summary>
        /// <param name="modsPath">The path from which mods will be loaded.</param>
        /// <param name="customSettings">The custom SMAPI settings.</param>
        public void LogIntro(string modsPath, IDictionary<string, object?> customSettings)
        {
            // log platform
            this.Monitor.Log($"SMAPI {Constants.ApiVersion} with Stardew Valley {Game1.GetVersionString()} on {EnvironmentUtility.GetFriendlyPlatformName(Constants.Platform)}", LogLevel.Info);

            // log basic info
            this.Monitor.Log($"Mods go here: {modsPath}", LogLevel.Info);
            if (modsPath != Constants.DefaultModsPath)
                this.Monitor.Log($"(Using custom --mods-path argument. Game folder: {PathUtilities.SanitisePath(Constants.GamePath)}.)");
            this.Monitor.Log($"Log started at {DateTime.UtcNow:s} UTC");

            // log custom settings
            if (customSettings.Any())
                this.Monitor.Log($"Loaded with custom settings: {string.Join(", ", customSettings.OrderBy(p => p.Key).Select(p => $"{p.Key}: {p.Value}"))}");
        }

        /// <summary>Log details for settings that don't match the default.</summary>
        /// <param name="settings">The settings to log.</param>
        public void LogSettingsHeader(SConfig settings)
        {
            // developer mode
            if (settings.DeveloperMode)
                this.Monitor.Log("You enabled developer mode, so the console will be much more verbose. You can disable it by installing the non-developer version of SMAPI.", LogLevel.Info);

            // warnings
            if (!settings.CheckForUpdates)
                this.Monitor.Log("You disabled update checks, so you won't be notified of new SMAPI or mod updates. Running an old version of SMAPI is not recommended. You can undo this by reinstalling SMAPI.", LogLevel.Warn);
            if (!settings.RewriteMods)
                this.Monitor.Log("You disabled rewriting broken mods, so many older mods may fail to load. You can undo this by reinstalling SMAPI.", LogLevel.Info);
            if (!this.Monitor.WriteToConsole)
                this.Monitor.Log("Writing to the terminal is disabled because the --no-terminal argument was received. This usually means launching the terminal failed.", LogLevel.Warn);

            // verbose logging
            this.Monitor.VerboseLog("Verbose logging enabled.");
        }

        /// <summary>Log info about loaded mods.</summary>
        /// <param name="loaded">The full list of loaded content packs and mods.</param>
        /// <param name="loadedContentPacks">The loaded content packs.</param>
        /// <param name="loadedMods">The loaded mods.</param>
        /// <param name="skippedMods">The mods which could not be loaded.</param>
        /// <param name="logParanoidWarnings">Whether to log issues for mods which directly use potentially sensitive .NET APIs like file or shell access.</param>
        /// <param name="logTechnicalDetailsForBrokenMods">Whether to include more technical details about broken mods in the TRACE logs. This is mainly useful for creating compatibility rewriters.</param>
        /// <param name="hasHarmonyFix">Whether Harmony was fixed to work with Stardew Valley.</param>
        public void LogModInfo(IModMetadata[] loaded, IModMetadata[] loadedContentPacks, IModMetadata[] loadedMods, IModMetadata[] skippedMods, bool logParanoidWarnings, bool logTechnicalDetailsForBrokenMods, bool hasHarmonyFix)
        {
            // log loaded mods
            this.Monitor.Log($"Loaded {loadedMods.Length} mods" + (loadedMods.Length > 0 ? ":" : "."), LogLevel.Info);
            foreach (IModMetadata metadata in loadedMods.OrderBy(p => p.DisplayName))
            {
                IManifest manifest = metadata.Manifest;
                this.Monitor.Log(
                    $"   {metadata.DisplayName} {manifest.Version}"
                    + (!string.IsNullOrWhiteSpace(manifest.Author) ? $" by {manifest.Author}" : "")
                    + (!string.IsNullOrWhiteSpace(manifest.Description) ? $" | {manifest.Description}" : ""),
                    LogLevel.Info
                );
            }

            this.Monitor.Newline();

            // log loaded content packs
            if (loadedContentPacks.Any())
            {
                string? GetModDisplayName(string id) => loadedMods.FirstOrDefault(p => p.HasID(id))?.DisplayName;

                this.Monitor.Log($"Loaded {loadedContentPacks.Length} content packs:", LogLevel.Info);
                foreach (IModMetadata metadata in loadedContentPacks.OrderBy(p => p.DisplayName))
                {
                    IManifest manifest = metadata.Manifest;
                    this.Monitor.Log(
                        $"   {metadata.DisplayName} {manifest.Version}"
                        + (!string.IsNullOrWhiteSpace(manifest.Author) ? $" by {manifest.Author}" : "")
                        + $" | for {GetModDisplayName(metadata.Manifest.ContentPackFor!.UniqueID)}"
                        + (!string.IsNullOrWhiteSpace(manifest.Description) ? $" | {manifest.Description}" : ""),
                        LogLevel.Info
                    );
                }

                this.Monitor.Newline();
            }

            // log mod warnings
            this.LogModWarnings(loaded, skippedMods, logParanoidWarnings, logTechnicalDetailsForBrokenMods, hasHarmonyFix);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LogFile.Dispose();
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Write a summary of mod warnings to the console and log.</summary>
        /// <param name="mods">The loaded mods.</param>
        /// <param name="skippedMods">The mods which could not be loaded.</param>
        /// <param name="logParanoidWarnings">Whether to log issues for mods which directly use potentially sensitive .NET APIs like file or shell access.</param>
        /// <param name="logTechnicalDetailsForBrokenMods">Whether to include more technical details about broken mods in the TRACE logs. This is mainly useful for creating compatibility rewriters.</param>
        /// <param name="hasHarmonyFix">Whether Harmony was fixed to work with Stardew Valley.</param>
        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "Manifests aren't guaranteed non-null at this point in the loading process.")]
        private void LogModWarnings(IEnumerable<IModMetadata> mods, IModMetadata[] skippedMods, bool logParanoidWarnings, bool logTechnicalDetailsForBrokenMods, bool hasHarmonyFix)
        {
            // get mods with warnings
            IModMetadata[] modsWithWarnings = mods
                .Where(p =>
                    (
                        logParanoidWarnings
                            ? p.Warnings
                            : p.Warnings & ~ModWarning.AccessesFilesystem & ~ModWarning.AccessesShell
                    ) != ModWarning.None
                )
                .ToArray();
            if (!modsWithWarnings.Any() && !skippedMods.Any())
                return;

            // log intro
            {
                int count = modsWithWarnings.Length + skippedMods.Length;
                this.Monitor.Log($"Found {count} mod{(count == 1 ? "" : "s")} with warnings:", LogLevel.Info);
            }

            // log skipped mods
            if (skippedMods.Any())
            {
                var loggedDuplicateIds = new HashSet<string>();

                this.Monitor.Log("   Skipped mods", LogLevel.Error);
                this.Monitor.Log("   " + "".PadRight(50, '-'), LogLevel.Error);
                this.Monitor.Log("      These mods could not be added to your game.", LogLevel.Error);
                this.Monitor.Newline();
                foreach (var list in this.GroupFailedModsByPriority(skippedMods))
                {
                    if (list.Any())
                    {
                        foreach (IModMetadata mod in list.OrderBy(p => p.DisplayName))
                        {
                            string technicalInfo = logTechnicalDetailsForBrokenMods
                                ? $" (ID: {mod.Manifest?.UniqueID ?? "???"}, path: {mod.RelativeDirectoryPath})"
                                : "";

                            string message = $"      - {mod.DisplayName}{(" " + mod.Manifest?.Version?.ToString()).TrimEnd()}{technicalInfo} because {mod.Error}";

                            // duplicate mod: log first one only, don't show redundant version
                            if (mod.FailReason == ModFailReason.Duplicate && mod.HasManifest())
                            {
                                if (loggedDuplicateIds.Add(mod.Manifest!.UniqueID))
                                    continue; // already logged

                                message = $"      - {mod.DisplayName} because {mod.Error}";
                            }

                            // log message
                            this.Monitor.Log(message, LogLevel.Error);
                            if (mod.ErrorDetails != null)
                                this.Monitor.Log($"        ({mod.ErrorDetails})");
                        }

                        this.Monitor.Newline();
                    }
                }
            }

            // log warnings
            if (modsWithWarnings.Any())
            {
                // broken code
                this.LogModWarningGroup(modsWithWarnings, ModWarning.BrokenCodeLoaded, LogLevel.Error, "Broken mods",
                    "These mods have broken code, but you configured SMAPI to load them anyway. This may cause bugs,",
                    "errors, or crashes in-game."
                );

                // missing Harmony fix
                if (!hasHarmonyFix)
                {
                    this.LogModWarningGroup(modsWithWarnings, ModWarning.PatchesGame, LogLevel.Warn, "Patched game code without Harmony fix",
                        $"These mods directly change the game code using Harmony, but you disabled the {nameof(SConfig.FixHarmony)} option.",
                        "The game will probably crash soon."
                    );
                }

                // changes serializer
                this.LogModWarningGroup(modsWithWarnings, ModWarning.ChangesSaveSerializer, LogLevel.Warn, "Changed save serializer",
                    "These mods change the save serializer. They may corrupt your save files, or make them unusable if",
                    "you uninstall these mods."
                );

                // patched game code
                if (hasHarmonyFix)
                {
                    this.LogModWarningGroup(modsWithWarnings, ModWarning.PatchesGame, LogLevel.Info, "Patched game code",
                        "These mods directly change the game code. They're more likely to cause errors or bugs in-game; if",
                        "your game has issues, try removing these first. Otherwise you can ignore this warning."
                    );
                }

                // unvalidated update tick
                this.LogModWarningGroup(modsWithWarnings, ModWarning.UsesUnvalidatedUpdateTick, LogLevel.Info, "Bypassed safety checks",
                    "These mods bypass SMAPI's normal safety checks, so they're more likely to cause errors or save",
                    "corruption. If your game has issues, try removing these first."
                );

                // direct console access
                this.LogModWarningGroup(modsWithWarnings, ModWarning.UsesUnvalidatedUpdateTick, LogLevel.Trace, "Direct console access",
                    "These mods access the SMAPI console window directly. This is more fragile, and their output may not",
                    "be logged by SMAPI."
                );

                // paranoid warnings
                if (logParanoidWarnings)
                {
                    this.LogModWarningGroup(
                        modsWithWarnings,
                        match: mod => mod.HasWarnings(ModWarning.AccessesFilesystem, ModWarning.AccessesShell),
                        level: LogLevel.Debug,
                        heading: "Direct system access",
                        blurb: new[]
                        {
                            "You enabled paranoid warnings and these mods directly access the filesystem, shells/processes, or",
                            "SMAPI console. (This is usually legitimate and innocent usage; this warning is only useful for",
                            "further investigation.)"
                        },
                        modLabel: mod =>
                        {
                            List<string> labels = new List<string>();
                            if (mod.HasWarnings(ModWarning.AccessesFilesystem))
                                labels.Add("files");
                            if (mod.HasWarnings(ModWarning.AccessesShell))
                                labels.Add("shells/processes");

                            return $"{mod.DisplayName} ({string.Join(", ", labels)})";
                        }
                    );
                }

                // no update keys
                this.LogModWarningGroup(modsWithWarnings, ModWarning.NoUpdateKeys, LogLevel.Debug, "No update keys",
                    "These mods have no update keys in their manifest. SMAPI may not notify you about updates for these",
                    "mods. Consider notifying the mod authors about this problem."
                );
            }
        }

        /// <summary>Group failed mods by the priority players should update them, where mods in earlier groups are more likely to fix multiple mods.</summary>
        /// <param name="failedMods">The failed mods to group.</param>
        private IEnumerable<IList<IModMetadata>> GroupFailedModsByPriority(IList<IModMetadata> failedMods)
        {
            var failedOthers = failedMods.ToList();
            var skippedModIds = new HashSet<string>(from mod in failedMods where mod.HasID() select mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);

            // group B: dependencies which failed
            var failedOtherDependencies = new List<IModMetadata>();
            {
                // get failed dependency IDs
                var skippedDependencyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (IModMetadata mod in failedMods)
                {
                    foreach (string requiredId in skippedModIds.Intersect(mod.GetRequiredModIds()))
                        skippedDependencyIds.Add(requiredId);
                }

                // group matching mods
                this.FilterThrough(
                    fromList: failedOthers,
                    toList: failedOtherDependencies,
                    match: mod => mod.HasID() && skippedDependencyIds.Contains(mod.Manifest.UniqueID)
                );
            }

            // group A: failed root dependencies which other dependencies need
            var failedRootDependencies = new List<IModMetadata>();
            {
                var skippedDependencyIds = new HashSet<string>(failedOtherDependencies.Select(p => p.Manifest.UniqueID));
                this.FilterThrough(
                    fromList: failedOtherDependencies,
                    toList: failedRootDependencies,
                    match: mod =>
                    {
                        // has no failed dependency
                        foreach (string requiredId in mod.GetRequiredModIds())
                        {
                            if (skippedDependencyIds.Contains(requiredId))
                                return false;
                        }

                        // another dependency depends on this mod
                        bool isDependedOn = false;
                        foreach (IModMetadata other in failedOtherDependencies)
                        {
                            if (other.HasRequiredModId(mod.Manifest.UniqueID, includeOptional: false))
                            {
                                isDependedOn = true;
                                break;
                            }
                        }

                        return isDependedOn;
                    }
                );
            }

            // return groups
            return new[]
            {
                failedRootDependencies,
                failedOtherDependencies,
                failedOthers
            };
        }

        /// <summary>Filter matching items from one list and add them to the other.</summary>
        /// <typeparam name="TItem">The list item type.</typeparam>
        /// <param name="fromList">The list to filter.</param>
        /// <param name="toList">The list to which to add filtered items.</param>
        /// <param name="match">Matches items to filter through.</param>
        private void FilterThrough<TItem>(IList<TItem> fromList, IList<TItem> toList, Func<TItem, bool> match)
        {
            for (int i = 0; i < fromList.Count; i++)
            {
                TItem item = fromList[i];
                if (match(item))
                {
                    toList.Add(item);
                    fromList.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>Write a mod warning group to the console and log.</summary>
        /// <param name="mods">The mods to search.</param>
        /// <param name="match">Matches mods to include in the warning group.</param>
        /// <param name="level">The log level for the logged messages.</param>
        /// <param name="heading">A brief heading label for the group.</param>
        /// <param name="blurb">A detailed explanation of the warning, split into lines.</param>
        /// <param name="modLabel">Formats the mod label, or <c>null</c> to use the <see cref="IModMetadata.DisplayName"/>.</param>
        private void LogModWarningGroup(IModMetadata[] mods, Func<IModMetadata, bool> match, LogLevel level, string heading, string[] blurb, Func<IModMetadata, string>? modLabel = null)
        {
            // get matching mods
            string[] modLabels = mods
                .Where(match)
                .Select(mod => modLabel?.Invoke(mod) ?? mod.DisplayName)
                .OrderBy(p => p)
                .ToArray();
            if (!modLabels.Any())
                return;

            // log header/blurb
            this.Monitor.Log("   " + heading, level);
            this.Monitor.Log("   " + "".PadRight(50, '-'), level);
            foreach (string line in blurb)
                this.Monitor.Log("      " + line, level);
            this.Monitor.Newline();

            // log mod list
            foreach (string label in modLabels)
                this.Monitor.Log($"      - {label}", level);

            this.Monitor.Newline();
        }

        /// <summary>Write a mod warning group to the console and log.</summary>
        /// <param name="mods">The mods to search.</param>
        /// <param name="warning">The mod warning to match.</param>
        /// <param name="level">The log level for the logged messages.</param>
        /// <param name="heading">A brief heading label for the group.</param>
        /// <param name="blurb">A detailed explanation of the warning, split into lines.</param>
        private void LogModWarningGroup(IModMetadata[] mods, ModWarning warning, LogLevel level, string heading, params string[] blurb)
        {
            this.LogModWarningGroup(mods, mod => mod.HasWarnings(warning), level, heading, blurb);
        }
    }
}
