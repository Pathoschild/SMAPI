using System.IO;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.GameScanning;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Installer.Framework
{
    /// <summary>The installer context.</summary>
    internal class InstallerContext
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying toolkit game scanner.</summary>
        private readonly GameScanner GameScanner = new GameScanner();


        /*********
        ** Accessors
        *********/
        /// <summary>The current OS.</summary>
        public Platform Platform { get; }

        /// <summary>The human-readable OS name and version.</summary>
        public string PlatformName { get; }

        /// <summary>Whether the installer is running on Windows.</summary>
        public bool IsWindows => this.Platform == Platform.Windows;

        /// <summary>Whether the installer is running on a Unix OS (including Linux or macOS).</summary>
        public bool IsUnix => !this.IsWindows;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public InstallerContext()
        {
            this.Platform = EnvironmentUtility.DetectPlatform();
            this.PlatformName = EnvironmentUtility.GetFriendlyPlatformName(this.Platform);
        }

        /// <summary>Get the installer's version number.</summary>
        public ISemanticVersion GetInstallerVersion()
        {
            var raw = this.GetType().Assembly.GetName().Version;
            return new SemanticVersion(raw);
        }

        /// <summary>Get whether a folder seems to contain the game files.</summary>
        /// <param name="dir">The folder to check.</param>
        public bool LooksLikeGameFolder(DirectoryInfo dir)
        {
            return this.GameScanner.LooksLikeGameFolder(dir);
        }
    }
}
