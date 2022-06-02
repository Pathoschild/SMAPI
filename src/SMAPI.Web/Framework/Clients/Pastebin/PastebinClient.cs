using System;
using System.Net;
using System.Threading.Tasks;
using Pathoschild.Http.Client;

namespace StardewModdingAPI.Web.Framework.Clients.Pastebin;

/// <summary>An API client for Pastebin.</summary>
internal class PastebinClient : IPastebinClient
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying HTTP client.</summary>
    private readonly IClient Client;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="baseUrl">The base URL for the Pastebin API.</param>
    /// <param name="userAgent">The user agent for the API client.</param>
    public PastebinClient(string baseUrl, string userAgent)
    {
        this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
    }

    /// <summary>Fetch a saved paste.</summary>
    /// <param name="id">The paste ID.</param>
    public async Task<PasteInfo> GetAsync(string id)
    {
        try
        {
            // get from API
            string? content = await this.Client
                .GetAsync($"raw/{id}")
                .AsString();

            // handle Pastebin errors
            if (string.IsNullOrWhiteSpace(content))
                return new PasteInfo(null, "Received an empty response from Pastebin.");
            if (content.StartsWith("<!DOCTYPE"))
                return new PasteInfo(null, $"Received a captcha challenge from Pastebin. Please visit https://pastebin.com/{id} in a new window to solve it.");
            return new PasteInfo(content, null);
        }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            return new PasteInfo(null, "There's no log with that ID.");
        }
        catch (Exception ex)
        {
            return new PasteInfo(null, $"Pastebin error: {ex}");
        }
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        this.Client.Dispose();
    }
}
