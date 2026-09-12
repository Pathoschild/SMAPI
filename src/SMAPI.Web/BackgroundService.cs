using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Humanizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.CompatibilityRepo;
using StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport;
using StardewModdingAPI.Toolkit.Framework.Clients.NexusExport;
using StardewModdingAPI.Web.Framework.Caching;
using StardewModdingAPI.Web.Framework.Caching.CompatibilityRepo;
using StardewModdingAPI.Web.Framework.Caching.CurseForgeExport;
using StardewModdingAPI.Web.Framework.Caching.ModDataset;
using StardewModdingAPI.Web.Framework.Caching.ModDropExport;
using StardewModdingAPI.Web.Framework.Caching.Mods;
using StardewModdingAPI.Web.Framework.Caching.NexusExport;
using StardewModdingAPI.Web.Framework.Clients.CurseForge;
using StardewModdingAPI.Web.Framework.Clients.ModDrop;
using StardewModdingAPI.Web.Framework.Clients.Nexus;
using StardewModdingAPI.Web.Framework.ConfigModels;

namespace StardewModdingAPI.Web;

/// <summary>A hosted service which runs background data updates.</summary>
/// <remarks>Task methods need to be static, since otherwise Hangfire will try to serialize the entire instance.</remarks>
internal class BackgroundService : IHostedService, IDisposable
{
    /*********
    ** Fields
    *********/
    /// <summary>The background task server.</summary>
    private static BackgroundJobServer? JobServer;

    /// <summary>The cache in which to store compatibility list data.</summary>
    private static ICompatibilityCacheRepository? CompatibilityCache;

    /// <summary>The cache in which to store mod data.</summary>
    private static IModCacheRepository? ModCache;

    /// <summary>The HTTP client for fetching the mod export from the CurseForge export API.</summary>
    private static ICurseForgeExportApiClient? CurseForgeExportApiClient;

    /// <summary>The cache in which to store the mod data from the CurseForge export API.</summary>
    private static ICurseForgeExportCacheRepository? CurseForgeExportCache;

    /// <summary>The HTTP client for fetching the mod export from the ModDrop export API.</summary>
    private static IModDropExportApiClient? ModDropExportApiClient;

    /// <summary>The cache in which to store the mod data from the ModDrop export API.</summary>
    private static IModDropExportCacheRepository? ModDropExportCache;

    /// <summary>The cache in which to store mod data from the Nexus export API.</summary>
    private static INexusExportCacheRepository? NexusExportCache;

    /// <summary>The HTTP client for fetching the mod export from the Nexus Mods export API.</summary>
    private static INexusExportApiClient? NexusExportApiClient;

    /// <summary>The config settings for mod update checks.</summary>
    private static IOptions<ModUpdateCheckConfig>? UpdateCheckConfig;

    /// <summary>The mod dataset repository.</summary>
    private static IModDatasetRepository? ModDatasetRepo;

    /// <summary>The web root path for writing static data files.</summary>
    private static string? WebRootPath;

    /// <summary>Whether the service has been started.</summary>
    [MemberNotNullWhen(true,
        nameof(BackgroundService.JobServer),
        nameof(BackgroundService.ModCache),
        nameof(BackgroundService.CompatibilityCache),
        nameof(BackgroundService.CurseForgeExportApiClient),
        nameof(BackgroundService.CurseForgeExportCache),
        nameof(BackgroundService.ModDropExportApiClient),
        nameof(BackgroundService.ModDropExportCache),
        nameof(BackgroundService.NexusExportApiClient),
        nameof(BackgroundService.NexusExportCache),
        nameof(BackgroundService.ModDatasetRepo),
        nameof(BackgroundService.UpdateCheckConfig),
        nameof(BackgroundService.WebRootPath)
    )]
    private static bool IsStarted { get; set; }

    /// <summary>The number of minutes a site export should be considered valid based on its last-updated date before it's ignored.</summary>
    private static int ExportStaleAge => (BackgroundService.UpdateCheckConfig?.Value.SuccessCacheMinutes ?? 0) + 10;


