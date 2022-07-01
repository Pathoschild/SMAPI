using System;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Framework.ContentManagers;

/// <summary>An extension of <see cref="GameContentManager"/> specifically optimized for asset propagation.</summary>
/// <remarks>This avoids sharing an asset cache with <see cref="Game1.content"/> or mods, so that assets can be safely disposed when the vanilla game no longer references them.</remarks>
internal class GameContentManagerForAssetPropagation : GameContentManager
{
    /*********
    ** Fields
    *********/
    /// <summary>A unique value used in <see cref="Texture2D"/> to identify assets loaded through this instance.</summary>
    private readonly string Tag = $"Pathoschild.SMAPI/LoadedBy:{nameof(GameContentManagerForAssetPropagation)}";


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public GameContentManagerForAssetPropagation(string name, IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, Action<BaseContentManager> onDisposing, Action onLoadingFirstAsset, Action<BaseContentManager, IAssetName> onAssetLoaded)
        : base(name, serviceProvider, rootDirectory, currentCulture, coordinator, monitor, reflection, onDisposing, onLoadingFirstAsset, onAssetLoaded) { }

    /// <inheritdoc />
    public override T LoadExact<T>(IAssetName assetName, bool useCache)
    {
        T data = base.LoadExact<T>(assetName, useCache);

        if (data is Texture2D texture)
            texture.Tag = this.Tag;

        return data;
    }

    /// <summary>Get whether a texture was loaded by this content manager.</summary>
    /// <param name="texture">The texture to check.</param>
    public bool IsResponsibleFor(Texture2D? texture)
    {
        return
            texture?.Tag is string tag
            && tag.Contains(this.Tag);
    }
}
