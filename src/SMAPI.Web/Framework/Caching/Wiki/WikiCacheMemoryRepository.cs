using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

namespace StardewModdingAPI.Web.Framework.Caching.Wiki;

/// <summary>Manages cached wiki data in-memory.</summary>
internal class WikiCacheMemoryRepository : BaseCacheRepository, IWikiCacheRepository
{
    /*********
    ** Fields
    *********/
    /// <summary>The saved wiki metadata.</summary>
    private Cached<WikiMetadata>? Metadata;

    /// <summary>The cached wiki data.</summary>
    private Cached<WikiModEntry>[] Mods = Array.Empty<Cached<WikiModEntry>>();


    /*********
    ** Public methods
    *********/
    /// <summary>Get the cached wiki metadata.</summary>
    /// <param name="metadata">The fetched metadata.</param>
    public bool TryGetWikiMetadata([NotNullWhen(true)] out Cached<WikiMetadata>? metadata)
    {
        metadata = this.Metadata;
        return metadata != null;
    }

    /// <summary>Get the cached wiki mods.</summary>
    /// <param name="filter">A filter to apply, if any.</param>
    public IEnumerable<Cached<WikiModEntry>> GetWikiMods(Func<WikiModEntry, bool>? filter = null)
    {
        foreach (var mod in this.Mods)
        {
            if (filter == null || filter(mod.Data))
                yield return mod;
        }
    }

    /// <summary>Save data fetched from the wiki compatibility list.</summary>
    /// <param name="stableVersion">The current stable Stardew Valley version.</param>
    /// <param name="betaVersion">The current beta Stardew Valley version.</param>
    /// <param name="mods">The mod data.</param>
    public void SaveWikiData(string? stableVersion, string? betaVersion, IEnumerable<WikiModEntry> mods)
    {
        this.Metadata = new Cached<WikiMetadata>(new WikiMetadata(stableVersion, betaVersion));
        this.Mods = mods.Select(mod => new Cached<WikiModEntry>(mod)).ToArray();
    }
}
