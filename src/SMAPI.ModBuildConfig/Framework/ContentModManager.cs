using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Framework;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Models;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.ModBuildConfig.Framework
{
    /// <summary>Manages the files that are part of a content-based mod.</summary>
    internal class ContentPatcherModManager : IModFileManager
    {
        private readonly string ManifestFileName = "manifest.json";

        /// <summary>The files that are part of the package.</summary>
        private readonly IDictionary<string, FileInfo> Files;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="projectDir">The folder containing the project files.</param>
        /// <param name="version">The mod version.</param>
        /// <param name="ignoreFilePaths">The custom relative file paths provided by the user to ignore.</param>
        /// <param name="ignoreFilePatterns">Custom regex patterns matching files to ignore when deploying or zipping the mod.</param>
        /// <param name="validateRequiredModFiles">Whether to validate that required mod files like the manifest are present.</param>
        /// <exception cref="UserErrorException">The mod package isn't valid.</exception>
        public ContentPatcherModManager(string projectDir, string version, string[] ignoreFilePaths, Regex[] ignoreFilePatterns, bool validateRequiredModFiles)
        {
            DirectoryInfo projectDirInfo = new DirectoryInfo(projectDir);
            if(!projectDirInfo.Exists)
                throw new UserErrorException($"The project directory '{projectDir}' for a Content Mod does not exist.");

            this.Files = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);

            // collect files
            foreach (FileInfo entry in projectDirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                string relativePath = PathUtilities.GetRelativePath(projectDirInfo.FullName, entry.FullName);
                FileInfo file = entry;

                if (!this.ShouldIgnore(file, relativePath, ignoreFilePaths, ignoreFilePatterns))
                    this.Files[relativePath] = file;
            }

            // check for required files
            if (validateRequiredModFiles)
            {
                // manifest
                FileInfo manifestFile = this.Files[this.ManifestFileName];
                Manifest manifest;
                if (manifestFile == null)
                    throw new UserErrorException($"Could not add Content Mod '{projectDir}' because no {this.ManifestFileName} was found in the project");
                try
                {
                    new JsonHelper().ReadJsonFileIfExists(manifestFile.FullName, out Manifest rawManifest);
                    manifest = rawManifest;
                }
                catch (JsonReaderException ex)
                {
                    Exception exToShow = ex.InnerException ?? ex;
                    throw new UserErrorException($"Could not add Content Mod '{projectDir}' because {this.ManifestFileName} is not valid JSON: {exToShow.Message}");
                }
                // validate manifest fields
                if (!ManifestValidator.TryValidateFields(manifest, out string error))
                {
                    throw new UserErrorException($"Could not add Content Mod '{projectDir}' because mod's {this.ManifestFileName} did not validate: {error}");
                }
                if (version == null)
                    throw new UserErrorException($"Could not add Content Mod '{projectDir}' because no version was provided");
                if(manifest.Version.ToString().CompareTo(version) != 0)
                    throw new UserErrorException($"Could not add Content Mod '{projectDir}' because the version in the manifest.json file does not match the version {version} provided.");
            }
        }

        ///<inheritdoc/>
        public IDictionary<string, FileInfo> GetFiles()
        {
            return new Dictionary<string, FileInfo>(this.Files, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Get whether a content file should be ignored.</summary>
        /// <param name="file">The file to check.</param>
        /// <param name="relativePath">The file's relative path in the package.</param>
        /// <param name="ignoreFilePaths">The custom relative file paths provided by the user to ignore.</param>
        /// <param name="ignoreFilePatterns">Custom regex patterns matching files to ignore when deploying or zipping the mod.</param>
        private bool ShouldIgnore(FileInfo file, string relativePath, string[] ignoreFilePaths, Regex[] ignoreFilePatterns)
        {
            // apply custom patterns
            if (ignoreFilePaths.Any(p => p == relativePath) || ignoreFilePatterns.Any(p => p.IsMatch(relativePath)))
                return true;

            {
                bool shouldIgnore =
                    // release zips
                    this.EqualsInvariant(file.Extension, ".zip")

                    // OS metadata files
                    || this.EqualsInvariant(file.Name, ".DS_Store")
                    || this.EqualsInvariant(file.Name, "Thumbs.db");
                if (shouldIgnore)
                    return true;
            }
            return false;
        }

        // <summary>Get whether a string is equal to another case-insensitively.</summary>
        /// <param name="str">The string value.</param>
        /// <param name="other">The string to compare with.</param>
        private bool EqualsInvariant(string str, string other)
        {
            if (str == null)
                return other == null;
            return str.Equals(other, StringComparison.OrdinalIgnoreCase);
        }
    }
}
