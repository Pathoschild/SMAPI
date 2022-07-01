using System;
using System.Reflection;
using StardewModdingAPI.Framework.Reflection;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <summary>Provides helper methods for accessing private game code.</summary>
/// <remarks>This implementation searches up the type hierarchy, and caches the reflected fields and methods with a sliding expiry (to optimize performance without unnecessary memory usage).</remarks>
internal class ReflectionHelper : BaseHelper, IReflectionHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying reflection helper.</summary>
    private readonly Reflector Reflector;

    /// <summary>The mod name for error messages.</summary>
    private readonly string ModName;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="modName">The mod name for error messages.</param>
    /// <param name="reflector">The underlying reflection helper.</param>
    public ReflectionHelper(IModMetadata mod, string modName, Reflector reflector)
        : base(mod)
    {
        this.ModName = modName;
        this.Reflector = reflector;
    }

    /// <inheritdoc />
    public IReflectedField<TValue> GetField<TValue>(object obj, string name, bool required = true)
    {
        return this.AssertAccessAllowed(
            this.Reflector.GetField<TValue>(obj, name, required)
        )!;
    }

    /// <inheritdoc />
    public IReflectedField<TValue> GetField<TValue>(Type type, string name, bool required = true)
    {
        return this.AssertAccessAllowed(
            this.Reflector.GetField<TValue>(type, name, required)
        )!;
    }

    /// <inheritdoc />
    public IReflectedProperty<TValue> GetProperty<TValue>(object obj, string name, bool required = true)
    {
        return this.AssertAccessAllowed(
            this.Reflector.GetProperty<TValue>(obj, name, required)
        )!;
    }

    /// <inheritdoc />
    public IReflectedProperty<TValue> GetProperty<TValue>(Type type, string name, bool required = true)
    {
        return this.AssertAccessAllowed(
            this.Reflector.GetProperty<TValue>(type, name, required)
        )!;
    }

    /// <inheritdoc />
    public IReflectedMethod GetMethod(object obj, string name, bool required = true)
    {
        return this.AssertAccessAllowed(
            this.Reflector.GetMethod(obj, name, required)
        )!;
    }

    /// <inheritdoc />
    public IReflectedMethod GetMethod(Type type, string name, bool required = true)
    {
        return this.AssertAccessAllowed(
            this.Reflector.GetMethod(type, name, required)
        )!;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Assert that mods can use the reflection helper to access the given member.</summary>
    /// <typeparam name="T">The field value type.</typeparam>
    /// <param name="field">The field being accessed.</param>
    /// <returns>Returns the same field instance for convenience.</returns>
    private IReflectedField<T>? AssertAccessAllowed<T>(IReflectedField<T>? field)
    {
        this.AssertAccessAllowed(field?.FieldInfo);
        return field;
    }

    /// <summary>Assert that mods can use the reflection helper to access the given member.</summary>
    /// <typeparam name="T">The property value type.</typeparam>
    /// <param name="property">The property being accessed.</param>
    /// <returns>Returns the same property instance for convenience.</returns>
    private IReflectedProperty<T>? AssertAccessAllowed<T>(IReflectedProperty<T>? property)
    {
        this.AssertAccessAllowed(property?.PropertyInfo.GetMethod?.GetBaseDefinition());
        this.AssertAccessAllowed(property?.PropertyInfo.SetMethod?.GetBaseDefinition());
        return property;
    }

    /// <summary>Assert that mods can use the reflection helper to access the given member.</summary>
    /// <param name="method">The method being accessed.</param>
    /// <returns>Returns the same method instance for convenience.</returns>
    private IReflectedMethod? AssertAccessAllowed(IReflectedMethod? method)
    {
        this.AssertAccessAllowed(method?.MethodInfo.GetBaseDefinition());
        return method;
    }

    /// <summary>Assert that mods can use the reflection helper to access the given member.</summary>
    /// <param name="member">The member being accessed.</param>
    private void AssertAccessAllowed(MemberInfo? member)
    {
        if (member == null)
            return;

        // get type which defines the member
        Type? declaringType = member.DeclaringType;
        if (declaringType == null)
            throw new InvalidOperationException($"Can't validate access to {member.MemberType} {member.Name} because it has no declaring type."); // should never happen

        // validate access
        string? rootNamespace = typeof(Program).Namespace;
        if (declaringType.Namespace == rootNamespace || declaringType.Namespace?.StartsWith(rootNamespace + ".") == true)
            throw new InvalidOperationException($"SMAPI blocked access by {this.ModName} to its internals through the reflection API. Accessing the SMAPI internals is strongly discouraged since they're subject to change, which means the mod can break without warning. (Detected access to {declaringType.FullName}.{member.Name}.)");
    }
}
