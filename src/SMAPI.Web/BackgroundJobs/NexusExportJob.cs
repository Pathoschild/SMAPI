using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.NexusExport;
using StardewModdingAPI.Web.Framework.BackgroundJobs;
using StardewModdingAPI.Web.Framework.Caching.NexusExport;
using StardewModdingAPI.Web.Framework.Clients.Nexus;
using StardewModdingAPI.Web.Framework.ConfigModels;

namespace StardewModdingAPI.Web.BackgroundJobs;

/// <summary>A background job which updates the cached Nexus mod export.</summary>
internal class NexusExportJob : ModSiteExportJob<INexusExportCacheRepository, INexusExportApiClient>, IBackgroundJob
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public static JobOptions Options { get; } = new("update Nexus export", Cron.MinuteInterval(10));

    /// <inheritdoc />
    public override bool IsEnabled => this.Client is not DisabledNexusExportApiClient;


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public NexusExportJob(INexusExportCacheRepository cache, INexusExportApiClient client, IOptions<ModUpdateCheckConfig> updateCheckConfig)
        : base(cache, client, updateCheckConfig) { }


    /*********
    ** Protected methods
    *********/
    /// <inheritdoc />
    protected override Task<ApiCacheHeaders> FetchCacheHeadersAsync()
    {
        return this.Client.FetchCacheHeadersAsync();
    }

    /// <inheritdoc />
    protected override async Task FetchDataAsync()
    {
        this.Cache.SetData(await this.Client.FetchExportAsync());
    }
}
