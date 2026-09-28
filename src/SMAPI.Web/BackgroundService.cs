using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Hosting;
using StardewModdingAPI.Web.BackgroundJobs;
using StardewModdingAPI.Web.Framework.BackgroundJobs;

namespace StardewModdingAPI.Web;

/// <summary>A hosted service which schedules and runs the background jobs.</summary>
internal class BackgroundService : IHostedService, IDisposable
{
    /*********
    ** Fields
    *********/
    /// <summary>The background job server, if started.</summary>
    private BackgroundJobServer? JobServer;

    /// <summary>Whether the <see cref="CurseForgeExportJob"/> job is enabled.</summary>
    private readonly bool EnableCurseForgeExport;

    /// <summary>Whether the <see cref="ModDropExportJob"/> job is enabled.</summary>
    private readonly bool EnableModDropExport;

    /// <summary>Whether the <see cref="NexusExportJob"/> job is enabled.</summary>
    private readonly bool EnableNexusExport;

    /// <summary>Whether the <see cref="MalwareBlacklistJob"/> job is enabled.</summary>
    private readonly bool EnableMalwareBlacklistSync;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="curseForgeExportJob"><inheritdoc cref="CurseForgeExportJob" path="/summary"/></param>
    /// <param name="modDropExportJob"><inheritdoc cref="ModDropExportJob" path="/summary"/></param>
    /// <param name="nexusExportJob"><inheritdoc cref="NexusExportJob" path="/summary"/></param>
    /// <param name="malwareBlacklistJob"><inheritdoc cref="MalwareBlacklistJob" path="/summary"/></param>
    /// <param name="hangfireStorage">The Hangfire storage implementation.</param>
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "The Hangfire reference forces it to initialize first, since it's needed by the background service.")]
    public BackgroundService(CurseForgeExportJob curseForgeExportJob, ModDropExportJob modDropExportJob, NexusExportJob nexusExportJob, MalwareBlacklistJob malwareBlacklistJob, JobStorage hangfireStorage)
    {
        this.EnableCurseForgeExport = curseForgeExportJob.IsEnabled;
        this.EnableModDropExport = modDropExportJob.IsEnabled;
        this.EnableNexusExport = nexusExportJob.IsEnabled;
        this.EnableMalwareBlacklistSync = malwareBlacklistJob.IsEnabled;

        _ = hangfireStorage; // parameter is only received to initialize it before the background service
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (this.JobServer != null)
            throw new InvalidOperationException("The scheduler service is already started.");

        // set retry policy
        GlobalJobFilters.Filters.Remove<AutomaticRetryAttribute>();
        GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = [30, 60, 120] });

        // start Hangfire server
        this.JobServer = new BackgroundJobServer();

        // register jobs
        this.RegisterJob<CompatibilityListJob>();
        this.RegisterJob<ModDatasetJob>();
        this.RegisterJob<RemoveStaleModsJob>();
        if (this.EnableMalwareBlacklistSync)
            this.RegisterJob<MalwareBlacklistJob>();
        if (this.EnableCurseForgeExport)
            this.RegisterJob<CurseForgeExportJob>();
        if (this.EnableModDropExport)
            this.RegisterJob<ModDropExportJob>();
        if (this.EnableNexusExport)
            this.RegisterJob<NexusExportJob>();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.JobServer != null)
            await this.JobServer.WaitForShutdownAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.JobServer?.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Register a background job so it's enqueued immediately, and also runs on a recurring schedule.</summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    private void RegisterJob<TJob>()
        where TJob : IBackgroundJob
    {
        JobOptions options = TJob.Options;

        BackgroundJob.Enqueue<TJob>(job => job.RunAsync(null));
        RecurringJob.AddOrUpdate<TJob>(options.Id, job => job.RunAsync(null), options.CronSchedule);
    }
}
