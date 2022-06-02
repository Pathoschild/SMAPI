using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework;
using StardewModdingAPI.Web.Framework.Caching;
using StardewModdingAPI.Web.Framework.Caching.Mods;
using StardewModdingAPI.Web.Framework.Caching.Wiki;
using StardewModdingAPI.Web.Framework.Clients;
using StardewModdingAPI.Web.Framework.Clients.Chucklefish;
using StardewModdingAPI.Web.Framework.Clients.CurseForge;
using StardewModdingAPI.Web.Framework.Clients.GitHub;
using StardewModdingAPI.Web.Framework.Clients.ModDrop;
using StardewModdingAPI.Web.Framework.Clients.Nexus;
using StardewModdingAPI.Web.Framework.ConfigModels;

namespace StardewModdingAPI.Web.Controllers;

/// <summary>Provides an API to perform mod update checks.</summary>
[Produces("application/json")]
[Route("api/v{version:semanticVersion}/mods")]
internal class ModsApiController : Controller
{
    /*********
    ** Fields
    *********/
    /// <summary>The mod sites which provide mod metadata.</summary>
    private readonly ModSiteManager ModSites;

    /// <summary>The cache in which to store wiki data.</summary>
    private readonly IWikiCacheRepository WikiCache;

    /// <summary>The cache in which to store mod data.</summary>
    private readonly IModCacheRepository ModCache;

    /// <summary>The config settings for mod update checks.</summary>
    private readonly IOptions<ModUpdateCheckConfig> Config;

    /// <summary>The internal mod metadata list.</summary>
    private readonly ModDatabase ModDatabase;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="environment">The web hosting environment.</param>
    /// <param name="wikiCache">The cache in which to store wiki data.</param>
    /// <param name="modCache">The cache in which to store mod metadata.</param>
    /// <param name="config">The config settings for mod update checks.</param>
    /// <param name="chucklefish">The Chucklefish API client.</param>
    /// <param name="curseForge">The CurseForge API client.</param>
    /// <param name="github">The GitHub API client.</param>
    /// <param name="modDrop">The ModDrop API client.</param>
    /// <param name="nexus">The Nexus API client.</param>
    public ModsApiController(IWebHostEnvironment environment, IWikiCacheRepository wikiCache, IModCacheRepository modCache, IOptions<ModUpdateCheckConfig> config, IChucklefishClient chucklefish, ICurseForgeClient curseForge, IGitHubClient github, IModDropClient modDrop, INexusClient nexus)
    {
        this.ModDatabase = new ModToolkit().GetModDatabase(Path.Combine(environment.WebRootPath, "SMAPI.metadata.json"));

        this.WikiCache = wikiCache;
        this.ModCache = modCache;
        this.Config = config;
        this.ModSites = new ModSiteManager(new IModSiteClient[] { chucklefish, curseForge, github, modDrop, nexus });
    }

