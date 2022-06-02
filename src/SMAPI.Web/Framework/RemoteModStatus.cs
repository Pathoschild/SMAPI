namespace StardewModdingAPI.Web.Framework;

/// <summary>The mod availability status on a remote site.</summary>
internal enum RemoteModStatus
{
    /// <summary>The mod is valid.</summary>
    Ok,

    /// <summary>The mod data was fetched, but the data is not valid (e.g. version isn't semantic).</summary>
    InvalidData,

    /// <summary>The mod does not exist.</summary>
    DoesNotExist,

    /// <summary>The mod was temporarily unavailable (e.g. the site could not be reached or an unknown error occurred).</summary>
    TemporaryError
}
