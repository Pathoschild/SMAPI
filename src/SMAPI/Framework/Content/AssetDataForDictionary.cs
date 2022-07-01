using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework.Content;

/// <summary>Encapsulates access and changes to dictionary content being read from a data file.</summary>
internal class AssetDataForDictionary<TKey, TValue> : AssetData<IDictionary<TKey, TValue>>, IAssetDataForDictionary<TKey, TValue>
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locale">The content's locale code, if the content is localized.</param>
    /// <param name="assetName">The asset name being read.</param>
    /// <param name="data">The content data being read.</param>
    /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
    /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
    public AssetDataForDictionary(string? locale, IAssetName assetName, IDictionary<TKey, TValue> data, Func<string, string> getNormalizedPath, Action<IDictionary<TKey, TValue>> onDataReplaced)
        : base(locale, assetName, data, getNormalizedPath, onDataReplaced) { }
}
