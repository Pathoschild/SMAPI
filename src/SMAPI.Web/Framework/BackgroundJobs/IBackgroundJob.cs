using System.Threading.Tasks;
using Hangfire.Server;

namespace StardewModdingAPI.Web.Framework.BackgroundJobs;

/// <summary>A background job scheduled via <see cref="BackgroundService"/>.</summary>
internal interface IBackgroundJob
{
    /*********
    ** Accessors
    *********/
    /// <summary>The recurring job options.</summary>
    static abstract JobOptions Options { get; }


    /*********
    ** Methods
    *********/
    /// <summary>Run the background job.</summary>
    /// <param name="context">Information about the context in which the job is performed. This is injected automatically by Hangfire.</param>
    Task RunAsync(PerformContext? context);
}
