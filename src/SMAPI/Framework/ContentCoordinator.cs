using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Metadata;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities.PathLookups;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData;
using xTile;

namespace StardewModdingAPI.Framework;

/// <summary>The central logic for creating content managers, invalidating caches, and propagating asset changes.</summary>
internal class ContentCoordinator : IDisposable
{
    /*********
    ** Fields
    *********/
    /// <summary>An asset key prefix for assets from SMAPI mod folders.</summary>
    private readonly string ManagedPrefix = "SMAPI";

    /// <summary>Whether to use raw image data when possible, instead of initializing an XNA Texture2D instance through the GPU.</summary>
    private readonly bool UseRawImageLoading;

    /// <summary>Get a file lookup for the given directory.</summary>
    private readonly Func<string, IFileLookup> GetFileLookup;

    /// <summary>Encapsulates monitoring and logging.</summary>
    private readonly IMonitor Monitor;

    /// <summary>Provides metadata for core game assets.</summary>
    private readonly CoreAssetPropagator CoreAssets;

    /// <summary>Simplifies access to private code.</summary>
    private readonly Reflector Reflection;

    /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
    private readonly JsonHelper JsonHelper;

    /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
    private readonly Action OnLoadingFirstAsset;

    /// <summary>A callback to invoke when an asset is fully loaded.</summary>
    private readonly Action<BaseContentManager, IAssetName> OnAssetLoaded;

    /// <summary>A callback to invoke when any asset names have been invalidated from the cache.</summary>
    private readonly Action<IList<IAssetName>> OnAssetsInvalidated;

    /// <summary>Get the load/edit operations to apply to an asset by querying registered <see cref="IContentEvents.AssetRequested"/> event handlers.</summary>
    private readonly Func<IAssetInfo, AssetOperationGroup?> RequestAssetOperations;

    /// <summary>The loaded content managers (including the <see cref="MainContentManager"/>).</summary>
    private readonly List<IContentManager> ContentManagers = new();

    /// <summary>Whether the content coordinator has been disposed.</summary>
    private bool IsDisposed;

    /// <summary>A lock used to prevent asynchronous changes to the content manager list.</summary>
    /// <remarks>The game may add content managers in asynchronous threads (e.g. when populating the load screen).</remarks>
    private readonly ReaderWriterLockSlim ContentManagerLock = new();

    /// <summary>A cache of ordered tilesheet IDs used by vanilla maps.</summary>
    private readonly Dictionary<string, TilesheetReference[]?> VanillaTilesheets = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>An unmodified content manager which doesn't intercept assets, used to compare asset data.</summary>
    private readonly LocalizedContentManager VanillaContentManager;

    /// <summary>The language enum values indexed by locale code.</summary>
    private Lazy<Dictionary<string, LocalizedContentManager.LanguageCode>> LocaleCodes;

    /// <summary>The cached asset load/edit operations to apply, indexed by asset name.</summary>
    private readonly TickCacheDictionary<IAssetName, AssetOperationGroup?> AssetOperationsByKey = new();

    /// <summary>A cache of asset operation groups created for legacy <see cref="IAssetLoader"/> implementations.</summary>
    [Obsolete("This only exists to support legacy code and will be removed in SMAPI 4.0.0.")]
    private readonly Dictionary<IAssetLoader, Dictionary<Type, AssetLoadOperation>> LegacyLoaderCache = new(ReferenceEqualityComparer.Instance);

    /// <summary>A cache of asset operation groups created for legacy <see cref="IAssetEditor"/> implementations.</summary>
    [Obsolete("This only exists to support legacy code and will be removed in SMAPI 4.0.0.")]
    private readonly Dictionary<IAssetEditor, Dictionary<Type, AssetEditOperation>> LegacyEditorCache = new(ReferenceEqualityComparer.Instance);


    /*********
    ** Accessors
    *********/
    /// <summary>The primary content manager used for most assets.</summary>
    public GameContentManager MainContentManager { get; private set; }

    /// <summary>The current language as a constant.</summary>
    public LocalizedContentManager.LanguageCode Language => this.MainContentManager.Language;

    /// <summary>Interceptors which provide the initial versions of matching assets.</summary>
    [Obsolete("This only exists to support legacy code and will be removed in SMAPI 4.0.0.")]
    public IList<ModLinked<IAssetLoader>> Loaders { get; } = new List<ModLinked<IAssetLoader>>();

