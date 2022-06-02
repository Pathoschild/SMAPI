using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Web.Framework;

/// <summary>Generic metadata about a mod page.</summary>
internal interface IModPage
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod site containing the mod.</summary>
    ModSiteKey Site { get; }

    /// <summary>The mod's unique ID within the site.</summary>
    string Id { get; }

    /// <summary>The mod name.</summary>
    string? Name { get; }

    /// <summary>The mod's semantic version number.</summary>
    string? Version { get; }

    /// <summary>The mod's web URL.</summary>
    string? Url { get; }

    /// <summary>The mod downloads.</summary>
    IModDownload[] Downloads { get; }

    /// <summary>The mod page status.</summary>
    RemoteModStatus Status { get; }

    /// <summary>A user-friendly error which indicates why fetching the mod info failed (if applicable).</summary>
    string? Error { get; }

    /// <summary>Whether the mod data is valid.</summary>
    [MemberNotNullWhen(true, nameof(IModPage.Name), nameof(IModPage.Url))]
    [MemberNotNullWhen(false, nameof(IModPage.Error))]
    bool IsValid { get; }


    /*********
    ** Methods
    *********/
    /// <summary>Set the fetched mod info.</summary>
    /// <param name="name">The mod name.</param>
    /// <param name="version">The mod's semantic version number.</param>
    /// <param name="url">The mod's web URL.</param>
    /// <param name="downloads">The mod downloads.</param>
    IModPage SetInfo(string name, string? version, string url, IEnumerable<IModDownload> downloads);

    /// <summary>Set a mod fetch error.</summary>
    /// <param name="status">The mod availability status on the remote site.</param>
    /// <param name="error">A user-friendly error which indicates why fetching the mod info failed (if applicable).</param>
    IModPage SetError(RemoteModStatus status, string error);
}