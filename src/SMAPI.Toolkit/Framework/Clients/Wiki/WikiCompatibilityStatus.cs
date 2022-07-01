namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

/// <summary>The compatibility status for a mod.</summary>
public enum WikiCompatibilityStatus
{
    /// <summary>The status is unknown.</summary>
    Unknown,

    /// <summary>The mod is compatible.</summary>
    Ok,

    /// <summary>The mod is compatible if you use an optional official download.</summary>
    Optional,

    /// <summary>The mod is compatible if you use an unofficial update.</summary>
    Unofficial,

    /// <summary>The mod isn't compatible, but the player can fix it or there's a good alternative.</summary>
    Workaround,

    /// <summary>The mod isn't compatible.</summary>
    Broken,

    /// <summary>The mod is no longer maintained by the author, and an unofficial update or continuation is unlikely.</summary>
    Abandoned,

    /// <summary>The mod is no longer needed and should be removed.</summary>
    Obsolete
}
