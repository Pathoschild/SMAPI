using System.IO;
using StardewModdingAPI.Toolkit.Framework;

namespace StardewModdingAPI.Installer.Framework;

/// <summary>Manages paths for the SMAPI installer.</summary>
internal class InstallerPaths
{
    /*********
    ** Accessors
    *********/
    /****
    ** Main folders
    ****/
    /// <summary>The directory path containing the files to copy into the game folder.</summary>
    public DirectoryInfo BundleDir { get; }

    /// <summary>The directory containing the installed game.</summary>
    public DirectoryInfo GameDir { get; }

    /// <summary>The directory into which to install mods.</summary>
    public DirectoryInfo ModsDir { get; }

    /****
    ** Installer paths
    ****/
    /// <summary>The full path to directory path containing the files to copy into the game folder.</summary>
    public string BundlePath => this.BundleDir.FullName;

    /// <summary>The full path to the backup API user settings folder, if applicable.</summary>
    public string BundleApiUserConfigPath { get; }

    /****
    ** Game paths
    ****/
    /// <summary>The full path to the directory containing the installed game.</summary>
    public string GamePath => this.GameDir.FullName;

    /// <summary>The full path to the directory into which to install mods.</summary>
    public string ModsPath => this.ModsDir.FullName;

    /// <summary>The full path to SMAPI's internal configuration file.</summary>
    public string ApiConfigPath { get; }

    /// <summary>The full path to the user's config overrides file.</summary>
    public string ApiUserConfigPath { get; }

    /// <summary>The full path to the installed game DLL.</summary>
    public string GameDllPath { get; }

    /// <summary>The full path to the installed SMAPI executable file.</summary>
    public string UnixSmapiExecutablePath { get; }

    /// <summary>The full path to the vanilla game launch script on Linux/macOS.</summary>
    public string VanillaLaunchScriptPath { get; }

    /// <summary>The full path to the installed SMAPI launch script on Linux/macOS before it's renamed.</summary>
    public string NewLaunchScriptPath { get; }

    /// <summary>The full path to the backed up game launch script on Linux/macOS after SMAPI is installed.</summary>
    public string BackupLaunchScriptPath { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="bundleDir">The directory path containing the files to copy into the game folder.</param>
    /// <param name="gameDir">The directory path for the installed game.</param>
    public InstallerPaths(DirectoryInfo bundleDir, DirectoryInfo gameDir)
    {
        // base paths
        this.BundleDir = bundleDir;
        this.GameDir = gameDir;
        this.ModsDir = new DirectoryInfo(Path.Combine(gameDir.FullName, "Mods"));
        this.GameDllPath = Path.Combine(gameDir.FullName, Constants.GameDllName);

        // launch scripts
        this.VanillaLaunchScriptPath = Path.Combine(gameDir.FullName, "StardewValley");
        this.NewLaunchScriptPath = Path.Combine(gameDir.FullName, "unix-launcher.sh");
        this.BackupLaunchScriptPath = Path.Combine(gameDir.FullName, "StardewValley-original");
        this.UnixSmapiExecutablePath = Path.Combine(gameDir.FullName, "StardewModdingAPI");

        // internal files
        this.BundleApiUserConfigPath = Path.Combine(bundleDir.FullName, "smapi-internal", "config.user.json");
        this.ApiConfigPath = Path.Combine(gameDir.FullName, "smapi-internal", "config.json");
        this.ApiUserConfigPath = Path.Combine(gameDir.FullName, "smapi-internal", "config.user.json");
    }
}
