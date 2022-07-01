using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;

namespace StardewModdingAPI.Web.Framework.RedirectRules;

/// <summary>Redirect matching requests to a URL.</summary>
internal abstract class RedirectMatchRule : IRule
{
    /*********
    ** Fields
    *********/
    /// <summary>The status code to use for redirects.</summary>
    protected HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Redirect;


    /*********
    ** Public methods
    *********/
    /// <summary>Applies the rule. Implementations of ApplyRule should set the value for <see cref="RewriteContext.Result" /> (defaults to RuleResult.ContinueRules).</summary>
    /// <param name="context">The rewrite context.</param>
    public void ApplyRule(RewriteContext context)
    {
        string? newUrl = this.GetNewUrl(context);
        if (newUrl == null)
            return;

        HttpResponse response = context.HttpContext.Response;
        response.StatusCode = (int)HttpStatusCode.Redirect;
        response.Headers["Location"] = newUrl;
        context.Result = RuleResult.EndResponse;
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Get the new redirect URL.</summary>
    /// <param name="context">The rewrite context.</param>
    /// <returns>Returns the redirect URL, or <c>null</c> if the redirect doesn't apply.</returns>
    protected abstract string? GetNewUrl(RewriteContext context);

    /// <summary>Get the full request URL.</summary>
    /// <param name="request">The request.</param>
    protected UriBuilder GetUrl(HttpRequest request)
    {
        return new UriBuilder
        {
            Scheme = request.Scheme,
            Host = request.Host.Host,
            Port = request.Host.Port ?? -1,
            Path = request.PathBase + request.Path,
            Query = request.QueryString.Value
        };
    }
}
