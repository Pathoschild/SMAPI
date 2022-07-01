using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

namespace StardewModdingAPI.Web.Framework.Caching.Wiki;

/// <summary>Manages cached wiki data.</summary>
internal interface IWikiCacheRepository : ICacheRepository
{
    /*********
    ** Methods
    *********/
    /// <summary>Get the cached wiki metadata.</summary>
    /// <param name="metadata">The fetched metadata.</param>
    bool TryGetWikiMetadata([NotNullWhen(true)] out Cached<WikiMetadata>? metadata);

    /// <summary>Get the cached wiki mods.</summary>
    /// <param name="filter">A filter to apply, if any.</param>
    IEnumerable<Cached<WikiModEntry>> GetWikiMods(Func<WikiModEntry, bool>? filter = null);

    /// <summary>Save data fetched from the wiki compatibility list.</summary>
    /// <param name="stableVersion">The current stable Stardew Valley version.</param>
    /// <param name="betaVersion">The current beta Stardew Valley version.</param>
    /// <param name="mods">The mod data.</param>
    void SaveWikiData(string? stableVersion, string? betaVersion, IEnumerable<WikiModEntry> mods);
}
