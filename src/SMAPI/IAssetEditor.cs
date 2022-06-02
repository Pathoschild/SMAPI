using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI;

/// <summary>Edits matching content assets.</summary>
[Obsolete($"Use {nameof(IMod.Helper)}.{nameof(IModHelper.Events)}.{nameof(IModEvents.Content)} instead. This interface will be removed in SMAPI 4.0.0.")]
public interface IAssetEditor
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get whether this instance can edit the given asset.</summary>
    /// <param name="asset">Basic metadata about the asset being loaded.</param>
    bool CanEdit<T>(IAssetInfo asset);

    /// <summary>Edit a matched asset.</summary>
    /// <param name="asset">A helper which encapsulates metadata about an asset and enables changes to it.</param>
    void Edit<T>(IAssetData asset);
}