using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Metadata;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;
using xTile;

namespace StardewModdingAPI.Framework
{
    /// <summary>The central logic for creating content managers, invalidating caches, and propagating asset changes.</summary>
    internal class ContentCoordinator : IDisposable
    {
        /*********
        ** Fields
        *********/
        /// <summary>An asset key prefix for assets from SMAPI mod folders.</summary>
        private readonly string ManagedPrefix = "SMAPI";

        /// <summary>Whether to enable more aggressive memory optimizations.</summary>
        private readonly bool AggressiveMemoryOptimizations;

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

        /// <summary>The loaded content managers (including the <see cref="MainContentManager"/>).</summary>
        private readonly IList<IContentManager> ContentManagers = new List<IContentManager>();

        /// <summary>The language code for language-agnostic mod assets.</summary>
        private readonly LocalizedContentManager.LanguageCode DefaultLanguage = Constants.DefaultLanguage;

        /// <summary>Whether the content coordinator has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>A lock used to prevent asynchronous changes to the content manager list.</summary>
        /// <remarks>The game may add content managers in asynchronous threads (e.g. when populating the load screen).</remarks>
        private readonly ReaderWriterLockSlim ContentManagerLock = new ReaderWriterLockSlim();

        /// <summary>A cache of ordered tilesheet IDs used by vanilla maps.</summary>
        private readonly IDictionary<string, TilesheetReference[]> VanillaTilesheets = new Dictionary<string, TilesheetReference[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>An unmodified content manager which doesn't intercept assets, used to compare asset data.</summary>
        private readonly LocalizedContentManager VanillaContentManager;

        /// <summary>The language enum values indexed by locale code.</summary>
        private Lazy<IDictionary<string, LocalizedContentManager.LanguageCode>> LocaleCodes;


        /*********
        ** Accessors
        *********/
        /// <summary>The primary content manager used for most assets.</summary>
        public GameContentManager MainContentManager { get; private set; }

        /// <summary>The current language as a constant.</summary>
        public LocalizedContentManager.LanguageCode Language => this.MainContentManager.Language;

        /// <summary>Interceptors which provide the initial versions of matching assets.</summary>
        public IList<ModLinked<IAssetLoader>> Loaders { get; } = new List<ModLinked<IAssetLoader>>();

        /// <summary>Interceptors which edit matching assets after they're loaded.</summary>
        public IList<ModLinked<IAssetEditor>> Editors { get; } = new List<ModLinked<IAssetEditor>>();

        /// <summary>The absolute path to the <see cref="ContentManager.RootDirectory"/>.</summary>
        public string FullRootDirectory { get; }


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
        /// <param name="aggressiveMemoryOptimizations">Whether to enable more aggressive memory optimizations.</param>
        public ContentCoordinator(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, IMonitor monitor, Reflector reflection, JsonHelper jsonHelper, Action onLoadingFirstAsset, bool aggressiveMemoryOptimizations)
        {
            this.AggressiveMemoryOptimizations = aggressiveMemoryOptimizations;
            this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            this.Reflection = reflection;
            this.JsonHelper = jsonHelper;
            this.OnLoadingFirstAsset = onLoadingFirstAsset;
            this.FullRootDirectory = Path.Combine(Constants.ExecutionPath, rootDirectory);
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
                    aggressiveMemoryOptimizations: aggressiveMemoryOptimizations
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
                aggressiveMemoryOptimizations: aggressiveMemoryOptimizations
            );
            this.ContentManagers.Add(contentManagerForAssetPropagation);
            this.VanillaContentManager = new LocalizedContentManager(serviceProvider, rootDirectory);
            this.CoreAssets = new CoreAssetPropagator(this.MainContentManager, contentManagerForAssetPropagation, this.Monitor, reflection, aggressiveMemoryOptimizations);
            this.LocaleCodes = new Lazy<IDictionary<string, LocalizedContentManager.LanguageCode>>(this.GetLocaleCodes);
        }

