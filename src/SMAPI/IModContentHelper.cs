using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using xTile;

namespace StardewModdingAPI;

/// <summary>Provides an API for loading content assets from the current mod's folder.</summary>
public interface IModContentHelper : IModLinked
{
    /*********
    ** Public methods
    *********/
    /// <summary>Load content from the mod folder and return it. When loading a <c>.png</c> file, this must be called outside the game's draw loop.</summary>
    /// <typeparam name="T">The expected data type. The main supported types are <see cref="Map"/>, <see cref="Texture2D"/>, <see cref="IRawTextureData"/>, and data structures; other types may be supported by the game's content pipeline.</typeparam>
    /// <param name="relativePath">The local path to a content file relative to the mod folder.</param>
    /// <exception cref="ArgumentException">The <paramref name="relativePath"/> is empty or contains invalid characters.</exception>
    /// <exception cref="ContentLoadException">The content asset couldn't be loaded (e.g. because it doesn't exist).</exception>
    T Load<T>(string relativePath)
        where T : notnull;

    /// <summary>Get the internal asset name which allows loading a mod file through any of the game's content managers. This can be used when passing asset names directly to the game (e.g. for map tilesheets), but should be avoided if you can use <see cref="Load{T}"/> instead. This does not validate whether the asset exists.</summary>
    /// <param name="relativePath">The local path to a content file relative to the mod folder.</param>
    /// <exception cref="ArgumentException">The <paramref name="relativePath"/> is empty or contains invalid characters.</exception>
    IAssetName GetInternalAssetName(string relativePath);

    /// <summary>Get a patch helper for arbitrary data.</summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="data">The asset data.</param>
    /// <param name="relativePath">The local path to the content file being edited relative to the mod folder. This is only used for tracking purposes and has no effect on the patch helper.</param>
    IAssetData GetPatchHelper<T>(T data, string? relativePath = null)
        where T : notnull;
}