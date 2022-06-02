using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Web.Framework.Clients.GitHub;
using StardewModdingAPI.Web.Framework.ConfigModels;
using StardewModdingAPI.Web.ViewModels;

namespace StardewModdingAPI.Web.Controllers;

/// <summary>Provides an info/download page about SMAPI.</summary>
[Route("")]
internal class IndexController : Controller
{
    /*********
    ** Fields
    *********/
    /// <summary>The site config settings.</summary>
    private readonly SiteConfig SiteConfig;

    /// <summary>The cache in which to store release data.</summary>
    private readonly IMemoryCache Cache;

    /// <summary>The GitHub API client.</summary>
    private readonly IGitHubClient GitHub;

    /// <summary>The cache time for release info.</summary>
    private readonly TimeSpan CacheTime = TimeSpan.FromMinutes(10);

    /// <summary>The GitHub repository name to check for update.</summary>
    private readonly string RepositoryName = "Pathoschild/SMAPI";


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="cache">The cache in which to store release data.</param>
    /// <param name="github">The GitHub API client.</param>
    /// <param name="siteConfig">The context config settings.</param>
    public IndexController(IMemoryCache cache, IGitHubClient github, IOptions<SiteConfig> siteConfig)
    {
        this.Cache = cache;
        this.GitHub = github;
        this.SiteConfig = siteConfig.Value;
    }

    /// <summary>Display the index page.</summary>
    [HttpGet]
    public async Task<ViewResult> Index()
    {
        // choose versions
        ReleaseVersion[] versions = await this.GetReleaseVersionsAsync();
        ReleaseVersion? stableVersion = versions.LastOrDefault(version => !version.IsForDevs);
        ReleaseVersion? stableVersionForDevs = versions.LastOrDefault(version => version.IsForDevs);

        // render view
        IndexVersionModel stableVersionModel = stableVersion != null
            ? new IndexVersionModel(stableVersion.Version.ToString(), stableVersion.Release.Body, stableVersion.Asset.DownloadUrl, stableVersionForDevs?.Asset.DownloadUrl)
            : new IndexVersionModel("unknown", "", "https://github.com/Pathoschild/SMAPI/releases", null); // just in case something goes wrong

        // render view
        var model = new IndexModel(stableVersionModel, this.SiteConfig.OtherBlurb, this.SiteConfig.SupporterList);
        return this.View(model);
    }

    /// <summary>Display the index page.</summary>
    [HttpGet("/privacy")]
    public ViewResult Privacy()
    {
        return this.View();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a sorted, parsed list of SMAPI downloads for the latest releases.</summary>
    private async Task<ReleaseVersion[]> GetReleaseVersionsAsync()
    {
        return await this.Cache.GetOrCreateAsync("available-versions", async entry =>
        {
            entry.AbsoluteExpiration = DateTimeOffset.UtcNow.Add(this.CacheTime);

            // get latest stable release
            GitRelease? release = await this.GitHub.GetLatestReleaseAsync(this.RepositoryName, includePrerelease: false);

            // strip 'noinclude' blocks from release description
            if (release != null)
            {
                HtmlDocument doc = new();
                doc.LoadHtml(release.Body);
                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//*[@class='noinclude']")?.ToArray() ?? Array.Empty<HtmlNode>())
                    node.Remove();
                release.Body = doc.DocumentNode.InnerHtml.Trim();
            }

            // get versions
            return this
                .ParseReleaseVersions(release)
                .OrderBy(p => p.Version)
                .ToArray();
        });
    }

    /// <summary>Get a parsed list of SMAPI downloads for a release.</summary>
    /// <param name="release">The GitHub release.</param>
    private IEnumerable<ReleaseVersion> ParseReleaseVersions(GitRelease? release)
    {
        if (release?.Assets == null)
            yield break;

        foreach (GitAsset asset in release.Assets)
        {
            if (asset.FileName.StartsWith("Z_"))
                continue;

            Match match = Regex.Match(asset.FileName, @"SMAPI-(?<version>[\d\.]+(?:-.+)?)-installer(?<forDevs>-for-developers)?.zip");
            if (!match.Success || !SemanticVersion.TryParse(match.Groups["version"].Value, out ISemanticVersion? version))
                continue;
            bool isForDevs = match.Groups["forDevs"].Success;

            yield return new ReleaseVersion(release, asset, version, isForDevs);
        }
    }

    /// <summary>A parsed release download.</summary>
    private class ReleaseVersion
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying GitHub release.</summary>
        public GitRelease Release { get; }

        /// <summary>The underlying download asset.</summary>
        public GitAsset Asset { get; }

        /// <summary>The SMAPI version.</summary>
        public ISemanticVersion Version { get; }

        /// <summary>Whether this is a 'for developers' download.</summary>
        public bool IsForDevs { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="release">The underlying GitHub release.</param>
        /// <param name="asset">The underlying download asset.</param>
        /// <param name="version">The SMAPI version.</param>
        /// <param name="isForDevs">Whether this is a 'for developers' download.</param>
        public ReleaseVersion(GitRelease release, GitAsset asset, ISemanticVersion version, bool isForDevs)
        {
            this.Release = release;
            this.Asset = asset;
            this.Version = version;
            this.IsForDevs = isForDevs;
        }
    }
}