using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;

namespace StardewModdingAPI.Web.Framework.RedirectRules;

/// <summary>Redirect requests to HTTPS.</summary>
internal class RedirectToHttpsRule : RedirectMatchRule
{
    /*********
    ** Fields
    *********/
    /// <summary>Matches requests which should be ignored.</summary>
    private readonly Func<HttpRequest, bool> Except;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="except">Matches requests which should be ignored.</param>
    public RedirectToHttpsRule(Func<HttpRequest, bool>? except = null)
    {
        this.Except = except ?? (_ => false);
        this.StatusCode = HttpStatusCode.RedirectKeepVerb;
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Get the new redirect URL.</summary>
    /// <param name="context">The rewrite context.</param>
    /// <returns>Returns the redirect URL, or <c>null</c> if the redirect doesn't apply.</returns>
    protected override string? GetNewUrl(RewriteContext context)
    {
        HttpRequest request = context.HttpContext.Request;
        if (request.IsHttps || this.Except(request))
            return null;

        UriBuilder uri = this.GetUrl(request);
        uri.Scheme = "https";
        return uri.ToString();
    }
}
