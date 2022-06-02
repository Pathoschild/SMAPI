using System;

namespace StardewModdingAPI;

/// <summary>Basic metadata for a content asset.</summary>
public interface IAssetInfo
{
    /*********
    ** Accessors
    *********/
    /// <summary>The content's locale code, if the content is localized.</summary>
    /// <remarks>LEGACY NOTE: when reading this field from an <see cref="IAssetLoader"/> or <see cref="IAssetEditor"/> implementation, for non-localized assets it will return the current game locale (or an empty string for English) instead of null.</remarks>
    string? Locale { get; }

    /// <summary>The asset name being read.</summary>
    /// <remarks>LEGACY NOTE: when reading this field from an <see cref="IAssetLoader"/> or <see cref="IAssetEditor"/> implementation, it's always equivalent to <see cref="NameWithoutLocale"/> for backwards compatibility.</remarks>
    public IAssetName Name { get; }

    /// <summary>The <see cref="Name"/> with any locale codes stripped.</summary>
    /// <remarks>For example, if <see cref="Name"/> contains a locale like <c>Data/Bundles.fr-FR</c>, this will be the name without locale like <c>Data/Bundles</c>. If the name has no locale, this field is equivalent.</remarks>
    public IAssetName NameWithoutLocale { get; }

    /// <summary>The normalized asset name being read. The format may change between platforms; see <see cref="AssetNameEquals"/> to compare with a known path.</summary>
    [Obsolete($"Use {nameof(Name)} or {nameof(NameWithoutLocale)} instead. This property will be removed in SMAPI 4.0.0.")]
    string AssetName { get; }

    /// <summary>The content data type.</summary>
    Type DataType { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Get whether the asset name being loaded matches a given name after normalization.</summary>
    /// <param name="path">The expected asset path, relative to the game's content folder and without the .xnb extension or locale suffix (like 'Data\ObjectInformation').</param>
    [Obsolete($"Use {nameof(Name)}.{nameof(IAssetName.IsEquivalentTo)} or {nameof(NameWithoutLocale)}.{nameof(IAssetName.IsEquivalentTo)} instead. This method will be removed in SMAPI 4.0.0.")]
    bool AssetNameEquals(string path);
}
