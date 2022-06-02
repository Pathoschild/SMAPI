namespace StardewModdingAPI.Web.Framework;

/// <summary>Generic metadata about a file download on a mod page.</summary>
internal interface IModDownload
{
    /*********
    ** Accessors
    *********/
    /// <summary>The download's display name.</summary>
    string Name { get; }

    /// <summary>The download's description.</summary>
    string? Description { get; }

    /// <summary>The download's file version.</summary>
    string? Version { get; }
}