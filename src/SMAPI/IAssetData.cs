using System;

namespace StardewModdingAPI;

/// <summary>Generic metadata and methods for a content asset being loaded.</summary>
/// <typeparam name="TValue">The expected data type.</typeparam>
public interface IAssetData<TValue> : IAssetInfo
    where TValue : notnull
{
    /*********
    ** Accessors
    *********/
    /// <summary>The content data being read.</summary>
    TValue Data { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Replace the entire content value with the given value. This is generally not recommended, since it may break compatibility with other mods or different versions of the game.</summary>
    /// <param name="value">The new content value.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="value"/> is null.</exception>
    /// <exception cref="InvalidCastException">The <paramref name="value"/>'s type is not compatible with the loaded asset's type.</exception>
    void ReplaceWith(TValue value);
}

/// <summary>Generic metadata and methods for a content asset being loaded.</summary>
public interface IAssetData : IAssetData<object>
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get a helper to manipulate the data as a dictionary.</summary>
    /// <typeparam name="TKey">The expected dictionary key.</typeparam>
    /// <typeparam name="TValue">The expected dictionary value.</typeparam>
    /// <exception cref="InvalidOperationException">The content being read isn't a dictionary.</exception>
    IAssetDataForDictionary<TKey, TValue> AsDictionary<TKey, TValue>();

    /// <summary>Get a helper to manipulate the data as an image.</summary>
    /// <exception cref="InvalidOperationException">The content being read isn't an image.</exception>
    IAssetDataForImage AsImage();

    /// <summary>Get a helper to manipulate the data as a map.</summary>
    /// <exception cref="InvalidOperationException">The content being read isn't a map.</exception>
    IAssetDataForMap AsMap();

    /// <summary>Get the data as a given type.</summary>
    /// <typeparam name="TData">The expected data type.</typeparam>
    /// <exception cref="InvalidCastException">The data can't be converted to <typeparamref name="TData"/>.</exception>
    TData GetData<TData>();
}
