using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Netcode;
using StardewModdingAPI.Framework.StateTracking.Comparers;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers;

/// <summary>Provides convenience wrappers for creating watchers.</summary>
internal static class WatcherFactory
{
    /*********
    ** Public methods
    *********/
    /****
    ** Values
    ****/
    /// <summary>Get a watcher which compares values using their <see cref="object.Equals(object)"/> method. This method should only be used when <see cref="ForEquatable{T}"/> won't work, since this doesn't validate whether they're comparable.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="getValue">Get the current value.</param>
    public static IValueWatcher<T> ForGenericEquality<T>(string name, Func<T> getValue)
        where T : struct
    {
        return new ComparableWatcher<T>(name, getValue, new GenericEqualsComparer<T>());
    }

    /// <summary>Get a watcher for an <see cref="IEquatable{T}"/> value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="getValue">Get the current value.</param>
    public static IValueWatcher<T> ForEquatable<T>(string name, Func<T> getValue)
        where T : IEquatable<T>
    {
        return new ComparableWatcher<T>(name, getValue, new EquatableComparer<T>());
    }

    /// <summary>Get a watcher which detects when an object reference changes.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="getValue">Get the current value.</param>
    public static IValueWatcher<T> ForReference<T>(string name, Func<T> getValue)
    {
        return new ComparableWatcher<T>(name, getValue, new ObjectReferenceComparer<T>());
    }

    /// <summary>Get a watcher for a net collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TSelf">The net field instance type.</typeparam>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="field">The net collection.</param>
    public static IValueWatcher<T> ForNetValue<T, TSelf>(string name, NetFieldBase<T, TSelf> field) where TSelf : NetFieldBase<T, TSelf>
    {
        return new NetValueWatcher<T, TSelf>(name, field);
    }

    /****
    ** Collections
    ****/
    /// <summary>Get a watcher which detects when an object reference in a collection changes.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="collection">The observable collection.</param>
    public static ICollectionWatcher<T> ForReferenceList<T>(string name, ICollection<T> collection)
    {
        return new ComparableListWatcher<T>(name, collection, new ObjectReferenceComparer<T>());
    }

    /// <summary>Get a watcher for an observable collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="collection">The observable collection.</param>
    public static ICollectionWatcher<T> ForObservableCollection<T>(string name, ObservableCollection<T> collection)
    {
        return new ObservableCollectionWatcher<T>(name, collection);
    }

    /// <summary>Get a watcher for a collection that never changes.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public static ICollectionWatcher<T> ForImmutableCollection<T>()
    {
        return ImmutableCollectionWatcher<T>.Instance;
    }

    /// <summary>Get a watcher for a net collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="collection">The net collection.</param>
    public static ICollectionWatcher<T> ForNetCollection<T>(string name, NetCollection<T> collection)
        where T : class, INetObject<INetSerializable>
    {
        return new NetCollectionWatcher<T>(name, collection);
    }

    /// <summary>Get a watcher for a net list.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="collection">The net list.</param>
    public static ICollectionWatcher<T> ForNetList<T>(string name, NetList<T, NetRef<T>> collection)
        where T : class, INetObject<INetSerializable>
    {
        return new NetListWatcher<T>(name, collection);
    }

    /// <summary>Get a watcher for a net dictionary.</summary>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TField">The net type equivalent to <typeparamref name="TValue"/>.</typeparam>
    /// <typeparam name="TSerialDict">The serializable dictionary type that can store the keys and values.</typeparam>
    /// <typeparam name="TSelf">The net field instance type.</typeparam>
    /// <param name="field">The net field.</param>
    public static NetDictionaryWatcher<TKey, TValue, TField, TSerialDict, TSelf> ForNetDictionary<TKey, TValue, TField, TSerialDict, TSelf>(string name, NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> field)
        where TKey : notnull
        where TField : class, INetObject<INetSerializable>, new()
        where TSerialDict : IDictionary<TKey, TValue>, new()
        where TSelf : NetDictionary<TKey, TValue, TField, TSerialDict, TSelf>
    {
        return new NetDictionaryWatcher<TKey, TValue, TField, TSerialDict, TSelf>(name, field);
    }
}