using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Framework.Exceptions;
using StardewValley;

namespace StardewModdingAPI.Framework.ContentManagers;

/// <summary>A content manager which handles reading files.</summary>
internal interface IContentManager : IDisposable
{
    /*********
    ** Accessors
    *********/
    /// <summary>A name for the mod manager. Not guaranteed to be unique.</summary>
    string Name { get; }

    /// <summary>The current language as a constant.</summary>
    LocalizedContentManager.LanguageCode Language { get; }

    /// <summary>The absolute path to the <see cref="ContentManager.RootDirectory"/>.</summary>
    string FullRootDirectory { get; }

    /// <summary>Whether this content manager can be targeted by managed asset keys (e.g. to load assets from a mod folder).</summary>
    bool IsNamespaced { get; }


    /*********
    ** Methods
    *********/
    /// <summary>Get whether an asset exists and can be loaded.</summary>
    /// <typeparam name="T">The expected asset type.</typeparam>
    /// <param name="assetName">The normalized asset name.</param>
    bool DoesAssetExist<T>(IAssetName assetName)
        where T : notnull;

    /// <summary>Load an asset through the content pipeline, using a localized variant of the <paramref name="assetName"/> if available.</summary>
    /// <typeparam name="T">The type of asset to load.</typeparam>
    /// <param name="assetName">The asset name relative to the loader root directory.</param>
    /// <param name="language">The language for which to load the asset.</param>
    /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
    T LoadLocalized<T>(IAssetName assetName, LocalizedContentManager.LanguageCode language, bool useCache)
        where T : notnull;

    /// <summary>Load an asset through the content pipeline, using the exact asset name without checking for localized variants.</summary>
    /// <typeparam name="T">The type of asset to load.</typeparam>
    /// <param name="assetName">The asset name relative to the loader root directory.</param>
    /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
    T LoadExact<T>(IAssetName assetName, bool useCache)
        where T : notnull;

    /// <summary>Assert that the given key has a valid format and return a normalized form consistent with the underlying cache.</summary>
    /// <param name="assetName">The asset key to check.</param>
    /// <exception cref="SContentLoadException">The asset key is empty or contains invalid characters.</exception>
    string AssertAndNormalizeAssetName(string? assetName);

    /// <summary>Get the current content locale.</summary>
    string GetLocale();

    /// <summary>The locale for a language.</summary>
    /// <param name="language">The language.</param>
    string GetLocale(LocalizedContentManager.LanguageCode language);

    /// <summary>Get whether the content manager has already loaded and cached the given asset.</summary>
    /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
    bool IsLoaded(IAssetName assetName);

    /// <summary>Get all assets in the cache.</summary>
    IEnumerable<KeyValuePair<string, object>> GetCachedAssets();

    /// <summary>Purge matched assets from the cache.</summary>
    /// <param name="assetName">The asset name to dispose.</param>
    /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
    /// <returns>Returns whether the asset was in the cache.</returns>
    bool InvalidateCache(IAssetName assetName, bool dispose = false);
}
