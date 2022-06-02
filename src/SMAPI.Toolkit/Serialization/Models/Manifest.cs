using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Serialization.Converters;

namespace StardewModdingAPI.Toolkit.Serialization.Models;

/// <summary>A manifest which describes a mod for SMAPI.</summary>
public class Manifest : IManifest
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod name.</summary>
    public string Name { get; }

    /// <summary>A brief description of the mod.</summary>
    public string Description { get; }

    /// <summary>The mod author's name.</summary>
    public string Author { get; }

    /// <summary>The mod version.</summary>
    public ISemanticVersion Version { get; }

    /// <summary>The minimum SMAPI version required by this mod, if any.</summary>
    public ISemanticVersion? MinimumApiVersion { get; }

    /// <summary>The name of the DLL in the directory that has the <c>Entry</c> method. Mutually exclusive with <see cref="ContentPackFor"/>.</summary>
    public string? EntryDll { get; }

    /// <summary>The mod which will read this as a content pack. Mutually exclusive with <see cref="Manifest.EntryDll"/>.</summary>
    [JsonConverter(typeof(ManifestContentPackForConverter))]
    public IManifestContentPackFor? ContentPackFor { get; }

    /// <summary>The other mods that must be loaded before this mod.</summary>
    [JsonConverter(typeof(ManifestDependencyArrayConverter))]
    public IManifestDependency[] Dependencies { get; }

    /// <summary>The namespaced mod IDs to query for updates (like <c>Nexus:541</c>).</summary>
    public string[] UpdateKeys { get; private set; }

    /// <summary>The unique mod ID.</summary>
    public string UniqueID { get; }

    /// <summary>Any manifest fields which didn't match a valid field.</summary>
    [JsonExtensionData]
    public IDictionary<string, object> ExtraFields { get; } = new Dictionary<string, object>();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance for a transitional content pack.</summary>
    /// <param name="uniqueID">The unique mod ID.</param>
    /// <param name="name">The mod name.</param>
    /// <param name="author">The mod author's name.</param>
    /// <param name="description">A brief description of the mod.</param>
    /// <param name="version">The mod version.</param>
    /// <param name="contentPackFor">The modID which will read this as a content pack.</param>
    public Manifest(string uniqueID, string name, string author, string description, ISemanticVersion version, string? contentPackFor = null)
        : this(
            uniqueId: uniqueID,
            name: name,
            author: author,
            description: description,
            version: version,
            minimumApiVersion: null,
            entryDll: null,
            contentPackFor: contentPackFor != null
                ? new ManifestContentPackFor(contentPackFor, null)
                : null,
            dependencies: null,
            updateKeys: null
        )
    { }

    /// <summary>Construct an instance for a transitional content pack.</summary>
    /// <param name="uniqueId">The unique mod ID.</param>
    /// <param name="name">The mod name.</param>
    /// <param name="author">The mod author's name.</param>
    /// <param name="description">A brief description of the mod.</param>
    /// <param name="version">The mod version.</param>
    /// <param name="minimumApiVersion">The minimum SMAPI version required by this mod, if any.</param>
    /// <param name="entryDll">The name of the DLL in the directory that has the <c>Entry</c> method. Mutually exclusive with <see cref="ContentPackFor"/>.</param>
    /// <param name="contentPackFor">The modID which will read this as a content pack.</param>
    /// <param name="dependencies">The other mods that must be loaded before this mod.</param>
    /// <param name="updateKeys">The namespaced mod IDs to query for updates (like <c>Nexus:541</c>).</param>
    [JsonConstructor]
    public Manifest(string uniqueId, string name, string author, string description, ISemanticVersion version, ISemanticVersion? minimumApiVersion, string? entryDll, IManifestContentPackFor? contentPackFor, IManifestDependency[]? dependencies, string[]? updateKeys)
    {
        this.UniqueID = this.NormalizeWhitespace(uniqueId);
        this.Name = this.NormalizeWhitespace(name);
        this.Author = this.NormalizeWhitespace(author);
        this.Description = this.NormalizeWhitespace(description);
        this.Version = version;
        this.MinimumApiVersion = minimumApiVersion;
        this.EntryDll = this.NormalizeWhitespace(entryDll);
        this.ContentPackFor = contentPackFor;
        this.Dependencies = dependencies ?? Array.Empty<IManifestDependency>();
        this.UpdateKeys = updateKeys ?? Array.Empty<string>();
    }

    /// <summary>Override the update keys loaded from the mod info.</summary>
    /// <param name="updateKeys">The new update keys to set.</param>
    internal void OverrideUpdateKeys(params string[] updateKeys)
    {
        this.UpdateKeys = updateKeys;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Normalize whitespace in a raw string.</summary>
    /// <param name="input">The input to strip.</param>
#if NET5_0_OR_GREATER
        [return: NotNullIfNotNull("input")]
#endif
    private string? NormalizeWhitespace(string? input)
    {
        return input
            ?.Trim()
            .Replace("\r", "")
            .Replace("\n", "");
    }
}
