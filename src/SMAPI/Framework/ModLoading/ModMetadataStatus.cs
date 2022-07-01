namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>Indicates the status of a mod's metadata resolution.</summary>
internal enum ModMetadataStatus
{
    /// <summary>The mod has been found, but hasn't been processed yet.</summary>
    Found,

    /// <summary>The mod cannot be loaded.</summary>
    Failed
}
