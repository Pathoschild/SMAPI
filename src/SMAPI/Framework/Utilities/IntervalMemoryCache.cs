using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework.Utilities;

/// <summary>A memory cache with sliding expiry based on custom intervals, with no background processing.</summary>
/// <typeparam name="TKey">The cache key type.</typeparam>
/// <typeparam name="TValue">The cache value type.</typeparam>
/// <remarks>This is optimized for small caches that are reset relatively rarely. Each cache entry is marked as hot (accessed since the interval started) or stale.
/// When a new interval is started, stale entries are cleared and hot entries become stale.</remarks>
internal class IntervalMemoryCache<TKey, TValue>
    where TKey : notnull
{
    /*********
    ** Fields
    *********/
    /// <summary>The cached values that were accessed during the current interval.</summary>
    private Dictionary<TKey, TValue> HotCache = new();

    /// <summary>The cached values that will expire on the next interval.</summary>
    private Dictionary<TKey, TValue> StaleCache = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Get a value from the cache, fetching it first if needed.</summary>
    /// <param name="cacheKey">The unique key for the cached value.</param>
    /// <param name="get">Get the latest data if it's not in the cache yet.</param>
    public TValue GetOrSet(TKey cacheKey, Func<TValue> get)
    {
        // from hot cache
        if (this.HotCache.TryGetValue(cacheKey, out TValue? value))
            return value;

        // from stale cache
        if (this.StaleCache.TryGetValue(cacheKey, out value))
        {
            this.HotCache[cacheKey] = value;
            return value;
        }

        // new value
        value = get();
        this.HotCache[cacheKey] = value;
        return value;
    }

    /// <summary>Start a new cache interval, removing any stale entries.</summary>
    public void StartNewInterval()
    {
        this.StaleCache.Clear();
        if (this.HotCache.Count is not 0)
            (this.StaleCache, this.HotCache) = (this.HotCache, this.StaleCache); // swap hot cache to stale
    }
}
