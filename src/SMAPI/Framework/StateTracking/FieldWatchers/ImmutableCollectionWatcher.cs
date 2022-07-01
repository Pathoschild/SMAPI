using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers;

/// <summary>A collection watcher which never changes.</summary>
/// <typeparam name="TValue">The value type within the collection.</typeparam>
internal class ImmutableCollectionWatcher<TValue> : BaseDisposableWatcher, ICollectionWatcher<TValue>
{
    /*********
    ** Accessors
    *********/
    /// <summary>A singleton collection watcher instance.</summary>
    public static ImmutableCollectionWatcher<TValue> Instance { get; } = new();

    /// <inheritdoc />
    public string Name => nameof(ImmutableCollectionWatcher<TValue>);

    /// <inheritdoc />
    public bool IsChanged { get; } = false;

    /// <inheritdoc />
    public IEnumerable<TValue> Added { get; } = Array.Empty<TValue>();

    /// <inheritdoc />
    public IEnumerable<TValue> Removed { get; } = Array.Empty<TValue>();


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public void Update() { }

    /// <inheritdoc />
    public void Reset() { }

    /// <inheritdoc />
    public override void Dispose() { }
}
