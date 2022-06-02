using System.Collections.Generic;
using Netcode;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers;

/// <summary>A watcher which detects changes to a Netcode collection.</summary>
/// <typeparam name="TValue">The value type within the collection.</typeparam>
internal class NetCollectionWatcher<TValue> : BaseDisposableWatcher, ICollectionWatcher<TValue>
    where TValue : class, INetObject<INetSerializable>
{
    /*********
    ** Fields
    *********/
    /// <summary>The field being watched.</summary>
    private readonly NetCollection<TValue> Field;

    /// <summary>The pairs added since the last reset.</summary>
    private readonly List<TValue> AddedImpl = new();

    /// <summary>The pairs removed since the last reset.</summary>
    private readonly List<TValue> RemovedImpl = new();


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public bool IsChanged => this.AddedImpl.Count > 0 || this.RemovedImpl.Count > 0;

    /// <inheritdoc />
    public IEnumerable<TValue> Added => this.AddedImpl;

    /// <inheritdoc />
    public IEnumerable<TValue> Removed => this.RemovedImpl;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">A name which identifies what the watcher is watching, used for troubleshooting.</param>
    /// <param name="field">The field to watch.</param>
    public NetCollectionWatcher(string name, NetCollection<TValue> field)
    {
        this.Name = name;
        this.Field = field;
        field.OnValueAdded += this.OnValueAdded;
        field.OnValueRemoved += this.OnValueRemoved;
    }

    /// <inheritdoc />
    public void Update()
    {
        this.AssertNotDisposed();
    }

    /// <inheritdoc />
    public void Reset()
    {
        this.AssertNotDisposed();

        this.AddedImpl.Clear();
        this.RemovedImpl.Clear();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (!this.IsDisposed)
        {
            this.Field.OnValueAdded -= this.OnValueAdded;
            this.Field.OnValueRemoved -= this.OnValueRemoved;
        }

        base.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>A callback invoked when an entry is added to the collection.</summary>
    /// <param name="value">The added value.</param>
    private void OnValueAdded(TValue value)
    {
        this.AddedImpl.Add(value);
    }

    /// <summary>A callback invoked when an entry is removed from the collection.</summary>
    /// <param name="value">The added value.</param>
    private void OnValueRemoved(TValue value)
    {
        this.RemovedImpl.Add(value);
    }
}
