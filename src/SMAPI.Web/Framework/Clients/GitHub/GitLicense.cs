using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub;

/// <summary>The license info for a GitHub project.</summary>
internal class GitLicense
{
    /*********
    ** Accessors
    *********/
    /// <summary>The license display name.</summary>
    [JsonProperty("name")]
    public string Name { get; }

    /// <summary>The SPDX ID for the license.</summary>
    [JsonProperty("spdx_id")]
    public string SpdxId { get; }

    /// <summary>The URL for the license info.</summary>
    [JsonProperty("url")]
    public string Url { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">The license display name.</param>
    /// <param name="spdxId">The SPDX ID for the license.</param>
    /// <param name="url">The URL for the license info.</param>
    public GitLicense(string name, string spdxId, string url)
    {
        this.Name = name;
        this.SpdxId = spdxId;
        this.Url = url;
    }
}
