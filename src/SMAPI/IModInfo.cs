namespace StardewModdingAPI;

/// <summary>Metadata for a loaded mod.</summary>
public interface IModInfo
{
    /// <summary>The mod manifest.</summary>
    IManifest Manifest { get; }

    /// <summary>Whether the mod is a content pack.</summary>
    bool IsContentPack { get; }
}
