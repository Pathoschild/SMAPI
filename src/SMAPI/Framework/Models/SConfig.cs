using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework.Models;

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
        [nameof(ParanoidWarnings)] = Constants.IsDebugBuild,
        [nameof(UseBetaChannel)] = Constants.ApiVersion.IsPrerelease(),
        [nameof(GitHubProjectName)] = "Pathoschild/SMAPI",
        [nameof(WebApiBaseUrl)] = "https://smapi.io/api/",
        [nameof(VerboseLogging)] = false,
        [nameof(LogNetworkTraffic)] = false,
        [nameof(RewriteMods)] = true,
        [nameof(UseRawImageLoading)] = true,
        [nameof(UseCaseInsensitivePaths)] = Constants.Platform is Platform.Android or Platform.Linux
    };

    /// <summary>The default values for <see cref="SuppressUpdateChecks"/>, to log changes if different.</summary>
    private static readonly HashSet<string> DefaultSuppressUpdateChecks = new(StringComparer.OrdinalIgnoreCase)
    {
        "SMAPI.ConsoleCommands",
        "SMAPI.ErrorHandler",
        "SMAPI.SaveBackup"
    };


    /********
    ** Accessors
    ********/
    /// <summary>Whether to enable development features.</summary>
    public bool DeveloperMode { get; private set; }

    /// <summary>Whether to check for newer versions of SMAPI and mods on startup.</summary>
    public bool CheckForUpdates { get; }

    /// <summary>Whether to add a section to the 'mod issues' list for mods which which directly use potentially sensitive .NET APIs like file or shell access.</summary>
    public bool ParanoidWarnings { get; }

    /// <summary>Whether to show beta versions as valid updates.</summary>
    public bool UseBetaChannel { get; }

    /// <summary>SMAPI's GitHub project name, used to perform update checks.</summary>
    public string GitHubProjectName { get; }

    /// <summary>The base URL for SMAPI's web API, used to perform update checks.</summary>
    public string WebApiBaseUrl { get; }

    /// <summary>Whether SMAPI should log more information about the game context.</summary>
    public bool VerboseLogging { get; }

    /// <summary>Whether SMAPI should rewrite mods for compatibility.</summary>
    public bool RewriteMods { get; }

    /// <summary>Whether to use raw image data when possible, instead of initializing an XNA Texture2D instance through the GPU.</summary>
    public bool UseRawImageLoading { get; }

    /// <summary>Whether to make SMAPI file APIs case-insensitive, even on Linux.</summary>
    public bool UseCaseInsensitivePaths { get; }

    /// <summary>Whether SMAPI should log network traffic. Best combined with <see cref="VerboseLogging"/>, which includes network metadata.</summary>
    public bool LogNetworkTraffic { get; }

    /// <summary>The colors to use for text written to the SMAPI console.</summary>
    public ColorSchemeConfig ConsoleColors { get; }

    /// <summary>The mod IDs SMAPI should ignore when performing update checks or validating update keys.</summary>
    public string[] SuppressUpdateChecks { get; }


    /********
    ** Public methods
    ********/
    /// <summary>Construct an instance.</summary>
    /// <param name="developerMode">Whether to enable development features.</param>
    /// <param name="checkForUpdates">Whether to check for newer versions of SMAPI and mods on startup.</param>
    /// <param name="paranoidWarnings">Whether to add a section to the 'mod issues' list for mods which which directly use potentially sensitive .NET APIs like file or shell access.</param>
    /// <param name="useBetaChannel">Whether to show beta versions as valid updates.</param>
    /// <param name="gitHubProjectName">SMAPI's GitHub project name, used to perform update checks.</param>
    /// <param name="webApiBaseUrl">The base URL for SMAPI's web API, used to perform update checks.</param>
    /// <param name="verboseLogging">Whether SMAPI should log more information about the game context.</param>
    /// <param name="rewriteMods">Whether SMAPI should rewrite mods for compatibility.</param>
    /// <param name="useRawImageLoading">Whether to use raw image data when possible, instead of initializing an XNA Texture2D instance through the GPU.</param>
    /// <param name="useCaseInsensitivePaths">>Whether to make SMAPI file APIs case-insensitive, even on Linux.</param>
    /// <param name="logNetworkTraffic">Whether SMAPI should log network traffic.</param>
    /// <param name="consoleColors">The colors to use for text written to the SMAPI console.</param>
    /// <param name="suppressUpdateChecks">The mod IDs SMAPI should ignore when performing update checks or validating update keys.</param>
    public SConfig(bool developerMode, bool? checkForUpdates, bool? paranoidWarnings, bool? useBetaChannel, string gitHubProjectName, string webApiBaseUrl, bool? verboseLogging, bool? rewriteMods, bool? usePintail, bool? useRawImageLoading, bool? useCaseInsensitivePaths, bool? logNetworkTraffic, ColorSchemeConfig consoleColors, string[]? suppressUpdateChecks)
    {
        this.DeveloperMode = developerMode;
        this.CheckForUpdates = checkForUpdates ?? (bool)SConfig.DefaultValues[nameof(this.CheckForUpdates)];
        this.ParanoidWarnings = paranoidWarnings ?? (bool)SConfig.DefaultValues[nameof(this.ParanoidWarnings)];
        this.UseBetaChannel = useBetaChannel ?? (bool)SConfig.DefaultValues[nameof(this.UseBetaChannel)];
        this.GitHubProjectName = gitHubProjectName;
        this.WebApiBaseUrl = webApiBaseUrl;
        this.VerboseLogging = verboseLogging ?? (bool)SConfig.DefaultValues[nameof(this.VerboseLogging)];
        this.RewriteMods = rewriteMods ?? (bool)SConfig.DefaultValues[nameof(this.RewriteMods)];
        this.UseRawImageLoading = useRawImageLoading ?? (bool)SConfig.DefaultValues[nameof(this.UseRawImageLoading)];
        this.UseCaseInsensitivePaths = useCaseInsensitivePaths ?? (bool)SConfig.DefaultValues[nameof(this.UseCaseInsensitivePaths)];
        this.LogNetworkTraffic = logNetworkTraffic ?? (bool)SConfig.DefaultValues[nameof(this.LogNetworkTraffic)];
        this.ConsoleColors = consoleColors;
        this.SuppressUpdateChecks = suppressUpdateChecks ?? Array.Empty<string>();
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

        HashSet<string> curSuppressUpdateChecks = new(this.SuppressUpdateChecks, StringComparer.OrdinalIgnoreCase);
        if (SConfig.DefaultSuppressUpdateChecks.Count != curSuppressUpdateChecks.Count || SConfig.DefaultSuppressUpdateChecks.Any(p => !curSuppressUpdateChecks.Contains(p)))
            custom[nameof(this.SuppressUpdateChecks)] = "[" + string.Join(", ", this.SuppressUpdateChecks) + "]";

        return custom;
    }
}
