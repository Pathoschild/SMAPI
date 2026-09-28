using System;
using System.Threading.Tasks;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub;

/// <summary>An HTTP client for fetching metadata from GitHub.</summary>
internal interface IGitHubClient : IModSiteClient, IDisposable
{
    /*********
    ** Methods
    *********/
    /// <summary>Get basic metadata for a GitHub repository, if available.</summary>
    /// <param name="repo">The repository key (like <c>Pathoschild/SMAPI</c>).</param>
    /// <returns>Returns the repository info if it exists, else <c>null</c>.</returns>
    Task<GitRepo?> GetRepositoryAsync(string repo);

    /// <summary>Get the latest release for a GitHub repository.</summary>
    /// <param name="repo">The repository key (like <c>Pathoschild/SMAPI</c>).</param>
    /// <param name="includePrerelease">Whether to return a prerelease version if it's latest.</param>
    /// <returns>Returns the release if found, else <c>null</c>.</returns>
    Task<GitRelease?> GetLatestReleaseAsync(string repo, bool includePrerelease = false);

    /// <summary>Get a file from a GitHub repository, if available.</summary>
    /// <param name="repo">The repository key (like <c>Pathoschild/SMAPI</c>).</param>
    /// <param name="path">The file path relative to the repository root.</param>
    /// <param name="gitRef">The branch, tag, or commit from which to get the file, or <c>null</c> for the repository's default branch.</param>
    /// <param name="eTag">The <see cref="GitFile.ETag"/> from a previous request, if the file should only be fetched if it changed since then.</param>
    /// <returns>Returns the file if it exists and is accessible, else <c>null</c>.</returns>
    Task<GitFile?> GetFileAsync(string repo, string path, string? gitRef = null, string? eTag = null);
}
