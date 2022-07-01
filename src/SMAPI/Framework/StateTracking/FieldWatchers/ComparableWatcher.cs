using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers;

/// <summary>A watcher which detects changes to a value using a specified <see cref="IEqualityComparer{T}"/> instance.</summary>
/// <typeparam name="TValue">The comparable value type.</typeparam>
internal class ComparableWatcher<TValue> : IValueWatcher<TValue>
{
    /*********
    ** Fields
    *********/
    /// <summary>Get the current value.</summary>
    private readonly Func<TValue> GetValue;

    /// <summary>The equality comparer.</summary>
    private readonly IEqualityComparer<TValue> Comparer;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public TValue PreviousValue { get; private set; }

    /// <inheritdoc />
    public TValue CurrentValue { get; private set; }

    /// <inheritdoc />
    public bool IsChanged { get; private set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="getValue">Get the current value.</param>
    /// <param name="comparer">The equality comparer which indicates whether two values are the same.</param>
    public ComparableWatcher(string name, Func<TValue> getValue, IEqualityComparer<TValue> comparer)
    {
        this.Name = name;
        this.GetValue = getValue;
        this.Comparer = comparer;
        this.CurrentValue = getValue();
        this.PreviousValue = this.CurrentValue;
    }

    /// <inheritdoc />
    public void Update()
    {
        this.CurrentValue = this.GetValue();
        this.IsChanged = !this.Comparer.Equals(this.PreviousValue, this.CurrentValue);
    }

    /// <inheritdoc />
    public void Reset()
    {
        this.PreviousValue = this.CurrentValue;
        this.IsChanged = false;
    }

    /// <inheritdoc />
    public void Dispose() { }
}
