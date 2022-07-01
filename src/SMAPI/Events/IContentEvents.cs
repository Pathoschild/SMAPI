using System;

namespace StardewModdingAPI.Events;

/// <summary>Events related to assets loaded from the content pipeline (including data, maps, and textures).</summary>
public interface IContentEvents
{
    /// <summary>Raised when an asset is being requested from the content pipeline.</summary>
    /// <remarks>
    /// The asset isn't necessarily being loaded yet (e.g. the game may be checking if it exists). Mods can register the changes they want to apply using methods on the event arguments. These will be applied when the asset is actually loaded.
    ///
    /// If the asset is requested multiple times in the same tick (e.g. once to check if it exists and once to load it), SMAPI might only raise the event once and reuse the cached result.
    /// </remarks>
    event EventHandler<AssetRequestedEventArgs> AssetRequested;

    /// <summary>Raised after one or more assets were invalidated from the content cache by a mod, so they'll be reloaded next time they're requested. If the assets will be reloaded or propagated automatically, this event is raised before that happens.</summary>
    event EventHandler<AssetsInvalidatedEventArgs> AssetsInvalidated;

    /// <summary>Raised after an asset is loaded by the content pipeline, after all mod edits specified via <see cref="AssetRequested"/> have been applied.</summary>
    /// <remarks>This event is only raised if something requested the asset from the content pipeline. Invalidating an asset from the content cache won't necessarily reload it automatically.</remarks>
    event EventHandler<AssetReadyEventArgs> AssetReady;

    /// <summary>Raised after the game language changes.</summary>
    /// <remarks>For non-English players, this may be raised during startup when the game switches to the previously selected language.</remarks>
    event EventHandler<LocaleChangedEventArgs> LocaleChanged;
}
