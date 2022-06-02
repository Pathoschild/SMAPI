using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StardewModdingAPI.Web.Framework;

/// <summary>A filter which increases the maximum request size for an endpoint.</summary>
/// <remarks>Derived from <a href="https://stackoverflow.com/a/38360093/262123"/>.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AllowLargePostsAttribute : Attribute, IAuthorizationFilter, IOrderedFilter
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying form options.</summary>
    private readonly FormOptions FormOptions;


    /*********
    ** Accessors
    *********/
    /// <summary>The attribute order.</summary>
    public int Order { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public AllowLargePostsAttribute()
    {
        this.FormOptions = new FormOptions
        {
            ValueLengthLimit = 200 * 1024 * 1024 // 200MB
        };
    }

    /// <summary>Called early in the filter pipeline to confirm request is authorized.</summary>
    /// <param name="context">The authorization filter context.</param>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        IFeatureCollection features = context.HttpContext.Features;
        IFormFeature? formFeature = features.Get<IFormFeature>();

        if (formFeature?.Form == null)
        {
            // Request form has not been read yet, so set the limits
            features.Set<IFormFeature>(new FormFeature(context.HttpContext.Request, this.FormOptions));
        }
    }
}
