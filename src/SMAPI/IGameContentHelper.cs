using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using xTile;

namespace StardewModdingAPI;

/// <summary>Provides an API for loading content assets from the game's <c>Content</c> folder or via <see cref="IModEvents.Content"/>.</summary>
public interface IGameContentHelper : IModLinked
{
    /*********
    ** Accessors
    *********/
    /// <summary>The game's current locale code (like <c>pt-BR</c>).</summary>
    string CurrentLocale { get; }

    /// <summary>The game's current locale as an enum value.</summary>
    LocalizedContentManager.LanguageCode CurrentLocaleConstant { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Parse a raw asset name.</summary>
    /// <param name="rawName">The raw asset name to parse.</param>
    /// <exception cref="ArgumentException">The <paramref name="rawName"/> is null or empty.</exception>
    IAssetName ParseAssetName(string rawName);

    /// <summary>Load content from the game folder or mod folder (if not already cached), and return it. When loading a <c>.png</c> file, this must be called outside the game's draw loop.</summary>
    /// <typeparam name="T">The expected data type. The main supported types are <see cref="Map"/>, <see cref="Texture2D"/>, dictionaries, and lists; other types may be supported by the game's content pipeline.</typeparam>
    /// <param name="assetName">The asset name to load.</param>
    /// <exception cref="ArgumentException">The <paramref name="assetName"/> is empty or contains invalid characters.</exception>
    /// <exception cref="ContentLoadException">The content asset couldn't be loaded (e.g. because it doesn't exist).</exception>
    T Load<T>(string assetName)
        where T : notnull;

    /// <summary>Load content from the game folder or mod folder (if not already cached), and return it. When loading a <c>.png</c> file, this must be called outside the game's draw loop.</summary>
    /// <typeparam name="T">The expected data type. The main supported types are <see cref="Map"/>, <see cref="Texture2D"/>, dictionaries, and lists; other types may be supported by the game's content pipeline.</typeparam>
    /// <param name="assetName">The asset name to load.</param>
    /// <exception cref="ArgumentException">The <paramref name="assetName"/> is empty or contains invalid characters.</exception>
    /// <exception cref="ContentLoadException">The content asset couldn't be loaded (e.g. because it doesn't exist).</exception>
    T Load<T>(IAssetName assetName)
        where T : notnull;

    /// <summary>Remove an asset from the content cache so it's reloaded on the next request. This will reload core game assets if needed, but references to the former asset will still show the previous content.</summary>
    /// <param name="assetName">The asset key to invalidate in the content folder.</param>
    /// <exception cref="ArgumentException">The <paramref name="assetName"/> is empty or contains invalid characters.</exception>
    /// <returns>Returns whether the given asset key was cached.</returns>
    bool InvalidateCache(string assetName);

    /// <summary>Remove an asset from the content cache so it's reloaded on the next request. This will reload core game assets if needed, but references to the former asset will still show the previous content.</summary>
    /// <param name="assetName">The asset key to invalidate in the content folder.</param>
    /// <exception cref="ArgumentException">The <paramref name="assetName"/> is empty or contains invalid characters.</exception>
    /// <returns>Returns whether the given asset key was cached.</returns>
    bool InvalidateCache(IAssetName assetName);

    /// <summary>Remove all assets of the given type from the cache so they're reloaded on the next request. <b>This can be a very expensive operation and should only be used in very specific cases.</b> This will reload core game assets if needed, but references to the former assets will still show the previous content.</summary>
    /// <typeparam name="T">The asset type to remove from the cache.</typeparam>
    /// <returns>Returns whether any assets were invalidated.</returns>
    bool InvalidateCache<T>()
        where T : notnull;

    /// <summary>Remove matching assets from the content cache so they're reloaded on the next request. This will reload core game assets if needed, but references to the former asset will still show the previous content.</summary>
    /// <param name="predicate">A predicate matching the assets to invalidate.</param>
    /// <returns>Returns whether any cache entries were invalidated.</returns>
    bool InvalidateCache(Func<IAssetInfo, bool> predicate);

    /// <summary>Get a patch helper for arbitrary data.</summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="data">The asset data.</param>
    /// <param name="assetName">The asset name. This is only used for tracking purposes and has no effect on the patch helper.</param>
    IAssetData GetPatchHelper<T>(T data, string? assetName = null)
        where T : notnull;
}