    /// <summary>Fetch version metadata for the given mods.</summary>
    /// <param name="model">The mod search criteria.</param>
    /// <param name="version">The requested API version.</param>
    [HttpPost]
    public async Task<IEnumerable<ModEntryModel>> PostAsync([FromBody] ModSearchModel? model, [FromRoute] string version)
    {
        if (model?.Mods == null)
            return Array.Empty<ModEntryModel>();

        ModUpdateCheckConfig config = this.Config.Value;

        // fetch wiki data
        WikiModEntry[] wikiData = this.WikiCache.GetWikiMods().Select(p => p.Data).ToArray();
        IDictionary<string, ModEntryModel> mods = new Dictionary<string, ModEntryModel>(StringComparer.CurrentCultureIgnoreCase);
        foreach (ModSearchEntryModel mod in model.Mods)
        {
            if (string.IsNullOrWhiteSpace(mod.ID))
                continue;

            // special case: if this is an update check for the official SMAPI repo, check the Nexus mod page for beta versions
            if (mod.ID == config.SmapiInfo.ID && mod.UpdateKeys.Any(key => key == config.SmapiInfo.DefaultUpdateKey) && mod.InstalledVersion?.IsPrerelease() == true)
                mod.AddUpdateKeys(config.SmapiInfo.AddBetaUpdateKeys);

            // fetch result
            ModEntryModel result = await this.GetModData(mod, wikiData, model.IncludeExtendedMetadata, model.ApiVersion);
            if (!model.IncludeExtendedMetadata && (model.ApiVersion == null || mod.InstalledVersion == null))
            {
                result.Errors = result.Errors
                    .Concat(new[] { $"This API can't suggest an update because {nameof(model.ApiVersion)} or {nameof(mod.InstalledVersion)} are null, and you didn't specify {nameof(model.IncludeExtendedMetadata)} to get other info. See the SMAPI technical docs for usage." })
                    .ToArray();
            }

            mods[mod.ID] = result;
        }

        // return data
        return mods.Values;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the metadata for a mod.</summary>
    /// <param name="search">The mod data to match.</param>
    /// <param name="wikiData">The wiki data.</param>
    /// <param name="includeExtendedMetadata">Whether to include extended metadata for each mod.</param>
    /// <param name="apiVersion">The SMAPI version installed by the player.</param>
    /// <returns>Returns the mod data if found, else <c>null</c>.</returns>
    private async Task<ModEntryModel> GetModData(ModSearchEntryModel search, WikiModEntry[] wikiData, bool includeExtendedMetadata, ISemanticVersion? apiVersion)
    {
        // cross-reference data
        ModDataRecord? record = this.ModDatabase.Get(search.ID);
        WikiModEntry? wikiEntry = wikiData.FirstOrDefault(entry => entry.ID.Contains(search.ID.Trim(), StringComparer.OrdinalIgnoreCase));
        UpdateKey[] updateKeys = this.GetUpdateKeys(search.UpdateKeys, record, wikiEntry).ToArray();
        ModOverrideConfig? overrides = this.Config.Value.ModOverrides.FirstOrDefault(p => p.ID.Equals(search.ID.Trim(), StringComparison.OrdinalIgnoreCase));
        bool allowNonStandardVersions = overrides?.AllowNonStandardVersions ?? false;

        // SMAPI versions with a '-beta' tag indicate major changes that may need beta mod versions.
        // This doesn't apply to normal prerelease versions which have an '-alpha' tag.
        bool isSmapiBeta = apiVersion != null && apiVersion.IsPrerelease() && apiVersion.PrereleaseTag.StartsWith("beta");

        // get latest versions
        ModEntryModel result = new(search.ID);
        IList<string> errors = new List<string>();
        ModEntryVersionModel? main = null;
        ModEntryVersionModel? optional = null;
        ModEntryVersionModel? unofficial = null;
        ModEntryVersionModel? unofficialForBeta = null;
        foreach (UpdateKey updateKey in updateKeys)
        {
            // validate update key
            if (!updateKey.LooksValid)
            {
                errors.Add($"The update key '{updateKey}' isn't in a valid format. It should contain the site key and mod ID like 'Nexus:541', with an optional subkey like 'Nexus:541@subkey'.");
                continue;
            }

            // fetch data
            ModInfoModel data = await this.GetInfoForUpdateKeyAsync(updateKey, allowNonStandardVersions, wikiEntry?.Overrides?.ChangeRemoteVersions);
            if (data.Status != RemoteModStatus.Ok)
            {
                errors.Add(data.Error ?? data.Status.ToString());
                continue;
            }

            // handle versions
            if (this.IsNewer(data.Version, main?.Version))
                main = new ModEntryVersionModel(data.Version, data.Url!);
            if (this.IsNewer(data.PreviewVersion, optional?.Version))
                optional = new ModEntryVersionModel(data.PreviewVersion, data.Url!);
        }

        // get unofficial version
        if (wikiEntry?.Compatibility.UnofficialVersion != null && this.IsNewer(wikiEntry.Compatibility.UnofficialVersion, main?.Version) && this.IsNewer(wikiEntry.Compatibility.UnofficialVersion, optional?.Version))
            unofficial = new ModEntryVersionModel(wikiEntry.Compatibility.UnofficialVersion, $"{this.Url.PlainAction("Index", "Mods", absoluteUrl: true)}#{wikiEntry.Anchor}");

        // get unofficial version for beta
        if (wikiEntry is { HasBetaInfo: true })
        {
            if (wikiEntry.BetaCompatibility.Status == WikiCompatibilityStatus.Unofficial)
            {
                if (wikiEntry.BetaCompatibility.UnofficialVersion != null)
                {
                    unofficialForBeta = (wikiEntry.BetaCompatibility.UnofficialVersion != null && this.IsNewer(wikiEntry.BetaCompatibility.UnofficialVersion, main?.Version) && this.IsNewer(wikiEntry.BetaCompatibility.UnofficialVersion, optional?.Version))
                        ? new ModEntryVersionModel(wikiEntry.BetaCompatibility.UnofficialVersion, $"{this.Url.PlainAction("Index", "Mods", absoluteUrl: true)}#{wikiEntry.Anchor}")
                        : null;
                }
                else
                    unofficialForBeta = unofficial;
            }
        }

        // fallback to preview if latest is invalid
        if (main == null && optional != null)
        {
            main = optional;
            optional = null;
        }

        // special cases
        if (overrides?.SetUrl != null)
        {
            if (main != null)
                main = new(main.Version, overrides.SetUrl);
            if (optional != null)
                optional = new(optional.Version, overrides.SetUrl);
        }

        // get recommended update (if any)
        ISemanticVersion? installedVersion = this.ModSites.GetMappedVersion(search.InstalledVersion?.ToString(), wikiEntry?.Overrides?.ChangeLocalVersions, allowNonStandard: allowNonStandardVersions);
        if (apiVersion != null && installedVersion != null)
        {
            // get newer versions
            List<ModEntryVersionModel> updates = new List<ModEntryVersionModel>();
            if (this.IsRecommendedUpdate(installedVersion, main?.Version, useBetaChannel: true))
                updates.Add(main);
            if (this.IsRecommendedUpdate(installedVersion, optional?.Version, useBetaChannel: isSmapiBeta || installedVersion.IsPrerelease() || search.IsBroken))
                updates.Add(optional);
            if (this.IsRecommendedUpdate(installedVersion, unofficial?.Version, useBetaChannel: true))
                updates.Add(unofficial);
            if (this.IsRecommendedUpdate(installedVersion, unofficialForBeta?.Version, useBetaChannel: apiVersion.IsPrerelease()))
                updates.Add(unofficialForBeta);

            // get newest version
            ModEntryVersionModel? newest = null;
            foreach (ModEntryVersionModel update in updates)
            {
                if (newest == null || update.Version.IsNewerThan(newest.Version))
                    newest = update;
            }

            // set field
            result.SuggestedUpdate = newest != null
                ? new ModEntryVersionModel(newest.Version, newest.Url)
                : null;
        }

        // add extended metadata
        if (includeExtendedMetadata)
            result.Metadata = new ModExtendedMetadataModel(wikiEntry, record, main: main, optional: optional, unofficial: unofficial, unofficialForBeta: unofficialForBeta);

        // add result
        result.Errors = errors.ToArray();
        return result;
    }

    /// <summary>Get whether a given version should be offered to the user as an update.</summary>
    /// <param name="currentVersion">The current semantic version.</param>
    /// <param name="newVersion">The target semantic version.</param>
    /// <param name="useBetaChannel">Whether the user enabled the beta channel and should be offered prerelease updates.</param>
    private bool IsRecommendedUpdate(ISemanticVersion currentVersion, [NotNullWhen(true)] ISemanticVersion? newVersion, bool useBetaChannel)
    {
        return
            newVersion != null
            && newVersion.IsNewerThan(currentVersion)
            && (useBetaChannel || !newVersion.IsPrerelease());
    }

    /// <summary>Get whether a <paramref name="current"/> version is newer than an <paramref name="other"/> version.</summary>
    /// <param name="current">The current version.</param>
    /// <param name="other">The other version.</param>
    private bool IsNewer([NotNullWhen(true)] ISemanticVersion? current, ISemanticVersion? other)
    {
        return current != null && (other == null || other.IsOlderThan(current));
    }

    /// <summary>Get the mod info for an update key.</summary>
    /// <param name="updateKey">The namespaced update key.</param>
    /// <param name="allowNonStandardVersions">Whether to allow non-standard versions.</param>
    /// <param name="mapRemoteVersions">The changes to apply to remote versions for update checks.</param>
    private async Task<ModInfoModel> GetInfoForUpdateKeyAsync(UpdateKey updateKey, bool allowNonStandardVersions, ChangeDescriptor? mapRemoteVersions)
    {
        if (!updateKey.LooksValid)
            return new ModInfoModel().SetError(RemoteModStatus.DoesNotExist, $"Invalid update key '{updateKey}'.");

        // get mod page
        IModPage page;
        {
            bool isCached =
                this.ModCache.TryGetMod(updateKey.Site, updateKey.ID, out Cached<IModPage>? cachedMod)
                && !this.ModCache.IsStale(cachedMod.LastUpdated, cachedMod.Data.Status == RemoteModStatus.TemporaryError ? this.Config.Value.ErrorCacheMinutes : this.Config.Value.SuccessCacheMinutes);

            if (isCached)
                page = cachedMod!.Data;
            else
            {
                page = await this.ModSites.GetModPageAsync(updateKey);
                this.ModCache.SaveMod(updateKey.Site, updateKey.ID, page);
            }
        }

        // get version info
        return this.ModSites.GetPageVersions(page, updateKey.Subkey, allowNonStandardVersions, mapRemoteVersions);
    }

    /// <summary>Get update keys based on the available mod metadata, while maintaining the precedence order.</summary>
    /// <param name="specifiedKeys">The specified update keys.</param>
    /// <param name="record">The mod's entry in SMAPI's internal database.</param>
    /// <param name="entry">The mod's entry in the wiki list.</param>
    private IEnumerable<UpdateKey> GetUpdateKeys(string[]? specifiedKeys, ModDataRecord? record, WikiModEntry? entry)
    {
        // get unique update keys
        List<UpdateKey> updateKeys = this.GetUnfilteredUpdateKeys(specifiedKeys, record, entry)
            .Select(UpdateKey.Parse)
            .Distinct()
            .ToList();

        // apply overrides from wiki
        if (entry?.Overrides?.ChangeUpdateKeys?.HasChanges == true)
        {
            List<string> newKeys = updateKeys.Select(p => p.ToString()).ToList();
            entry.Overrides.ChangeUpdateKeys.Apply(newKeys);
            updateKeys = newKeys.Select(UpdateKey.Parse).ToList();
        }

        // if the list has both an update key (like "Nexus:2400") and subkey (like "Nexus:2400@subkey") for the same page, the subkey takes priority
        {
            var removeKeys = new HashSet<UpdateKey>();
            foreach (UpdateKey key in updateKeys)
            {
                if (key.Subkey != null)
                    removeKeys.Add(new UpdateKey(key.Site, key.ID, null));
            }
            if (removeKeys.Any())
                updateKeys.RemoveAll(removeKeys.Contains);
        }

        return updateKeys;
    }

    /// <summary>Get every available update key based on the available mod metadata, including duplicates and keys which should be filtered.</summary>
    /// <param name="specifiedKeys">The specified update keys.</param>
    /// <param name="record">The mod's entry in SMAPI's internal database.</param>
    /// <param name="entry">The mod's entry in the wiki list.</param>
    private IEnumerable<string> GetUnfilteredUpdateKeys(string[]? specifiedKeys, ModDataRecord? record, WikiModEntry? entry)
    {
        // specified update keys
        foreach (string key in specifiedKeys ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(key))
                yield return key.Trim();
        }

        // default update key
        {
            string? defaultKey = record?.GetDefaultUpdateKey();
            if (!string.IsNullOrWhiteSpace(defaultKey))
                yield return defaultKey;
        }

        // wiki metadata
        if (entry != null)
        {
            if (entry.NexusID.HasValue)
                yield return UpdateKey.GetString(ModSiteKey.Nexus, entry.NexusID.ToString());
            if (entry.ModDropID.HasValue)
                yield return UpdateKey.GetString(ModSiteKey.ModDrop, entry.ModDropID.ToString());
            if (entry.CurseForgeID.HasValue)
                yield return UpdateKey.GetString(ModSiteKey.CurseForge, entry.CurseForgeID.ToString());
            if (entry.ChucklefishID.HasValue)
                yield return UpdateKey.GetString(ModSiteKey.Chucklefish, entry.ChucklefishID.ToString());
        }
    }
}