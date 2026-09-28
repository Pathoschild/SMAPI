using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.CurseForgeExport;
using StardewModdingAPI.Web.Framework.BackgroundJobs;
using StardewModdingAPI.Web.Framework.Caching.CurseForgeExport;
using StardewModdingAPI.Web.Framework.Clients.CurseForge;
using StardewModdingAPI.Web.Framework.ConfigModels;

namespace StardewModdingAPI.Web.BackgroundJobs;

/// <summary>A background job which updates the cached CurseForge mod export.</summary>
internal class CurseForgeExportJob : ModSiteExportJob<ICurseForgeExportCacheRepository, ICurseForgeExportApiClient>, IBackgroundJob
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public static JobOptions Options { get; } = new("update CurseForge export", Cron.MinuteInterval(10));

    /// <inheritdoc />
    public override bool IsEnabled => this.Client is not DisabledCurseForgeExportApiClient;


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public CurseForgeExportJob(ICurseForgeExportCacheRepository cache, ICurseForgeExportApiClient client, IOptions<ModUpdateCheckConfig> updateCheckConfig)
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
