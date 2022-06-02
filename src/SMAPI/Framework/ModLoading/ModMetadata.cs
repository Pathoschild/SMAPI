using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using StardewModdingAPI.Framework.ModHelpers;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>Metadata for a mod.</summary>
internal class ModMetadata : IModMetadata
{
    /*********
    ** Fields
    *********/
    /// <summary>The non-error issues with the mod, including warnings suppressed by the data record.</summary>
    private ModWarning ActualWarnings = ModWarning.None;

    /// <summary>The mod IDs which are listed as a requirement by this mod. The value for each pair indicates whether the dependency is required (i.e. not an optional dependency).</summary>
    private readonly Lazy<IDictionary<string, bool>> Dependencies;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public string DirectoryPath { get; }

    /// <inheritdoc />
    public string RelativeDirectoryPath { get; }

    /// <inheritdoc />
    public IManifest Manifest { get; }

    /// <inheritdoc />
    public ModDataRecordVersionedFields? DataRecord { get; }

    /// <inheritdoc />
    public ModMetadataStatus Status { get; private set; }

    /// <inheritdoc />
    public ModFailReason? FailReason { get; private set; }

    /// <inheritdoc />
    public ModWarning Warnings => this.ActualWarnings & ~(this.DataRecord?.DataRecord.SuppressWarnings ?? ModWarning.None);

    /// <inheritdoc />
    public string? Error { get; private set; }

    /// <inheritdoc />
    public string? ErrorDetails { get; private set; }

    /// <inheritdoc />
    public bool IsIgnored { get; }

    /// <inheritdoc />
    public IMod? Mod { get; private set; }

    /// <inheritdoc />
    public IContentPack? ContentPack { get; private set; }

    /// <inheritdoc />
    public TranslationHelper? Translations { get; private set; }

    /// <inheritdoc />
    public IMonitor? Monitor { get; private set; }

    /// <inheritdoc />
    public object? Api { get; private set; }

    /// <inheritdoc />
    public ModEntryModel? UpdateCheckData { get; private set; }

