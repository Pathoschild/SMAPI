using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub;

/// <summary>Basic metadata about a GitHub project.</summary>
internal class GitRepo
{
    /*********
    ** Accessors
    *********/
    /// <summary>The full repository name, including the owner.</summary>
    [JsonProperty("full_name")]
    public string FullName { get; }

    /// <summary>The URL to the repository web page, if any.</summary>
    [JsonProperty("html_url")]
    public string? WebUrl { get; }

    /// <summary>The code license, if any.</summary>
    [JsonProperty("license")]
    public GitLicense? License { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fullName">The full repository name, including the owner.</param>
    /// <param name="webUrl">The URL to the repository web page, if any.</param>
    /// <param name="license">The code license, if any.</param>
    public GitRepo(string fullName, string? webUrl, GitLicense? license)
    {
        this.FullName = fullName;
        this.WebUrl = webUrl;
        this.License = license;
    }
}
