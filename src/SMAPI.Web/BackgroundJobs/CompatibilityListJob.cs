using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.CompatibilityRepo;
using StardewModdingAPI.Web.Framework.BackgroundJobs;
using StardewModdingAPI.Web.Framework.Caching.CompatibilityRepo;

namespace StardewModdingAPI.Web.BackgroundJobs;

/// <summary>A background job which updates the cached compatibility list data.</summary>
internal class CompatibilityListJob : IBackgroundJob
{
    /*********
    ** Fields
    *********/
    /// <summary>The cache in which to store compatibility list data.</summary>
    private readonly ICompatibilityCacheRepository CompatibilityCache;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public static JobOptions Options { get; } = new("update compatibility list", Cron.MinuteInterval(10));


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="compatibilityCache"><inheritdoc cref="CompatibilityCache" path="/summary"/></param>
    public CompatibilityListJob(ICompatibilityCacheRepository compatibilityCache)
    {
        this.CompatibilityCache = compatibilityCache;
    }

    /// <inheritdoc />
    public async Task RunAsync(PerformContext? context)
    {
        context.WriteLine("Fetching data from compatibility repo...");
        ModCompatibilityEntry[] compatList = await new ModToolkit().GetCompatibilityListAsync();

        context.WriteLine("Saving data...");
        this.CompatibilityCache.SaveData(compatList);

        context.WriteLine("Done!");
    }
}
