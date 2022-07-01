using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Web.Framework.Clients;

/// <summary>Generic metadata about a mod page.</summary>
internal class GenericModPage : IModPage
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod site containing the mod.</summary>
    public ModSiteKey Site { get; set; }

    /// <summary>The mod's unique ID within the site.</summary>
    public string Id { get; set; }

    /// <summary>The mod name.</summary>
    public string? Name { get; set; }

    /// <summary>The mod's semantic version number.</summary>
    public string? Version { get; set; }

    /// <summary>The mod's web URL.</summary>
    public string? Url { get; set; }

    /// <summary>The mod downloads.</summary>
    public IModDownload[] Downloads { get; set; } = Array.Empty<IModDownload>();

    /// <summary>The mod availability status on the remote site.</summary>
    public RemoteModStatus Status { get; set; } = RemoteModStatus.InvalidData;

    /// <summary>A user-friendly error which indicates why fetching the mod info failed (if applicable).</summary>
    public string? Error { get; set; }

    /// <summary>Whether the mod data is valid.</summary>
    [MemberNotNullWhen(true, nameof(IModPage.Name), nameof(IModPage.Url))]
    public bool IsValid => this.Status == RemoteModStatus.Ok;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="site">The mod site containing the mod.</param>
    /// <param name="id">The mod's unique ID within the site.</param>
    public GenericModPage(ModSiteKey site, string id)
    {
        this.Site = site;
        this.Id = id;
    }

    /// <summary>Set the fetched mod info.</summary>
    /// <param name="name">The mod name.</param>
    /// <param name="version">The mod's semantic version number.</param>
    /// <param name="url">The mod's web URL.</param>
    /// <param name="downloads">The mod downloads.</param>
    public IModPage SetInfo(string name, string? version, string url, IEnumerable<IModDownload> downloads)
    {
        this.Name = name;
        this.Version = version;
        this.Url = url;
        this.Downloads = downloads.ToArray();
        this.Status = RemoteModStatus.Ok;

        return this;
    }

    /// <summary>Set a mod fetch error.</summary>
    /// <param name="status">The mod availability status on the remote site.</param>
    /// <param name="error">A user-friendly error which indicates why fetching the mod info failed (if applicable).</param>
    public IModPage SetError(RemoteModStatus status, string error)
    {
        this.Status = status;
        this.Error = error;

        return this;
    }
}