    /// <summary>Interceptors which edit matching assets after they're loaded.</summary>
    [Obsolete("This only exists to support legacy code and will be removed in SMAPI 4.0.0.")]
    public IList<ModLinked<IAssetEditor>> Editors { get; } = new List<ModLinked<IAssetEditor>>();

    /// <summary>The absolute path to the <see cref="ContentManager.RootDirectory"/>.</summary>
    public string FullRootDirectory { get; }

    /// <summary>A lookup which tracks whether each given asset name has a localized form.</summary>
    /// <remarks>This is a per-screen equivalent to the base game's <see cref="LocalizedContentManager.localizedAssetNames"/> field, since mods may provide different assets per-screen.</remarks>
    public PerScreen<Dictionary<string, string>> LocalizedAssetNames { get; } = new(() => new());


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="serviceProvider">The service provider to use to locate services.</param>
    /// <param name="rootDirectory">The root directory to search for content.</param>
    /// <param name="currentCulture">The current culture for which to localize content.</param>
    /// <param name="monitor">Encapsulates monitoring and logging.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
    /// <param name="onLoadingFirstAsset">A callback to invoke the first time *any* game content manager loads an asset.</param>
    /// <param name="onAssetLoaded">A callback to invoke when an asset is fully loaded.</param>
    /// <param name="getFileLookup">Get a file lookup for the given directory.</param>
    /// <param name="onAssetsInvalidated">A callback to invoke when any asset names have been invalidated from the cache.</param>
    /// <param name="requestAssetOperations">Get the load/edit operations to apply to an asset by querying registered <see cref="IContentEvents.AssetRequested"/> event handlers.</param>
    /// <param name="useRawImageLoading">Whether to use raw image data when possible, instead of initializing an XNA Texture2D instance through the GPU.</param>
    public ContentCoordinator(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, IMonitor monitor, Reflector reflection, JsonHelper jsonHelper, Action onLoadingFirstAsset, Action<BaseContentManager, IAssetName> onAssetLoaded, Func<string, IFileLookup> getFileLookup, Action<IList<IAssetName>> onAssetsInvalidated, Func<IAssetInfo, AssetOperationGroup?> requestAssetOperations, bool useRawImageLoading)
    {
        this.GetFileLookup = getFileLookup;
        this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        this.Reflection = reflection;
        this.JsonHelper = jsonHelper;
        this.OnLoadingFirstAsset = onLoadingFirstAsset;
        this.OnAssetLoaded = onAssetLoaded;
        this.OnAssetsInvalidated = onAssetsInvalidated;
        this.RequestAssetOperations = requestAssetOperations;
        this.FullRootDirectory = Path.Combine(Constants.GamePath, rootDirectory);
        this.UseRawImageLoading = useRawImageLoading;
        this.ContentManagers.Add(
            this.MainContentManager = new GameContentManager(
                name: "Game1.content",
                serviceProvider: serviceProvider,
                rootDirectory: rootDirectory,
                currentCulture: currentCulture,
                coordinator: this,
                monitor: monitor,
                reflection: reflection,
                onDisposing: this.OnDisposing,
                onLoadingFirstAsset: onLoadingFirstAsset,
                onAssetLoaded: onAssetLoaded
            )
        );

        var contentManagerForAssetPropagation = new GameContentManagerForAssetPropagation(
            name: nameof(GameContentManagerForAssetPropagation),
            serviceProvider: serviceProvider,
            rootDirectory: rootDirectory,
            currentCulture: currentCulture,
            coordinator: this,
            monitor: monitor,
            reflection: reflection,
            onDisposing: this.OnDisposing,
            onLoadingFirstAsset: onLoadingFirstAsset,
            onAssetLoaded: onAssetLoaded
        );
        this.ContentManagers.Add(contentManagerForAssetPropagation);

        this.VanillaContentManager = new LocalizedContentManager(serviceProvider, rootDirectory);
        this.CoreAssets = new CoreAssetPropagator(this.MainContentManager, contentManagerForAssetPropagation, this.Monitor, reflection, name => this.ParseAssetName(name, allowLocales: true));
        this.LocaleCodes = new Lazy<Dictionary<string, LocalizedContentManager.LanguageCode>>(() => this.GetLocaleCodes(customLanguages: Enumerable.Empty<ModLanguage>()));
    }

