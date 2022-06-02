namespace StardewModdingAPI.Framework.StateTracking;

/// <summary>A watcher which tracks changes to a value.</summary>
internal interface IValueWatcher<out T> : IWatcher
{
    /*********
    ** Accessors
    *********/
    /// <summary>The field value at the last reset.</summary>
    T PreviousValue { get; }

    /// <summary>The latest value.</summary>
    T CurrentValue { get; }
}
