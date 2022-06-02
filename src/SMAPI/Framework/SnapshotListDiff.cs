using System.Collections.Generic;
using StardewModdingAPI.Framework.StateTracking;

namespace StardewModdingAPI.Framework;

/// <summary>A snapshot of a tracked list.</summary>
/// <typeparam name="T">The tracked list value type.</typeparam>
internal class SnapshotListDiff<T>
{
    /*********
    ** Fields
    *********/
    /// <summary>The removed values.</summary>
    private readonly List<T> RemovedImpl = new();

    /// <summary>The added values.</summary>
    private readonly List<T> AddedImpl = new();


    /*********
    ** Accessors
    *********/
    /// <summary>Whether the value changed since the last update.</summary>
    public bool IsChanged { get; private set; }

    /// <summary>The removed values.</summary>
    public IEnumerable<T> Removed => this.RemovedImpl;

    /// <summary>The added values.</summary>
    public IEnumerable<T> Added => this.AddedImpl;


    /*********
    ** Public methods
    *********/
    /// <summary>Update the snapshot.</summary>
    /// <param name="isChanged">Whether the value changed since the last update.</param>
    /// <param name="removed">The removed values.</param>
    /// <param name="added">The added values.</param>
    public void Update(bool isChanged, IEnumerable<T>? removed, IEnumerable<T>? added)
    {
        this.IsChanged = isChanged;

        this.RemovedImpl.Clear();
        if (removed != null)
            this.RemovedImpl.AddRange(removed);

        this.AddedImpl.Clear();
        if (added != null)
            this.AddedImpl.AddRange(added);
    }

    /// <summary>Update the snapshot.</summary>
    /// <param name="watcher">The value watcher to snapshot.</param>
    public void Update(ICollectionWatcher<T> watcher)
    {
        this.Update(watcher.IsChanged, watcher.Removed, watcher.Added);
    }
}
