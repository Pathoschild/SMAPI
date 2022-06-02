namespace StardewModdingAPI;

/// <summary>Indicates which mod can read the content pack represented by the containing manifest.</summary>
public interface IManifestContentPackFor
{
    /// <summary>The unique ID of the mod which can read this content pack.</summary>
    string UniqueID { get; }

    /// <summary>The minimum required version (if any).</summary>
    ISemanticVersion? MinimumVersion { get; }
}
