using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member: This is internal code to support rewriters that shouldn't be called directly.

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades;

/// <summary>Maps Harmony 1.x <see cref="HarmonyMethod"/> methods to Harmony 2.x to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should not be referenced directly by mods.</remarks>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used via assembly rewriting")]
[SuppressMessage("ReSharper", "CS1591", Justification = "Documentation not needed for facade classes.")]
public class HarmonyMethodFacade : HarmonyMethod
{
    /*********
    ** Public methods
    *********/
    public HarmonyMethodFacade(MethodInfo method)
    {
        this.ImportMethodImpl(method);
    }

    public HarmonyMethodFacade(Type type, string name, Type[]? parameters = null)
    {
        this.ImportMethodImpl(AccessTools.Method(type, name, parameters));
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Import a method directly using the internal HarmonyMethod code.</summary>
    /// <param name="methodInfo">The method to import.</param>
    private void ImportMethodImpl(MethodInfo methodInfo)
    {
        // A null method is no longer allowed in the constructor with Harmony 2.0, but the
        // internal code still handles null fine. For backwards compatibility, this bypasses
        // the new restriction when the mod hasn't been updated for Harmony 2.0 yet.

        MethodInfo? importMethod = typeof(HarmonyMethod).GetMethod("ImportMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        if (importMethod == null)
            throw new InvalidOperationException("Can't find 'HarmonyMethod.ImportMethod' method");
        importMethod.Invoke(this, new object[] { methodInfo });
    }
}