    /*********
    ** Public methods
    *********/
    /****
    ** Hosted service
    ****/
    /// <summary>Construct an instance.</summary>
    /// <param name="compatibilityCache"><inheritdoc cref="CompatibilityCache" path="/summary"/></param>
    /// <param name="modCache"><inheritdoc cref="ModCache" path="/summary"/></param>
    /// <param name="curseForgeExportCache"><inheritdoc cref="CurseForgeExportCache" path="/summary"/></param>
    /// <param name="curseForgeExportApiClient"><inheritdoc cref="CurseForgeExportApiClient" path="/summary"/></param>
    /// <param name="modDropExportCache"><inheritdoc cref="ModDropExportCache" path="/summary"/></param>
    /// <param name="modDropExportApiClient"><inheritdoc cref="ModDropExportApiClient" path="/summary"/></param>
    /// <param name="nexusExportCache"><inheritdoc cref="NexusExportCache" path="/summary"/></param>
    /// <param name="nexusExportApiClient"><inheritdoc cref="NexusExportApiClient" path="/summary"/></param>
    /// <param name="modDatasetRepo"><inheritdoc cref="ModDatasetRepo" path="/summary"/></param>
    /// <param name="hangfireStorage">The Hangfire storage implementation.</param>
    /// <param name="updateCheckConfig"><inheritdoc cref="UpdateCheckConfig" path="/summary"/></param>
    /// <param name="hostEnvironment">The web host environment metadata.</param>
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "The Hangfire reference forces it to initialize first, since it's needed by the background service.")]
    public BackgroundService(
        ICompatibilityCacheRepository compatibilityCache,
        IModCacheRepository modCache,
        ICurseForgeExportCacheRepository curseForgeExportCache,
        ICurseForgeExportApiClient curseForgeExportApiClient,
        IModDropExportCacheRepository modDropExportCache,
        IModDropExportApiClient modDropExportApiClient,
        INexusExportCacheRepository nexusExportCache,
        INexusExportApiClient nexusExportApiClient,
        IModDatasetRepository modDatasetRepo,
        JobStorage hangfireStorage,
        IOptions<ModUpdateCheckConfig> updateCheckConfig,
        IWebHostEnvironment hostEnvironment
    )
    {
        BackgroundService.CompatibilityCache = compatibilityCache;
        BackgroundService.ModCache = modCache;
        BackgroundService.CurseForgeExportApiClient = curseForgeExportApiClient;
        BackgroundService.CurseForgeExportCache = curseForgeExportCache;
        BackgroundService.ModDropExportApiClient = modDropExportApiClient;
        BackgroundService.ModDropExportCache = modDropExportCache;
        BackgroundService.NexusExportCache = nexusExportCache;
        BackgroundService.NexusExportApiClient = nexusExportApiClient;
        BackgroundService.ModDatasetRepo = modDatasetRepo;
        BackgroundService.UpdateCheckConfig = updateCheckConfig;
        BackgroundService.WebRootPath = hostEnvironment.WebRootPath;

        _ = hangfireStorage; // parameter is only received to initialize it before the background service
    }

    /// <summary>Start the service.</summary>
    /// <param name="cancellationToken">Tracks whether the start process has been aborted.</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.TryInit();

        bool enableCurseForgeExport = BackgroundService.CurseForgeExportApiClient is not DisabledCurseForgeExportApiClient;
        bool enableModDropExport = BackgroundService.ModDropExportApiClient is not DisabledModDropExportApiClient;
        bool enableNexusExport = BackgroundService.NexusExportApiClient is not DisabledNexusExportApiClient;

        // set startup tasks
        BackgroundJob.Enqueue(() => BackgroundService.UpdateCompatibilityListAsync(null));
        if (enableCurseForgeExport)
            BackgroundJob.Enqueue(() => BackgroundService.UpdateCurseForgeExportAsync(null));
        if (enableModDropExport)
            BackgroundJob.Enqueue(() => BackgroundService.UpdateModDropExportAsync(null));
        if (enableNexusExport)
            BackgroundJob.Enqueue(() => BackgroundService.UpdateNexusExportAsync(null));
        BackgroundJob.Enqueue(() => BackgroundService.RemoveStaleModsAsync());
        BackgroundJob.Enqueue(() => BackgroundService.UpdateModDatasetAsync(null));

        // set recurring tasks
        RecurringJob.AddOrUpdate("update compatibility list", () => BackgroundService.UpdateCompatibilityListAsync(null), "*/10 * * * *");      // every 10 minutes
        if (enableCurseForgeExport)
            RecurringJob.AddOrUpdate("update CurseForge export", () => BackgroundService.UpdateCurseForgeExportAsync(null), "*/10 * * * *");
        if (enableModDropExport)
            RecurringJob.AddOrUpdate("update ModDrop export", () => BackgroundService.UpdateModDropExportAsync(null), "*/10 * * * *");
        if (enableNexusExport)
            RecurringJob.AddOrUpdate("update Nexus export", () => BackgroundService.UpdateNexusExportAsync(null), "*/10 * * * *");
        RecurringJob.AddOrUpdate("remove stale mods", () => BackgroundService.RemoveStaleModsAsync(), "2/10 * * * *"); // offset by 2 minutes so it runs after updates (e.g. 00:02, 00:12, etc)
        RecurringJob.AddOrUpdate("update mod dataset", () => BackgroundService.UpdateModDatasetAsync(null), "0 * * * *"); // hourly

        BackgroundService.IsStarted = true;

        return Task.CompletedTask;
    }

    /// <summary>Triggered when the application host is performing a graceful shutdown.</summary>
    /// <param name="cancellationToken">Tracks whether the shutdown process should no longer be graceful.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        BackgroundService.IsStarted = false;

        if (BackgroundService.JobServer != null)
            await BackgroundService.JobServer.WaitForShutdownAsync(cancellationToken);
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        BackgroundService.IsStarted = false;

        BackgroundService.JobServer?.Dispose();
    }

    /****
    ** Tasks
    ****/
    /// <summary>Update the cached compatibility list data.</summary>
    /// <param name="context">Information about the context in which the job is performed. This is injected automatically by Hangfire.</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 60, 120])]
    public static async Task UpdateCompatibilityListAsync(PerformContext? context)
    {
        if (!BackgroundService.IsStarted)
            throw new InvalidOperationException($"Must call {nameof(BackgroundService.StartAsync)} before scheduling tasks.");

        context.WriteLine("Fetching data from compatibility repo...");
        ModCompatibilityEntry[] compatList = await new ModToolkit().GetCompatibilityListAsync();

        context.WriteLine("Saving data...");
        BackgroundService.CompatibilityCache.SaveData(compatList);

        context.WriteLine("Done!");
    }

    /// <summary>Update the cached CurseForge mod export.</summary>
    /// <param name="context">Information about the context in which the job is performed. This is injected automatically by Hangfire.</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 60, 120])]
    public static async Task UpdateCurseForgeExportAsync(PerformContext? context)
    {
        await UpdateExportAsync(
            context,
            BackgroundService.CurseForgeExportCache!,
            BackgroundService.CurseForgeExportApiClient!,
            fetchCacheHeadersAsync: client => client.FetchCacheHeadersAsync(),
            fetchDataAsync: async (cache, client) => cache.SetData(await client.FetchExportAsync())
        );
    }

    /// <summary>Update the cached ModDrop mod export.</summary>
    /// <param name="context">Information about the context in which the job is performed. This is injected automatically by Hangfire.</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 60, 120])]
    public static async Task UpdateModDropExportAsync(PerformContext? context)
    {
        await UpdateExportAsync(
            context,
            BackgroundService.ModDropExportCache!,
            BackgroundService.ModDropExportApiClient!,
            fetchCacheHeadersAsync: client => client.FetchCacheHeadersAsync(),
            fetchDataAsync: async (cache, client) => cache.SetData(await client.FetchExportAsync())
        );
    }

    /// <summary>Update the cached Nexus mod export.</summary>
    /// <param name="context">Information about the context in which the job is performed. This is injected automatically by Hangfire.</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 60, 120])]
    public static async Task UpdateNexusExportAsync(PerformContext? context)
    {
        await UpdateExportAsync(
            context,
            BackgroundService.NexusExportCache!,
            BackgroundService.NexusExportApiClient!,
            fetchCacheHeadersAsync: client => client.FetchCacheHeadersAsync(),
            fetchDataAsync: async (cache, client) => cache.SetData(await client.FetchExportAsync())
        );
    }

    /// <summary>Clone or pull the Stardew mod dataset repo, then copy the latest data files into the web root for use by frontend scripts.</summary>
    /// <param name="context">Information about the context in which the job is performed. This is injected automatically by Hangfire.</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 60, 120])]
    public static async Task UpdateModDatasetAsync(PerformContext? context)
    {
        if (!BackgroundService.IsStarted)
            throw new InvalidOperationException($"Must call {nameof(BackgroundService.StartAsync)} before scheduling tasks.");

        // update repo
        context.WriteLine("Updating mod dataset repo...");
        DatasetDownload result = await BackgroundService.ModDatasetRepo.UpdateAsync(context.WriteLine);

        // copy files
        context.WriteLine("Copying data files for script use...");
        string copyToPath = Path.Combine(BackgroundService.WebRootPath, "Content", "data", "mods-by-type.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(copyToPath)!);
        File.Copy(BackgroundService.ModDatasetRepo.GetFilePath("stats/mods by type.jsonl"), copyToPath, overwrite: true);
        File.Copy(BackgroundService.ModDatasetRepo.GetFilePath("stats/Content Patcher packs by format version.jsonl"), Path.Combine(BackgroundService.WebRootPath, "Content", "data", "content-packs-by-format.jsonl"), overwrite: true);
        File.Copy(BackgroundService.ModDatasetRepo.GetFilePath("reference-data/SMAPI costs.jsonl"), Path.Combine(BackgroundService.WebRootPath, "Content", "data", "smapi-costs.jsonl"), overwrite: true);
        File.Copy(BackgroundService.ModDatasetRepo.GetFilePath("reference-data/SMAPI DNS queries.json"), Path.Combine(BackgroundService.WebRootPath, "Content", "data", "smapi-dns-queries.json"), overwrite: true);

        Program.ModDatasetCacheBustValue = result.ETag ?? $"epoch-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        context.WriteLine("Done!");
    }

    /// <summary>Remove mods which haven't been requested in over 48 hours.</summary>
    public static Task RemoveStaleModsAsync()
    {
        if (!BackgroundService.IsStarted)
            throw new InvalidOperationException($"Must call {nameof(BackgroundService.StartAsync)} before scheduling tasks.");

        // remove mods in mod cache
        BackgroundService.ModCache.RemoveStaleMods(TimeSpan.FromHours(48));

        return Task.CompletedTask;
    }


    /*********
    ** Private method
    *********/
    /// <summary>Initialize the background service if it's not already initialized.</summary>
    /// <exception cref="InvalidOperationException">The background service is already initialized.</exception>
    private void TryInit()
    {
        if (BackgroundService.JobServer != null)
            throw new InvalidOperationException("The scheduler service is already started.");

        BackgroundService.JobServer = new BackgroundJobServer();
    }

    /// <summary>Update the cached mods export for a site.</summary>
    /// <typeparam name="TCacheRepository">The export cache repository type.</typeparam>
    /// <typeparam name="TExportApiClient">The export API client.</typeparam>
    /// <param name="context">Information about the context in which the job is performed. This is injected automatically by Hangfire.</param>
    /// <param name="cache">The export cache to update.</param>
    /// <param name="client">The export API with which to fetch data from the remote API.</param>
    /// <param name="fetchCacheHeadersAsync">Fetch the HTTP cache headers set by the remote API.</param>
    /// <param name="fetchDataAsync">Fetch the latest export file from the Nexus Mods export API.</param>
    /// <exception cref="InvalidOperationException">The <see cref="StartAsync"/> method wasn't called before running this task.</exception>
    private static async Task UpdateExportAsync<TCacheRepository, TExportApiClient>(PerformContext? context, TCacheRepository cache, TExportApiClient client, Func<TExportApiClient, Task<ApiCacheHeaders>> fetchCacheHeadersAsync, Func<TCacheRepository, TExportApiClient, Task> fetchDataAsync)
        where TCacheRepository : IExportCacheRepository
    {
        if (!BackgroundService.IsStarted)
            throw new InvalidOperationException($"Must call {nameof(BackgroundService.StartAsync)} before scheduling tasks.");

        // log initial state
        context.WriteLine(cache.IsLoaded
            ? $"The previous export is cached with data from {BackgroundService.FormatDateModified(cache.CacheHeaders.LastModified)}."
            : "No previous export is cached."
        );

        // fetch cache headers
        context.WriteLine("Fetching cache headers...");
        ApiCacheHeaders serverCacheHeaders = await fetchCacheHeadersAsync(client);
        DateTimeOffset serverModified = serverCacheHeaders.LastModified;
        string? serverEntityTag = serverCacheHeaders.EntityTag;

        // update data
        {
            // skip if no update needed
            if (cache.IsStale(serverModified, BackgroundService.ExportStaleAge))
                context.WriteLine($"Skipped data fetch: server was last modified {BackgroundService.FormatDateModified(serverModified)}, which exceeds the {BackgroundService.ExportStaleAge}-minute-stale limit.");
            else if (cache.IsLoaded && cache.CacheHeaders.LastModified >= serverModified)
                context.WriteLine($"Skipped data fetch: server was last modified {BackgroundService.FormatDateModified(serverModified)}, which {(serverModified == cache.CacheHeaders.LastModified ? "matches" : "is older than")} our cached data.");

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
                await fetchDataAsync(cache, client);
            }
        }

        // clear if stale
        if (cache.IsStale(BackgroundService.ExportStaleAge))
        {
            context.WriteLine("The cached data is stale, clearing cache...");
            cache.Clear();
        }

        // log final result
        context.WriteLine(cache.IsLoaded
            ? $"Done! The export is currently cached with data from {BackgroundService.FormatDateModified(cache.CacheHeaders.LastModified)}."
            : "Done! The export cache is currently disabled."
        );
    }

    /// <summary>Format a 'date modified' value for the task logs.</summary>
    /// <param name="date">The date to log.</param>
    private static string FormatDateModified(DateTimeOffset? date)
    {
        if (!date.HasValue)
            return "<null>";

        string ageLabel = (DateTimeOffset.UtcNow - date.Value).Humanize(precision: 2, minUnit: TimeUnit.Minute, maxUnit: TimeUnit.Hour);

        return $"{date.Value:O} (age: {ageLabel})";
    }
}
