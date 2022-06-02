using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Content;

/// <summary>An operation which provides the initial instance of an asset when it's requested from the content pipeline.</summary>
/// <param name="Mod">The mod applying the edit.</param>
/// <param name="Priority">If there are multiple loads that apply to the same asset, the priority with which this one should be applied.</param>
/// <param name="OnBehalfOf">The content pack on whose behalf the asset is being loaded, if any.</param>
/// <param name="GetData">Load the initial value for an asset.</param>
internal record AssetLoadOperation(IModMetadata Mod, IModMetadata? OnBehalfOf, AssetLoadPriority Priority, Func<IAssetInfo, object> GetData);
