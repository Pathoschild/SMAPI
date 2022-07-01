using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework.Content;

/// <summary>A low-level wrapper around the content cache which handles reading, writing, and invalidating entries in the cache. This doesn't handle any higher-level logic like localization, loading content, etc. It assumes all keys passed in are already normalized.</summary>
internal class ContentCache
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying asset cache.</summary>
    private readonly Dictionary<string, object> Cache;


    /*********
    ** Accessors
    *********/
    /// <summary>Get or set the value of a raw cache entry.</summary>
    /// <param name="key">The cache key.</param>
    public object this[string key]
    {
        get => this.Cache[key];
        set => this.Cache[key] = value;
    }

    /// <summary>The current cache keys.</summary>
    public Dictionary<string, object>.KeyCollection Keys => this.Cache.Keys;


    /*********
    ** Public methods
    *********/
    /****
    ** Constructor
    ****/
    /// <summary>Construct an instance.</summary>
    /// <param name="loadedAssets">The asset cache for the underlying content manager.</param>
    public ContentCache(Dictionary<string, object> loadedAssets)
    {
        this.Cache = loadedAssets;
    }

    /****
    ** Fetch
    ****/
    /// <summary>Get whether the cache contains a given key.</summary>
    /// <param name="key">The cache key.</param>
    public bool ContainsKey(string key)
    {
        return this.Cache.ContainsKey(key);
    }


    /****
    ** Normalize
    ****/
    /// <summary>Normalize path separators in an asset name.</summary>
    /// <param name="path">The file path to normalize.</param>
    [Pure]
    [return: NotNullIfNotNull("path")]
    public string? NormalizePathSeparators(string? path)
    {
        return PathUtilities.NormalizeAssetName(path);
    }

    /// <summary>Normalize a cache key so it's consistent with the underlying cache.</summary>
    /// <param name="key">The asset key.</param>
    /// <remarks>This is equivalent to <see cref="NormalizePathSeparators"/> with added file extension logic.</remarks>
    [Pure]
    public string NormalizeKey(string key)
    {
        key = this.NormalizePathSeparators(key);
        return key.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase)
            ? key[..^4]
            : key;
    }

    /****
    ** Remove
    ****/
    /// <summary>Remove an asset with the given key.</summary>
    /// <param name="key">The cache key.</param>
    /// <param name="dispose">Whether to dispose the entry value, if applicable.</param>
    /// <returns>Returns the removed key (if any).</returns>
    public bool Remove(string key, bool dispose)
    {
        // remove and get entry
        if (!this.Cache.Remove(key, out object? value))
            return false;

        // dispose & remove entry
        if (dispose && value is IDisposable disposable)
            disposable.Dispose();

        return true;
    }

    /// <summary>Purge assets matching <paramref name="predicate"/> from the cache.</summary>
    /// <param name="predicate">Matches the asset keys to invalidate.</param>
    /// <param name="dispose">Whether to dispose invalidated assets. This should only be <see langword="true"/> when they're being invalidated as part of a <see cref="IDisposable.Dispose"/>, to avoid crashing the game.</param>
    /// <returns>Returns any removed keys.</returns>
    public IEnumerable<string> Remove(Func<string, object, bool> predicate, bool dispose)
    {
        List<string> removed = new();
        foreach ((string key, object value) in this.Cache)
        {
            if (predicate(key, value))
                removed.Add(key);
        }

        foreach (string key in removed)
            this.Remove(key, dispose);

        return removed.Count == 0
            ? Enumerable.Empty<string>() // let GC collect the list in gen0 instead of potentially living longer
            : removed;
    }
}
