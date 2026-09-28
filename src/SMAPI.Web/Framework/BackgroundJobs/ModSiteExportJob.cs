using System;
using System.Threading.Tasks;
using Hangfire.Console;
using Hangfire.Server;
using Humanizer;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Web.Framework.Caching;
using StardewModdingAPI.Web.Framework.ConfigModels;

namespace StardewModdingAPI.Web.Framework.BackgroundJobs;

/// <summary>The base implementation for a background job which updates the cached mod export for a mod site.</summary>
/// <typeparam name="TCacheRepository">The export cache repository type.</typeparam>
/// <typeparam name="TExportApiClient">The export API client type.</typeparam>
internal abstract class ModSiteExportJob<TCacheRepository, TExportApiClient>
    where TCacheRepository : IExportCacheRepository
{
    /*********
    ** Fields
    *********/
    /// <summary>The config settings for mod update checks.</summary>
    private readonly IOptions<ModUpdateCheckConfig> UpdateCheckConfig;

    /// <summary>The cache in which to store the mod export.</summary>
    protected readonly TCacheRepository Cache;

    /// <summary>The HTTP client for fetching the mod export from the site's export API.</summary>
    protected readonly TExportApiClient Client;


    /*********
    ** Accessors
    *********/
    /// <summary>Whether the job should be scheduled.</summary>
    public abstract bool IsEnabled { get; }

    /// <summary>The number of minutes a site export should be considered valid based on its last-updated date before it's ignored.</summary>
    private int ExportStaleAge => this.UpdateCheckConfig.Value.SuccessCacheMinutes + 10;


    /*********
    ** Public methods
    *********/
    /// <inheritdoc cref="IBackgroundJob.RunAsync" />
    public async Task RunAsync(PerformContext? context)
    {
        TCacheRepository cache = this.Cache;

        // log initial state
        context.WriteLine(cache.IsLoaded
            ? $"The previous export is cached with data from {this.FormatDateModified(cache.CacheHeaders.LastModified)}."
            : "No previous export is cached."
        );

        // fetch cache headers
        context.WriteLine("Fetching cache headers...");
        ApiCacheHeaders serverCacheHeaders = await this.FetchCacheHeadersAsync();
        DateTimeOffset serverModified = serverCacheHeaders.LastModified;
        string? serverEntityTag = serverCacheHeaders.EntityTag;

        // update data
        {
            // skip if no update needed
            if (cache.IsStale(serverModified, this.ExportStaleAge))
                context.WriteLine($"Skipped data fetch: server was last modified {this.FormatDateModified(serverModified)}, which exceeds the {this.ExportStaleAge}-minute-stale limit.");
            else if (cache.IsLoaded && cache.CacheHeaders.LastModified >= serverModified)
                context.WriteLine($"Skipped data fetch: server was last modified {this.FormatDateModified(serverModified)}, which {(serverModified == cache.CacheHeaders.LastModified ? "matches" : "is older than")} our cached data.");

            // update cache headers if data unchanged
            else if (cache.IsLoaded && cache.CacheHeaders.EntityTag != null && cache.CacheHeaders.EntityTag == serverEntityTag)
            {
                context.WriteLine($"Skipped data fetch: server provided entity tag '{serverEntityTag}', which already matches the data we have.");
                cache.SetCacheHeaders(serverCacheHeaders);
            }

            // else update data
            else
            {
                context.WriteLine("Fetching data...");
                await this.FetchDataAsync();
            }
        }

        // clear if stale
        if (cache.IsStale(this.ExportStaleAge))
        {
            context.WriteLine("The cached data is stale, clearing cache...");
            cache.Clear();
        }

        // log final result
        context.WriteLine(cache.IsLoaded
            ? $"Done! The export is currently cached with data from {this.FormatDateModified(cache.CacheHeaders.LastModified)}."
            : "Done! The export cache is currently disabled."
        );
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="cache"><inheritdoc cref="Cache" path="/summary"/></param>
    /// <param name="client"><inheritdoc cref="Client" path="/summary"/></param>
    /// <param name="updateCheckConfig"><inheritdoc cref="UpdateCheckConfig" path="/summary"/></param>
    protected ModSiteExportJob(TCacheRepository cache, TExportApiClient client, IOptions<ModUpdateCheckConfig> updateCheckConfig)
    {
        this.Cache = cache;
        this.Client = client;
        this.UpdateCheckConfig = updateCheckConfig;
    }

    /// <summary>Fetch the HTTP cache headers set by the export API.</summary>
    protected abstract Task<ApiCacheHeaders> FetchCacheHeadersAsync();

    /// <summary>Fetch the latest export from the export API, and save it to the <see cref="Cache"/>.</summary>
    protected abstract Task FetchDataAsync();

    /// <summary>Format a 'date modified' value for the task logs.</summary>
    /// <param name="date">The date to log.</param>
    private string FormatDateModified(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return "<null>";

        string ageLabel = (DateTimeOffset.UtcNow - date.Value).Humanize(precision: 2, minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Hour);

        return $"{date.Value:O} (age: {ageLabel})";
    }
}
