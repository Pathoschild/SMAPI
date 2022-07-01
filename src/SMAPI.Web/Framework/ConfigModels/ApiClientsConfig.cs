namespace StardewModdingAPI.Web.Framework.ConfigModels;

/// <summary>The config settings for the API clients.</summary>
internal class ApiClientsConfig
{
    /*********
    ** Accessors
    *********/
    /****
    ** Generic
    ****/
    /// <summary>The user agent for API clients, where {0} is the SMAPI version.</summary>
    public string UserAgent { get; set; } = null!;


    /****
    ** Azure
    ****/
    /// <summary>The connection string for the Azure Blob storage account.</summary>
    public string? AzureBlobConnectionString { get; set; }

    /// <summary>The Azure Blob container in which to store temporary uploaded logs.</summary>
    public string AzureBlobTempContainer { get; set; } = null!;

    /// <summary>The number of days since the blob's last-modified date when it will be deleted.</summary>
    public int AzureBlobTempExpiryDays { get; set; }


    /****
    ** Chucklefish
    ****/
    /// <summary>The base URL for the Chucklefish mod site.</summary>
    public string ChucklefishBaseUrl { get; set; } = null!;

    /// <summary>The URL for a mod page on the Chucklefish mod site excluding the <see cref="GitHubBaseUrl"/>, where {0} is the mod ID.</summary>
    public string ChucklefishModPageUrlFormat { get; set; } = null!;


    /****
    ** CurseForge
    ****/
    /// <summary>The base URL for the CurseForge API.</summary>
    public string CurseForgeBaseUrl { get; set; } = null!;

    /// <summary>The API authentication key for the CurseForge API.</summary>
    public string CurseForgeApiKey { get; set; } = null!;


    /****
    ** GitHub
    ****/
    /// <summary>The base URL for the GitHub API.</summary>
    public string GitHubBaseUrl { get; set; } = null!;

    /// <summary>The Accept header value expected by the GitHub API.</summary>
    public string GitHubAcceptHeader { get; set; } = null!;

    /// <summary>The username with which to authenticate to the GitHub API (if any).</summary>
    public string? GitHubUsername { get; set; }

    /// <summary>The password with which to authenticate to the GitHub API (if any).</summary>
    public string? GitHubPassword { get; set; }


    /****
    ** ModDrop
    ****/
    /// <summary>The base URL for the ModDrop API.</summary>
    public string ModDropApiUrl { get; set; } = null!;

    /// <summary>The URL for a ModDrop mod page for the user, where {0} is the mod ID.</summary>
    public string ModDropModPageUrl { get; set; } = null!;


    /****
    ** Nexus Mods
    ****/
    /// <summary>The base URL for the Nexus Mods API.</summary>
    public string NexusBaseUrl { get; set; } = null!;

    /// <summary>The URL for a Nexus mod page for the user, excluding the <see cref="NexusBaseUrl"/>, where {0} is the mod ID.</summary>
    public string NexusModUrlFormat { get; set; } = null!;

    /// <summary>The URL for a Nexus mod page to scrape for versions, excluding the <see cref="NexusBaseUrl"/>, where {0} is the mod ID.</summary>
    public string NexusModScrapeUrlFormat { get; set; } = null!;

    /// <summary>The Nexus API authentication key.</summary>
    public string? NexusApiKey { get; set; }


    /****
    ** Pastebin
    ****/
    /// <summary>The base URL for the Pastebin API.</summary>
    public string PastebinBaseUrl { get; set; } = null!;
}
