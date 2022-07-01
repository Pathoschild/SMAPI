using System.Collections.Generic;

namespace StardewModdingAPI;

/// <summary>Provides an API for managing content packs.</summary>
public interface IContentPackHelper : IModLinked
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get all content packs loaded for this mod.</summary>
    IEnumerable<IContentPack> GetOwned();

    /// <summary>Create a temporary content pack to read files from a directory, using randomized manifest fields. Temporary content packs will not appear in the SMAPI log and update checks will not be performed.</summary>
    /// <param name="directoryPath">The absolute directory path containing the content pack files.</param>
    IContentPack CreateFake(string directoryPath);

    /// <summary>Create a temporary content pack to read files from a directory. Temporary content packs will not appear in the SMAPI log and update checks will not be performed.</summary>
    /// <param name="directoryPath">The absolute directory path containing the content pack files.</param>
    /// <param name="id">The content pack's unique ID.</param>
    /// <param name="name">The content pack name.</param>
    /// <param name="description">The content pack description.</param>
    /// <param name="author">The content pack author's name.</param>
    /// <param name="version">The content pack version.</param>
    IContentPack CreateTemporary(string directoryPath, string id, string name, string description, string author, ISemanticVersion version);
}
