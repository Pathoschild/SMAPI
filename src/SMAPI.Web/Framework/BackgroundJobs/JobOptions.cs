namespace StardewModdingAPI.Web.Framework.BackgroundJobs;

/// <summary>The Hangfire options for a recurring scheduled job.</summary>
/// <param name="Id">The unique job ID.</param>
/// <param name="CronSchedule">The CRON schedule expression.</param>
internal record JobOptions(string Id, string CronSchedule);
