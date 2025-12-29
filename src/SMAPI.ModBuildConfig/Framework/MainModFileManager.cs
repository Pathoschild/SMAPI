using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.ModBuildConfig.Framework;

/// <summary>Manages the files that are part of the main C# mod.</summary>
internal class MainModFileManager : IModFileManager
{
    /*********
    ** Fields
    *********/
    /// <summary>The files that are part of the package.</summary>
    private readonly List<BundleFile> Files = [];

    /// <summary>The file extensions used by assembly files.</summary>
    private readonly HashSet<string> AssemblyFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll",
        ".exe",
        ".pdb",
        ".xml"
    };

    /// <summary>The DLLs which match the <see cref="ExtraAssemblyTypes.Game"/> type.</summary>
    private readonly HashSet<string> GameDllNames =
    [
        // SMAPI
        "0Harmony",
        "Mono.Cecil",
        "Mono.Cecil.Mdb",
        "Mono.Cecil.Pdb",
        "MonoMod.Backports",
        "MonoMod.Core",
        "MonoMod.Iced",
        "MonoMod.ILHelpers",
        "MonoMod.Utils",
        "Newtonsoft.Json",
        "StardewModdingAPI",
        "SMAPI.Toolkit",
        "SMAPI.Toolkit.CoreInterfaces",
        "TMXTile",

        // game + framework
        "BmFont",
        "FAudio-CS",
        "GalaxyCSharp",
        "GalaxyCSharpGlue",
        "Lidgren.Network",
        "MonoGame.Framework",
        "SkiaSharp",
        "Stardew Valley",
        "StardewValley.GameData",
        "Steamworks.NET",
        "TextCopy",
        "xTile"
    ];


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="projectDir">The folder containing the project files.</param>
    /// <param name="targetDir">The folder containing the build output.</param>
    /// <param name="ignoreFilePaths">The custom relative file paths provided by the user to ignore.</param>
    /// <param name="ignoreFilePatterns">Custom regex patterns matching files to ignore when deploying or zipping the mod.</param>
    /// <param name="bundleAssemblyTypes">The extra assembly types which should be bundled with the mod.</param>
    /// <param name="modDllName">The name (without extension or path) for the current mod's DLL.</param>
    /// <param name="overrideManifestJson">If set, replace the mod's <samp>manifest.json</samp> file with this content.</param>
    /// <param name="validateRequiredModFiles">Whether to validate that required mod files like the manifest are present.</param>
    /// <exception cref="UserErrorException">The mod package isn't valid.</exception>
    public MainModFileManager(string projectDir, string targetDir, string[] ignoreFilePaths, Regex[] ignoreFilePatterns, ExtraAssemblyTypes bundleAssemblyTypes, string modDllName, string overrideManifestJson, bool validateRequiredModFiles)
    {
        // validate paths
        if (!Directory.Exists(projectDir))
            throw new UserErrorException("Could not create mod package because the project folder wasn't found.");
        if (!Directory.Exists(targetDir))
            throw new UserErrorException("Could not create mod package because no build output was found.");

        // collect files
        BundleFile manifestEntry = null;
        foreach (BundleFile entry in this.GetPossibleFiles(projectDir, targetDir, overrideManifestJson))
        {
            if (!this.ShouldIgnore(entry.File, entry.RelativePath, ignoreFilePaths, ignoreFilePatterns, bundleAssemblyTypes, modDllName))
            {
                this.Files.Add(entry);

                if (manifestEntry is null && entry.IsModManifest())
                    manifestEntry = entry;
            }
        }

        // check for required files
        if (validateRequiredModFiles)
        {
            // manifest
            if (manifestEntry is null)
                throw new UserErrorException($"Could not create mod package because no {BundleFile.ManifestFileName} was found in the project or build output.");

            // DLL
            // ReSharper disable once SimplifyLinqExpression
            if (!this.Files.Any(p => !p.RelativePath.EndsWith(".dll")))
                throw new UserErrorException("Could not create mod package because no .dll file was found in the project or build output.");
        }
    }

    ///<inheritdoc/>
    public IEnumerable<BundleFile> GetFiles()
    {
        return this.Files;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get all files to include in the mod folder, not accounting for ignore patterns.</summary>
    /// <param name="projectDir">The folder containing the project files.</param>
    /// <param name="targetDir">The folder containing the build output.</param>
    /// <param name="overrideManifestJson">If set, replace the mod's <samp>manifest.json</samp> file with this content.</param>
    /// <returns>Returns tuples containing the relative path within the mod folder, and the file to copy to it.</returns>
    private IEnumerable<BundleFile> GetPossibleFiles(string projectDir, string targetDir, string overrideManifestJson)
    {
        // project manifest
        bool hasProjectManifest = false;
        {
            FileInfo manifest = new(Path.Combine(projectDir, BundleFile.ManifestFileName));
            if (manifest.Exists)
            {
                yield return new BundleFile(BundleFile.ManifestFileName, manifest, overrideManifestJson);
                hasProjectManifest = true;
            }
        }

        // project i18n files
        bool hasProjectTranslations = false;
        DirectoryInfo translationsFolder = new(Path.Combine(projectDir, "i18n"));
        if (translationsFolder.Exists)
        {
            foreach (FileInfo file in translationsFolder.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                string relativePath = PathUtilities.GetRelativePath(projectDir, file.FullName);
                yield return new BundleFile(relativePath, file);
            }
            hasProjectTranslations = true;
        }

        // project assets folder
        bool hasAssetsFolder = false;
        DirectoryInfo assetsFolder = new(Path.Combine(projectDir, "assets"));
        if (assetsFolder.Exists)
        {
            foreach (FileInfo file in assetsFolder.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                string relativePath = PathUtilities.GetRelativePath(projectDir, file.FullName);
                yield return new BundleFile(relativePath, file);
            }
            hasAssetsFolder = true;
        }

        // build output
        DirectoryInfo buildFolder = new(targetDir);
        foreach (FileInfo file in buildFolder.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            // get path info
            string relativePath = PathUtilities.GetRelativePath(buildFolder.FullName, file.FullName);
            string[] segments = PathUtilities.GetSegments(relativePath);

            // prefer project manifest/i18n/assets files
            if (hasProjectManifest && BundleFile.IsModManifest(relativePath))
                continue;
            if (hasProjectTranslations && this.EqualsInvariant(segments[0], "i18n"))
                continue;
            if (hasAssetsFolder && this.EqualsInvariant(segments[0], "assets"))
                continue;

            // add file
            yield return new BundleFile(relativePath, file);
        }
    }

    /// <summary>Get whether a build output file should be ignored.</summary>
    /// <param name="file">The file to check.</param>
    /// <param name="relativePath">The file's relative path in the package.</param>
    /// <param name="ignoreFilePaths">The custom relative file paths provided by the user to ignore.</param>
    /// <param name="ignoreFilePatterns">Custom regex patterns matching files to ignore when deploying or zipping the mod.</param>
    /// <param name="bundleAssemblyTypes">The extra assembly types which should be bundled with the mod.</param>
    /// <param name="modDllName">The name (without extension or path) for the current mod's DLL.</param>
    private bool ShouldIgnore(FileInfo file, string relativePath, string[] ignoreFilePaths, Regex[] ignoreFilePatterns, ExtraAssemblyTypes bundleAssemblyTypes, string modDllName)
    {
        // apply custom patterns
        if (ignoreFilePaths.Any(p => p == relativePath) || ignoreFilePatterns.Any(p => p.IsMatch(relativePath)))
            return true;

        // ignore unneeded files
        {
            bool shouldIgnore =
                // release zips
                this.EqualsInvariant(file.Extension, ".zip")

                // *.deps.json (only SMAPI's top-level one is used)
                || file.Name.EndsWith(".deps.json")

                // code analysis files
                || file.Name.EndsWith(".CodeAnalysisLog.xml", StringComparison.OrdinalIgnoreCase)
                || file.Name.EndsWith(".lastcodeanalysissucceeded", StringComparison.OrdinalIgnoreCase)

                // translation class builder (not used at runtime)
                || (
                    file.Name.StartsWith("Pathoschild.Stardew.ModTranslationClassBuilder")
                    && this.AssemblyFileExtensions.Contains(file.Extension)
                )

                // OS metadata files
                || this.EqualsInvariant(file.Name, ".DS_Store")
                || this.EqualsInvariant(file.Name, "Thumbs.db");
            if (shouldIgnore)
                return true;
        }

        // ignore by assembly type
        ExtraAssemblyTypes type = this.GetExtraAssemblyType(file, modDllName);
        switch (bundleAssemblyTypes)
        {
            // Only explicitly-referenced assemblies are in the build output. These should be added to the zip,
            // since it's possible the game won't load them (except game assemblies which will always be loaded
            // separately). If they're already loaded, SMAPI will just ignore them.
            case ExtraAssemblyTypes.None:
                if (type is ExtraAssemblyTypes.Game)
                    return true;
                break;

            // All assemblies are in the build output (due to how .NET builds references), but only those which
            // match the bundled type should be in the zip.
            default:
                if (type != ExtraAssemblyTypes.None && !bundleAssemblyTypes.HasFlag(type))
                    return true;
                break;
        }

        return false;
    }

    /// <summary>Get the extra assembly type for a file, assuming that the user specified one or more extra types to bundle.</summary>
    /// <param name="file">The file to check.</param>
    /// <param name="modDllName">The name (without extension or path) for the current mod's DLL.</param>
    private ExtraAssemblyTypes GetExtraAssemblyType(FileInfo file, string modDllName)
    {
        string baseName = Path.GetFileNameWithoutExtension(file.Name);
        string extension = file.Extension;

        if (baseName == modDllName || !this.AssemblyFileExtensions.Contains(extension))
            return ExtraAssemblyTypes.None;

        if (this.GameDllNames.Contains(baseName))
            return ExtraAssemblyTypes.Game;

        if (baseName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) || baseName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
            return ExtraAssemblyTypes.System;

        return ExtraAssemblyTypes.ThirdParty;
    }

    /// <summary>Get whether a string is equal to another case-insensitively.</summary>
    /// <param name="str">The string value.</param>
    /// <param name="other">The string to compare with.</param>
    private bool EqualsInvariant(string str, string other)
    {
        if (str == null)
            return other == null;
        return str.Equals(other, StringComparison.OrdinalIgnoreCase);
    }
}