    /// <summary>Get a new content manager which handles reading files from the game content folder with support for interception.</summary>
    /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
    public GameContentManager CreateGameContentManager(string name)
    {
        return this.ContentManagerLock.InWriteLock(() =>
        {
            GameContentManager manager = new(
                name: name,
                serviceProvider: this.MainContentManager.ServiceProvider,
                rootDirectory: this.MainContentManager.RootDirectory,
                currentCulture: this.MainContentManager.CurrentCulture,
                coordinator: this,
                monitor: this.Monitor,
                reflection: this.Reflection,
                onDisposing: this.OnDisposing,
                onLoadingFirstAsset: this.OnLoadingFirstAsset,
                onAssetLoaded: this.OnAssetLoaded
            );
            this.ContentManagers.Add(manager);
            return manager;
        });
    }

    /// <summary>Get a new content manager which handles reading files from a SMAPI mod folder with support for unpacked files.</summary>
    /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
    /// <param name="modName">The mod display name to show in errors.</param>
    /// <param name="rootDirectory">The root directory to search for content (or <c>null</c> for the default).</param>
    /// <param name="gameContentManager">The game content manager used for map tilesheets not provided by the mod.</param>
    public ModContentManager CreateModContentManager(string name, string modName, string rootDirectory, IContentManager gameContentManager)
    {
        return this.ContentManagerLock.InWriteLock(() =>
        {
            ModContentManager manager = new(
                name: name,
                gameContentManager: gameContentManager,
                serviceProvider: this.MainContentManager.ServiceProvider,
                rootDirectory: rootDirectory,
                modName: modName,
                currentCulture: this.MainContentManager.CurrentCulture,
                coordinator: this,
                monitor: this.Monitor,
                reflection: this.Reflection,
                jsonHelper: this.JsonHelper,
                onDisposing: this.OnDisposing,
                fileLookup: this.GetFileLookup(rootDirectory),
                useRawImageLoading: this.UseRawImageLoading
            );
            this.ContentManagers.Add(manager);
            return manager;
        });
    }

    /// <summary>Get the current content locale.</summary>
    public string GetLocale()
    {
        return this.MainContentManager.GetLocale(LocalizedContentManager.CurrentLanguageCode);
    }

    /// <summary>Perform any updates needed when the game loads custom languages from <c>Data/AdditionalLanguages</c>.</summary>
    public void OnAdditionalLanguagesInitialized()
    {
        // update locale cache for custom languages, and load it now (since languages added later won't work)
        var customLanguages = this.MainContentManager.Load<List<ModLanguage?>>("Data/AdditionalLanguages");
        this.LocaleCodes = new Lazy<Dictionary<string, LocalizedContentManager.LanguageCode>>(() => this.GetLocaleCodes(customLanguages));
        _ = this.LocaleCodes.Value;
    }

    /// <summary>Perform any updates needed when the locale changes.</summary>
    public void OnLocaleChanged()
    {
        // reset baseline cache
        this.ContentManagerLock.InReadLock(() =>
        {
            this.VanillaContentManager.Unload();
        });

        // forget localized flags (to match the logic in Game1.TranslateFields, which is called on language change)
        this.LocalizedAssetNames.Value.Clear();
    }

    /// <summary>Clean up when the player is returning to the title screen.</summary>
    /// <remarks>This is called after the player returns to the title screen, but before <see cref="Game1.CleanupReturningToTitle"/> runs.</remarks>
    public void OnReturningToTitleScreen()
    {
        // The game clears LocalizedContentManager.localizedAssetNames after returning to the title screen. That
        // causes an inconsistency in the SMAPI asset cache, which leads to an edge case where assets already
        // provided by mods via IAssetLoader when playing in non-English are ignored.
        //
        // For example, let's say a mod provides the 'Data\mail' asset through IAssetLoader when playing in
        // Portuguese. Here's the normal load process after it's loaded:
        //   1. The game requests Data\mail.
        //   2. SMAPI sees that it's already cached, and calls LoadRaw to bypass asset interception.
        //   3. LoadRaw sees that there's a localized key mapping, and gets the mapped key.
        //   4. In this case "Data\mail" is mapped to "Data\mail" since it was loaded by a mod, so it loads that
        //      asset.
        //
        // When the game clears localizedAssetNames, that process goes wrong in step 4:
        //  3. LoadRaw sees that there's no localized key mapping *and* the locale is non-English, so it attempts
        //     to load from the localized key format.
        //  4. In this case that's 'Data\mail.pt-BR', so it successfully loads that asset.
        //  5. Since we've bypassed asset interception at this point, it's loaded directly from the base content
        //     manager without mod changes.
        //
        // To avoid issues, we just remove affected assets from the cache here so they'll be reloaded normally.
        // Note that we *must* propagate changes here, otherwise when mods invalidate the cache later to reapply
        // their changes, the assets won't be found in the cache so no changes will be propagated.
        if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.en)
            this.InvalidateCache((contentManager, _, _) => contentManager is GameContentManager);

