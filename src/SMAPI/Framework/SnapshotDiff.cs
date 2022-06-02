using StardewModdingAPI.Framework.StateTracking;

namespace StardewModdingAPI.Framework;

/// <summary>A snapshot of a tracked value.</summary>
/// <typeparam name="T">The tracked value type.</typeparam>
internal class SnapshotDiff<T>
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether the value changed since the last update.</summary>
    public bool IsChanged { get; private set; }

    /// <summary>The previous value.</summary>
    public T? Old { get; private set; }

    /// <summary>The current value.</summary>
    public T? New { get; private set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Update the snapshot.</summary>
    /// <param name="isChanged">Whether the value changed since the last update.</param>
    /// <param name="old">The previous value.</param>
    /// <param name="now">The current value.</param>
    public void Update(bool isChanged, T old, T now)
    {
        this.IsChanged = isChanged;
        this.Old = old;
        this.New = now;
    }

    /// <summary>Update the snapshot.</summary>
    /// <param name="watcher">The value watcher to snapshot.</param>
    public void Update(IValueWatcher<T> watcher)
    {
        this.Update(watcher.IsChanged, watcher.PreviousValue, watcher.CurrentValue);
    }
}
