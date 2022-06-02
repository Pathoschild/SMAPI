using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Hosting;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;
using StardewModdingAPI.Web.Framework.Caching.Mods;
using StardewModdingAPI.Web.Framework.Caching.Wiki;

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

    /// <summary>The cache in which to store wiki metadata.</summary>
    private static IWikiCacheRepository? WikiCache;

    /// <summary>The cache in which to store mod data.</summary>
    private static IModCacheRepository? ModCache;

    /// <summary>Whether the service has been started.</summary>
    [MemberNotNullWhen(true, nameof(BackgroundService.JobServer), nameof(BackgroundService.WikiCache), nameof(BackgroundService.ModCache))]
    private static bool IsStarted { get; set; }


    /*********
    ** Public methods
    *********/
    /****
    ** Hosted service
    ****/
    /// <summary>Construct an instance.</summary>
    /// <param name="wikiCache">The cache in which to store wiki metadata.</param>
    /// <param name="modCache">The cache in which to store mod data.</param>
    /// <param name="hangfireStorage">The Hangfire storage implementation.</param>
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "The Hangfire reference forces it to initialize first, since it's needed by the background service.")]
    public BackgroundService(IWikiCacheRepository wikiCache, IModCacheRepository modCache, JobStorage hangfireStorage)
    {
        BackgroundService.WikiCache = wikiCache;
        BackgroundService.ModCache = modCache;
    }

    /// <summary>Start the service.</summary>
    /// <param name="cancellationToken">Tracks whether the start process has been aborted.</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.TryInit();

        // set startup tasks
        BackgroundJob.Enqueue(() => BackgroundService.UpdateWikiAsync());
        BackgroundJob.Enqueue(() => BackgroundService.RemoveStaleModsAsync());

        // set recurring tasks
        RecurringJob.AddOrUpdate(() => BackgroundService.UpdateWikiAsync(), "*/10 * * * *"); // every 10 minutes
        RecurringJob.AddOrUpdate(() => BackgroundService.RemoveStaleModsAsync(), "0 * * * *"); // hourly

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
    /// <summary>Update the cached wiki metadata.</summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public static async Task UpdateWikiAsync()
    {
        if (!BackgroundService.IsStarted)
            throw new InvalidOperationException($"Must call {nameof(BackgroundService.StartAsync)} before scheduling tasks.");

        WikiModList wikiCompatList = await new ModToolkit().GetWikiCompatibilityListAsync();
        BackgroundService.WikiCache.SaveWikiData(wikiCompatList.StableVersion, wikiCompatList.BetaVersion, wikiCompatList.Mods);
    }

    /// <summary>Remove mods which haven't been requested in over 48 hours.</summary>
    public static Task RemoveStaleModsAsync()
    {
        if (!BackgroundService.IsStarted)
            throw new InvalidOperationException($"Must call {nameof(BackgroundService.StartAsync)} before scheduling tasks.");

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
}
