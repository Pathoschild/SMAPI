using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI.Toolkit.Serialization.Models;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Toolkit.Framework.ModScanning;

/// <summary>The info about a mod read from its folder.</summary>
public class ModFolder
{
    /*********
    ** Accessors
    *********/
    /// <summary>A suggested display name for the mod folder.</summary>
    public string DisplayName { get; }

    /// <summary>The folder containing the mod's manifest.json.</summary>
    public DirectoryInfo Directory { get; }

    /// <summary>The mod type.</summary>
    public ModType Type { get; }

    /// <summary>The mod manifest.</summary>
    public Manifest? Manifest { get; }

    /// <summary>The error which occurred parsing the manifest, if any.</summary>
    public ModParseError ManifestParseError { get; set; }

    /// <summary>A human-readable message for the <see cref="ManifestParseError"/>, if any.</summary>
    public string? ManifestParseErrorText { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="root">The root folder containing mods.</param>
    /// <param name="directory">The folder containing the mod's manifest.json.</param>
    /// <param name="type">The mod type.</param>
    /// <param name="manifest">The mod manifest.</param>
    public ModFolder(DirectoryInfo root, DirectoryInfo directory, ModType type, Manifest manifest)
        : this(root, directory, type, manifest, ModParseError.None, null) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="root">The root folder containing mods.</param>
    /// <param name="directory">The folder containing the mod's manifest.json.</param>
    /// <param name="type">The mod type.</param>
    /// <param name="manifest">The mod manifest.</param>
    /// <param name="manifestParseError">The error which occurred parsing the manifest, if any.</param>
    /// <param name="manifestParseErrorText">A human-readable message for the <paramref name="manifestParseError"/>, if any.</param>
    public ModFolder(DirectoryInfo root, DirectoryInfo directory, ModType type, Manifest? manifest, ModParseError manifestParseError, string? manifestParseErrorText)
    {
        // save info
        this.Directory = directory;
        this.Type = type;
        this.Manifest = manifest;
        this.ManifestParseError = manifestParseError;
        this.ManifestParseErrorText = manifestParseErrorText;

        // set display name
        this.DisplayName = !string.IsNullOrWhiteSpace(manifest?.Name)
            ? manifest.Name
            : PathUtilities.GetRelativePath(root.FullName, directory.FullName);
    }

    /// <summary>Get the update keys for a mod.</summary>
    /// <param name="manifest">The mod manifest.</param>
    public IEnumerable<string> GetUpdateKeys(Manifest manifest)
    {
        return
            manifest.UpdateKeys
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
    }
}