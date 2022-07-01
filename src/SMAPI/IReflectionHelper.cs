using System;

namespace StardewModdingAPI;

/// <summary>Provides an API for accessing inaccessible code.</summary>
public interface IReflectionHelper : IModLinked
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get an instance field.</summary>
    /// <typeparam name="TValue">The field type.</typeparam>
    /// <param name="obj">The object which has the field.</param>
    /// <param name="name">The field name.</param>
    /// <param name="required">Whether to throw an exception if the field isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the return value non-nullable.</strong></param>
    /// <returns>Returns the method wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the field doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target field doesn't exist, and <paramref name="required"/> is true.</exception>
    IReflectedField<TValue> GetField<TValue>(object obj, string name, bool required = true);

    /// <summary>Get a static field.</summary>
    /// <typeparam name="TValue">The field type.</typeparam>
    /// <param name="type">The type which has the field.</param>
    /// <param name="name">The field name.</param>
    /// <param name="required">Whether to throw an exception if the field isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the return value non-nullable.</strong></param>
    /// <returns>Returns the method wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the field doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target field doesn't exist, and <paramref name="required"/> is true.</exception>
    IReflectedField<TValue> GetField<TValue>(Type type, string name, bool required = true);

    /// <summary>Get an instance property.</summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="obj">The object which has the property.</param>
    /// <param name="name">The property name.</param>
    /// <param name="required">Whether to throw an exception if the property isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the return value non-nullable.</strong></param>
    /// <returns>Returns the method wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the property doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target property doesn't exist, and <paramref name="required"/> is true.</exception>
    IReflectedProperty<TValue> GetProperty<TValue>(object obj, string name, bool required = true);

    /// <summary>Get a static property.</summary>
    /// <typeparam name="TValue">The property type.</typeparam>
    /// <param name="type">The type which has the property.</param>
    /// <param name="name">The property name.</param>
    /// <param name="required">Whether to throw an exception if the property isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the return value non-nullable.</strong></param>
    /// <returns>Returns the method wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the property doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target property doesn't exist, and <paramref name="required"/> is true.</exception>
    IReflectedProperty<TValue> GetProperty<TValue>(Type type, string name, bool required = true);

    /// <summary>Get an instance method.</summary>
    /// <param name="obj">The object which has the method.</param>
    /// <param name="name">The method name.</param>
    /// <param name="required">Whether to throw an exception if the method isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the return value non-nullable.</strong></param>
    /// <returns>Returns the method wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the method doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target method doesn't exist, and <paramref name="required"/> is true.</exception>
    IReflectedMethod GetMethod(object obj, string name, bool required = true);

    /// <summary>Get a static method.</summary>
    /// <param name="type">The type which has the method.</param>
    /// <param name="name">The method name.</param>
    /// <param name="required">Whether to throw an exception if the method isn't found. <strong>Due to limitations with nullable reference types, setting this to <c>false</c> will still mark the return value non-nullable.</strong></param>
    /// <returns>Returns the method wrapper, or <c>null</c> if <paramref name="required"/> is <c>false</c> and the method doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The target method doesn't exist, and <paramref name="required"/> is true.</exception>
    IReflectedMethod GetMethod(Type type, string name, bool required = true);
}
