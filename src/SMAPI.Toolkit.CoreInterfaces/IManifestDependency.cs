namespace StardewModdingAPI;

/// <summary>A mod dependency listed in a mod manifest.</summary>
public interface IManifestDependency
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique mod ID to require.</summary>
    string UniqueID { get; }

    /// <summary>The minimum required version (if any).</summary>
    ISemanticVersion? MinimumVersion { get; }

    /// <summary>Whether the dependency must be installed to use the mod.</summary>
    bool IsRequired { get; }
}
