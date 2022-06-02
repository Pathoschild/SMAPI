using System;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IContentEvents.AssetReady"/> event.</summary>
public class AssetReadyEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The name of the asset being requested.</summary>
    public IAssetName Name { get; }

    /// <summary>The <see cref="Name"/> with any locale codes stripped.</summary>
    /// <remarks>For example, if <see cref="Name"/> contains a locale like <c>Data/Bundles.fr-FR</c>, this will be the name without locale like <c>Data/Bundles</c>. If the name has no locale, this field is equivalent.</remarks>
    public IAssetName NameWithoutLocale { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">The name of the asset being requested.</param>
    /// <param name="nameWithoutLocale">The <paramref name="name"/> with any locale codes stripped.</param>
    internal AssetReadyEventArgs(IAssetName name, IAssetName nameWithoutLocale)
    {
        this.Name = name;
        this.NameWithoutLocale = nameWithoutLocale;
    }
}
