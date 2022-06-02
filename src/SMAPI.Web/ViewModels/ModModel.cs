using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

namespace StardewModdingAPI.Web.ViewModels;

/// <summary>Metadata about a mod.</summary>
public class ModModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod name.</summary>
    public string? Name { get; }

    /// <summary>The mod's alternative names, if any.</summary>
    public string AlternateNames { get; }

    /// <summary>The mod author's name.</summary>
    public string? Author { get; }

    /// <summary>The mod author's alternative names, if any.</summary>
    public string AlternateAuthors { get; }

    /// <summary>The GitHub repo, if any.</summary>
    public string? GitHubRepo { get; }

    /// <summary>The URL to the mod's source code, if any.</summary>
    public string? SourceUrl { get; }

    /// <summary>The compatibility status for the stable version of the game.</summary>
    public ModCompatibilityModel Compatibility { get; }

    /// <summary>The compatibility status for the beta version of the game.</summary>
    public ModCompatibilityModel? BetaCompatibility { get; }

    /// <summary>Links to the available mod pages.</summary>
    public ModLinkModel[] ModPages { get; }

    /// <summary>The human-readable warnings for players about this mod.</summary>
    public string[] Warnings { get; }

    /// <summary>The URL of the pull request which submits changes for an unofficial update to the author, if any.</summary>
    public string? PullRequestUrl { get; }

    /// <summary>Special notes intended for developers who maintain unofficial updates or submit pull requests.</summary>
    public string? DevNote { get; }

    /// <summary>A unique identifier for the mod that can be used in an anchor URL.</summary>
    public string? Slug { get; }

    /// <summary>The sites where the mod can be downloaded.</summary>
    public string[] ModPageSites => this.ModPages.Select(p => p.Text).ToArray();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">The mod name.</param>
    /// <param name="alternateNames">The mod's alternative names, if any.</param>
    /// <param name="author">The mod author's name.</param>
    /// <param name="alternateAuthors">The mod author's alternative names, if any.</param>
    /// <param name="gitHubRepo">The GitHub repo, if any.</param>
    /// <param name="sourceUrl">The URL to the mod's source code, if any.</param>
    /// <param name="compatibility">The compatibility status for the stable version of the game.</param>
    /// <param name="betaCompatibility">The compatibility status for the beta version of the game.</param>
    /// <param name="modPages">Links to the available mod pages.</param>
    /// <param name="warnings">The human-readable warnings for players about this mod.</param>
    /// <param name="pullRequestUrl">The URL of the pull request which submits changes for an unofficial update to the author, if any.</param>
    /// <param name="devNote">Special notes intended for developers who maintain unofficial updates or submit pull requests.</param>
    /// <param name="slug">A unique identifier for the mod that can be used in an anchor URL.</param>
    [JsonConstructor]
    public ModModel(string? name, string alternateNames, string author, string alternateAuthors, string gitHubRepo, string sourceUrl, ModCompatibilityModel compatibility, ModCompatibilityModel betaCompatibility, ModLinkModel[] modPages, string[] warnings, string pullRequestUrl, string devNote, string slug)
    {
        this.Name = name;
        this.AlternateNames = alternateNames;
        this.Author = author;
        this.AlternateAuthors = alternateAuthors;
        this.GitHubRepo = gitHubRepo;
        this.SourceUrl = sourceUrl;
        this.Compatibility = compatibility;
        this.BetaCompatibility = betaCompatibility;
        this.ModPages = modPages;
        this.Warnings = warnings;
        this.PullRequestUrl = pullRequestUrl;
        this.DevNote = devNote;
        this.Slug = slug;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="entry">The mod metadata.</param>
    public ModModel(WikiModEntry entry)
    {
        // basic info
        this.Name = entry.Name.FirstOrDefault();
        this.AlternateNames = string.Join(", ", entry.Name.Skip(1).ToArray());
        this.Author = entry.Author.FirstOrDefault();
        this.AlternateAuthors = string.Join(", ", entry.Author.Skip(1).ToArray());
        this.GitHubRepo = entry.GitHubRepo;
        this.SourceUrl = this.GetSourceUrl(entry);
        this.Compatibility = new ModCompatibilityModel(entry.Compatibility);
        this.BetaCompatibility = entry.BetaCompatibility != null ? new ModCompatibilityModel(entry.BetaCompatibility) : null;
        this.ModPages = this.GetModPageUrls(entry).ToArray();
        this.Warnings = entry.Warnings;
        this.PullRequestUrl = entry.PullRequestUrl;
        this.DevNote = entry.DevNote;
        this.Slug = entry.Anchor;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the web URL for the mod's source code repository, if any.</summary>
    /// <param name="entry">The mod metadata.</param>
    private string? GetSourceUrl(WikiModEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.GitHubRepo))
            return $"https://github.com/{entry.GitHubRepo}";
        if (!string.IsNullOrWhiteSpace(entry.CustomSourceUrl))
            return entry.CustomSourceUrl;
        return null;
    }

    /// <summary>Get the web URLs for the mod pages, if any.</summary>
    /// <param name="entry">The mod metadata.</param>
    private IEnumerable<ModLinkModel> GetModPageUrls(WikiModEntry entry)
    {
        bool anyFound = false;

        // normal mod pages
        if (entry.NexusID.HasValue)
        {
            anyFound = true;
            yield return new ModLinkModel($"https://www.nexusmods.com/stardewvalley/mods/{entry.NexusID}", "Nexus");
        }
        if (entry.ModDropID.HasValue)
        {
            anyFound = true;
            yield return new ModLinkModel($"https://www.moddrop.com/stardew-valley/mod/{entry.ModDropID}", "ModDrop");
        }
        if (!string.IsNullOrWhiteSpace(entry.CurseForgeKey))
        {
            anyFound = true;
            yield return new ModLinkModel($"https://www.curseforge.com/stardewvalley/mods/{entry.CurseForgeKey}", "CurseForge");
        }
        if (entry.ChucklefishID.HasValue)
        {
            anyFound = true;
            yield return new ModLinkModel($"https://community.playstarbound.com/resources/{entry.ChucklefishID}", "Chucklefish");
        }

        // fallback
        if (!anyFound && !string.IsNullOrWhiteSpace(entry.CustomUrl))
        {
            anyFound = true;
            yield return new ModLinkModel(entry.CustomUrl, "custom");
        }
        if (!anyFound && !string.IsNullOrWhiteSpace(entry.GitHubRepo))
            yield return new ModLinkModel($"https://github.com/{entry.GitHubRepo}/releases", "GitHub");
    }
}