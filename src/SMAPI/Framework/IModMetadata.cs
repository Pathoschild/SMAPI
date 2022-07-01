using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework.ModHelpers;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Framework;

/// <summary>Metadata for a mod.</summary>
internal interface IModMetadata : IModInfo
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod's display name.</summary>
    string DisplayName { get; }

    /// <summary>The root path containing mods.</summary>
    string RootPath { get; }

    /// <summary>The mod's full directory path within the <see cref="RootPath"/>.</summary>
    string DirectoryPath { get; }

    /// <summary>The <see cref="DirectoryPath"/> relative to the <see cref="RootPath"/>.</summary>
    string RelativeDirectoryPath { get; }

    /// <summary>Metadata about the mod from SMAPI's internal data (if any).</summary>
    ModDataRecordVersionedFields? DataRecord { get; }

    /// <summary>The metadata resolution status.</summary>
    ModMetadataStatus Status { get; }

    /// <summary>The reason the mod failed to load, if applicable.</summary>
    ModFailReason? FailReason { get; }

    /// <summary>The non-error issues with the mod, ignoring those suppressed via <see cref="DataRecord"/>.</summary>
    ModWarning Warnings { get; }

    /// <summary>The reason the metadata is invalid, if any.</summary>
    string? Error { get; }

    /// <summary>A detailed technical message for <see cref="Error"/>, if any.</summary>
    string? ErrorDetails { get; }

    /// <summary>Whether the mod folder should be ignored. This is <c>true</c> if it was found within a folder whose name starts with a dot.</summary>
    bool IsIgnored { get; }

    /// <summary>The mod instance (if loaded and <see cref="IModInfo.IsContentPack"/> is false).</summary>
    IMod? Mod { get; }

    /// <summary>The content pack instance (if loaded and <see cref="IModInfo.IsContentPack"/> is true).</summary>
    IContentPack? ContentPack { get; }

    /// <summary>The translations for this mod (if loaded).</summary>
    TranslationHelper? Translations { get; }

    /// <summary>Writes messages to the console and log file as this mod.</summary>
    IMonitor? Monitor { get; }

    /// <summary>The mod-provided API (if any).</summary>
    object? Api { get; }

    /// <summary>The update-check metadata for this mod (if any).</summary>
    ModEntryModel? UpdateCheckData { get; }

    /// <summary>The fake content packs created by this mod, if any.</summary>
    ISet<WeakReference<ContentPack>> FakeContentPacks { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Set the mod status to <see cref="ModMetadataStatus.Found"/>.</summary>
    /// <returns>Return the instance for chaining.</returns>
    IModMetadata SetStatusFound();

    /// <summary>Set the mod status.</summary>
    /// <param name="status">The metadata resolution status.</param>
    /// <param name="reason">The reason a mod could not be loaded.</param>
    /// <param name="error">The reason the metadata is invalid, if any.</param>
    /// <param name="errorDetails">A detailed technical message, if any.</param>
    /// <returns>Return the instance for chaining.</returns>
    IModMetadata SetStatus(ModMetadataStatus status, ModFailReason reason, string? error, string? errorDetails = null);

    /// <summary>Set a warning flag for the mod.</summary>
    /// <param name="warning">The warning to set.</param>
    IModMetadata SetWarning(ModWarning warning);

    /// <summary>Remove a warning flag for the mod.</summary>
    /// <param name="warning">The warning to remove.</param>
    IModMetadata RemoveWarning(ModWarning warning);

    /// <summary>Set the mod instance.</summary>
    /// <param name="mod">The mod instance to set.</param>
    /// <param name="translations">The translations for this mod (if loaded).</param>
    IModMetadata SetMod(IMod mod, TranslationHelper translations);

    /// <summary>Set the mod instance.</summary>
    /// <param name="contentPack">The contentPack instance to set.</param>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="translations">The translations for this mod (if loaded).</param>
    IModMetadata SetMod(IContentPack contentPack, IMonitor monitor, TranslationHelper translations);

    /// <summary>Set the mod-provided API instance.</summary>
    /// <param name="api">The mod-provided API.</param>
    IModMetadata SetApi(object? api);

    /// <summary>Set the update-check metadata for this mod.</summary>
    /// <param name="data">The update-check metadata.</param>
    IModMetadata SetUpdateData(ModEntryModel data);

    /// <summary>Whether the mod manifest was loaded (regardless of whether the mod itself was loaded).</summary>
    bool HasManifest();

    /// <summary>Whether the mod has an ID (regardless of whether the ID is valid or the mod itself was loaded).</summary>
    bool HasID();

    /// <summary>Whether the mod has the given ID.</summary>
    /// <param name="id">The mod ID to check.</param>
    bool HasID(string? id);

    /// <summary>Get the defined update keys.</summary>
    /// <param name="validOnly">Only return valid update keys.</param>
    IEnumerable<UpdateKey> GetUpdateKeys(bool validOnly = true);

    /// <summary>Get whether the given mod ID must be installed to load this mod.</summary>
    /// <param name="modId">The mod ID to check.</param>
    /// <param name="includeOptional">Whether to include optional dependencies.</param>
    bool HasRequiredModId(string modId, bool includeOptional);

    /// <summary>Get the mod IDs that must be installed to load this mod.</summary>
    /// <param name="includeOptional">Whether to include optional dependencies.</param>
    IEnumerable<string> GetRequiredModIds(bool includeOptional = false);

    /// <summary>Whether the mod has at least one valid update key set.</summary>
    bool HasValidUpdateKeys();

    /// <summary>Get whether the mod has any of the given warnings, ignoring those suppressed via <see cref="DataRecord"/>.</summary>
    /// <param name="warnings">The warnings to check.</param>
    bool HasWarnings(params ModWarning[] warnings);

    /// <summary>Get a relative path which includes the root folder name.</summary>
    string GetRelativePathWithRoot();

    /// <summary>Get the currently live fake content packs created by this mod.</summary>
    IEnumerable<ContentPack> GetFakeContentPacks();
}
