using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace StardewModdingAPI.Web.Framework.Caching.ModDataset;

/// <inheritdoc cref="IModDatasetRepository" />
internal class ModDatasetRepository : IModDatasetRepository
{
    /*********
    ** Fields
    *********/
    /// <summary>The URL of the zip file to download which contains the Stardew mod dataset (e.g. a GitHub branch download URL).</summary>
    private readonly string DownloadZipUrl;

    /// <summary>The full path to the folder into which to download the mod dataset. This should be a new folder, since anything inside it may be deleted.</summary>
    private readonly string LocalRootPath;

    /// <summary>The HTTP client with which to fetch the mod dataset archive.</summary>
    private readonly HttpClient HttpClient;

    /// <summary>The name of the file within the <see cref="LocalRootPath"/> in which to cache data about the last fetched dataset.</summary>
    private const string CacheFileName = ".cache.json";

    /// <summary>The path to the actual 'dataset' folder.</summary>
    private string? DatasetPath;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="downloadZipUrl"><inheritdoc cref="DownloadZipUrl" path="/summary" /></param>
    /// <param name="localPath"><inheritdoc cref="LocalRootPath" path="/summary" /></param>
    /// <param name="userAgent">The user agent to use when fetching the archive URL.</param>
    public ModDatasetRepository(string downloadZipUrl, string localPath, string userAgent)
    {
        this.DownloadZipUrl = downloadZipUrl;
        this.LocalRootPath = Path.GetFullPath(this.ExpandEnvironmentVariables(localPath));

        this.HttpClient = new HttpClient();
        this.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    }

    /// <inheritdoc />
    public async Task<DatasetDownload> UpdateAsync(Action<string>? log = null)
    {
        // check for new archive
        log?.Invoke("  Checking for newer dataset...");
        DatasetDownload? cached = await this.GetCacheInfoAsync();
        using HttpRequestMessage request = this.BuildDownloadRequest(cached?.ETag);
        using HttpResponseMessage response = await this.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        // download new archive
        if (cached is null || response.StatusCode is not HttpStatusCode.NotModified)
        {
            // read response data
            if (response.StatusCode is not HttpStatusCode.NotModified)
                response.EnsureSuccessStatusCode();
            string? newEtag = response.Headers.ETag?.ToString();

            // download & unzip dataset
            log?.Invoke("  Downloading dataset...");
            string newFolderName = Guid.NewGuid().ToString("N");
            string newRootPath = Path.Combine(this.LocalRootPath, newFolderName);
            Directory.CreateDirectory(newRootPath);
            await using (Stream downloadStream = await response.Content.ReadAsStreamAsync())
            await using (ZipArchive archive = new(downloadStream, ZipArchiveMode.Read))
                await archive.ExtractToDirectoryAsync(newRootPath);

            // locate 'dataset' folder
            log?.Invoke("  Locating 'dataset' folder...");
            string? newDatasetPath = null;
            foreach (DirectoryInfo entry in new DirectoryInfo(newRootPath).EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                if (entry.Name == "dataset")
                {
                    newDatasetPath = Path.GetRelativePath(newRootPath, entry.FullName);
                    break;
                }
            }
            if (newDatasetPath is null)
                throw new InvalidOperationException("No 'dataset' folder found in the downloaded archive.");

            // save cache info
            cached = new DatasetDownload(newFolderName, newDatasetPath, newEtag);
            await this.SaveCacheInfoAsync(cached);
            log?.Invoke($"  Dataset saved to {newRootPath} with {(newEtag != null ? $"ETag header {newEtag}" : "no ETag header")}.");
        }

        // track dataset path
        this.DatasetPath = Path.Combine(this.LocalRootPath, cached.FolderName, cached.RelativePathToDataset);

        // clear previous datasets if possible
        foreach (DirectoryInfo directory in new DirectoryInfo(this.LocalRootPath).EnumerateDirectories())
        {
            if (directory.Name == cached.FolderName)
                continue;

            log?.Invoke($"  Deleting previous dataset download at {directory.FullName}...");
            try
            {
                directory.Delete(recursive: true);
            }
            catch (Exception ex)
            {
                log?.Invoke($"    Deletion failed: {ex}");
            }
        }

        return cached;
    }

    /// <inheritdoc />
    public string GetFilePath(string relativePath)
    {
        return this.DatasetPath is not null
            ? Path.Combine(this.DatasetPath, relativePath)
            : throw new InvalidOperationException($"Must call '{nameof(this.UpdateAsync)}' before '{nameof(this.GetFilePath)}'.");
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Build the HTTP request to download the archive from the server if it changed.</summary>
    /// <param name="etag">The ETag value for the last successful dataset download, if any.</param>
    private HttpRequestMessage BuildDownloadRequest(string? etag)
    {
        HttpRequestMessage request = new(HttpMethod.Get, this.DownloadZipUrl);
        try
        {
            if (etag != null)
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
        }
        catch
        {
            request.Dispose();
            throw;
        }

        return request;
    }

    /// <summary>Get the cached info about the last dataset download, if it was previously downloaded.</summary>
    private async Task<DatasetDownload?> GetCacheInfoAsync()
    {
        string path = Path.Combine(this.LocalRootPath, CacheFileName);
        if (!File.Exists(path))
            return null;

        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DatasetDownload>(stream);
    }

    /// <summary>Save the cache info about a dataset download.</summary>
    /// <param name="cacheInfo">The cache info to save.</param>
    private async Task SaveCacheInfoAsync(DatasetDownload cacheInfo)
    {
        string path = Path.Combine(this.LocalRootPath, CacheFileName);

        await using FileStream stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, cacheInfo);
    }

    /// <summary>Expand environment variables in a path.</summary>
    /// <param name="path">The path to parse.</param>
    /// <returns>Returns the path with any environment variables replaced.</returns>
    private string ExpandEnvironmentVariables(string path)
    {
        if (OperatingSystem.IsLinux())
            path = path.Replace("%TEMP%", Path.GetTempPath(), StringComparison.OrdinalIgnoreCase);

        return Environment.ExpandEnvironmentVariables(path);
    }
}
