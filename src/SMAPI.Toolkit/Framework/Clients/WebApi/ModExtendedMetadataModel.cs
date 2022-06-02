using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;
using StardewModdingAPI.Toolkit.Framework.ModData;

namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi;

/// <summary>Extended metadata about a mod.</summary>
public class ModExtendedMetadataModel
{
    /*********
    ** Accessors
    *********/
    /****
    ** Mod info
    ****/
    /// <summary>The mod's unique ID. A mod may have multiple current IDs in rare cases (e.g. due to parallel releases or unofficial updates).</summary>
    public string[] ID { get; set; } = Array.Empty<string>();

    /// <summary>The mod's display name.</summary>
    public string? Name { get; set; }

    /// <summary>The mod ID on Nexus.</summary>
    public int? NexusID { get; set; }

    /// <summary>The mod ID in the Chucklefish mod repo.</summary>
    public int? ChucklefishID { get; set; }

    /// <summary>The mod ID in the CurseForge mod repo.</summary>
    public int? CurseForgeID { get; set; }

    /// <summary>The mod key in the CurseForge mod repo (used in mod page URLs).</summary>
    public string? CurseForgeKey { get; set; }

    /// <summary>The mod ID in the ModDrop mod repo.</summary>
    public int? ModDropID { get; set; }

    /// <summary>The GitHub repository in the form 'owner/repo'.</summary>
    public string? GitHubRepo { get; set; }

    /// <summary>The URL to a non-GitHub source repo.</summary>
    public string? CustomSourceUrl { get; set; }

    /// <summary>The custom mod page URL (if applicable).</summary>
    public string? CustomUrl { get; set; }

    /// <summary>The main version.</summary>
    public ModEntryVersionModel? Main { get; set; }

    /// <summary>The latest optional version, if newer than <see cref="Main"/>.</summary>
    public ModEntryVersionModel? Optional { get; set; }

    /// <summary>The latest unofficial version, if newer than <see cref="Main"/> and <see cref="Optional"/>.</summary>
    public ModEntryVersionModel? Unofficial { get; set; }

    /// <summary>The latest unofficial version for the current Stardew Valley or SMAPI beta, if any.</summary>
    public ModEntryVersionModel? UnofficialForBeta { get; set; }

    /****
    ** Stable compatibility
    ****/
    /// <summary>The compatibility status.</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public WikiCompatibilityStatus? CompatibilityStatus { get; set; }

    /// <summary>The human-readable summary of the compatibility status or workaround, without HTML formatting.</summary>
    public string? CompatibilitySummary { get; set; }

    /// <summary>The game or SMAPI version which broke this mod, if applicable.</summary>
    public string? BrokeIn { get; set; }

    /****
    ** Beta compatibility
    ****/
    /// <summary>The compatibility status for the Stardew Valley beta (if any).</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public WikiCompatibilityStatus? BetaCompatibilityStatus { get; set; }

    /// <summary>The human-readable summary of the compatibility status or workaround for the Stardew Valley beta (if any), without HTML formatting.</summary>
    public string? BetaCompatibilitySummary { get; set; }

    /// <summary>The beta game or SMAPI version which broke this mod, if applicable.</summary>
    public string? BetaBrokeIn { get; set; }

    /****
    ** Version mappings
    ****/
    /// <summary>A serialized change descriptor to apply to the local version during update checks (see <see cref="ChangeDescriptor"/>).</summary>
    public string? ChangeLocalVersions { get; set; }

    /// <summary>A serialized change descriptor to apply to the remote version during update checks (see <see cref="ChangeDescriptor"/>).</summary>
    public string? ChangeRemoteVersions { get; set; }

    /// <summary>A serialized change descriptor to apply to the update keys during update checks (see <see cref="ChangeDescriptor"/>).</summary>
    public string? ChangeUpdateKeys { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public ModExtendedMetadataModel() { }

    /// <summary>Construct an instance.</summary>
    /// <param name="wiki">The mod metadata from the wiki (if available).</param>
    /// <param name="db">The mod metadata from SMAPI's internal DB (if available).</param>
    /// <param name="main">The main version.</param>
    /// <param name="optional">The latest optional version, if newer than <paramref name="main"/>.</param>
    /// <param name="unofficial">The latest unofficial version, if newer than <paramref name="main"/> and <paramref name="optional"/>.</param>
    /// <param name="unofficialForBeta">The latest unofficial version for the current Stardew Valley or SMAPI beta, if any.</param>
    public ModExtendedMetadataModel(WikiModEntry? wiki, ModDataRecord? db, ModEntryVersionModel? main, ModEntryVersionModel? optional, ModEntryVersionModel? unofficial, ModEntryVersionModel? unofficialForBeta)
    {
        // versions
        this.Main = main;
        this.Optional = optional;
        this.Unofficial = unofficial;
        this.UnofficialForBeta = unofficialForBeta;

        // wiki data
        if (wiki != null)
        {
            this.ID = wiki.ID;
            this.Name = wiki.Name.FirstOrDefault();
            this.NexusID = wiki.NexusID;
            this.ChucklefishID = wiki.ChucklefishID;
            this.CurseForgeID = wiki.CurseForgeID;
            this.CurseForgeKey = wiki.CurseForgeKey;
            this.ModDropID = wiki.ModDropID;
            this.GitHubRepo = wiki.GitHubRepo;
            this.CustomSourceUrl = wiki.CustomSourceUrl;
            this.CustomUrl = wiki.CustomUrl;

            this.CompatibilityStatus = wiki.Compatibility.Status;
            this.CompatibilitySummary = wiki.Compatibility.Summary;
            this.BrokeIn = wiki.Compatibility.BrokeIn;

            this.BetaCompatibilityStatus = wiki.BetaCompatibility?.Status;
            this.BetaCompatibilitySummary = wiki.BetaCompatibility?.Summary;
            this.BetaBrokeIn = wiki.BetaCompatibility?.BrokeIn;

            this.ChangeLocalVersions = wiki.Overrides?.ChangeLocalVersions?.ToString();
            this.ChangeRemoteVersions = wiki.Overrides?.ChangeRemoteVersions?.ToString();
            this.ChangeUpdateKeys = wiki.Overrides?.ChangeUpdateKeys?.ToString();
        }

        // internal DB data
        if (db != null)
        {
            this.ID = this.ID.Union(db.FormerIDs).ToArray();
            this.Name ??= db.DisplayName;
        }
    }

    /// <summary>Get update keys based on the metadata.</summary>
    public IEnumerable<string> GetUpdateKeys()
    {
        if (this.NexusID.HasValue)
            yield return $"Nexus:{this.NexusID}";
        if (this.ChucklefishID.HasValue)
            yield return $"Chucklefish:{this.ChucklefishID}";
        if (this.GitHubRepo != null)
            yield return $"GitHub:{this.GitHubRepo}";
    }
}
