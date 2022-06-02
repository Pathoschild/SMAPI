using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework;

/// <summary>Provides extensions on ASP.NET Core types.</summary>
public static class Extensions
{
    /*********
    ** Public methods
    *********/
    /****
    ** View helpers
    ****/
    /// <summary>Get a URL for an action method. Unlike <see cref="IUrlHelper.Action"/>, only the specified <paramref name="values"/> are added to the URL without merging values from the current HTTP request.</summary>
    /// <param name="helper">The URL helper to extend.</param>
    /// <param name="action">The name of the action method.</param>
    /// <param name="controller">The name of the controller.</param>
    /// <param name="values">An object that contains route values.</param>
    /// <param name="absoluteUrl">Get an absolute URL instead of a server-relative path/</param>
    /// <returns>The generated URL.</returns>
    public static string? PlainAction(this IUrlHelper helper, [AspMvcAction] string action, [AspMvcController] string controller, object? values = null, bool absoluteUrl = false)
    {
        // get route values
        RouteValueDictionary valuesDict = new(values);
        foreach (var value in helper.ActionContext.RouteData.Values)
        {
            if (!valuesDict.ContainsKey(value.Key))
                valuesDict[value.Key] = null; // explicitly remove it from the URL
        }

        // get relative URL
        string? url = helper.Action(action, controller, valuesDict);
        if (url == null && action.EndsWith("Async"))
            url = helper.Action(action[..^"Async".Length], controller, valuesDict);

        // get absolute URL
        if (absoluteUrl)
        {
            HttpRequest request = helper.ActionContext.HttpContext.Request;
            Uri baseUri = new($"{request.Scheme}://{request.Host}");
            url = new Uri(baseUri, url).ToString();
        }

        return url;
    }

    /// <summary>Get a serialized JSON representation of the value.</summary>
    /// <param name="page">The page to extend.</param>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized JSON.</returns>
    /// <remarks>This bypasses unnecessary validation (e.g. not allowing null values) in <see cref="IJsonHelper.Serialize"/>.</remarks>
    public static IHtmlContent ForJson(this RazorPageBase page, object? value)
    {
        string json = JsonConvert.SerializeObject(value);
        return new HtmlString(json);
    }
}