        // clear the localized assets lookup (to match the logic in Game1.CleanupReturningToTitle)
        foreach ((_, Dictionary<string, string> localizedAssets) in this.LocalizedAssetNames.GetActiveValues())
            localizedAssets.Clear();
    }

    /// <summary>Parse a raw asset name.</summary>
    /// <param name="rawName">The raw asset name to parse.</param>
    /// <param name="allowLocales">Whether to parse locales in the <paramref name="rawName"/>. If this is false, any locale codes in the name are treated as if they were part of the base name (e.g. for mod files).</param>
    /// <exception cref="ArgumentException">The <paramref name="rawName"/> is null or empty.</exception>
    public AssetName ParseAssetName(string rawName, bool allowLocales)
    {
        return !string.IsNullOrWhiteSpace(rawName)
            ? AssetName.Parse(
                rawName: rawName,
                parseLocale: allowLocales
                    ? locale => this.LocaleCodes.Value.TryGetValue(locale, out LocalizedContentManager.LanguageCode langCode) ? langCode : null
                    : _ => null
            )
            : throw new ArgumentException("The asset name can't be null or empty.", nameof(rawName));
    }

    /// <summary>Get whether this asset is mapped to a mod folder.</summary>
    /// <param name="key">The asset name.</param>
    public bool IsManagedAssetKey(IAssetName key)
    {
        return key.StartsWith(this.ManagedPrefix);
    }

    /// <summary>Parse a managed SMAPI asset key which maps to a mod folder.</summary>
    /// <param name="key">The asset key.</param>
    /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
    /// <param name="relativePath">The asset name within the mod folder.</param>
    /// <returns>Returns whether the asset was parsed successfully.</returns>
    public bool TryParseManagedAssetKey(string key, [NotNullWhen(true)] out string? contentManagerID, [NotNullWhen(true)] out IAssetName? relativePath)
    {
        contentManagerID = null;
        relativePath = null;

        // not a managed asset
        if (!key.StartsWith(this.ManagedPrefix))
            return false;

        // parse
        string[] parts = PathUtilities.GetSegments(key, 3);
        if (parts.Length != 3) // managed key prefix, mod id, relative path
            return false;
        contentManagerID = Path.Combine(parts[0], parts[1]);
        relativePath = this.ParseAssetName(parts[2], allowLocales: false);
        return true;
    }

    /// <summary>Get the managed asset key prefix for a mod.</summary>
    /// <param name="modID">The mod's unique ID.</param>
    public string GetManagedAssetPrefix(string modID)
    {
        return Path.Combine(this.ManagedPrefix, modID.ToLower());
    }

    /// <summary>Get whether an asset from a mod folder exists.</summary>
    /// <typeparam name="T">The expected asset type.</typeparam>
    /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
    /// <param name="assetName">The asset name within the mod folder.</param>
    public bool DoesManagedAssetExist<T>(string contentManagerID, IAssetName assetName)
        where T : notnull
    {
        // get content manager
        IContentManager? contentManager = this.ContentManagerLock.InReadLock(() =>
            this.ContentManagers.FirstOrDefault(p => p.IsNamespaced && p.Name == contentManagerID)
        );
        if (contentManager == null)
            throw new InvalidOperationException($"The '{contentManagerID}' prefix isn't handled by any mod.");

        // get whether the asset exists
        return contentManager.DoesAssetExist<T>(assetName);
    }

    /// <summary>Get a copy of an asset from a mod folder.</summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
    /// <param name="relativePath">The asset name within the mod folder.</param>
    public T LoadManagedAsset<T>(string contentManagerID, IAssetName relativePath)
        where T : notnull
    {
        // get content manager
        IContentManager? contentManager = this.ContentManagerLock.InReadLock(() =>
            this.ContentManagers.FirstOrDefault(p => p.IsNamespaced && p.Name == contentManagerID)
        );
        if (contentManager == null)
            throw new InvalidOperationException($"The '{contentManagerID}' prefix isn't handled by any mod.");

        // get fresh asset
        return contentManager.LoadExact<T>(relativePath, useCache: false);
    }

    /// <summary>Purge matched assets from the cache.</summary>
    /// <param name="predicate">Matches the asset keys to invalidate.</param>
    /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
    /// <returns>Returns the invalidated asset keys.</returns>
    public IEnumerable<IAssetName> InvalidateCache(Func<IAssetInfo, bool> predicate, bool dispose = false)
    {
        string locale = this.GetLocale();
        return this.InvalidateCache((_, rawName, type) =>
        {
            IAssetName assetName = this.ParseAssetName(rawName, allowLocales: true);
            IAssetInfo info = new AssetInfo(locale, assetName, type, this.MainContentManager.AssertAndNormalizeAssetName);
            return predicate(info);
        }, dispose);
    }

    /// <summary>Purge matched assets from the cache.</summary>
    /// <param name="predicate">Matches the asset keys to invalidate.</param>
    /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
    /// <returns>Returns the invalidated asset names.</returns>
    public IEnumerable<IAssetName> InvalidateCache(Func<IContentManager, string, Type, bool> predicate, bool dispose = false)
    {
        // invalidate cache & track removed assets
        IDictionary<IAssetName, Type> invalidatedAssets = new Dictionary<IAssetName, Type>();
        this.ContentManagerLock.InReadLock(() =>
        {
            // cached assets
            foreach (IContentManager contentManager in this.ContentManagers)
            {
                foreach ((string key, object asset) in contentManager.GetCachedAssets())
                {
                    if (!predicate(contentManager, key, asset.GetType()))
                        continue;

                    AssetName assetName = this.ParseAssetName(key, allowLocales: true);
                    contentManager.InvalidateCache(assetName, dispose);

                    if (!invalidatedAssets.ContainsKey(assetName))
                        invalidatedAssets[assetName] = asset.GetType();
                }
            }

            // forget localized flags
            // A mod might provide a localized variant of a normally non-localized asset (like
            // `Maps/MovieTheater.fr-FR`). When the asset is invalidated, we need to recheck
            // whether the asset is localized in case it stops providing it.
            {
                Dictionary<string, string> localizedAssetNames = this.LocalizedAssetNames.Value;
                foreach (IAssetName assetName in invalidatedAssets.Keys)
                {
                    localizedAssetNames.Remove(assetName.Name);

                    if (localizedAssetNames.TryGetValue(assetName.BaseName, out string? targetForBaseKey) && targetForBaseKey == assetName.Name)
                        localizedAssetNames.Remove(assetName.BaseName);
                }
            }

            // special case: maps may be loaded through a temporary content manager that's removed while the map is still in use.
            // This notably affects the town and farmhouse maps.
            if (Game1.locations != null)
            {
                foreach (GameLocation location in Game1.locations)
                {
                    if (location.map == null || string.IsNullOrWhiteSpace(location.mapPath.Value))
                        continue;

                    // get map path
                    AssetName mapPath = this.ParseAssetName(this.MainContentManager.AssertAndNormalizeAssetName(location.mapPath.Value), allowLocales: true);
                    if (!invalidatedAssets.ContainsKey(mapPath) && predicate(this.MainContentManager, mapPath.Name, typeof(Map)))
                        invalidatedAssets[mapPath] = typeof(Map);
                }
            }
        });

        // handle invalidation
        if (invalidatedAssets.Any())
        {
            // clear cached editor checks
            foreach (IAssetName name in invalidatedAssets.Keys)
                this.AssetOperationsByKey.Remove(name);

            // raise event
            this.OnAssetsInvalidated(invalidatedAssets.Keys.ToArray());

            // propagate changes to the game
            this.CoreAssets.Propagate(
                assets: invalidatedAssets.ToDictionary(p => p.Key, p => p.Value),
                ignoreWorld: Context.IsWorldFullyUnloaded,
                out IDictionary<IAssetName, bool> propagated,
                out bool updatedNpcWarps
            );

            // log summary
            StringBuilder report = new();
            {
                IAssetName[] invalidatedKeys = invalidatedAssets.Keys.ToArray();
                IAssetName[] propagatedKeys = propagated.Where(p => p.Value).Select(p => p.Key).ToArray();

                string FormatKeyList(IEnumerable<IAssetName> keys) => string.Join(", ", keys.Select(p => p.Name).OrderBy(p => p, StringComparer.OrdinalIgnoreCase));

                report.AppendLine($"Invalidated {invalidatedKeys.Length} asset names ({FormatKeyList(invalidatedKeys)}).");
                report.AppendLine(propagated.Count > 0
                    ? $"Propagated {propagatedKeys.Length} core assets ({FormatKeyList(propagatedKeys)})."
                    : "Propagated 0 core assets."
                );
                if (updatedNpcWarps)
                    report.AppendLine("Updated NPC pathfinding cache.");
            }
            this.Monitor.Log(report.ToString().TrimEnd());
        }
        else
            this.Monitor.Log("Invalidated 0 cache entries.");

        return invalidatedAssets.Keys;
    }

    /// <summary>Get the asset load and edit operations to apply to a given asset if it's (re)loaded now.</summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="info">The asset info to load or edit.</param>
    public AssetOperationGroup? GetAssetOperations<T>(IAssetInfo info)
        where T : notnull
    {
        return this.AssetOperationsByKey.GetOrSet(
            info.Name,
            () => this.GetAssetOperationsWithoutCache<T>(info)
        );
    }

    /// <summary>Get all loaded instances of an asset name.</summary>
    /// <param name="assetName">The asset name.</param>
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "This method is provided for Content Patcher.")]
    public IEnumerable<object> GetLoadedValues(IAssetName assetName)
    {
        return this.ContentManagerLock.InReadLock(() =>
        {
            List<object> values = new List<object>();
            foreach (IContentManager content in this.ContentManagers.Where(p => !p.IsNamespaced && p.IsLoaded(assetName)))
            {
                object value = content.LoadExact<object>(assetName, useCache: true);
                values.Add(value);
            }
            return values;
        });
    }

    /// <summary>Get the tilesheet ID order used by the unmodified version of a map asset.</summary>
    /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
    public TilesheetReference[] GetVanillaTilesheetIds(string assetName)
    {
        if (!this.VanillaTilesheets.TryGetValue(assetName, out TilesheetReference[]? tilesheets))
        {
            tilesheets = this.TryLoadVanillaAsset(assetName, out Map? map)
                ? map.TileSheets.Select((sheet, index) => new TilesheetReference(index, sheet.Id, sheet.ImageSource, sheet.SheetSize, sheet.TileSize)).ToArray()
                : null;

            this.VanillaTilesheets[assetName] = tilesheets;
            this.VanillaContentManager.Unload();
        }

        return tilesheets ?? Array.Empty<TilesheetReference>();
    }

    /// <summary>Get the locale code which corresponds to a language enum (e.g. <c>fr-FR</c> given <see cref="LocalizedContentManager.LanguageCode.fr"/>).</summary>
    /// <param name="language">The language enum to search.</param>
    public string? GetLocaleCode(LocalizedContentManager.LanguageCode language)
    {
        if (language == LocalizedContentManager.LanguageCode.mod && LocalizedContentManager.CurrentModLanguage == null)
            return null;

        return this.MainContentManager.LanguageCodeString(language);
    }

    /// <summary>Dispose held resources.</summary>
    public void Dispose()
    {
        if (this.IsDisposed)
            return;
        this.IsDisposed = true;

        this.Monitor.Log("Disposing the content coordinator. Content managers will no longer be usable after this point.");
        foreach (IContentManager contentManager in this.ContentManagers)
            contentManager.Dispose();
        this.ContentManagers.Clear();
        this.MainContentManager = null!; // instance no longer usable

        this.ContentManagerLock.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>A callback invoked when a content manager is disposed.</summary>
    /// <param name="contentManager">The content manager being disposed.</param>
    private void OnDisposing(IContentManager contentManager)
    {
        if (this.IsDisposed)
            return;

        this.ContentManagerLock.InWriteLock(() =>
            this.ContentManagers.Remove(contentManager)
        );
    }

    /// <summary>Get a vanilla asset without interception.</summary>
    /// <typeparam name="T">The type of asset to load.</typeparam>
    /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
    /// <param name="asset">The loaded asset data.</param>
    private bool TryLoadVanillaAsset<T>(string assetName, [NotNullWhen(true)] out T? asset)
        where T : notnull
    {
        try
        {
            asset = this.VanillaContentManager.Load<T>(assetName);
            return true;
        }
        catch
        {
            asset = default;
            return false;
        }
    }

    /// <summary>Get the language enums (like <see cref="LocalizedContentManager.LanguageCode.ja"/>) indexed by locale code (like <c>ja-JP</c>).</summary>
    /// <param name="customLanguages">The custom languages to add to the lookup.</param>
    private Dictionary<string, LocalizedContentManager.LanguageCode> GetLocaleCodes(IEnumerable<ModLanguage?> customLanguages)
    {
        var map = new Dictionary<string, LocalizedContentManager.LanguageCode>(StringComparer.OrdinalIgnoreCase);

        // custom languages
        foreach (ModLanguage? language in customLanguages)
        {
            if (!string.IsNullOrWhiteSpace(language?.LanguageCode))
                map[language.LanguageCode] = LocalizedContentManager.LanguageCode.mod;
        }

        // vanilla languages (override custom language if they conflict)
        foreach (LocalizedContentManager.LanguageCode code in Enum.GetValues(typeof(LocalizedContentManager.LanguageCode)))
        {
            string? locale = this.GetLocaleCode(code);
            if (locale != null)
                map[locale] = code;
        }

        return map;
    }

    /// <summary>Get the asset load and edit operations to apply to a given asset if it's (re)loaded now, ignoring the <see cref="AssetOperationsByKey"/> cache.</summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="info">The asset info to load or edit.</param>
    private AssetOperationGroup? GetAssetOperationsWithoutCache<T>(IAssetInfo info)
        where T : notnull
    {
        // new content API
        AssetOperationGroup? group = this.RequestAssetOperations(info);

        // legacy load operations
#pragma warning disable CS0612, CS0618 // deprecated code
        if (this.Editors.Count > 0 || this.Loaders.Count > 0)
        {
            IAssetInfo legacyInfo = this.GetLegacyAssetInfo(info);

            foreach (ModLinked<IAssetLoader> loader in this.Loaders)
            {
                // check if loader applies
                Context.HeuristicModsRunningCode.Push(loader.Mod);
                try
                {
                    if (!loader.Data.CanLoad<T>(legacyInfo))
                        continue;
                }
                catch (Exception ex)
                {
                    loader.Mod.LogAsMod($"Mod failed when checking whether it could load asset '{legacyInfo.Name}', and will be ignored. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                    continue;
                }
                finally
                {
                    Context.HeuristicModsRunningCode.TryPop(out _);
                }

                // add operation
                group ??= new AssetOperationGroup(new List<AssetLoadOperation>(), new List<AssetEditOperation>());
                group.LoadOperations.Add(
                    this.GetOrCreateLegacyOperation(
                        cache: this.LegacyLoaderCache,
                        editor: loader.Data,
                        dataType: info.DataType,
                        create: () => new AssetLoadOperation(
                            Mod: loader.Mod,
                            OnBehalfOf: null,
                            Priority: AssetLoadPriority.Exclusive,
                            GetData: assetInfo => loader.Data.Load<T>(this.GetLegacyAssetInfo(assetInfo))
                        )
                    )
                );
            }

            // legacy edit operations
            foreach (var editor in this.Editors)
            {
                // check if editor applies
                Context.HeuristicModsRunningCode.Push(editor.Mod);
                try
                {
                    if (!editor.Data.CanEdit<T>(legacyInfo))
                        continue;
                }
                catch (Exception ex)
                {
                    editor.Mod.LogAsMod($"Mod crashed when checking whether it could edit asset '{legacyInfo.Name}', and will be ignored. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                    continue;
                }
                finally
                {
                    Context.HeuristicModsRunningCode.TryPop(out _);
                }

                // HACK
                //
                // If two editors have the same priority, they're applied in registration order (so
                // whichever was registered first is applied first). Mods often depend on this
                // behavior, like Json Assets registering its interceptors before Content Patcher.
                //
                // Unfortunately the old & new content APIs have separate lists, so new-API
                // interceptors always ran before old-API interceptors with the same priority,
                // regardless of the registration order *between* APIs. Since the new API works in
                // a fundamentally different way (i.e. loads/edits are defined on asset request
                // instead of by registering a global 'editor' or 'loader' class), there's no way
                // to track registration order between them.
                //
                // Until we drop the old content API in SMAPI 4.0.0, this sets the priority for
                // specific legacy editors to maintain compatibility.
                AssetEditPriority priority = editor.Data.GetType().FullName switch
                {
                    "JsonAssets.Framework.ContentInjector1" => AssetEditPriority.Default - 1, // must be applied before Content Patcher
                    _ => AssetEditPriority.Default
                };

                // add operation
                group ??= new AssetOperationGroup(new List<AssetLoadOperation>(), new List<AssetEditOperation>());
                group.EditOperations.Add(
                    this.GetOrCreateLegacyOperation(
                        cache: this.LegacyEditorCache,
                        editor: editor.Data,
                        dataType: info.DataType,
                        create: () => new AssetEditOperation(
                            Mod: editor.Mod,
                            OnBehalfOf: null,
                            Priority: priority,
                            ApplyEdit: assetData => editor.Data.Edit<T>(this.GetLegacyAssetData(assetData))
                        )
                    )
                );
            }
        }
#pragma warning restore CS0612, CS0618

        return group;
    }

    /// <summary>Get a cached asset operation group for a legacy <see cref="IAssetLoader"/> or <see cref="IAssetEditor"/> instance, creating it if needed.</summary>
    /// <typeparam name="TInterceptor">The editor type (one of <see cref="IAssetLoader"/> or <see cref="IAssetEditor"/>).</typeparam>
    /// <typeparam name="TOperation">The operation model type.</typeparam>
    /// <param name="cache">The cached operation groups for the interceptor type.</param>
    /// <param name="editor">The legacy asset interceptor.</param>
    /// <param name="dataType">The asset data type.</param>
    /// <param name="create">Create the asset operation group if it's not cached yet.</param>
    private TOperation GetOrCreateLegacyOperation<TInterceptor, TOperation>(Dictionary<TInterceptor, Dictionary<Type, TOperation>> cache, TInterceptor editor, Type dataType, Func<TOperation> create)
        where TInterceptor : class
    {
        if (!cache.TryGetValue(editor, out Dictionary<Type, TOperation>? cacheByType))
            cache[editor] = cacheByType = new Dictionary<Type, TOperation>();

        if (!cacheByType.TryGetValue(dataType, out TOperation? operation))
            cacheByType[dataType] = operation = create();

        return operation;
    }

    /// <summary>Get an asset info compatible with legacy <see cref="IAssetLoader"/> and <see cref="IAssetEditor"/> instances, which always expect the base name.</summary>
    /// <param name="asset">The asset info.</param>
    private IAssetInfo GetLegacyAssetInfo(IAssetInfo asset)
    {
        return new AssetInfo(
            locale: this.GetLegacyLocale(asset),
            assetName: this.GetLegacyAssetName(asset.Name),
            type: asset.DataType,
            getNormalizedPath: this.MainContentManager.AssertAndNormalizeAssetName
        );
    }

    /// <summary>Get an asset data compatible with legacy <see cref="IAssetLoader"/> and <see cref="IAssetEditor"/> instances, which always expect the base name.</summary>
    /// <param name="asset">The asset data.</param>
    private IAssetData GetLegacyAssetData(IAssetData asset)
    {
        return new AssetDataForObject(
            locale: this.GetLegacyLocale(asset),
            assetName: this.GetLegacyAssetName(asset.Name),
            data: asset.Data,
            getNormalizedPath: this.MainContentManager.AssertAndNormalizeAssetName,
            reflection: this.Reflection,
            onDataReplaced: asset.ReplaceWith
        );
    }

    /// <summary>Get the <see cref="IAssetInfo.Locale"/> value compatible with legacy <see cref="IAssetLoader"/> and <see cref="IAssetEditor"/> instances, which expect the locale to default to the current game locale or an empty string.</summary>
    /// <param name="asset">The non-legacy asset info to map.</param>
    private string GetLegacyLocale(IAssetInfo asset)
    {
        return asset.Locale ?? this.GetLocale();
    }

    /// <summary>Get an asset name compatible with legacy <see cref="IAssetLoader"/> and <see cref="IAssetEditor"/> instances, which always expect the base name.</summary>
    /// <param name="asset">The asset name to map.</param>
    /// <returns>Returns the legacy asset name if needed, or the <paramref name="asset"/> if no change is needed.</returns>
    private IAssetName GetLegacyAssetName(IAssetName asset)
    {
        // strip _international suffix
        const string internationalSuffix = "_international";
        if (asset.Name.EndsWith(internationalSuffix))
        {
            return new AssetName(
                baseName: asset.Name[..^internationalSuffix.Length],
                localeCode: null,
                languageCode: null
            );
        }

        // else strip locale
        if (asset.LocaleCode != null)
            return new AssetName(asset.BaseName, null, null);

        // else no change needed
        return asset;
    }
}
