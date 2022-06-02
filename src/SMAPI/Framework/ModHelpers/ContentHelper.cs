using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Deprecations;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <summary>Provides an API for loading content assets.</summary>
[Obsolete($"Use {nameof(IMod.Helper)}.{nameof(IModHelper.GameContent)} or {nameof(IMod.Helper)}.{nameof(IModHelper.ModContent)} instead. This interface will be removed in SMAPI 4.0.0.")]
internal class ContentHelper : BaseHelper, IContentHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>SMAPI's core content logic.</summary>
    private readonly ContentCoordinator ContentCore;

    /// <summary>A content manager for this mod which manages files from the game's Content folder.</summary>
    private readonly IContentManager GameContentManager;

    /// <summary>A content manager for this mod which manages files from the mod's folder.</summary>
    private readonly ModContentManager ModContentManager;

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

    /// <summary>The observable implementation of <see cref="AssetEditors"/>.</summary>
    internal ObservableCollection<IAssetEditor> ObservableAssetEditors { get; } = new();

    /// <summary>The observable implementation of <see cref="AssetLoaders"/>.</summary>
    internal ObservableCollection<IAssetLoader> ObservableAssetLoaders { get; } = new();

    /// <inheritdoc />
    public IList<IAssetLoader> AssetLoaders
    {
        get
        {
            SCore.DeprecationManager.Warn(
                source: this.Mod,
                nounPhrase: $"{nameof(IContentHelper)}.{nameof(IContentHelper.AssetLoaders)}",
                version: "3.14.0",
                severity: DeprecationLevel.Notice
            );

            return this.ObservableAssetLoaders;
        }
    }

    /// <inheritdoc />
    public IList<IAssetEditor> AssetEditors
    {
        get
        {
            SCore.DeprecationManager.Warn(
                source: this.Mod,
                nounPhrase: $"{nameof(IContentHelper)}.{nameof(IContentHelper.AssetEditors)}",
                version: "3.14.0",
                severity: DeprecationLevel.Notice
            );

            return this.ObservableAssetEditors;
        }
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="contentCore">SMAPI's core content logic.</param>
    /// <param name="modFolderPath">The absolute path to the mod folder.</param>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="monitor">Encapsulates monitoring and logging.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    public ContentHelper(ContentCoordinator contentCore, string modFolderPath, IModMetadata mod, IMonitor monitor, Reflector reflection)
        : base(mod)
    {
        string managedAssetPrefix = contentCore.GetManagedAssetPrefix(mod.Manifest.UniqueID);

        this.ContentCore = contentCore;
        this.GameContentManager = contentCore.CreateGameContentManager(managedAssetPrefix + ".content");
        this.ModContentManager = contentCore.CreateModContentManager(managedAssetPrefix, this.Mod.DisplayName, modFolderPath, this.GameContentManager);
        this.Monitor = monitor;
        this.Reflection = reflection;
    }

    /// <inheritdoc />
    public T Load<T>(string key, ContentSource source = ContentSource.ModFolder)
        where T : notnull
    {
        IAssetName assetName = this.ContentCore.ParseAssetName(key, allowLocales: source == ContentSource.GameContent);

        try
        {
            this.AssertAndNormalizeAssetName(key);
            switch (source)
            {
                case ContentSource.GameContent:
                    if (assetName.Name.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                    {
                        assetName = this.ContentCore.ParseAssetName(assetName.Name[..^4], allowLocales: true);
                        SCore.DeprecationManager.Warn(
                            this.Mod,
                            "loading assets from the Content folder with a .xnb file extension",
                            "3.14.0",
                            DeprecationLevel.Notice
                        );
                    }

                    return this.GameContentManager.LoadLocalized<T>(assetName, this.CurrentLocaleConstant, useCache: false);

                case ContentSource.ModFolder:
                    try
                    {
                        return this.ModContentManager.LoadExact<T>(assetName, useCache: false);
                    }
                    catch (SContentLoadException ex) when (ex.ErrorType == ContentLoadErrorType.AssetDoesNotExist)
                    {
                        // legacy behavior: you can load a .xnb file without the file extension
                        try
                        {
                            IAssetName newName = this.ContentCore.ParseAssetName(assetName.Name + ".xnb", allowLocales: false);
                            if (this.ModContentManager.DoesAssetExist<T>(newName))
                            {
                                T data = this.ModContentManager.LoadExact<T>(newName, useCache: false);
                                SCore.DeprecationManager.Warn(
                                    this.Mod,
                                    "loading XNB files from the mod folder without the .xnb file extension",
                                    "3.14.0",
                                    DeprecationLevel.Notice
                                );
                                return data;
                            }
                        }
                        catch { /* legacy behavior failed, rethrow original error */ }

                        throw;
                    }

                default:
                    throw new SContentLoadException(ContentLoadErrorType.Other, $"{this.Mod.DisplayName} failed loading content asset '{key}' from {source}: unknown content source '{source}'.");
            }
        }
        catch (Exception ex) when (ex is not SContentLoadException)
        {
            throw new SContentLoadException(ContentLoadErrorType.Other, $"{this.Mod.DisplayName} failed loading content asset '{key}' from {source}.", ex);
        }
    }

    /// <inheritdoc />
    [Pure]
    public string NormalizeAssetName(string? assetName)
    {
        return this.ModContentManager.AssertAndNormalizeAssetName(assetName);
    }

    /// <inheritdoc />
    public string GetActualAssetKey(string key, ContentSource source = ContentSource.ModFolder)
    {
        switch (source)
        {
            case ContentSource.GameContent:
                return this.GameContentManager.AssertAndNormalizeAssetName(key);

            case ContentSource.ModFolder:
                return this.ModContentManager.GetInternalAssetKey(key).Name;

            default:
                throw new NotSupportedException($"Unknown content source '{source}'.");
        }
    }

    /// <inheritdoc />
    public bool InvalidateCache(string key)
    {
        string actualKey = this.GetActualAssetKey(key, ContentSource.GameContent);
        this.Monitor.Log($"Requested cache invalidation for '{actualKey}'.");
        return this.ContentCore.InvalidateCache(asset => asset.Name.IsEquivalentTo(actualKey)).Any();
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
            assetName: this.ContentCore.ParseAssetName(assetName, allowLocales: true/* no way to know if it's a game or mod asset here*/),
            data: data,
            getNormalizedPath: this.NormalizeAssetName,
            reflection: this.Reflection
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Assert that the given key has a valid format.</summary>
    /// <param name="key">The asset key to check.</param>
    /// <exception cref="ArgumentException">The asset key is empty or contains invalid characters.</exception>
    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Parameter is only used for assertion checks by design.")]
    private void AssertAndNormalizeAssetName(string key)
    {
        this.ModContentManager.AssertAndNormalizeAssetName(key);
        if (Path.IsPathRooted(key))
            throw new ArgumentException("The asset key must not be an absolute path.");
    }
}