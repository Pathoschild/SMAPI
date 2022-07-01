using System;

namespace StardewModdingAPI.Framework.Content;

/// <summary>Base implementation for a content helper which encapsulates access and changes to content being read from a data file.</summary>
/// <typeparam name="TValue">The interface value type.</typeparam>
internal class AssetData<TValue> : AssetInfo, IAssetData<TValue>
    where TValue : notnull
{
    /*********
    ** Fields
    *********/
    /// <summary>A callback to invoke when the data is replaced (if any).</summary>
    private readonly Action<TValue>? OnDataReplaced;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public TValue Data { get; protected set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locale">The content's locale code, if the content is localized.</param>
    /// <param name="assetName">The asset name being read.</param>
    /// <param name="data">The content data being read.</param>
    /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
    /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
    public AssetData(string? locale, IAssetName assetName, TValue data, Func<string, string> getNormalizedPath, Action<TValue>? onDataReplaced)
        : base(locale, assetName, data.GetType(), getNormalizedPath)
    {
        this.Data = data;
        this.OnDataReplaced = onDataReplaced;
    }

    /// <inheritdoc />
    public void ReplaceWith(TValue value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Can't set a loaded asset to a null value.");
        if (!this.DataType.IsInstanceOfType(value))
            throw new InvalidCastException($"Can't replace loaded asset of type {this.GetFriendlyTypeName(this.DataType)} with value of type {this.GetFriendlyTypeName(value.GetType())}. The new type must be compatible to prevent game errors.");

        this.Data = value;
        this.OnDataReplaced?.Invoke(value);
    }
}
