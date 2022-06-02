using System.Collections.Generic;

namespace StardewModdingAPI.Framework.StateTracking;

/// <summary>A watcher which tracks changes to a collection.</summary>
internal interface ICollectionWatcher<out TValue> : IWatcher
{
    /*********
    ** Accessors
    *********/
    /// <summary>The values added since the last reset.</summary>
    IEnumerable<TValue> Added { get; }

    /// <summary>The values removed since the last reset.</summary>
    IEnumerable<TValue> Removed { get; }
}
