using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Deprecations;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Internal;
using StardewValley;
using xTile;
using xTile.Tiles;

namespace StardewModdingAPI.Framework.ContentManagers;

/// <summary>A content manager which handles reading files from the game content folder with support for interception.</summary>
internal class GameContentManager : BaseContentManager
{
    /*********
    ** Fields
    *********/
    /// <summary>The assets currently being intercepted by <see cref="IAssetLoader"/> instances. This is used to prevent infinite loops when a loader loads a new asset.</summary>
    private readonly ContextHash<string> AssetsBeingLoaded = new();

    /// <summary>Whether the next load is the first for any game content manager.</summary>
    private static bool IsFirstLoad = true;

    /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
    private readonly Action OnLoadingFirstAsset;

    /// <summary>A callback to invoke when an asset is fully loaded.</summary>
    private readonly Action<BaseContentManager, IAssetName> OnAssetLoaded;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
    /// <param name="serviceProvider">The service provider to use to locate services.</param>
    /// <param name="rootDirectory">The root directory to search for content.</param>
    /// <param name="currentCulture">The current culture for which to localize content.</param>
    /// <param name="coordinator">The central coordinator which manages content managers.</param>
    /// <param name="monitor">Encapsulates monitoring and logging.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    /// <param name="onDisposing">A callback to invoke when the content manager is being disposed.</param>
    /// <param name="onLoadingFirstAsset">A callback to invoke the first time *any* game content manager loads an asset.</param>
    /// <param name="onAssetLoaded">A callback to invoke when an asset is fully loaded.</param>
    public GameContentManager(string name, IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, Action<BaseContentManager> onDisposing, Action onLoadingFirstAsset, Action<BaseContentManager, IAssetName> onAssetLoaded)
        : base(name, serviceProvider, rootDirectory, currentCulture, coordinator, monitor, reflection, onDisposing, isNamespaced: false)
    {
        this.OnLoadingFirstAsset = onLoadingFirstAsset;
        this.OnAssetLoaded = onAssetLoaded;
    }

