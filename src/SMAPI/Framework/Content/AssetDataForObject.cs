using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Reflection;
using xTile;

namespace StardewModdingAPI.Framework.Content;

/// <summary>Encapsulates access and changes to content being read from a data file.</summary>
internal class AssetDataForObject : AssetData<object>, IAssetData
{
    /*********
    ** Fields
    *********/
    /// <summary>Simplifies access to private code.</summary>
    private readonly Reflector Reflection;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locale">The content's locale code, if the content is localized.</param>
    /// <param name="assetName">The asset name being read.</param>
    /// <param name="data">The content data being read.</param>
    /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
    public AssetDataForObject(string? locale, IAssetName assetName, object data, Func<string, string> getNormalizedPath, Reflector reflection, Action<object>? onDataReplaced = null)
        : base(locale, assetName, data, getNormalizedPath, onDataReplaced)
    {
        this.Reflection = reflection;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="info">The asset metadata.</param>
    /// <param name="data">The content data being read.</param>
    /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
    public AssetDataForObject(IAssetInfo info, object data, Func<string, string> getNormalizedPath, Reflector reflection, Action<object>? onDataReplaced = null)
        : this(info.Locale, info.Name, data, getNormalizedPath, reflection, onDataReplaced) { }

    /// <inheritdoc />
    public IAssetDataForDictionary<TKey, TValue> AsDictionary<TKey, TValue>()
    {
        return new AssetDataForDictionary<TKey, TValue>(this.Locale, this.Name, this.GetData<IDictionary<TKey, TValue>>(), this.GetNormalizedPath, this.ReplaceWith);
    }

    /// <inheritdoc />
    public IAssetDataForImage AsImage()
    {
        return new AssetDataForImage(this.Locale, this.Name, this.GetData<Texture2D>(), this.GetNormalizedPath, this.ReplaceWith);
    }

    /// <inheritdoc />
    public IAssetDataForMap AsMap()
    {
        return new AssetDataForMap(this.Locale, this.Name, this.GetData<Map>(), this.GetNormalizedPath, this.ReplaceWith, this.Reflection);
    }

    /// <inheritdoc />
    public TData GetData<TData>()
    {
        if (this.Data is not TData data)
            throw new InvalidCastException($"The content data of type {this.Data.GetType().FullName} can't be converted to the requested {typeof(TData).FullName}.");
        return data;
    }
}
