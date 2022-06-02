using System;

namespace StardewModdingAPI.Web.Framework.ConfigModels;

/// <summary>The update-check config for SMAPI's own update checks.</summary>
internal class SmapiInfoConfig
{
    /// <summary>The mod ID used for SMAPI update checks.</summary>
    public string ID { get; set; } = null!;

    /// <summary>The default update key used for SMAPI update checks.</summary>
    public string DefaultUpdateKey { get; set; } = null!;

    /// <summary>The update keys to add for SMAPI update checks when the player has a beta version installed.</summary>
    public string[] AddBetaUpdateKeys { get; set; } = Array.Empty<string>();
}
