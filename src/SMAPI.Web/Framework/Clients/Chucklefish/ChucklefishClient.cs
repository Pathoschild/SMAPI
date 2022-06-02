using System;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Web.Framework.Clients.Chucklefish;

/// <summary>An HTTP client for fetching mod metadata from the Chucklefish mod site.</summary>
internal class ChucklefishClient : IChucklefishClient
{
    /*********
    ** Fields
    *********/
    /// <summary>The URL for a mod page excluding the base URL, where {0} is the mod ID.</summary>
    private readonly string ModPageUrlFormat;

    /// <summary>The underlying HTTP client.</summary>
    private readonly IClient Client;


    /*********
    ** Accessors
    *********/
    /// <summary>The unique key for the mod site.</summary>
    public ModSiteKey SiteKey => ModSiteKey.Chucklefish;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="userAgent">The user agent for the API client.</param>
    /// <param name="baseUrl">The base URL for the Chucklefish mod site.</param>
    /// <param name="modPageUrlFormat">The URL for a mod page excluding the <paramref name="baseUrl"/>, where {0} is the mod ID.</param>
    public ChucklefishClient(string userAgent, string baseUrl, string modPageUrlFormat)
    {
        this.ModPageUrlFormat = modPageUrlFormat;
        this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
    }

    /// <summary>Get update check info about a mod.</summary>
    /// <param name="id">The mod ID.</param>
    public async Task<IModPage?> GetModData(string id)
    {
        IModPage page = new GenericModPage(this.SiteKey, id);

        // get mod ID
        if (!uint.TryParse(id, out uint parsedId))
            return page.SetError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid Chucklefish mod ID, must be an integer ID.");

        // fetch HTML
        string? html;
        try
        {
            html = await this.Client
                .GetAsync(string.Format(this.ModPageUrlFormat, parsedId))
                .AsString();
        }
        catch (ApiException ex) when (ex.Status is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return page.SetError(RemoteModStatus.DoesNotExist, "Found no Chucklefish mod with this ID.");
        }
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // extract mod info
        string url = this.GetModUrl(parsedId);
        string? version = doc.DocumentNode.SelectSingleNode("//h1/span")?.InnerText;
        string name = doc.DocumentNode.SelectSingleNode("//h1").ChildNodes[0].InnerText.Trim();
        if (name.StartsWith("[SMAPI]"))
            name = name.Substring("[SMAPI]".Length).TrimStart();

        // return info
        return page.SetInfo(name: name, version: version, url: url, downloads: Array.Empty<IModDownload>());
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        this.Client.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the full mod page URL for a given ID.</summary>
    /// <param name="id">The mod ID.</param>
    private string GetModUrl(uint id)
    {
        UriBuilder builder = new(this.Client.BaseClient.BaseAddress!);
        builder.Path += string.Format(this.ModPageUrlFormat, id);
        return builder.Uri.ToString();
    }
}
