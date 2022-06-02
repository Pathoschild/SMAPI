using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Deprecations;

namespace StardewModdingAPI.Framework.Content;

internal class AssetInfo : IAssetInfo
{
    /*********
    ** Fields
    *********/
    /// <summary>Normalizes an asset key to match the cache key.</summary>
    protected readonly Func<string, string> GetNormalizedPath;

    /// <summary>The backing field for <see cref="NameWithoutLocale"/>.</summary>
    private IAssetName? NameWithoutLocaleImpl;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string? Locale { get; }

    /// <inheritdoc />
    public IAssetName Name { get; }

    /// <inheritdoc />
    public IAssetName NameWithoutLocale => this.NameWithoutLocaleImpl ??= this.Name.GetBaseAssetName();

    /// <inheritdoc />
    [Obsolete($"Use {nameof(AssetInfo.Name)} or {nameof(AssetInfo.NameWithoutLocale)} instead. This property will be removed in SMAPI 4.0.0.")]
    public string AssetName
    {
        get
        {
            SCore.DeprecationManager.Warn(
                source: null,
                nounPhrase: $"{nameof(IAssetInfo)}.{nameof(IAssetInfo.AssetName)}",
                version: "3.14.0",
                severity: DeprecationLevel.Notice,
                unlessStackIncludes: new[]
                {
                    $"{typeof(AssetInterceptorChange).FullName}.{nameof(AssetInterceptorChange.CanIntercept)}",
                    $"{typeof(ContentCoordinator).FullName}.{nameof(ContentCoordinator.GetAssetOperations)}"
                }
            );

            return this.NameWithoutLocale.Name;
        }
    }

    /// <inheritdoc />
    public Type DataType { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locale">The content's locale code, if the content is localized.</param>
    /// <param name="assetName">The asset name being read.</param>
    /// <param name="type">The content type being read.</param>
    /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
    public AssetInfo(string? locale, IAssetName assetName, Type type, Func<string, string> getNormalizedPath)
    {
        this.Locale = locale;
        this.Name = assetName;
        this.DataType = type;
        this.GetNormalizedPath = getNormalizedPath;
    }

    /// <inheritdoc />
    [Obsolete($"Use {nameof(Name)}.{nameof(IAssetName.IsEquivalentTo)} or {nameof(AssetInfo.NameWithoutLocale)}.{nameof(IAssetName.IsEquivalentTo)} instead. This method will be removed in SMAPI 4.0.0.")]
    public bool AssetNameEquals(string path)
    {
        SCore.DeprecationManager.Warn(
            source: null,
            nounPhrase: $"{nameof(IAssetInfo)}.{nameof(IAssetInfo.AssetNameEquals)}",
            version: "3.14.0",
            severity: DeprecationLevel.Notice,
            unlessStackIncludes: new[]
            {
                $"{typeof(AssetInterceptorChange).FullName}.{nameof(AssetInterceptorChange.CanIntercept)}",
                $"{typeof(ContentCoordinator).FullName}.{nameof(ContentCoordinator.GetAssetOperations)}"
            }
        );


        return this.NameWithoutLocale.IsEquivalentTo(path);
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Get a human-readable type name.</summary>
    /// <param name="type">The type to name.</param>
    protected string GetFriendlyTypeName(Type type)
    {
        // dictionary
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            Type[] genericArgs = type.GetGenericArguments();
            return $"Dictionary<{this.GetFriendlyTypeName(genericArgs[0])}, {this.GetFriendlyTypeName(genericArgs[1])}>";
        }

        // texture
        if (type == typeof(Texture2D))
            return type.Name;

        // native type
        if (type == typeof(int))
            return "int";
        if (type == typeof(string))
            return "string";

        // default
        return type.FullName!;
    }
}
