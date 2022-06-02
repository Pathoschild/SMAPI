using System;
using System.Net;
using Microsoft.AspNetCore.Rewrite;

namespace StardewModdingAPI.Web.Framework.RedirectRules;

/// <summary>Redirect hostnames to a URL if they match a condition.</summary>
internal class RedirectHostsToUrlsRule : RedirectMatchRule
{
    /*********
    ** Fields
    *********/
    /// <summary>Maps a lowercase hostname to the resulting redirect URL.</summary>
    private readonly Func<string, string?> Map;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="statusCode">The status code to use for redirects.</param>
    /// <param name="map">Hostnames mapped to the resulting redirect URL.</param>
    public RedirectHostsToUrlsRule(HttpStatusCode statusCode, Func<string, string?> map)
    {
        this.StatusCode = statusCode;
        this.Map = map ?? throw new ArgumentNullException(nameof(map));
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the new redirect URL.</summary>
    /// <param name="context">The rewrite context.</param>
    /// <returns>Returns the redirect URL, or <c>null</c> if the redirect doesn't apply.</returns>
    protected override string? GetNewUrl(RewriteContext context)
    {
        // get requested host
        string? host = context.HttpContext.Request.Host.Host;

        // get new host
        host = this.Map(host);
        if (host == null)
            return null;

        // rewrite URL
        UriBuilder uri = this.GetUrl(context.HttpContext.Request);
        uri.Host = host;
        return uri.ToString();
    }
}
