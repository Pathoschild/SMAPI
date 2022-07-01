using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IContentEvents.AssetsInvalidated"/> event.</summary>
public class AssetsInvalidatedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The asset names that were invalidated.</summary>
    public IReadOnlySet<IAssetName> Names { get; }

    /// <summary>The <see cref="Names"/> with any locale codes stripped.</summary>
    /// <remarks>For example, if <see cref="Names"/> contains a locale like <c>Data/Bundles.fr-FR</c>, this will have the name without locale like <c>Data/Bundles</c>. If the name has no locale, this field is equivalent.</remarks>
    public IReadOnlySet<IAssetName> NamesWithoutLocale { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="names">The asset names that were invalidated.</param>
    /// <param name="namesWithoutLocale">The <paramref name="names"/> with any locale codes stripped.</param>
    internal AssetsInvalidatedEventArgs(IEnumerable<IAssetName> names, IEnumerable<IAssetName> namesWithoutLocale)
    {
        this.Names = names.ToImmutableHashSet();
        this.NamesWithoutLocale = namesWithoutLocale.ToImmutableHashSet();
    }
}
