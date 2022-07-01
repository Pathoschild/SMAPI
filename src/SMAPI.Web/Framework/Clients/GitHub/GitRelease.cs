using System;
using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub;

/// <summary>A GitHub project release.</summary>
internal class GitRelease
{
    /*********
    ** Accessors
    *********/
    /// <summary>The display name.</summary>
    [JsonProperty("name")]
    public string Name { get; }

    /// <summary>The semantic version string.</summary>
    [JsonProperty("tag_name")]
    public string Tag { get; }

    /// <summary>The Markdown description for the release.</summary>
    public string Body { get; internal set; }

    /// <summary>Whether this is a draft version.</summary>
    [JsonProperty("draft")]
    public bool IsDraft { get; }

    /// <summary>Whether this is a prerelease version.</summary>
    [JsonProperty("prerelease")]
    public bool IsPrerelease { get; }

    /// <summary>The attached files.</summary>
    public GitAsset[] Assets { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">The display name.</param>
    /// <param name="tag">The semantic version string.</param>
    /// <param name="body">The Markdown description for the release.</param>
    /// <param name="isDraft">Whether this is a draft version.</param>
    /// <param name="isPrerelease">Whether this is a prerelease version.</param>
    /// <param name="assets">The attached files.</param>
    public GitRelease(string name, string tag, string? body, bool isDraft, bool isPrerelease, GitAsset[]? assets)
    {
        this.Name = name;
        this.Tag = tag;
        this.Body = body ?? string.Empty;
        this.IsDraft = isDraft;
        this.IsPrerelease = isPrerelease;
        this.Assets = assets ?? Array.Empty<GitAsset>();
    }
}
