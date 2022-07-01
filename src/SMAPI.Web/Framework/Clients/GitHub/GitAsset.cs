using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub;

/// <summary>A GitHub download attached to a release.</summary>
internal class GitAsset
{
    /*********
    ** Accessors
    *********/
    /// <summary>The file name.</summary>
    [JsonProperty("name")]
    public string FileName { get; }

    /// <summary>The file content type.</summary>
    [JsonProperty("content_type")]
    public string ContentType { get; }

    /// <summary>The download URL.</summary>
    [JsonProperty("browser_download_url")]
    public string DownloadUrl { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">The file content type.</param>
    /// <param name="downloadUrl">The download URL.</param>
    public GitAsset(string fileName, string contentType, string downloadUrl)
    {
        this.FileName = fileName;
        this.ContentType = contentType;
        this.DownloadUrl = downloadUrl;
    }
}
