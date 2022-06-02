using System.Collections.Generic;

namespace StardewModdingAPI;

/// <summary>A manifest which describes a mod for SMAPI.</summary>
public interface IManifest
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod name.</summary>
    string Name { get; }

    /// <summary>A brief description of the mod.</summary>
    string Description { get; }

    /// <summary>The mod author's name.</summary>
    string Author { get; }

    /// <summary>The mod version.</summary>
    ISemanticVersion Version { get; }

    /// <summary>The minimum SMAPI version required by this mod, if any.</summary>
    ISemanticVersion? MinimumApiVersion { get; }

    /// <summary>The unique mod ID.</summary>
    string UniqueID { get; }

    /// <summary>The name of the DLL in the directory that has the <c>Entry</c> method. Mutually exclusive with <see cref="ContentPackFor"/>.</summary>
    string? EntryDll { get; }

    /// <summary>The mod which will read this as a content pack. Mutually exclusive with <see cref="EntryDll"/>.</summary>
    IManifestContentPackFor? ContentPackFor { get; }

    /// <summary>The other mods that must be loaded before this mod.</summary>
    IManifestDependency[] Dependencies { get; }

    /// <summary>The namespaced mod IDs to query for updates (like <c>Nexus:541</c>).</summary>
    string[] UpdateKeys { get; }

    /// <summary>Any manifest fields which didn't match a valid field.</summary>
    IDictionary<string, object> ExtraFields { get; }
}