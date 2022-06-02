using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Toolkit.Serialization.Models;

/// <summary>Indicates which mod can read the content pack represented by the containing manifest.</summary>
public class ManifestContentPackFor : IManifestContentPackFor
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique ID of the mod which can read this content pack.</summary>
    public string UniqueID { get; }

    /// <summary>The minimum required version (if any).</summary>
    public ISemanticVersion? MinimumVersion { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="uniqueId">The unique ID of the mod which can read this content pack.</param>
    /// <param name="minimumVersion">The minimum required version (if any).</param>
    public ManifestContentPackFor(string uniqueId, ISemanticVersion? minimumVersion)
    {
        this.UniqueID = this.NormalizeWhitespace(uniqueId);
        this.MinimumVersion = minimumVersion;
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
        return input?.Trim();
    }
}