        /// <summary>Get a new content manager which handles reading files from the game content folder with support for interception.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        public GameContentManager CreateGameContentManager(string name)
        {
            return this.ContentManagerLock.InWriteLock(() =>
            {
                GameContentManager manager = new GameContentManager(
                    name: name,
                    serviceProvider: this.MainContentManager.ServiceProvider,
                    rootDirectory: this.MainContentManager.RootDirectory,
                    currentCulture: this.MainContentManager.CurrentCulture,
                    coordinator: this,
                    monitor: this.Monitor,
                    reflection: this.Reflection,
                    onDisposing: this.OnDisposing,
                    onLoadingFirstAsset: this.OnLoadingFirstAsset,
                    aggressiveMemoryOptimizations: this.AggressiveMemoryOptimizations
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
                ModContentManager manager = new ModContentManager(
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
                    aggressiveMemoryOptimizations: this.AggressiveMemoryOptimizations
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

        /// <summary>Perform any cleanup needed when the locale changes.</summary>
        public void OnLocaleChanged()
        {
            // rebuild locale cache (which may change due to custom mod languages)
            this.LocaleCodes = new Lazy<IDictionary<string, LocalizedContentManager.LanguageCode>>(this.GetLocaleCodes);

            // reload affected content
            this.ContentManagerLock.InReadLock(() =>
            {
                foreach (IContentManager contentManager in this.ContentManagers)
                    contentManager.OnLocaleChanged();

                this.VanillaContentManager.Unload();
            });
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
                this.InvalidateCache((contentManager, key, type) => contentManager is GameContentManager);
        }

        /// <summary>Get whether this asset is mapped to a mod folder.</summary>
        /// <param name="key">The asset key.</param>
        public bool IsManagedAssetKey(string key)
        {
            return key.StartsWith(this.ManagedPrefix);
        }

        /// <summary>Parse a managed SMAPI asset key which maps to a mod folder.</summary>
        /// <param name="key">The asset key.</param>
        /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
        /// <param name="relativePath">The relative path within the mod folder.</param>
        /// <returns>Returns whether the asset was parsed successfully.</returns>
        public bool TryParseManagedAssetKey(string key, out string contentManagerID, out string relativePath)
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
            relativePath = parts[2];
            return true;
        }

        /// <summary>Get the managed asset key prefix for a mod.</summary>
        /// <param name="modID">The mod's unique ID.</param>
        public string GetManagedAssetPrefix(string modID)
        {
            return Path.Combine(this.ManagedPrefix, modID.ToLower());
        }

        /// <summary>Get a copy of an asset from a mod folder.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
        /// <param name="relativePath">The internal SMAPI asset key.</param>
        public T LoadManagedAsset<T>(string contentManagerID, string relativePath)
        {
            // get content manager
            IContentManager contentManager = this.ContentManagerLock.InReadLock(() =>
                this.ContentManagers.FirstOrDefault(p => p.IsNamespaced && p.Name == contentManagerID)
            );
            if (contentManager == null)
                throw new InvalidOperationException($"The '{contentManagerID}' prefix isn't handled by any mod.");

            // get fresh asset
            return contentManager.Load<T>(relativePath, this.DefaultLanguage, useCache: false);
        }

        /// <summary>Purge matched assets from the cache.</summary>
        /// <param name="predicate">Matches the asset keys to invalidate.</param>
        /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
        /// <returns>Returns the invalidated asset keys.</returns>
        public IEnumerable<string> InvalidateCache(Func<IAssetInfo, bool> predicate, bool dispose = false)
        {
            string locale = this.GetLocale();
            return this.InvalidateCache((contentManager, assetName, type) =>
            {
                IAssetInfo info = new AssetInfo(locale, assetName, type, this.MainContentManager.AssertAndNormalizeAssetName);
                return predicate(info);
            }, dispose);
        }

        /// <summary>Purge matched assets from the cache.</summary>
        /// <param name="predicate">Matches the asset keys to invalidate.</param>
        /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
        /// <returns>Returns the invalidated asset names.</returns>
        public IEnumerable<string> InvalidateCache(Func<IContentManager, string, Type, bool> predicate, bool dispose = false)
        {
            // invalidate cache & track removed assets
            IDictionary<string, Type> removedAssets = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            this.ContentManagerLock.InReadLock(() =>
            {
                // cached assets
                foreach (IContentManager contentManager in this.ContentManagers)
                {
                    foreach (var entry in contentManager.InvalidateCache((key, type) => predicate(contentManager, key, type), dispose))
                    {
                        if (!removedAssets.ContainsKey(entry.Key))
                            removedAssets[entry.Key] = entry.Value.GetType();
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
                        string mapPath = this.MainContentManager.AssertAndNormalizeAssetName(location.mapPath.Value);
                        if (!removedAssets.ContainsKey(mapPath) && predicate(this.MainContentManager, mapPath, typeof(Map)))
                            removedAssets[mapPath] = typeof(Map);
                    }
                }
            });

            // reload core game assets
            if (removedAssets.Any())
            {
                // propagate changes to the game
                this.CoreAssets.Propagate(
                    assets: removedAssets.ToDictionary(p => p.Key, p => p.Value),
                    ignoreWorld: Context.IsWorldFullyUnloaded,
                    out IDictionary<string, bool> propagated,
                    out bool updatedNpcWarps
                );

                // log summary
                StringBuilder report = new StringBuilder();
                {
                    string[] invalidatedKeys = removedAssets.Keys.ToArray();
                    string[] propagatedKeys = propagated.Where(p => p.Value).Select(p => p.Key).ToArray();

                    string FormatKeyList(IEnumerable<string> keys) => string.Join(", ", keys.OrderBy(p => p, StringComparer.OrdinalIgnoreCase));

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

            return removedAssets.Keys;
        }

        /// <summary>Get all loaded instances of an asset name.</summary>
        /// <param name="assetName">The asset name.</param>
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "This method is provided for Content Patcher.")]
        public IEnumerable<object> GetLoadedValues(string assetName)
        {
            return this.ContentManagerLock.InReadLock(() =>
            {
                List<object> values = new List<object>();
                foreach (IContentManager content in this.ContentManagers.Where(p => !p.IsNamespaced && p.IsLoaded(assetName, p.Language)))
                {
                    object value = content.Load<object>(assetName, this.Language, useCache: true);
                    values.Add(value);
                }
                return values;
            });
        }

        /// <summary>Get the tilesheet ID order used by the unmodified version of a map asset.</summary>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        public TilesheetReference[] GetVanillaTilesheetIds(string assetName)
        {
            if (!this.VanillaTilesheets.TryGetValue(assetName, out TilesheetReference[] tilesheets))
            {
                tilesheets = this.TryLoadVanillaAsset(assetName, out Map map)
                    ? map.TileSheets.Select((sheet, index) => new TilesheetReference(index, sheet.Id, sheet.ImageSource)).ToArray()
                    : null;

                this.VanillaTilesheets[assetName] = tilesheets;
                this.VanillaContentManager.Unload();
            }

            return tilesheets ?? new TilesheetReference[0];
        }

        /// <summary>Get the language enum which corresponds to a locale code (e.g. <see cref="LocalizedContentManager.LanguageCode.fr"/> given <c>fr-FR</c>).</summary>
        /// <param name="locale">The locale code to search. This must exactly match the language; no fallback is performed.</param>
        /// <param name="language">The matched language enum, if any.</param>
        /// <returns>Returns whether a valid language was found.</returns>
        public bool TryGetLanguageEnum(string locale, out LocalizedContentManager.LanguageCode language)
        {
            return this.LocaleCodes.Value.TryGetValue(locale, out language);
        }

        /// <summary>Get the locale code which corresponds to a language enum (e.g. <c>fr-FR</c> given <see cref="LocalizedContentManager.LanguageCode.fr"/>).</summary>
        /// <param name="language">The language enum to search.</param>
        public string GetLocaleCode(LocalizedContentManager.LanguageCode language)
        {
            if (language == LocalizedContentManager.LanguageCode.mod && LocalizedContentManager.CurrentModLanguage == null)
                return null;

            return Game1.content.LanguageCodeString(language);
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
            this.MainContentManager = null;

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
        private bool TryLoadVanillaAsset<T>(string assetName, out T asset)
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
        private IDictionary<string, LocalizedContentManager.LanguageCode> GetLocaleCodes()
        {
            IDictionary<string, LocalizedContentManager.LanguageCode> map = new Dictionary<string, LocalizedContentManager.LanguageCode>();
            foreach (LocalizedContentManager.LanguageCode code in Enum.GetValues(typeof(LocalizedContentManager.LanguageCode)))
            {
                string locale = this.GetLocaleCode(code);
                if (locale != null)
                    map[locale] = code;
            }

            return map;
        }
    }
}
