using System;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;

namespace StardewModdingAPI.Framework.ModHelpers;

/// <inheritdoc cref="IModContentHelper"/>
internal class ModContentHelper : BaseHelper, IModContentHelper
{
    /*********
    ** Fields
    *********/
    /// <summary>SMAPI's core content logic.</summary>
    private readonly ContentCoordinator ContentCore;

    /// <summary>A content manager for this mod which manages files from the mod's folder.</summary>
    private readonly ModContentManager ModContentManager;

    /// <summary>The friendly mod name for use in errors.</summary>
    private readonly string ModName;

    /// <summary>Simplifies access to private code.</summary>
    private readonly Reflector Reflection;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="contentCore">SMAPI's core content logic.</param>
    /// <param name="modFolderPath">The absolute path to the mod folder.</param>
    /// <param name="mod">The mod using this instance.</param>
    /// <param name="modName">The friendly mod name for use in errors.</param>
    /// <param name="gameContentManager">The game content manager used for map tilesheets not provided by the mod.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    public ModContentHelper(ContentCoordinator contentCore, string modFolderPath, IModMetadata mod, string modName, IContentManager gameContentManager, Reflector reflection)
        : base(mod)
    {
        string managedAssetPrefix = contentCore.GetManagedAssetPrefix(mod.Manifest.UniqueID);

        this.ContentCore = contentCore;
        this.ModContentManager = contentCore.CreateModContentManager(managedAssetPrefix, modName, modFolderPath, gameContentManager);
        this.ModName = modName;
        this.Reflection = reflection;
    }

    /// <inheritdoc />
    public T Load<T>(string relativePath)
        where T : notnull
    {
        IAssetName assetName = this.ContentCore.ParseAssetName(relativePath, allowLocales: false);

        try
        {
            return this.ModContentManager.LoadExact<T>(assetName, useCache: false);
        }
        catch (Exception ex) when (ex is not SContentLoadException)
        {
            throw new SContentLoadException(ContentLoadErrorType.Other, $"{this.ModName} failed loading content asset '{relativePath}' from its mod folder.", ex);
        }
    }

    /// <inheritdoc />
    public IAssetName GetInternalAssetName(string relativePath)
    {
        return this.ModContentManager.GetInternalAssetKey(relativePath);
    }

    /// <inheritdoc />
    public IAssetData GetPatchHelper<T>(T data, string? relativePath = null)
        where T : notnull
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "Can't get a patch helper for a null value.");

        relativePath ??= $"temp/{Guid.NewGuid():N}";

        return new AssetDataForObject(
            locale: this.ContentCore.GetLocale(),
            assetName: this.ContentCore.ParseAssetName(relativePath, allowLocales: false),
            data: data,
            getNormalizedPath: key => this.ContentCore.ParseAssetName(key, allowLocales: false).Name,
            reflection: this.Reflection
        );
    }
}
