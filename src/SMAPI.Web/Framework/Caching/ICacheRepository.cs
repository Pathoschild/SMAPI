using System;

namespace StardewModdingAPI.Web.Framework.Caching;

/// <summary>Encapsulates logic for accessing data in the cache.</summary>
internal interface ICacheRepository
{
    /// <summary>Whether cached data is stale.</summary>
    /// <param name="lastUpdated">The date when the data was updated.</param>
    /// <param name="staleMinutes">The age in minutes before data is considered stale.</param>
    bool IsStale(DateTimeOffset lastUpdated, int staleMinutes);
}