    /// <inheritdoc />
    public override bool DoesAssetExist<T>(IAssetName assetName)
    {
        if (base.DoesAssetExist<T>(assetName))
            return true;

        // vanilla asset
        if (File.Exists(Path.Combine(this.RootDirectory, $"{assetName.Name}.xnb")))
            return true;

        // managed asset
        if (this.Coordinator.TryParseManagedAssetKey(assetName.Name, out string? contentManagerID, out IAssetName? relativePath))
            return this.Coordinator.DoesManagedAssetExist<T>(contentManagerID, relativePath);

        // custom asset from a loader
        string locale = this.GetLocale();
        IAssetInfo info = new AssetInfo(locale, assetName, typeof(T), this.AssertAndNormalizeAssetName);
        AssetOperationGroup? operations = this.Coordinator.GetAssetOperations
#if SMAPI_DEPRECATED
            <T>
#endif
            (info);
        if (operations?.LoadOperations.Count > 0)
        {
            if (!this.AssertMaxOneRequiredLoader(info, operations.LoadOperations, out string? error))
            {
                this.Monitor.Log(error, LogLevel.Warn);
                return false;
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override T LoadExact<T>(IAssetName assetName, bool useCache)
    {
        if (typeof(IRawTextureData).IsAssignableFrom(typeof(T)))
            throw new SContentLoadException(ContentLoadErrorType.Other, $"Can't load {nameof(IRawTextureData)} assets from the game content pipeline. This asset type is only available for mod files.");

        // raise first-load callback
        if (GameContentManager.IsFirstLoad)
        {
            GameContentManager.IsFirstLoad = false;
            this.OnLoadingFirstAsset();
        }

        // get from cache
        if (useCache && this.IsLoaded(assetName))
            return this.RawLoad<T>(assetName, useCache: true);

        // get managed asset
        if (this.Coordinator.TryParseManagedAssetKey(assetName.Name, out string? contentManagerID, out IAssetName? relativePath))
        {
            T managedAsset = this.Coordinator.LoadManagedAsset<T>(contentManagerID, relativePath);
            this.TrackAsset(assetName, managedAsset, useCache);
            return managedAsset;
        }

        // load asset
        T data;
        if (this.AssetsBeingLoaded.Contains(assetName.Name))
        {
            this.Monitor.Log($"Broke loop while loading asset '{assetName}'.", LogLevel.Warn);
            this.Monitor.Log($"Bypassing mod loaders for this asset. Stack trace:\n{Environment.StackTrace}");
            data = this.RawLoad<T>(assetName, useCache);
        }
        else
        {
            data = this.AssetsBeingLoaded.Track(assetName.Name, () =>
            {
                IAssetInfo info = new AssetInfo(assetName.LocaleCode, assetName, typeof(T), this.AssertAndNormalizeAssetName);
                AssetOperationGroup? operations = this.Coordinator.GetAssetOperations
#if SMAPI_DEPRECATED
                    <T>
#endif
                    (info);
                IAssetData asset =
                    this.ApplyLoader<T>(info, operations?.LoadOperations)
                    ?? new AssetDataForObject(info, this.RawLoad<T>(assetName, useCache), this.AssertAndNormalizeAssetName, this.Reflection);
                asset = this.ApplyEditors<T>(info, asset, operations?.EditOperations);
                return (T)asset.Data;
            });
        }

        // update cache
        this.TrackAsset(assetName, data, useCache);

        // raise event & return data
        this.OnAssetLoaded(this, assetName);
        return data;
    }

    /// <inheritdoc />
    public override LocalizedContentManager CreateTemporary()
    {
        return this.Coordinator.CreateGameContentManager("(temporary)");
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Load the initial asset from the registered loaders.</summary>
    /// <param name="info">The basic asset metadata.</param>
    /// <param name="loadOperations">The load operations to apply to the asset.</param>
    /// <returns>Returns the loaded asset metadata, or <c>null</c> if no loader matched.</returns>
    private IAssetData? ApplyLoader<T>(IAssetInfo info, List<AssetLoadOperation>? loadOperations)
        where T : notnull
    {
        // find matching loader
        AssetLoadOperation? loader = null;
        if (loadOperations?.Count > 0)
        {
            if (!this.AssertMaxOneRequiredLoader(info, loadOperations, out string? error))
            {
                this.Monitor.Log(error, LogLevel.Warn);
                return null;
            }

            loader = loadOperations.OrderByDescending(p => p.Priority).FirstOrDefault();
        }
        if (loader == null)
            return null;

        // fetch asset from loader
        IModMetadata mod = loader.Mod;
        T data;
        Context.HeuristicModsRunningCode.Push(loader.Mod);
        try
        {
            data = (T)loader.GetData(info);
            this.Monitor.Log($"{mod.DisplayName} loaded asset '{info.Name}'{this.GetOnBehalfOfLabel(loader.OnBehalfOf)}.");
        }
        catch (Exception ex)
        {
            mod.LogAsMod($"Mod crashed when loading asset '{info.Name}'{this.GetOnBehalfOfLabel(loader.OnBehalfOf)}. SMAPI will use the default asset instead. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
            return null;
        }
        finally
        {
            Context.HeuristicModsRunningCode.TryPop(out _);
        }

        // return matched asset
        return this.TryFixAndValidateLoadedAsset(info, data, loader)
            ? new AssetDataForObject(info, data, this.AssertAndNormalizeAssetName, this.Reflection)
            : null;
    }

    /// <summary>Apply any editors to a loaded asset.</summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="info">The basic asset metadata.</param>
    /// <param name="asset">The loaded asset.</param>
    /// <param name="editOperations">The edit operations to apply to the asset.</param>
    private IAssetData ApplyEditors<T>(IAssetInfo info, IAssetData asset, List<AssetEditOperation>? editOperations)
        where T : notnull
    {
        if (editOperations?.Count is not > 0)
            return asset;

        IAssetData GetNewData(object data) => new AssetDataForObject(info, data, this.AssertAndNormalizeAssetName, this.Reflection);

        // special case: if the asset was loaded with a more general type like 'object', call editors with the actual type instead.
        {
            Type actualType = asset.Data.GetType();
            Type? actualOpenType = actualType.IsGenericType ? actualType.GetGenericTypeDefinition() : null;

            if (typeof(T) != actualType && (actualOpenType == typeof(Dictionary<,>) || actualOpenType == typeof(List<>) || actualType == typeof(Texture2D) || actualType == typeof(Map)))
            {
                return (IAssetData)this.GetType()
                    .GetMethod(nameof(this.ApplyEditors), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(actualType)
                    .Invoke(this, new object[] { info, asset, editOperations })!;
            }
        }

        // edit asset
        AssetEditOperation[] editors = editOperations.OrderBy(p => p.Priority).ToArray();
        foreach (AssetEditOperation editor in editors)
        {
            IModMetadata mod = editor.Mod;

            // try edit
            object prevAsset = asset.Data;
            Context.HeuristicModsRunningCode.Push(editor.Mod);
            try
            {
                editor.ApplyEdit(asset);
                this.Monitor.Log($"{mod.DisplayName} edited {info.Name}{this.GetOnBehalfOfLabel(editor.OnBehalfOf)}.");
            }
            catch (Exception ex)
            {
                mod.LogAsMod($"Mod crashed when editing asset '{info.Name}'{this.GetOnBehalfOfLabel(editor.OnBehalfOf)}, which may cause errors in-game. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
            }
            finally
            {
                Context.HeuristicModsRunningCode.TryPop(out _);
            }

            // validate edit
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract -- it's only guaranteed non-null after this method
            if (asset.Data == null)
            {
                mod.LogAsMod($"Mod incorrectly set asset '{info.Name}'{this.GetOnBehalfOfLabel(editor.OnBehalfOf)} to a null value; ignoring override.", LogLevel.Warn);
                asset = GetNewData(prevAsset);
            }
            else if (asset.Data is not T)
            {
                mod.LogAsMod($"Mod incorrectly set asset '{asset.Name}'{this.GetOnBehalfOfLabel(editor.OnBehalfOf)} to incompatible type '{asset.Data.GetType()}', expected '{typeof(T)}'; ignoring override.", LogLevel.Warn);
                asset = GetNewData(prevAsset);
            }
        }

        // return result
        return asset;
    }

    /// <summary>Assert that at most one loader will be applied to an asset.</summary>
    /// <param name="info">The basic asset metadata.</param>
    /// <param name="loaders">The asset loaders to apply.</param>
    /// <param name="error">The error message to show to the user, if the method returns false.</param>
    /// <returns>Returns true if only one loader will apply, else false.</returns>
    private bool AssertMaxOneRequiredLoader(IAssetInfo info, List<AssetLoadOperation> loaders, [NotNullWhen(false)] out string? error)
    {
        AssetLoadOperation[] required = loaders.Where(p => p.Priority == AssetLoadPriority.Exclusive).ToArray();
        if (required.Length <= 1)
        {
            error = null;
            return true;
        }

        string[] loaderNames = required
            .Select(p => p.Mod.DisplayName + this.GetOnBehalfOfLabel(p.OnBehalfOf))
            .OrderBy(p => p)
            .Distinct()
            .ToArray();
        string errorPhrase = loaderNames.Length > 1
            ? $"Multiple mods want to provide the '{info.Name}' asset: {string.Join(", ", loaderNames)}"
            : $"The '{loaderNames[0]}' mod wants to provide the '{info.Name}' asset multiple times";

        error = $"{errorPhrase}. An asset can't be loaded multiple times, so SMAPI will use the default asset instead. Uninstall one of the mods to fix this. (Message for modders: you should avoid {nameof(AssetLoadPriority)}.{nameof(AssetLoadPriority.Exclusive)}"
#if SMAPI_DEPRECATED
                + " and {nameof(IAssetLoader)}"
#endif
                + " if possible to avoid conflicts.)";
        return false;
    }

    /// <summary>Get a parenthetical label for log messages for the content pack on whose behalf the action is being performed, if any.</summary>
    /// <param name="onBehalfOf">The content pack on whose behalf the action is being performed.</param>
    /// <param name="parenthetical">whether to format the label as a parenthetical shown after the mod name like <c> (for the 'X' content pack)</c>, instead of a standalone label like <c>the 'X' content pack</c>.</param>
    /// <returns>Returns the on-behalf-of label if applicable, else <c>null</c>.</returns>
    [return: NotNullIfNotNull("onBehalfOf")]
    private string? GetOnBehalfOfLabel(IModMetadata? onBehalfOf, bool parenthetical = true)
    {
        if (onBehalfOf == null)
            return null;

        return parenthetical
            ? $" (for the '{onBehalfOf.Manifest.Name}' content pack)"
            : $"the '{onBehalfOf.Manifest.Name}' content pack";
    }

    /// <summary>Validate that an asset loaded by a mod is valid and won't cause issues, and fix issues if possible.</summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="info">The basic asset metadata.</param>
    /// <param name="data">The loaded asset data.</param>
    /// <param name="loader">The loader which loaded the asset.</param>
    /// <returns>Returns whether the asset passed validation checks (after any fixes were applied).</returns>
    private bool TryFixAndValidateLoadedAsset<T>(IAssetInfo info, [NotNullWhen(true)] T? data, AssetLoadOperation loader)
        where T : notnull
    {
        IModMetadata mod = loader.Mod;

        // can't load a null asset
        if (data == null)
        {
            mod.LogAsMod($"SMAPI blocked asset replacement for '{info.Name}': {this.GetOnBehalfOfLabel(loader.OnBehalfOf, parenthetical: false) ?? "mod"} incorrectly set asset to a null value.", LogLevel.Error);
            return false;
        }

        // when replacing a map, the vanilla tilesheets must have the same order and IDs
        if (data is Map loadedMap)
        {
            TilesheetReference[] vanillaTilesheetRefs = this.Coordinator.GetVanillaTilesheetIds(info.Name.Name);
            foreach (TilesheetReference vanillaSheet in vanillaTilesheetRefs)
            {
                // add missing tilesheet
                if (loadedMap.GetTileSheet(vanillaSheet.Id) == null)
                {
                    mod.Monitor!.LogOnce("SMAPI fixed maps loaded by this mod to prevent errors. See the log file for details.", LogLevel.Warn);
                    this.Monitor.Log($"Fixed broken map replacement: {mod.DisplayName} loaded '{info.Name}' without a required tilesheet (id: {vanillaSheet.Id}, source: {vanillaSheet.ImageSource}).");

                    loadedMap.AddTileSheet(new TileSheet(vanillaSheet.Id, loadedMap, vanillaSheet.ImageSource, vanillaSheet.SheetSize, vanillaSheet.TileSize));
                }

                // handle mismatch
                if (loadedMap.TileSheets.Count <= vanillaSheet.Index || loadedMap.TileSheets[vanillaSheet.Index].Id != vanillaSheet.Id)
                {
#if SMAPI_DEPRECATED
                    // only show warning if not farm map
                    // This is temporary: mods shouldn't do this for any vanilla map, but these are the ones we know will crash. Showing a warning for others instead gives modders time to update their mods, while still simplifying troubleshooting.
                    bool isFarmMap = info.Name.IsEquivalentTo("Maps/Farm") || info.Name.IsEquivalentTo("Maps/Farm_Combat") || info.Name.IsEquivalentTo("Maps/Farm_Fishing") || info.Name.IsEquivalentTo("Maps/Farm_Foraging") || info.Name.IsEquivalentTo("Maps/Farm_FourCorners") || info.Name.IsEquivalentTo("Maps/Farm_Island") || info.Name.IsEquivalentTo("Maps/Farm_Mining");

                    string reason = $"{this.GetOnBehalfOfLabel(loader.OnBehalfOf, parenthetical: false) ?? "mod"} reordered the original tilesheets, which {(isFarmMap ? "would cause a crash" : "often causes crashes")}.\nTechnical details for mod author: Expected order: {string.Join(", ", vanillaTilesheetRefs.Select(p => p.Id))}. See https://stardewvalleywiki.com/Modding:Maps#Tilesheet_order for help.";

                    SCore.DeprecationManager.PlaceholderWarn("3.8.2", DeprecationLevel.PendingRemoval);
                    if (isFarmMap)
                    {
                        mod.LogAsMod($"SMAPI blocked a '{info.Name}' map load: {reason}", LogLevel.Error);
                        return false;
                    }

                    mod.LogAsMod($"SMAPI found an issue with a '{info.Name}' map load: {reason}", LogLevel.Warn);
#else
                        mod.LogAsMod($"SMAPI found an issue with a '{info.Name}' map load: {this.GetOnBehalfOfLabel(loader.OnBehalfOf, parenthetical: false) ?? "mod"} reordered the original tilesheets, which often causes crashes.\nTechnical details for mod author: Expected order: {string.Join(", ", vanillaTilesheetRefs.Select(p => p.Id))}. See https://stardewvalleywiki.com/Modding:Maps#Tilesheet_order for help.", LogLevel.Error);
                        return false;
#endif
                }
            }
        }

        return true;
    }
}
