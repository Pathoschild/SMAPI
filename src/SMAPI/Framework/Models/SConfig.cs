using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework.Models
{
    /// <summary>The SMAPI configuration settings.</summary>
    internal class SConfig
    {
        /********
        ** Fields
        ********/
        /// <summary>The default config values, for fields that should be logged if different.</summary>
        private static readonly IDictionary<string, object> DefaultValues = new Dictionary<string, object>
        {
            [nameof(CheckForUpdates)] = true,
            [nameof(ListenForConsoleInput)] = true,
            [nameof(LegacyConsoleMode)] = false,
            [nameof(ParanoidWarnings)] = Constants.IsDebugBuild,
            [nameof(UseBetaChannel)] = Constants.ApiVersion.IsPrerelease(),
            [nameof(GitHubProjectName)] = "Pathoschild/SMAPI",
            [nameof(WebApiBaseUrl)] = "https://smapi.io/api/",
            [nameof(LogNetworkTraffic)] = false,
            [nameof(LogTechnicalDetailsForBrokenMods)] = false,
            [nameof(RewriteMods)] = true,
            [nameof(FixHarmony)] = true,
            [nameof(UseCaseInsensitivePaths)] = Constants.Platform is Platform.Android or Platform.Linux,
            [nameof(SuppressHarmonyDebugMode)] = true
        };

        /// <summary>The default values for <see cref="SuppressUpdateChecks"/>, to log changes if different.</summary>
        private static readonly HashSet<string> DefaultSuppressUpdateChecks = new(StringComparer.OrdinalIgnoreCase)
        {
            "SMAPI.ConsoleCommands",
            "SMAPI.SaveBackup"
        };


        /********
        ** Accessors
        ********/
        //
        // Note: properties must be writable to support merging config.user.json into it.
        //

        /// <summary>Whether to enable development features.</summary>
        public bool DeveloperMode { get; set; }

        /// <summary>Whether to check for newer versions of SMAPI and mods on startup.</summary>
        public bool CheckForUpdates { get; set; }

        /// <summary>Whether SMAPI should listen for console input to support console commands.</summary>
        public bool ListenForConsoleInput { get; set; }

        /// <summary>Whether SMAPI should use legacy console mode. This will prevent tab auto completion from working, and will not keep input at the bottom of the console.</summary>
        public bool LegacyConsoleMode { get; set; }

        /// <summary>Whether to add a section to the 'mod issues' list for mods which which directly use potentially sensitive .NET APIs like file or shell access.</summary>
        public bool ParanoidWarnings { get; set; }

        /// <summary>Whether to show beta versions as valid updates.</summary>
        public bool UseBetaChannel { get; set; }

        /// <summary>SMAPI's GitHub project name, used to perform update checks.</summary>
        public string GitHubProjectName { get; set; }

        /// <summary>The base URL for SMAPI's web API, used to perform update checks.</summary>
        public string WebApiBaseUrl { get; set; }

        /// <summary>The log contexts for which to enable verbose logging, which may show a lot more information to simplify troubleshooting.</summary>
        /// <remarks>The possible values are "*" (everything is verbose), "SMAPI", (SMAPI itself), or mod IDs.</remarks>
        public HashSet<string> VerboseLogging { get; set; }

        /// <summary>Whether SMAPI should rewrite mods for compatibility.</summary>
        public bool RewriteMods { get; set; }

        /// <summary>Whether to apply fixes to Harmony so it works with Stardew Valley.</summary>
        public bool FixHarmony { get; set; }

        /// <summary>Whether to make SMAPI file APIs case-insensitive, even on Linux.</summary>
        public bool UseCaseInsensitivePaths { get; set; }

        /// <summary>Whether SMAPI should log network traffic. Best combined with <see cref="VerboseLogging"/>, which includes network metadata.</summary>
        public bool LogNetworkTraffic { get; set; }

        /// <summary>Whether to include more technical details about broken mods in the TRACE logs. This is mainly useful for creating compatibility rewriters.</summary>
        public bool LogTechnicalDetailsForBrokenMods { get; set; }

        /// <summary>The colors to use for text written to the SMAPI console.</summary>
        public ColorSchemeConfig ConsoleColors { get; set; }

        /// <summary>Whether to prevent mods from enabling Harmony's debug mode, which impacts performance and creates a file on your desktop. Debug mode should never be enabled by a released mod.</summary>
        public bool SuppressHarmonyDebugMode { get; set; }

        /// <summary>The mod IDs SMAPI should ignore when performing update checks or validating update keys.</summary>
        public HashSet<string> SuppressUpdateChecks { get; set; }

        /// <summary>The mod IDs SMAPI should load before any other mods (except those needed to load them).</summary>
        public HashSet<string> ModsToLoadEarly { get; set; }

        /// <summary>The mod IDs SMAPI should load after any other mods.</summary>
        public HashSet<string> ModsToLoadLate { get; set; }


        /********
        ** Public methods
        ********/
        /// <summary>Construct an instance.</summary>
        /// <param name="developerMode"><inheritdoc cref="DeveloperMode" path="/summary" /></param>
        /// <param name="checkForUpdates"><inheritdoc cref="CheckForUpdates" path="/summary" /></param>
        /// <param name="listenForConsoleInput"><inheritdoc cref="ListenForConsoleInput" path="/summary" /></param>
        /// <param name="legacyConsoleMode"><inheritdoc cref="LegacyConsoleMode" path="/summary" /></param>
        /// <param name="paranoidWarnings"><inheritdoc cref="ParanoidWarnings" path="/summary" /></param>
        /// <param name="useBetaChannel"><inheritdoc cref="UseBetaChannel" path="/summary" /></param>
        /// <param name="gitHubProjectName"><inheritdoc cref="GitHubProjectName" path="/summary" /></param>
        /// <param name="webApiBaseUrl"><inheritdoc cref="WebApiBaseUrl" path="/summary" /></param>
        /// <param name="verboseLogging"><inheritdoc cref="VerboseLogging" path="/summary" /></param>
        /// <param name="rewriteMods"><inheritdoc cref="RewriteMods" path="/summary" /></param>
        /// <param name="fixHarmony"><inheritdoc cref="FixHarmony" path="/summary" /></param>
        /// <param name="useCaseInsensitivePaths"><inheritdoc cref="UseCaseInsensitivePaths" path="/summary" /></param>
        /// <param name="logNetworkTraffic"><inheritdoc cref="LogNetworkTraffic" path="/summary" /></param>
        /// <param name="logTechnicalDetailsForBrokenMods"><inheritdoc cref="LogTechnicalDetailsForBrokenMods" path="/summary" /></param>
        /// <param name="consoleColors"><inheritdoc cref="ConsoleColors" path="/summary" /></param>
        /// <param name="suppressHarmonyDebugMode"><inheritdoc cref="SuppressHarmonyDebugMode" path="/summary" /></param>
        /// <param name="suppressUpdateChecks"><inheritdoc cref="SuppressUpdateChecks" path="/summary" /></param>
        /// <param name="modsToLoadEarly"><inheritdoc cref="ModsToLoadEarly" path="/summary" /></param>
        /// <param name="modsToLoadLate"><inheritdoc cref="ModsToLoadLate" path="/summary" /></param>
        public SConfig(bool developerMode, bool? checkForUpdates, bool? listenForConsoleInput, bool? legacyConsoleMode, bool? paranoidWarnings, bool? useBetaChannel, string gitHubProjectName, string webApiBaseUrl, string[]? verboseLogging, bool? rewriteMods, bool? fixHarmony, bool? useCaseInsensitivePaths, bool? logNetworkTraffic, bool? logTechnicalDetailsForBrokenMods, ColorSchemeConfig consoleColors, bool? suppressHarmonyDebugMode, string[]? suppressUpdateChecks, string[]? modsToLoadEarly, string[]? modsToLoadLate)
        {
            this.DeveloperMode = developerMode;
            this.CheckForUpdates = checkForUpdates ?? (bool)SConfig.DefaultValues[nameof(this.CheckForUpdates)];
            this.ListenForConsoleInput = listenForConsoleInput ?? (bool)SConfig.DefaultValues[nameof(this.ListenForConsoleInput)];
            this.LegacyConsoleMode = legacyConsoleMode ?? (bool)SConfig.DefaultValues[nameof(this.LegacyConsoleMode)];
            this.ParanoidWarnings = paranoidWarnings ?? (bool)SConfig.DefaultValues[nameof(this.ParanoidWarnings)];
            this.UseBetaChannel = useBetaChannel ?? (bool)SConfig.DefaultValues[nameof(this.UseBetaChannel)];
            this.GitHubProjectName = gitHubProjectName;
            this.WebApiBaseUrl = webApiBaseUrl;
            this.VerboseLogging = new HashSet<string>(verboseLogging ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            this.RewriteMods = rewriteMods ?? (bool)SConfig.DefaultValues[nameof(this.RewriteMods)];
            this.FixHarmony = fixHarmony ?? (bool)SConfig.DefaultValues[nameof(this.FixHarmony)];
            this.UseCaseInsensitivePaths = useCaseInsensitivePaths ?? (bool)SConfig.DefaultValues[nameof(this.UseCaseInsensitivePaths)];
            this.LogNetworkTraffic = logNetworkTraffic ?? (bool)SConfig.DefaultValues[nameof(this.LogNetworkTraffic)];
            this.LogTechnicalDetailsForBrokenMods = logTechnicalDetailsForBrokenMods ?? (bool)SConfig.DefaultValues[nameof(this.LogTechnicalDetailsForBrokenMods)];
            this.ConsoleColors = consoleColors;
            this.SuppressHarmonyDebugMode = suppressHarmonyDebugMode ?? (bool)SConfig.DefaultValues[nameof(this.SuppressHarmonyDebugMode)];
            this.SuppressUpdateChecks = new HashSet<string>(suppressUpdateChecks ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            this.ModsToLoadEarly = new HashSet<string>(modsToLoadEarly ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            this.ModsToLoadLate = new HashSet<string>(modsToLoadLate ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Override the value of <see cref="DeveloperMode"/>.</summary>
        /// <param name="value">The value to set.</param>
        public void OverrideDeveloperMode(bool value)
        {
            this.DeveloperMode = value;
        }

        /// <summary>Get the settings which have been customized by the player.</summary>
        public IDictionary<string, object?> GetCustomSettings()
        {
            Dictionary<string, object?> custom = new();

            foreach ((string? name, object defaultValue) in SConfig.DefaultValues)
            {
                object? value = typeof(SConfig).GetProperty(name)?.GetValue(this);
                if (!defaultValue.Equals(value))
                    custom[name] = value;
            }

            if (this.ModsToLoadEarly.Any())
                custom[nameof(this.ModsToLoadEarly)] = $"[{string.Join(", ", this.ModsToLoadEarly)}]";

            if (this.ModsToLoadLate.Any())
                custom[nameof(this.ModsToLoadLate)] = $"[{string.Join(", ", this.ModsToLoadLate)}]";

            if (!this.SuppressUpdateChecks.SetEquals(SConfig.DefaultSuppressUpdateChecks))
                custom[nameof(this.SuppressUpdateChecks)] = $"[{string.Join(", ", this.SuppressUpdateChecks)}]";

            if (this.VerboseLogging.Any())
                custom[nameof(this.VerboseLogging)] = $"[{string.Join(", ", this.VerboseLogging)}]";

            return custom;
        }
    }
}
