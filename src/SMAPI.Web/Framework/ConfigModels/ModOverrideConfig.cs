namespace StardewModdingAPI.Web.Framework.ConfigModels;

/// <summary>Override update-check metadata for a mod.</summary>
internal class ModOverrideConfig
{
    /// <summary>The unique ID from the mod's manifest.</summary>
    public string ID { get; set; } = null!;

    /// <summary>Whether to allow non-standard versions.</summary>
    public bool AllowNonStandardVersions { get; set; }

    /// <summary>The mod page URL to use regardless of which site has the update, or <c>null</c> to use the site URL.</summary>
    public string? SetUrl { get; set; }
}
