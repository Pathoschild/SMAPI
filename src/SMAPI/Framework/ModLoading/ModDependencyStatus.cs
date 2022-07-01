namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>The status of a given mod in the dependency-sorting algorithm.</summary>
internal enum ModDependencyStatus
{
    /// <summary>The mod hasn't been visited yet.</summary>
    Queued,

    /// <summary>The mod is currently being analyzed as part of a dependency chain.</summary>
    Checking,

    /// <summary>The mod has already been sorted.</summary>
    Sorted,

    /// <summary>The mod couldn't be sorted due to a metadata issue (e.g. missing dependencies).</summary>
    Failed
}
