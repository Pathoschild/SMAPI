using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StardewModdingAPI.Toolkit;

namespace StardewModdingAPI.Web.Framework;

/// <summary>Constrains a route value to a valid semantic version.</summary>
internal class VersionConstraint : IRouteConstraint
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get whether the URL parameter contains a valid value for this constraint.</summary>
    /// <param name="httpContext">An object that encapsulates information about the HTTP request.</param>
    /// <param name="route">The router that this constraint belongs to.</param>
    /// <param name="routeKey">The name of the parameter that is being checked.</param>
    /// <param name="values">A dictionary that contains the parameters for the URL.</param>
    /// <param name="routeDirection">An object that indicates whether the constraint check is being performed when an incoming request is being handled or when a URL is being generated.</param>
    /// <returns><c>true</c> if the URL parameter contains a valid value; otherwise, <c>false</c>.</returns>
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (routeKey == null)
            throw new ArgumentNullException(nameof(routeKey));
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return
            values.TryGetValue(routeKey, out object? routeValue)
            && routeValue is string routeStr
            && SemanticVersion.TryParse(routeStr, allowNonStandard: true, out _);
    }
}
