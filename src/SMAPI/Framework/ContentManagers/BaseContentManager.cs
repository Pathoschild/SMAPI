using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;
using xTile;

namespace StardewModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files from a SMAPI mod folder with support for unpacked files.</summary>
    internal abstract class BaseContentManager : LocalizedContentManager, IContentManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>The central coordinator which manages content managers.</summary>
        protected readonly ContentCoordinator Coordinator;

        /// <summary>The underlying asset cache.</summary>
        protected readonly ContentCache Cache;

        /// <summary>Encapsulates monitoring and logging.</summary>
        protected readonly IMonitor Monitor;

        /// <summary>Whether to enable more aggressive memory optimizations.</summary>
        protected readonly bool AggressiveMemoryOptimizations;

        /// <summary>Whether the content coordinator has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>A callback to invoke when the content manager is being disposed.</summary>
        private readonly Action<BaseContentManager> OnDisposing;

        /// <summary>A list of disposable assets.</summary>
        private readonly List<WeakReference<IDisposable>> Disposables = new List<WeakReference<IDisposable>>();

        /// <summary>The disposable assets tracked by the base content manager.</summary>
        /// <remarks>This should be kept empty to avoid keeping disposable assets referenced forever, which prevents garbage collection when they're unused. Disposable assets are tracked by <see cref="Disposables"/> instead, which avoids a hard reference.</remarks>
        private readonly List<IDisposable> BaseDisposableReferences;


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public LanguageCode Language => this.GetCurrentLanguage();

        /// <inheritdoc />
        public string FullRootDirectory => Path.Combine(Constants.ExecutionPath, this.RootDirectory);

        /// <inheritdoc />
        public bool IsNamespaced { get; }


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
        /// <param name="isNamespaced">Whether this content manager handles managed asset keys (e.g. to load assets from a mod folder).</param>
        /// <param name="aggressiveMemoryOptimizations">Whether to enable more aggressive memory optimizations.</param>
        protected BaseContentManager(string name, IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, Action<BaseContentManager> onDisposing, bool isNamespaced, bool aggressiveMemoryOptimizations)
            : base(serviceProvider, rootDirectory, currentCulture)
        {
            // init
            this.Name = name;
            this.Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.Cache = new ContentCache(this, reflection);
            this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            this.OnDisposing = onDisposing;
            this.IsNamespaced = isNamespaced;
            this.AggressiveMemoryOptimizations = aggressiveMemoryOptimizations;

            // get asset data
            this.BaseDisposableReferences = reflection.GetField<List<IDisposable>>(this, "disposableAssets").GetValue();
        }

        /// <inheritdoc />
        public override T Load<T>(string assetName)
        {
            return this.Load<T>(assetName, this.Language, useCache: true);
        }

        /// <inheritdoc />
        public override T Load<T>(string assetName, LanguageCode language)
        {
            return this.Load<T>(assetName, language, useCache: true);
        }

        /// <inheritdoc />
        public abstract T Load<T>(string assetName, LocalizedContentManager.LanguageCode language, bool useCache);

        /// <inheritdoc />
        [Obsolete("This method is implemented for the base game and should not be used directly. To load an asset from the underlying content manager directly, use " + nameof(BaseContentManager.RawLoad) + " instead.")]
        public override T LoadBase<T>(string assetName)
        {
            return this.Load<T>(assetName, LanguageCode.en, useCache: true);
        }

        /// <inheritdoc />
        public virtual void OnLocaleChanged() { }

        /// <inheritdoc />
        [Pure]
        public string NormalizePathSeparators(string path)
        {
            return this.Cache.NormalizePathSeparators(path);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Parameter is only used for assertion checks by design.")]
        public string AssertAndNormalizeAssetName(string assetName)
        {
            // NOTE: the game checks for ContentLoadException to handle invalid keys, so avoid
            // throwing other types like ArgumentException here.
            if (string.IsNullOrWhiteSpace(assetName))
                throw new SContentLoadException("The asset key or local path is empty.");
            if (assetName.Intersect(Path.GetInvalidPathChars()).Any())
                throw new SContentLoadException("The asset key or local path contains invalid characters.");

            return this.Cache.NormalizeKey(assetName);
        }

        /****
        ** Content loading
        ****/
        /// <inheritdoc />
        public string GetLocale()
        {
            return this.GetLocale(this.GetCurrentLanguage());
        }

        /// <inheritdoc />
        public string GetLocale(LanguageCode language)
        {
            return this.LanguageCodeString(language);
        }

        /// <inheritdoc />
        public bool IsLoaded(string assetName, LanguageCode language)
        {
            assetName = this.Cache.NormalizeKey(assetName);
            return this.IsNormalizedKeyLoaded(assetName, language);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAssetKeys()
        {
            return this.Cache.Keys
                .Select(this.GetAssetName)
                .Distinct();
        }

        /****
        ** Cache invalidation
        ****/
        /// <inheritdoc />
        public IDictionary<string, object> InvalidateCache(Func<string, Type, bool> predicate, bool dispose = false)
        {
            IDictionary<string, object> removeAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            this.Cache.Remove((key, asset) =>
            {
                this.ParseCacheKey(key, out string assetName, out _);

                // check if asset should be removed
                bool remove = removeAssets.ContainsKey(assetName);
                if (!remove && predicate(assetName, asset.GetType()))
                {
                    removeAssets[assetName] = asset;
                    remove = true;
                }

                // dispose if safe
                if (remove && this.AggressiveMemoryOptimizations)
                {
                    if (asset is Map map)
                        map.DisposeTileSheets(Game1.mapDisplayDevice);
                }

                return remove;
            }, dispose);

            return removeAssets;
        }

        /// <inheritdoc />
        protected override void Dispose(bool isDisposing)
        {
            // ignore if disposed
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;

            // dispose uncached assets
            foreach (WeakReference<IDisposable> reference in this.Disposables)
            {
                if (reference.TryGetTarget(out IDisposable disposable))
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch { /* ignore dispose errors */ }
                }
            }
            this.Disposables.Clear();

            // raise event
            this.OnDisposing(this);

            base.Dispose(isDisposing);
        }

        /// <inheritdoc />
        public override void Unload()
        {
            if (this.IsDisposed)
                return; // base logic doesn't allow unloading twice, which happens due to SMAPI and the game both unloading

            base.Unload();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Load an asset file directly from the underlying content manager.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The normalized asset key.</param>
        /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
        protected virtual T RawLoad<T>(string assetName, bool useCache)
        {
            return useCache
                ? base.LoadBase<T>(assetName)
                : base.ReadAsset<T>(assetName, disposable => this.Disposables.Add(new WeakReference<IDisposable>(disposable)));
        }

        /// <summary>Add tracking data to an asset and add it to the cache.</summary>
        /// <typeparam name="T">The type of asset to inject.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="value">The asset value.</param>
        /// <param name="language">The language code for which to inject the asset.</param>
        /// <param name="useCache">Whether to save the asset to the asset cache.</param>
        protected virtual void TrackAsset<T>(string assetName, T value, LanguageCode language, bool useCache)
        {
            // track asset key
            if (value is Texture2D texture)
                texture.Name = assetName;

            // cache asset
            if (useCache)
            {
                assetName = this.AssertAndNormalizeAssetName(assetName);
                this.Cache[assetName] = value;
            }

            // avoid hard disposable references; see remarks on the field
            this.BaseDisposableReferences.Clear();
        }

        /// <summary>Parse a cache key into its component parts.</summary>
        /// <param name="cacheKey">The input cache key.</param>
        /// <param name="assetName">The original asset name.</param>
        /// <param name="localeCode">The asset locale code (or <c>null</c> if not localized).</param>
        protected void ParseCacheKey(string cacheKey, out string assetName, out string localeCode)
        {
            // handle localized key
            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                int lastSepIndex = cacheKey.LastIndexOf(".", StringComparison.Ordinal);
                if (lastSepIndex >= 0)
                {
                    string suffix = cacheKey.Substring(lastSepIndex + 1, cacheKey.Length - lastSepIndex - 1);
                    if (this.Coordinator.TryGetLanguageEnum(suffix, out _))
                    {
                        assetName = cacheKey.Substring(0, lastSepIndex);
                        localeCode = cacheKey.Substring(lastSepIndex + 1, cacheKey.Length - lastSepIndex - 1);
                        return;
                    }
                }
            }

            // handle simple key
            assetName = cacheKey;
            localeCode = null;
        }

        /// <summary>Get whether an asset has already been loaded.</summary>
        /// <param name="normalizedAssetName">The normalized asset name.</param>
        /// <param name="language">The language to check.</param>
        protected abstract bool IsNormalizedKeyLoaded(string normalizedAssetName, LanguageCode language);

        /// <summary>Get the asset name from a cache key.</summary>
        /// <param name="cacheKey">The input cache key.</param>
        private string GetAssetName(string cacheKey)
        {
            this.ParseCacheKey(cacheKey, out string assetName, out string _);
            return assetName;
        }
    }
}
