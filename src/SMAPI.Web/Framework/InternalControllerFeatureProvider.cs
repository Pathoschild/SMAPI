using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace StardewModdingAPI.Web.Framework;

/// <summary>Discovers controllers with support for non-public controllers.</summary>
internal class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    /*********
    ** Public methods
    *********/
    /// <summary>Determines if a given type is a controller.</summary>
    /// <param name="type">The <see cref="T:System.Reflection.TypeInfo" /> candidate.</param>
    /// <returns><code>true</code> if the type is a controller; otherwise <code>false</code>.</returns>
    protected override bool IsController(TypeInfo type)
    {
        return
            type.IsClass
            && !type.IsAbstract
            && (/*type.IsPublic &&*/ !type.ContainsGenericParameters)
            && (!type.IsDefined(typeof(NonControllerAttribute))
                && (type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) || type.IsDefined(typeof(ControllerAttribute))));
    }
}
