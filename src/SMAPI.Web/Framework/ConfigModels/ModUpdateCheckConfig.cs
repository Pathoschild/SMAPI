using System;

namespace StardewModdingAPI.Web.Framework.ConfigModels;

/// <summary>The config settings for mod update checks.</summary>
internal class ModUpdateCheckConfig
{
    /*********
    ** Accessors
    *********/
    /// <summary>The number of minutes successful update checks should be cached before re-fetching them.</summary>
    public int SuccessCacheMinutes { get; set; }

    /// <summary>The number of minutes failed update checks should be cached before re-fetching them.</summary>
    public int ErrorCacheMinutes { get; set; }

    /// <summary>Update-check metadata to override.</summary>
    public ModOverrideConfig[] ModOverrides { get; set; } = Array.Empty<ModOverrideConfig>();

    /// <summary>The update-check config for SMAPI's own update checks.</summary>
    public SmapiInfoConfig SmapiInfo { get; set; } = null!;
}
