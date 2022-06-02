using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Clients.CurseForge.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Clients.CurseForge;

/// <summary>An HTTP client for fetching mod metadata from the CurseForge API.</summary>
internal class CurseForgeClient : ICurseForgeClient
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying HTTP client.</summary>
    private readonly IClient Client;

    /// <summary>A regex pattern which matches a version number in a CurseForge mod file name.</summary>
    private readonly Regex VersionInNamePattern = new(@"^(?:.+? | *)v?(\d+\.\d+(?:\.\d+)?(?:-.+?)?) *(?:\.(?:zip|rar|7z))?$", RegexOptions.Compiled);


    /*********
    ** Accessors
    *********/
    /// <summary>The unique key for the mod site.</summary>
    public ModSiteKey SiteKey => ModSiteKey.CurseForge;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="userAgent">The user agent for the API client.</param>
    /// <param name="apiUrl">The base URL for the CurseForge API.</param>
    /// <param name="apiKey">The API authentication key.</param>
    public CurseForgeClient(string userAgent, string apiUrl, string apiKey)
    {
        this.Client = new FluentClient(apiUrl)
            .SetUserAgent(userAgent)
            .AddDefault(request => request.WithHeader("x-api-key", apiKey));
    }

    /// <summary>Get update check info about a mod.</summary>
    /// <param name="id">The mod ID.</param>
    public async Task<IModPage?> GetModData(string id)
    {
        IModPage page = new GenericModPage(this.SiteKey, id);

        // get ID
        if (!uint.TryParse(id, out uint parsedId))
            return page.SetError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid CurseForge mod ID, must be an integer ID.");

        // get raw data
        ModModel? mod;
        try
        {
            ResponseModel<ModModel> response = await this.Client
                .GetAsync($"mods/{parsedId}")
                .As<ResponseModel<ModModel>>();
            mod = response.Data;
        }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            return page.SetError(RemoteModStatus.DoesNotExist, "Found no CurseForge mod with this ID.");
        }

        // get downloads
        List<IModDownload> downloads = new List<IModDownload>();
        foreach (ModFileModel file in mod.LatestFiles)
        {
            downloads.Add(
                new GenericModDownload(name: file.DisplayName ?? file.FileName, description: null, version: this.GetRawVersion(file))
            );
        }

        // return info
        return page.SetInfo(name: mod.Name, version: null, url: mod.Links.WebsiteUrl, downloads: downloads);
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        this.Client.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a raw version string for a mod file, if available.</summary>
    /// <param name="file">The file whose version to get.</param>
    private string? GetRawVersion(ModFileModel file)
    {
        Match match = this.VersionInNamePattern.Match(file.DisplayName ?? "");
        if (!match.Success)
            match = this.VersionInNamePattern.Match(file.FileName);

        return match.Success
            ? match.Groups[1].Value
            : null;
    }
}