using System;
using System.Threading.Tasks;
using Hangfire.Server;
using StardewModdingAPI.Web.Framework.BackgroundJobs;
using StardewModdingAPI.Web.Framework.Caching.Mods;

namespace StardewModdingAPI.Web.BackgroundJobs;

/// <summary>A background job which removes mods from the cache if they haven't been requested recently.</summary>
internal class RemoveStaleModsJob : IBackgroundJob
{
    /*********
    ** Fields
    *********/
    /// <summary>The time for which to keep cached mods after they're last requested.</summary>
    private readonly TimeSpan CacheTime = TimeSpan.FromHours(48);

    /// <summary>The cache in which to store mod data.</summary>
    private readonly IModCacheRepository ModCache;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public static JobOptions Options { get; } = new("remove stale mods", "2/10 * * * *"); // every 10 minutes, but offset by 2 minutes so it runs after updates (e.g. 00:02, 00:12, etc)


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="modCache"><inheritdoc cref="ModCache" path="/summary"/></param>
    public RemoveStaleModsJob(IModCacheRepository modCache)
    {
        this.ModCache = modCache;
    }

    /// <inheritdoc />
    public Task RunAsync(PerformContext? context)
    {
        this.ModCache.RemoveStaleMods(this.CacheTime);

        return Task.CompletedTask;
    }
}
