using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework.Utilities;

/// <summary>An in-memory dictionary cache that stores data for the duration of a game update tick.</summary>
/// <typeparam name="TKey">The dictionary key type.</typeparam>
/// <typeparam name="TValue">The dictionary value type.</typeparam>
internal class TickCacheDictionary<TKey, TValue>
    where TKey : notnull
{
    /*********
    ** Fields
    *********/
    /// <summary>The last game tick for which data was cached.</summary>
    private uint? LastGameTick;

    /// <summary>The underlying cached data.</summary>
    private readonly Dictionary<TKey, TValue> Cache = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Get a value from the cache, fetching it first if it's not cached yet.</summary>
    /// <param name="cacheKey">The unique key for the cached value.</param>
    /// <param name="get">Get the latest data if it's not in the cache yet.</param>
    public TValue GetOrSet(TKey cacheKey, Func<TValue> get)
    {
        // clear cache on new tick
        if (SCore.ProcessTicksElapsed != this.LastGameTick)
        {
            this.Cache.Clear();
            this.LastGameTick = SCore.ProcessTicksElapsed;
        }

        // fetch value
        if (!this.Cache.TryGetValue(cacheKey, out TValue? cached))
            this.Cache[cacheKey] = cached = get();
        return cached;
    }

    /// <summary>Remove an entry from the cache.</summary>
    /// <param name="cacheKey">The unique key for the cached value.</param>
    /// <returns>Returns whether the key was present in the dictionary.</returns>
    public bool Remove(TKey cacheKey)
    {
        return this.Cache.Remove(cacheKey);
    }
}

/// <summary>An in-memory dictionary cache that stores data for the duration of a game update tick.</summary>
/// <typeparam name="TKey">The dictionary key type.</typeparam>
internal class TickCacheDictionary<TKey> : TickCacheDictionary<TKey, object>
    where TKey : notnull
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get a value from the cache, fetching it first if it's not cached yet.</summary>
    /// <param name="cacheKey">The unique key for the cached value.</param>
    /// <param name="get">Get the latest data if it's not in the cache yet.</param>
    public TValue GetOrSet<TValue>(TKey cacheKey, Func<TValue> get)
    {
        object? value = base.GetOrSet(cacheKey, () => get()!);

        try
        {
            return (TValue)value;
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Can't cast value of the '{cacheKey}' cache entry from {value?.GetType().FullName ?? "null"} to {typeof(TValue).FullName}.", ex);
        }
    }
}
