using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite;

namespace StardewModdingAPI.Web.Framework.RedirectRules;

/// <summary>Redirect paths to URLs if they match a condition.</summary>
internal class RedirectPathsToUrlsRule : RedirectMatchRule
{
    /*********
    ** Fields
    *********/
    /// <summary>Regex patterns matching the current URL mapped to the resulting redirect URL.</summary>
    private readonly IDictionary<Regex, string> Map;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="map">Regex patterns matching the current URL mapped to the resulting redirect URL.</param>
    public RedirectPathsToUrlsRule(IDictionary<string, string> map)
    {
        this.StatusCode = HttpStatusCode.RedirectKeepVerb;
        this.Map = map.ToDictionary(
            p => new Regex(p.Key, RegexOptions.IgnoreCase | RegexOptions.Compiled),
            p => p.Value
        );
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Get the new redirect URL.</summary>
    /// <param name="context">The rewrite context.</param>
    /// <returns>Returns the redirect URL, or <c>null</c> if the redirect doesn't apply.</returns>
    protected override string? GetNewUrl(RewriteContext context)
    {
        string? path = context.HttpContext.Request.Path.Value;

        if (!string.IsNullOrWhiteSpace(path))
        {
            foreach ((Regex pattern, string url) in this.Map)
            {
                if (pattern.IsMatch(path))
                    return pattern.Replace(path, url);
            }
        }

        return null;
    }
}
