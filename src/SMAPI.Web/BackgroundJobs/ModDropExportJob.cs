using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit.Framework.Clients;
using StardewModdingAPI.Toolkit.Framework.Clients.ModDropExport;
using StardewModdingAPI.Web.Framework.BackgroundJobs;
using StardewModdingAPI.Web.Framework.Caching.ModDropExport;
using StardewModdingAPI.Web.Framework.Clients.ModDrop;
using StardewModdingAPI.Web.Framework.ConfigModels;

namespace StardewModdingAPI.Web.BackgroundJobs;

/// <summary>A background job which updates the cached ModDrop mod export.</summary>
internal class ModDropExportJob : ModSiteExportJob<IModDropExportCacheRepository, IModDropExportApiClient>, IBackgroundJob
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public static JobOptions Options { get; } = new("update ModDrop export", Cron.MinuteInterval(10));

    /// <inheritdoc />
    public override bool IsEnabled => this.Client is not DisabledModDropExportApiClient;


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public ModDropExportJob(IModDropExportCacheRepository cache, IModDropExportApiClient client, IOptions<ModUpdateCheckConfig> updateCheckConfig)
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
