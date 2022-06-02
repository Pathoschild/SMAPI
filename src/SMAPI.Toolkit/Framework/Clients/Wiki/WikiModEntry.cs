using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

/// <summary>A mod entry in the wiki list.</summary>
public class WikiModEntry
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod's unique ID. If the mod has alternate/old IDs, they're listed in latest to oldest order.</summary>
    public string[] ID { get; }

    /// <summary>The mod's display name. If the mod has multiple names, the first one is the most canonical name.</summary>
    public string[] Name { get; }

    /// <summary>The mod's author name. If the author has multiple names, the first one is the most canonical name.</summary>
    public string[] Author { get; }

    /// <summary>The mod ID on Nexus.</summary>
    public int? NexusID { get; }

    /// <summary>The mod ID in the Chucklefish mod repo.</summary>
    public int? ChucklefishID { get; }

    /// <summary>The mod ID in the CurseForge mod repo.</summary>
    public int? CurseForgeID { get; }

    /// <summary>The mod key in the CurseForge mod repo (used in mod page URLs).</summary>
    public string? CurseForgeKey { get; }

    /// <summary>The mod ID in the ModDrop mod repo.</summary>
    public int? ModDropID { get; }

    /// <summary>The GitHub repository in the form 'owner/repo'.</summary>
    public string? GitHubRepo { get; }

    /// <summary>The URL to a non-GitHub source repo.</summary>
    public string? CustomSourceUrl { get; }

    /// <summary>The custom mod page URL (if applicable).</summary>
    public string? CustomUrl { get; }

    /// <summary>The name of the mod which loads this content pack, if applicable.</summary>
    public string? ContentPackFor { get; }

    /// <summary>The mod's compatibility with the latest stable version of the game.</summary>
    public WikiCompatibilityInfo Compatibility { get; }

    /// <summary>The mod's compatibility with the latest beta version of the game (if any).</summary>
    public WikiCompatibilityInfo? BetaCompatibility { get; }

    /// <summary>Whether a Stardew Valley or SMAPI beta which affects mod compatibility is in progress. If this is true, <see cref="BetaCompatibility"/> should be used for beta versions of SMAPI instead of <see cref="Compatibility"/>.</summary>
#if NET5_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(WikiModEntry.BetaCompatibility))]
#endif
    public bool HasBetaInfo => this.BetaCompatibility != null;

    /// <summary>The human-readable warnings for players about this mod.</summary>
    public string[] Warnings { get; }

    /// <summary>The URL of the pull request which submits changes for an unofficial update to the author, if any.</summary>
    public string? PullRequestUrl { get; }

    /// <summary>Special notes intended for developers who maintain unofficial updates or submit pull requests.</summary>
    public string? DevNote { get; }

    /// <summary>The data overrides to apply to the mod's manifest or remote mod page data, if any.</summary>
    public WikiDataOverrideEntry? Overrides { get; }

    /// <summary>The link anchor for the mod entry in the wiki compatibility list.</summary>
    public string? Anchor { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="id">The mod's unique ID. If the mod has alternate/old IDs, they're listed in latest to oldest order.</param>
    /// <param name="name">The mod's display name. If the mod has multiple names, the first one is the most canonical name.</param>
    /// <param name="author">The mod's author name. If the author has multiple names, the first one is the most canonical name.</param>
    /// <param name="nexusId">The mod ID on Nexus.</param>
    /// <param name="chucklefishId">The mod ID in the Chucklefish mod repo.</param>
    /// <param name="curseForgeId">The mod ID in the CurseForge mod repo.</param>
    /// <param name="curseForgeKey">The mod ID in the CurseForge mod repo.</param>
    /// <param name="modDropId">The mod ID in the ModDrop mod repo.</param>
    /// <param name="githubRepo">The GitHub repository in the form 'owner/repo'.</param>
    /// <param name="customSourceUrl">The URL to a non-GitHub source repo.</param>
    /// <param name="customUrl">The custom mod page URL (if applicable).</param>
    /// <param name="contentPackFor">The name of the mod which loads this content pack, if applicable.</param>
    /// <param name="compatibility">The mod's compatibility with the latest stable version of the game.</param>
    /// <param name="betaCompatibility">The mod's compatibility with the latest beta version of the game (if any).</param>
    /// <param name="warnings">The human-readable warnings for players about this mod.</param>
    /// <param name="pullRequestUrl">The URL of the pull request which submits changes for an unofficial update to the author, if any.</param>
    /// <param name="devNote">Special notes intended for developers who maintain unofficial updates or submit pull requests.</param>
    /// <param name="overrides">The data overrides to apply to the mod's manifest or remote mod page data, if any.</param>
    /// <param name="anchor">The link anchor for the mod entry in the wiki compatibility list.</param>
    public WikiModEntry(string[] id, string[] name, string[] author, int? nexusId, int? chucklefishId, int? curseForgeId, string? curseForgeKey, int? modDropId, string? githubRepo, string? customSourceUrl, string? customUrl, string? contentPackFor, WikiCompatibilityInfo compatibility, WikiCompatibilityInfo? betaCompatibility, string[] warnings, string? pullRequestUrl, string? devNote, WikiDataOverrideEntry? overrides, string? anchor)
    {
        this.ID = id;
        this.Name = name;
        this.Author = author;
        this.NexusID = nexusId;
        this.ChucklefishID = chucklefishId;
        this.CurseForgeID = curseForgeId;
        this.CurseForgeKey = curseForgeKey;
        this.ModDropID = modDropId;
        this.GitHubRepo = githubRepo;
        this.CustomSourceUrl = customSourceUrl;
        this.CustomUrl = customUrl;
        this.ContentPackFor = contentPackFor;
        this.Compatibility = compatibility;
        this.BetaCompatibility = betaCompatibility;
        this.Warnings = warnings;
        this.PullRequestUrl = pullRequestUrl;
        this.DevNote = devNote;
        this.Overrides = overrides;
        this.Anchor = anchor;
    }
}