    /// <inheritdoc />
    [MemberNotNullWhen(true, nameof(ModMetadata.ContentPack))]
    [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier", Justification = "The manifest may be null for broken mods while loading.")]
    public bool IsContentPack => this.Manifest?.ContentPackFor != null;

    /// <summary>The fake content packs created by this mod, if any.</summary>
    public ISet<WeakReference<ContentPack>> FakeContentPacks { get; } = new HashSet<WeakReference<ContentPack>>();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="displayName">The mod's display name.</param>
    /// <param name="directoryPath">The mod's full directory path within the <paramref name="rootPath"/>.</param>
    /// <param name="rootPath">The root path containing mods.</param>
    /// <param name="manifest">The mod manifest.</param>
    /// <param name="dataRecord">Metadata about the mod from SMAPI's internal data (if any).</param>
    /// <param name="isIgnored">Whether the mod folder should be ignored. This should be <c>true</c> if it was found within a folder whose name starts with a dot.</param>
    public ModMetadata(string displayName, string directoryPath, string rootPath, IManifest? manifest, ModDataRecordVersionedFields? dataRecord, bool isIgnored)
    {
        this.DisplayName = displayName;
        this.DirectoryPath = directoryPath;
        this.RootPath = rootPath;
        this.RelativeDirectoryPath = PathUtilities.GetRelativePath(this.RootPath, this.DirectoryPath);
        this.Manifest = manifest!; // manifest may be null in low-level SMAPI code, but won't be null once it's received by mods via IModInfo
        this.DataRecord = dataRecord;
        this.IsIgnored = isIgnored;

        this.Dependencies = new Lazy<IDictionary<string, bool>>(this.ExtractDependencies);
    }

    /// <inheritdoc />
    public IModMetadata SetStatusFound()
    {
        this.SetStatus(ModMetadataStatus.Found, ModFailReason.Incompatible, null);
        this.FailReason = null;
        return this;
    }

    /// <inheritdoc />
    public IModMetadata SetStatus(ModMetadataStatus status, ModFailReason reason, string? error, string? errorDetails = null)
    {
        this.Status = status;
        this.FailReason = reason;
        this.Error = error;
        this.ErrorDetails = errorDetails;
        return this;
    }

    /// <inheritdoc />
    public IModMetadata SetWarning(ModWarning warning)
    {
        this.ActualWarnings |= warning;
        return this;
    }

    /// <inheritdoc />
    public IModMetadata SetMod(IMod mod, TranslationHelper translations)
    {
        if (this.ContentPack != null)
            throw new InvalidOperationException("A mod can't be both an assembly mod and content pack.");

        this.Mod = mod;
        this.Monitor = mod.Monitor;
        this.Translations = translations;
        return this;
    }

    /// <inheritdoc />
    public IModMetadata SetMod(IContentPack contentPack, IMonitor monitor, TranslationHelper translations)
    {
        if (this.Mod != null)
            throw new InvalidOperationException("A mod can't be both an assembly mod and content pack.");

        this.ContentPack = contentPack;
        this.Monitor = monitor;
        this.Translations = translations;
        return this;
    }

    /// <inheritdoc />
    public IModMetadata SetApi(object? api)
    {
        this.Api = api;
        return this;
    }

    /// <inheritdoc />
    public IModMetadata SetUpdateData(ModEntryModel data)
    {
        this.UpdateCheckData = data;
        return this;
    }

    /// <inheritdoc />
    [MemberNotNullWhen(true, nameof(IModInfo.Manifest))]
    public bool HasManifest()
    {
        return this.Manifest != null;
    }

    /// <inheritdoc />
    public bool HasID()
    {
        return
            this.HasManifest()
            && !string.IsNullOrWhiteSpace(this.Manifest.UniqueID);
    }

    /// <inheritdoc />
    public bool HasID(string? id)
    {
        return
            this.HasID()
            && string.Equals(this.Manifest.UniqueID.Trim(), id?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IEnumerable<UpdateKey> GetUpdateKeys(bool validOnly = false)
    {
        if (!this.HasManifest())
            yield break;

        foreach (string rawKey in this.Manifest.UpdateKeys)
        {
            UpdateKey updateKey = UpdateKey.Parse(rawKey);
            if (updateKey.LooksValid || !validOnly)
                yield return updateKey;
        }
    }

    /// <inheritdoc />
    public bool HasRequiredModId(string modId, bool includeOptional)
    {
        return
            this.Dependencies.Value.TryGetValue(modId, out bool isRequired)
            && (includeOptional || isRequired);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRequiredModIds(bool includeOptional = false)
    {
        foreach (var pair in this.Dependencies.Value)
        {
            if (includeOptional || pair.Value)
                yield return pair.Key;
        }
    }

    /// <inheritdoc />
    public bool HasValidUpdateKeys()
    {
        return this.GetUpdateKeys(validOnly: true).Any();
    }

    /// <inheritdoc />
    public bool HasWarnings(params ModWarning[] warnings)
    {
        ModWarning curWarnings = this.Warnings;
        return warnings.Any(warning => curWarnings.HasFlag(warning));
    }

    /// <inheritdoc />
    public string GetRelativePathWithRoot()
    {
        string rootFolderName = Path.GetFileName(this.RootPath);
        return Path.Combine(rootFolderName, this.RelativeDirectoryPath);
    }

    /// <summary>Get the currently live fake content packs created by this mod.</summary>
    public IEnumerable<ContentPack> GetFakeContentPacks()
    {
        foreach (var reference in this.FakeContentPacks.ToArray())
        {
            if (!reference.TryGetTarget(out ContentPack? pack))
            {
                this.FakeContentPacks.Remove(reference);
                continue;
            }

            yield return pack;
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Extract mod IDs from the manifest that must be installed to load this mod.</summary>
    /// <returns>Returns a dictionary of mod ID => is required (i.e. not an optional dependency).</returns>
    public IDictionary<string, bool> ExtractDependencies()
    {
        var ids = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        if (this.HasManifest())
        {
            // yield dependencies
            foreach (IManifestDependency entry in this.Manifest.Dependencies)
            {
                if (!string.IsNullOrWhiteSpace(entry.UniqueID))
                    ids[entry.UniqueID] = entry.IsRequired;
            }

            // yield content pack parent
            if (!string.IsNullOrWhiteSpace(this.Manifest.ContentPackFor?.UniqueID))
                ids[this.Manifest.ContentPackFor.UniqueID] = true;
        }

        return ids;
    }
}