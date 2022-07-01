using System;
using System.Collections.Generic;
using System.IO;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <summary>Provides an API for managing content packs.</summary>
internal class ContentPackHelper : BaseHelper, IContentPackHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>The content packs loaded for this mod.</summary>
    private readonly Lazy<IContentPack[]> ContentPacks;

    /// <summary>Create a temporary content pack.</summary>
    private readonly Func<string, IManifest, IContentPack> CreateContentPack;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="contentPacks">The content packs loaded for this mod.</param>
    /// <param name="createContentPack">Create a temporary content pack.</param>
    public ContentPackHelper(IModMetadata mod, Lazy<IContentPack[]> contentPacks, Func<string, IManifest, IContentPack> createContentPack)
        : base(mod)
    {
        this.ContentPacks = contentPacks;
        this.CreateContentPack = createContentPack;
    }

    /// <inheritdoc />
    public IEnumerable<IContentPack> GetOwned()
    {
        return this.ContentPacks.Value;
    }

    /// <inheritdoc />
    public IContentPack CreateFake(string directoryPath)
    {
        string id = Guid.NewGuid().ToString("N");
        string relativePath = Path.GetRelativePath(Constants.ModsPath, directoryPath);
        return this.CreateTemporary(
            directoryPath: directoryPath,
            id: id,
            name: $"{this.Mod.DisplayName} (fake content pack: {relativePath})",
            description: $"A temporary content pack created by the {this.Mod.DisplayName} mod.",
            author: "???",
            new SemanticVersion(1, 0, 0)
        );
    }

    /// <inheritdoc />
    public IContentPack CreateTemporary(string directoryPath, string id, string name, string description, string author, ISemanticVersion version)
    {
        // validate
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentNullException(nameof(directoryPath));
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!Directory.Exists(directoryPath))
            throw new ArgumentException($"Can't create content pack for directory path '{directoryPath}' because no such directory exists.");

        // create manifest
        IManifest manifest = new Manifest(
            uniqueID: id,
            name: name,
            author: author,
            description: description,
            version: version,
            contentPackFor: this.ModID
        );

        // create content pack
        return this.CreateContentPack(directoryPath, manifest);
    }
}
