using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI;

/// <summary>Provides the initial version for matching assets loaded by the game. SMAPI will raise an error if two mods try to load the same asset; in most cases you should use <see cref="IAssetEditor"/> instead.</summary>
[Obsolete($"Use {nameof(IMod.Helper)}.{nameof(IModHelper.Events)}.{nameof(IModEvents.Content)} instead. This interface will be removed in SMAPI 4.0.0.")]
public interface IAssetLoader
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get whether this instance can load the initial version of the given asset.</summary>
    /// <param name="asset">Basic metadata about the asset being loaded.</param>
    bool CanLoad<T>(IAssetInfo asset);

    /// <summary>Load a matched asset.</summary>
    /// <param name="asset">Basic metadata about the asset being loaded.</param>
    T Load<T>(IAssetInfo asset);
}
