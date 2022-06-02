using System;
using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.Nexus.ResponseModels;

/// <summary>Mod metadata from Nexus Mods.</summary>
internal class NexusMod
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod name.</summary>
    public string? Name { get; }

    /// <summary>The mod's semantic version number.</summary>
    public string? Version { get; }

    /// <summary>The mod's web URL.</summary>
    [JsonProperty("mod_page_uri")]
    public string? Url { get; }

    /// <summary>The mod's publication status.</summary>
    [JsonIgnore]
    public NexusModStatus Status { get; }

    /// <summary>The files available to download.</summary>
    [JsonIgnore]
    public IModDownload[] Downloads { get; }

    /// <summary>A custom user-friendly error which indicates why fetching the mod info failed (if applicable).</summary>
    [JsonIgnore]
    public string? Error { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">The mod name</param>
    /// <param name="version">The mod's semantic version number.</param>
    /// <param name="url">The mod's web URL.</param>
    /// <param name="downloads">The files available to download.</param>
    public NexusMod(string name, string? version, string url, IModDownload[] downloads)
    {
        this.Name = name;
        this.Version = version;
        this.Url = url;
        this.Status = NexusModStatus.Ok;
        this.Downloads = downloads;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="status">The mod's publication status.</param>
    /// <param name="error">A custom user-friendly error which indicates why fetching the mod info failed (if applicable).</param>
    public NexusMod(NexusModStatus status, string error)
    {
        this.Status = status;
        this.Error = error;
        this.Downloads = Array.Empty<IModDownload>();
    }
}
