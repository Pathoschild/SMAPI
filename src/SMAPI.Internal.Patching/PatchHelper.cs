using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace StardewModdingAPI.Internal.Patching;

/// <summary>Provides utility methods for patching game code with Harmony.</summary>
internal static class PatchHelper
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get a constructor and assert that it was found.</summary>
    /// <typeparam name="TTarget">The type containing the method.</typeparam>
    /// <param name="parameters">The method parameter types, or <c>null</c> if it's not overloaded.</param>
    /// <exception cref="InvalidOperationException">The type has no matching constructor.</exception>
    public static ConstructorInfo RequireConstructor<TTarget>(Type[]? parameters = null)
    {
        return
            AccessTools.Constructor(typeof(TTarget), parameters)
            ?? throw new InvalidOperationException($"Can't find constructor {PatchHelper.GetMethodString(typeof(TTarget), null, parameters)} to patch.");
    }

    /// <summary>Get a method and assert that it was found.</summary>
    /// <typeparam name="TTarget">The type containing the method.</typeparam>
    /// <param name="name">The method name.</param>
    /// <param name="parameters">The method parameter types, or <c>null</c> if it's not overloaded.</param>
    /// <param name="generics">The method generic types, or <c>null</c> if it's not generic.</param>
    /// <exception cref="InvalidOperationException">The type has no matching method.</exception>
    public static MethodInfo RequireMethod<TTarget>(string name, Type[]? parameters = null, Type[]? generics = null)
    {
        return
            AccessTools.Method(typeof(TTarget), name, parameters, generics)
            ?? throw new InvalidOperationException($"Can't find method {PatchHelper.GetMethodString(typeof(TTarget), name, parameters, generics)} to patch.");
    }

    /// <summary>Get a human-readable representation of a method target.</summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="name">The method name, or <c>null</c> for a constructor.</param>
    /// <param name="parameters">The method parameter types, or <c>null</c> if it's not overloaded.</param>
    /// <param name="generics">The method generic types, or <c>null</c> if it's not generic.</param>
    public static string GetMethodString(Type type, string? name, Type[]? parameters = null, Type[]? generics = null)
    {
        StringBuilder str = new();

        // type
        str.Append(type.FullName);

        // method name (if not constructor)
        if (name != null)
        {
            str.Append('.');
            str.Append(name);
        }

        // generics
        if (generics?.Any() == true)
        {
            str.Append('<');
            str.Append(string.Join(", ", generics.Select(p => p.FullName)));
            str.Append('>');
        }

        // parameters
        if (parameters?.Any() == true)
        {
            str.Append('(');
            str.Append(string.Join(", ", parameters.Select(p => p.FullName)));
            str.Append(')');
        }

        return str.ToString();
    }
}
