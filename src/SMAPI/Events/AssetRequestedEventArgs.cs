using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Content;
using xTile;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IContentEvents.AssetRequested"/> event.</summary>
public class AssetRequestedEventArgs : EventArgs
{
    /*********
    ** Fields
    *********/
    /// <summary>The mod handling the event.</summary>
    private IModMetadata? Mod;

    /// <summary>Get the mod metadata for a content pack, if it's a valid content pack for the mod.</summary>
    private readonly Func<IModMetadata, string?, string, IModMetadata?> GetOnBehalfOf;

    /// <summary>The asset info being requested.</summary>
    private readonly IAssetInfo AssetInfo;


    /*********
    ** Accessors
    *********/
    /// <summary>The name of the asset being requested.</summary>
    public IAssetName Name => this.AssetInfo.Name;

    /// <summary>The <see cref="Name"/> with any locale codes stripped.</summary>
    /// <remarks>For example, if <see cref="Name"/> contains a locale like <c>Data/Bundles.fr-FR</c>, this will be the name without locale like <c>Data/Bundles</c>. If the name has no locale, this field is equivalent.</remarks>
    public IAssetName NameWithoutLocale => this.AssetInfo.NameWithoutLocale;

    /// <summary>The requested data type.</summary>
    public Type DataType => this.AssetInfo.DataType;

    /// <summary>The load operations requested by the event handler.</summary>
    internal List<AssetLoadOperation> LoadOperations { get; } = new();

    /// <summary>The edit operations requested by the event handler.</summary>
    internal List<AssetEditOperation> EditOperations { get; } = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="assetInfo">The asset info being requested.</param>
    /// <param name="getOnBehalfOf">Get the mod metadata for a content pack, if it's a valid content pack for the mod.</param>
    internal AssetRequestedEventArgs(IAssetInfo assetInfo, Func<IModMetadata, string?, string, IModMetadata?> getOnBehalfOf)
    {
        this.AssetInfo = assetInfo;
        this.GetOnBehalfOf = getOnBehalfOf;
    }

    /// <summary>Set the mod handling the event.</summary>
    /// <param name="mod">The mod handling the event.</param>
    internal void SetMod(IModMetadata mod)
    {
        this.Mod = mod;
    }

    /// <summary>Provide the initial instance for the asset, instead of trying to load it from the game's <c>Content</c> folder.</summary>
    /// <param name="load">Get the initial instance of an asset.</param>
    /// <param name="priority">If there are multiple loads that apply to the same asset, the priority with which this one should be applied.</param>
    /// <param name="onBehalfOf">The content pack ID on whose behalf you're applying the change. This is only valid for content packs for your mod.</param>
    /// <remarks>
    /// Usage notes:
    /// <list type="bullet">
    ///   <item>The asset doesn't need to exist in the game's <c>Content</c> folder. If any mod loads the asset, the game will see it as an existing asset as if it was in that folder.</item>
    ///   <item>Each asset can logically only have one initial instance. If multiple loads apply at the same time, SMAPI will use the <paramref name="priority"/> parameter to decide what happens. If you're making changes to the existing asset instead of replacing it, you should use <see cref="Edit"/> instead to avoid those limitations and improve mod compatibility.</item>
    /// </list>
    /// </remarks>
    public void LoadFrom(Func<object> load, AssetLoadPriority priority, string? onBehalfOf = null)
    {
        IModMetadata mod = this.GetMod();
        this.LoadOperations.Add(
            new AssetLoadOperation(
                Mod: mod,
                OnBehalfOf: this.GetOnBehalfOf(mod, onBehalfOf, "load assets"),
                Priority: priority,
                GetData: _ => load()
            )
        );
    }

    /// <summary>Provide the initial instance for the asset from a file in your mod folder, instead of trying to load it from the game's <c>Content</c> folder.</summary>
    /// <typeparam name="TAsset">The expected data type. The main supported types are <see cref="Map"/>, <see cref="Texture2D"/>, dictionaries, and lists; other types may be supported by the game's content pipeline.</typeparam>
    /// <param name="relativePath">The relative path to the file in your mod folder.</param>
    /// <param name="priority">If there are multiple loads that apply to the same asset, the priority with which this one should be applied.</param>
    /// <remarks>
    /// Usage notes:
    /// <list type="bullet">
    ///   <item>The asset doesn't need to exist in the game's <c>Content</c> folder. If any mod loads the asset, the game will see it as an existing asset as if it was in that folder.</item>
    ///   <item>Each asset can logically only have one initial instance. If multiple loads apply at the same time, SMAPI will raise an error and ignore all of them. If you're making changes to the existing asset instead of replacing it, you should use <see cref="Edit"/> instead to avoid those limitations and improve mod compatibility.</item>
    /// </list>
    /// </remarks>
    public void LoadFromModFile<TAsset>(string relativePath, AssetLoadPriority priority)
        where TAsset : notnull
    {
        IModMetadata mod = this.GetMod();
        this.LoadOperations.Add(
            new AssetLoadOperation(
                Mod: mod,
                OnBehalfOf: null,
                Priority: priority,
                GetData: _ => mod.Mod!.Helper.ModContent.Load<TAsset>(relativePath)
            )
        );
    }

    /// <summary>Edit the asset after it's loaded.</summary>
    /// <param name="apply">Apply changes to the asset.</param>
    /// <param name="priority">If there are multiple edits that apply to the same asset, the priority with which this one should be applied.</param>
    /// <param name="onBehalfOf">The content pack ID on whose behalf you're applying the change. This is only valid for content packs for your mod.</param>
    /// <remarks>
    /// Usage notes:
    /// <list type="bullet">
    ///   <item>Editing an asset which doesn't exist has no effect. This is applied after the asset is loaded from the game's <c>Content</c> folder, or from any mod's <see cref="LoadFrom"/> or <see cref="LoadFromModFile{TAsset}"/>.</item>
    ///   <item>You can apply any number of edits to the asset. Each edit will be applied on top of the previous one (i.e. it'll see the merged asset from all previous edits as its input).</item>
    /// </list>
    /// </remarks>
    public void Edit(Action<IAssetData> apply, AssetEditPriority priority = AssetEditPriority.Default, string? onBehalfOf = null)
    {
        IModMetadata mod = this.GetMod();
        this.EditOperations.Add(
            new AssetEditOperation(
                Mod: mod,
                Priority: priority,
                OnBehalfOf: this.GetOnBehalfOf(mod, onBehalfOf, "edit assets"),
                ApplyEdit: apply
            )
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the mod handling the event.</summary>
    /// <exception cref="InvalidOperationException">This instance hasn't been initialized with the mod metadata yet.</exception>
    private IModMetadata GetMod()
    {
        return this.Mod ?? throw new InvalidOperationException($"This {nameof(AssetRequestedEventArgs)} instance hasn't been initialized yet.");
    }
}