namespace StardewModdingAPI.Web.Framework.Clients.Nexus;

/// <summary>The status of a Nexus mod.</summary>
internal enum NexusModStatus
{
    /// <summary>The mod is published and valid.</summary>
    Ok,

    /// <summary>The mod is hidden by the author.</summary>
    Hidden,

    /// <summary>The mod hasn't been published yet.</summary>
    NotPublished,

    /// <summary>The mod contains adult content which is hidden for anonymous web users.</summary>
    AdultContentForbidden,

    /// <summary>The Nexus API returned an unhandled error.</summary>
    Other
}
