using System;
using System.Reflection;
using StardewModdingAPI.Framework.Utilities;

namespace StardewModdingAPI.Framework.Reflection;

/// <summary>Provides helper methods for accessing inaccessible code.</summary>
/// <remarks>This implementation searches up the type hierarchy, and caches the reflected fields and methods with a sliding expiry (to optimize performance without unnecessary memory usage).</remarks>
internal class Reflector
{
    /*********
    ** Fields
    *********/
    /// <summary>The cached fields and methods found via reflection.</summary>
    private readonly IntervalMemoryCache<string, MemberInfo?> Cache = new();


    /*********
    ** Public methods
    *********/
    /****
    ** Fields
    ****/
    /// <summary>Get a instance field.</summary>
    /// <typeparam name="TValue">The field type.</typeparam>
    /// <param name="obj">The object which has the field.</param>
    /// <param name="name">The field name.</param>
    /// <param name="required">Whether to throw an exception if the field isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the value non-nullable.</strong></param>
    /// <returns>Returns the field wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the field doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target field doesn't exist, and <paramref name="required"/> is true.</exception>
    public IReflectedField<TValue> GetField<TValue>(object obj, string name, bool required = true)
    {
        // validate
        if (obj == null)
            throw new ArgumentNullException(nameof(obj), "Can't get a instance field from a null object.");

        // get field from hierarchy
        IReflectedField<TValue>? field = this.GetFieldFromHierarchy<TValue>(obj.GetType(), obj, name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (required && field == null)
            throw new InvalidOperationException($"The {obj.GetType().FullName} object doesn't have a '{name}' instance field.");
        return field!;
    }

    /// <summary>Get a static field.</summary>
    /// <typeparam name="TValue">The field type.</typeparam>
    /// <param name="type">The type which has the field.</param>
    /// <param name="name">The field name.</param>
    /// <param name="required">Whether to throw an exception if the field isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the value non-nullable.</strong></param>
    /// <returns>Returns the field wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the field doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target field doesn't exist, and <paramref name="required"/> is true.</exception>
    public IReflectedField<TValue> GetField<TValue>(Type type, string name, bool required = true)
    {
        // get field from hierarchy
        IReflectedField<TValue>? field = this.GetFieldFromHierarchy<TValue>(type, null, name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        if (required && field == null)
            throw new InvalidOperationException($"The {type.FullName} object doesn't have a '{name}' static field.");
        return field!;
    }

    /****
    ** Properties
    ****/
    /// <summary>Get a instance property.</summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="obj">The object which has the property.</param>
    /// <param name="name">The property name.</param>
    /// <param name="required">Whether to throw an exception if the property isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the value non-nullable.</strong></param>
    /// <returns>Returns the property wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the property doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target property doesn't exist, and <paramref name="required"/> is true.</exception>
    public IReflectedProperty<TValue> GetProperty<TValue>(object obj, string name, bool required = true)
    {
        // validate
        if (obj == null)
            throw new ArgumentNullException(nameof(obj), "Can't get a instance property from a null object.");

        // get property from hierarchy
        IReflectedProperty<TValue>? property = this.GetPropertyFromHierarchy<TValue>(obj.GetType(), obj, name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (required && property == null)
            throw new InvalidOperationException($"The {obj.GetType().FullName} object doesn't have a '{name}' instance property.");
        return property!;
    }

    /// <summary>Get a static property.</summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="type">The type which has the property.</param>
    /// <param name="name">The property name.</param>
    /// <param name="required">Whether to throw an exception if the property isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the value non-nullable.</strong></param>
    /// <returns>Returns the property wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the property doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target property doesn't exist, and <paramref name="required"/> is true.</exception>
    public IReflectedProperty<TValue> GetProperty<TValue>(Type type, string name, bool required = true)
    {
        // get field from hierarchy
        IReflectedProperty<TValue>? property = this.GetPropertyFromHierarchy<TValue>(type, null, name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        if (required && property == null)
            throw new InvalidOperationException($"The {type.FullName} object doesn't have a '{name}' static property.");
        return property!;
    }

    /****
    ** Methods
    ****/
    /// <summary>Get a instance method.</summary>
    /// <param name="obj">The object which has the method.</param>
    /// <param name="name">The method name.</param>
    /// <param name="required">Whether to throw an exception if the method isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the value non-nullable.</strong></param>
    /// <returns>Returns the method wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the method doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target method doesn't exist, and <paramref name="required"/> is true.</exception>
    public IReflectedMethod GetMethod(object obj, string name, bool required = true)
    {
        // validate
        if (obj == null)
            throw new ArgumentNullException(nameof(obj), "Can't get a instance method from a null object.");

        // get method from hierarchy
        IReflectedMethod? method = this.GetMethodFromHierarchy(obj.GetType(), obj, name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (required && method == null)
            throw new InvalidOperationException($"The {obj.GetType().FullName} object doesn't have a '{name}' instance method.");
        return method!;
    }

    /// <summary>Get a static method.</summary>
    /// <param name="type">The type which has the method.</param>
    /// <param name="name">The method name.</param>
    /// <param name="required">Whether to throw an exception if the method isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the value non-nullable.</strong></param>
    /// <returns>Returns the method wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the method doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target method doesn't exist, and <paramref name="required"/> is true.</exception>
    public IReflectedMethod GetMethod(Type type, string name, bool required = true)
    {
        // get method from hierarchy
        IReflectedMethod? method = this.GetMethodFromHierarchy(type, null, name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        if (required && method == null)
            throw new InvalidOperationException($"The {type.FullName} object doesn't have a '{name}' static method.");
        return method!;
    }

    /****
    ** Management
    ****/
    /// <summary>Start a new cache interval, clearing stale reflection lookups.</summary>
    public void NewCacheInterval()
    {
        this.Cache.StartNewInterval();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a field from the type hierarchy.</summary>
    /// <typeparam name="TValue">The expected field type.</typeparam>
    /// <param name="type">The type which has the field.</param>
    /// <param name="obj">The object which has the field, or <c>null</c> for a static field.</param>
    /// <param name="name">The field name.</param>
    /// <param name="bindingFlags">The reflection binding which flags which indicates what type of field to find.</param>
    private IReflectedField<TValue>? GetFieldFromHierarchy<TValue>(Type type, object? obj, string name, BindingFlags bindingFlags)
    {
        bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
        FieldInfo? field = this.GetCached(
            'f', type, name, isStatic,
            fetch: () =>
            {
                for (Type? curType = type; curType != null; curType = curType.BaseType)
                {
                    FieldInfo? fieldInfo = curType.GetField(name, bindingFlags);
                    if (fieldInfo != null)
                    {
                        type = curType;
                        return fieldInfo;
                    }
                }

                return null;
            }
        );

        return field != null
            ? new ReflectedField<TValue>(type, obj, field, isStatic)
            : null;
    }

    /// <summary>Get a property from the type hierarchy.</summary>
    /// <typeparam name="TValue">The expected property type.</typeparam>
    /// <param name="type">The type which has the property.</param>
    /// <param name="obj">The object which has the property, or <c>null</c> for a static property.</param>
    /// <param name="name">The property name.</param>
    /// <param name="bindingFlags">The reflection binding which flags which indicates what type of property to find.</param>
    private IReflectedProperty<TValue>? GetPropertyFromHierarchy<TValue>(Type type, object? obj, string name, BindingFlags bindingFlags)
    {
        bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
        PropertyInfo? property = this.GetCached(
            'p', type, name, isStatic,
            fetch: () =>
            {
                for (Type? curType = type; curType != null; curType = curType.BaseType)
                {
                    PropertyInfo? propertyInfo = curType.GetProperty(name, bindingFlags);
                    if (propertyInfo != null)
                    {
                        type = curType;
                        return propertyInfo;
                    }
                }

                return null;
            }
        );

        return property != null
            ? new ReflectedProperty<TValue>(type, obj, property, isStatic)
            : null;
    }

    /// <summary>Get a method from the type hierarchy.</summary>
    /// <param name="type">The type which has the method.</param>
    /// <param name="obj">The object which has the method, or <c>null</c> for a static method.</param>
    /// <param name="name">The method name.</param>
    /// <param name="bindingFlags">The reflection binding which flags which indicates what type of method to find.</param>
    private IReflectedMethod? GetMethodFromHierarchy(Type type, object? obj, string name, BindingFlags bindingFlags)
    {
        bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
        MethodInfo? method = this.GetCached(
            'm', type, name, isStatic,
            fetch: () =>
            {
                for (Type? curType = type; curType != null; curType = curType.BaseType)
                {
                    MethodInfo? methodInfo = curType.GetMethod(name, bindingFlags);
                    if (methodInfo != null)
                    {
                        type = curType;
                        return methodInfo;
                    }
                }

                return null;
            }
        );

        return method != null
            ? new ReflectedMethod(type, obj, method, isStatic: isStatic)
            : null;
    }

    /// <summary>Get a method or field through the cache.</summary>
    /// <typeparam name="TMemberInfo">The expected <see cref="MemberInfo"/> type.</typeparam>
    /// <param name="memberType">A letter representing the member type (like 'm' for method).</param>
    /// <param name="type">The type whose members are being reflected.</param>
    /// <param name="memberName">The member name.</param>
    /// <param name="isStatic">Whether the member is static.</param>
    /// <param name="fetch">Fetches a new value to cache.</param>
    private TMemberInfo? GetCached<TMemberInfo>(char memberType, Type type, string memberName, bool isStatic, Func<TMemberInfo?> fetch)
        where TMemberInfo : MemberInfo
    {
        string key = $"{memberType}{(isStatic ? 's' : 'i')}{type.FullName}:{memberName}";
        return (TMemberInfo?)this.Cache.GetOrSet(key, fetch);
    }
}
