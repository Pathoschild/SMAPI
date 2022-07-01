using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace StardewModdingAPI.Toolkit.Serialization.Models;

/// <summary>A mod dependency listed in a mod manifest.</summary>
public class ManifestDependency : IManifestDependency
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique mod ID to require.</summary>
    public string UniqueID { get; }

    /// <summary>The minimum required version (if any).</summary>
    public ISemanticVersion? MinimumVersion { get; }

    /// <summary>Whether the dependency must be installed to use the mod.</summary>
    public bool IsRequired { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="uniqueID">The unique mod ID to require.</param>
    /// <param name="minimumVersion">The minimum required version (if any).</param>
    /// <param name="required">Whether the dependency must be installed to use the mod.</param>
    public ManifestDependency(string uniqueID, string? minimumVersion, bool required = true)
        : this(
            uniqueID: uniqueID,
            minimumVersion: !string.IsNullOrWhiteSpace(minimumVersion)
                ? new SemanticVersion(minimumVersion)
                : null,
            required: required
        )
    { }

    /// <summary>Construct an instance.</summary>
    /// <param name="uniqueID">The unique mod ID to require.</param>
    /// <param name="minimumVersion">The minimum required version (if any).</param>
    /// <param name="required">Whether the dependency must be installed to use the mod.</param>
    [JsonConstructor]
    public ManifestDependency(string uniqueID, ISemanticVersion? minimumVersion, bool required = true)
    {
        this.UniqueID = this.NormalizeWhitespace(uniqueID);
        this.MinimumVersion = minimumVersion;
        this.IsRequired = required;
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
