namespace StardewModdingAPI.Framework.Exceptions;

/// <summary>Indicates why loading an asset through the content pipeline failed.</summary>
internal enum ContentLoadErrorType
{
    /// <summary>The asset name is empty or has an invalid format.</summary>
    InvalidName,

    /// <summary>The asset doesn't exist.</summary>
    AssetDoesNotExist,

    /// <summary>The asset is not available in the current context (e.g. an attempt to load another mod's assets).</summary>
    AccessDenied,

    /// <summary>The asset exists, but the data could not be deserialized or it doesn't match the expected type.</summary>
    InvalidData,

    /// <summary>An unknown error occurred.</summary>
    Other
}
