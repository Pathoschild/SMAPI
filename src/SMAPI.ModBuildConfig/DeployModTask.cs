using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using StardewModdingAPI.ModBuildConfig.Framework;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.ModBuildConfig
{
    /// <summary>A build task which deploys the mod files and prepares a release zip.</summary>
    public class DeployModTask : Task
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The name of the mod folder.</summary>
        [Required]
        public string ModFolderName { get; set; }

        /// <summary>The absolute or relative path to the folder which should contain the generated zip file.</summary>
        [Required]
        public string ModZipPath { get; set; }

        /// <summary>The folder containing the project files.</summary>
        [Required]
        public string ProjectDir { get; set; }

        /// <summary>The folder containing the build output.</summary>
        [Required]
        public string TargetDir { get; set; }

        /// <summary>The folder containing the game's mod folders.</summary>
        [Required]
        public string GameModsDir { get; set; }

        /// <summary>Whether to enable copying the mod files into the game's Mods folder.</summary>
        [Required]
        public bool EnableModDeploy { get; set; }

        /// <summary>Whether to enable the release zip.</summary>
        [Required]
        public bool EnableModZip { get; set; }

        /// <summary>A comma-separated list of regex patterns matching files to ignore when deploying or zipping the mod.</summary>
        public string IgnoreModFilePatterns { get; set; }

        /// <summary>A comma-separated list of relative file paths to ignore when deploying or zipping the mod.</summary>
        public string IgnoreModFilePaths { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>When overridden in a derived class, executes the task.</summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
        public override bool Execute()
        {
            // log build settings
            {
                var properties = this
                    .GetPropertiesToLog()
                    .Select(p => $"{p.Key}: {p.Value}");
                this.Log.LogMessage(MessageImportance.High, $"[mod build package] Handling build with options {string.Join(", ", properties)}");
            }

            if (!this.EnableModDeploy && !this.EnableModZip)
                return true; // nothing to do

            try
            {
                // parse ignore patterns
                string[] ignoreFilePaths = this.GetCustomIgnoreFilePaths().ToArray();
                Regex[] ignoreFilePatterns = this.GetCustomIgnorePatterns().ToArray();

                // get mod info
                ModFileManager package = new ModFileManager(this.ProjectDir, this.TargetDir, ignoreFilePaths, ignoreFilePatterns, validateRequiredModFiles: this.EnableModDeploy || this.EnableModZip);

                // deploy mod files
                if (this.EnableModDeploy)
                {
                    string outputPath = Path.Combine(this.GameModsDir, this.EscapeInvalidFilenameCharacters(this.ModFolderName));
                    this.Log.LogMessage(MessageImportance.High, $"[mod build package] Copying the mod files to {outputPath}...");
                    this.CreateModFolder(package.GetFiles(), outputPath);
                }

                // create release zip
                if (this.EnableModZip)
                {
                    string zipName = this.EscapeInvalidFilenameCharacters($"{this.ModFolderName} {package.GetManifestVersion()}.zip");
                    string zipPath = Path.Combine(this.ModZipPath, zipName);

                    this.Log.LogMessage(MessageImportance.High, $"[mod build package] Generating the release zip at {zipPath}...");
                    this.CreateReleaseZip(package.GetFiles(), this.ModFolderName, zipPath);
                }

                return true;
            }
            catch (UserErrorException ex)
            {
                this.Log.LogErrorFromException(ex);
                return false;
            }
            catch (Exception ex)
            {
                this.Log.LogError($"[mod build package] Failed trying to deploy the mod.\n{ex}");
                return false;
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the properties to write to the log.</summary>
        private IEnumerable<KeyValuePair<string, string>> GetPropertiesToLog()
        {
            var properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (PropertyInfo property in properties.OrderBy(p => p.Name))
            {
                if (property.Name == nameof(this.IgnoreModFilePatterns) && string.IsNullOrWhiteSpace(this.IgnoreModFilePatterns))
                    continue;

                string name = property.Name;

                string value = property.GetValue(this)?.ToString();
                if (value == null)
                    value = "null";
                else if (property.PropertyType == typeof(bool))
                    value = value.ToLower();
                else
                    value = $"'{value}'";

                yield return new KeyValuePair<string, string>(name, value);
            }
        }

        /// <summary>Get the custom ignore patterns provided by the user.</summary>
        private IEnumerable<Regex> GetCustomIgnorePatterns()
        {
            if (string.IsNullOrWhiteSpace(this.IgnoreModFilePatterns))
                yield break;

            foreach (string raw in this.IgnoreModFilePatterns.Split(','))
            {
                Regex regex;
                try
                {
                    regex = new Regex(raw.Trim(), RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    this.Log.LogWarning($"[mod build package] Ignored invalid <{nameof(this.IgnoreModFilePatterns)}> pattern {raw}:\n{ex}");
                    continue;
                }

                yield return regex;
            }
        }

        /// <summary>Get the custom relative file paths provided by the user to ignore.</summary>
        private IEnumerable<string> GetCustomIgnoreFilePaths()
        {
            if (string.IsNullOrWhiteSpace(this.IgnoreModFilePaths))
                yield break;

            foreach (string raw in this.IgnoreModFilePaths.Split(','))
            {
                string path;
                try
                {
                    path = PathUtilities.NormalizePath(raw);
                }
                catch (Exception ex)
                {
                    this.Log.LogWarning($"[mod build package] Ignored invalid <{nameof(this.IgnoreModFilePaths)}> path {raw}:\n{ex}");
                    continue;
                }

                yield return path;
            }
        }

        /// <summary>Copy the mod files into the game's mod folder.</summary>
        /// <param name="files">The files to include.</param>
        /// <param name="modFolderPath">The folder path to create with the mod files.</param>
        private void CreateModFolder(IDictionary<string, FileInfo> files, string modFolderPath)
        {
            foreach (var entry in files)
            {
                string fromPath = entry.Value.FullName;
                string toPath = Path.Combine(modFolderPath, entry.Key);

                // ReSharper disable once AssignNullToNotNullAttribute -- not applicable in this context
                Directory.CreateDirectory(Path.GetDirectoryName(toPath));

                File.Copy(fromPath, toPath, overwrite: true);
            }
        }

        /// <summary>Create a release zip in the recommended format for uploading to mod sites.</summary>
        /// <param name="files">The files to include.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <param name="zipPath">The absolute path to the zip file to create.</param>
        private void CreateReleaseZip(IDictionary<string, FileInfo> files, string modName, string zipPath)
        {
            // get folder name within zip
            string folderName = this.EscapeInvalidFilenameCharacters(modName);

            // create zip file
            Directory.CreateDirectory(Path.GetDirectoryName(zipPath)!);
            using Stream zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            using ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            foreach (var fileEntry in files)
            {
                string relativePath = fileEntry.Key;
                FileInfo file = fileEntry.Value;

                // get file info
                string filePath = file.FullName;
                string entryName = folderName + '/' + relativePath.Replace(Path.DirectorySeparatorChar, '/');

                // add to zip
                using Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using Stream fileStreamInZip = archive.CreateEntry(entryName).Open();
                fileStream.CopyTo(fileStreamInZip);
            }
        }

        /// <summary>Get a copy of a filename with all invalid filename characters substituted.</summary>
        /// <param name="name">The filename.</param>
        private string EscapeInvalidFilenameCharacters(string name)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                name = name.Replace(invalidChar, '.');
            return name;
        }
    }
}
