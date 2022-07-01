using System;
using System.Linq;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <inheritdoc cref="IGameContentHelper"/>
internal class GameContentHelper : BaseHelper, IGameContentHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>SMAPI's core content logic.</summary>
    private readonly ContentCoordinator ContentCore;

    /// <summary>The underlying game content manager.</summary>
    private readonly IContentManager GameContentManager;

    /// <summary>The friendly mod name for use in errors.</summary>
    private readonly string ModName;

    /// <summary>Encapsulates monitoring and logging.</summary>
    private readonly IMonitor Monitor;

    /// <summary>Simplifies access to private code.</summary>
    private readonly Reflector Reflection;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string CurrentLocale => this.GameContentManager.GetLocale();

    /// <inheritdoc />
    public LocalizedContentManager.LanguageCode CurrentLocaleConstant => this.GameContentManager.Language;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="contentCore">SMAPI's core content logic.</param>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="modName">The friendly mod name for use in errors.</param>
    /// <param name="monitor">Encapsulates monitoring and logging.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    public GameContentHelper(ContentCoordinator contentCore, IModMetadata mod, string modName, IMonitor monitor, Reflector reflection)
        : base(mod)
    {
        string managedAssetPrefix = contentCore.GetManagedAssetPrefix(mod.Manifest.UniqueID);

        this.ContentCore = contentCore;
        this.GameContentManager = contentCore.CreateGameContentManager(managedAssetPrefix + ".content");
        this.ModName = modName;
        this.Monitor = monitor;
        this.Reflection = reflection;
    }

    /// <inheritdoc />
    public IAssetName ParseAssetName(string rawName)
    {
        return this.ContentCore.ParseAssetName(rawName, allowLocales: true);
    }

    /// <inheritdoc />
    public T Load<T>(string key)
        where T : notnull
    {
        IAssetName assetName = this.ContentCore.ParseAssetName(key, allowLocales: true);
        return this.Load<T>(assetName);
    }

    /// <inheritdoc />
    public T Load<T>(IAssetName assetName)
        where T : notnull
    {
        try
        {
            return this.GameContentManager.LoadLocalized<T>(assetName, this.CurrentLocaleConstant, useCache: true);
        }
        catch (Exception ex) when (ex is not SContentLoadException)
        {
            throw new SContentLoadException(ContentLoadErrorType.Other, $"{this.ModName} failed loading content asset '{assetName}' from the game content.", ex);
        }
    }

    /// <inheritdoc />
    public bool InvalidateCache(string key)
    {
        IAssetName assetName = this.ParseAssetName(key);
        return this.InvalidateCache(assetName);
    }

    /// <inheritdoc />
    public bool InvalidateCache(IAssetName assetName)
    {
        this.Monitor.Log($"Requested cache invalidation for '{assetName}'.");
        return this.ContentCore.InvalidateCache(asset => asset.Name.IsEquivalentTo(assetName)).Any();
    }

    /// <inheritdoc />
    public bool InvalidateCache<T>()
        where T : notnull
    {
        this.Monitor.Log($"Requested cache invalidation for all assets of type {typeof(T)}. This is an expensive operation and should be avoided if possible.");
        return this.ContentCore.InvalidateCache((_, _, type) => typeof(T).IsAssignableFrom(type)).Any();
    }

    /// <inheritdoc />
    public bool InvalidateCache(Func<IAssetInfo, bool> predicate)
    {
        this.Monitor.Log("Requested cache invalidation for all assets matching a predicate.");
        return this.ContentCore.InvalidateCache(predicate).Any();
    }

    /// <inheritdoc />
    public IAssetData GetPatchHelper<T>(T data, string? assetName = null)
        where T : notnull
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "Can't get a patch helper for a null value.");

        assetName ??= $"temp/{Guid.NewGuid():N}";

        return new AssetDataForObject(
            locale: this.CurrentLocale,
            assetName: this.ContentCore.ParseAssetName(assetName, allowLocales: true),
            data: data,
            getNormalizedPath: key => this.ParseAssetName(key).Name,
            reflection: this.Reflection
        );
    }

    /// <summary>Get the underlying game content manager.</summary>
    internal IContentManager GetUnderlyingContentManager()
    {
        return this.GameContentManager;
    }
}
