using System;
using System.IO;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.AspNetCore.Hosting;
using StardewModdingAPI.Web.Framework.BackgroundJobs;
using StardewModdingAPI.Web.Framework.Caching.ModDataset;

namespace StardewModdingAPI.Web.BackgroundJobs;

/// <summary>A background job which updates the local copy of the Stardew mod dataset for use by the frontend scripts.</summary>
internal class ModDatasetJob : IBackgroundJob
{
    /*********
    ** Fields
    *********/
    /// <summary>The mod dataset repository.</summary>
    private readonly IModDatasetRepository ModDatasetRepo;

    /// <summary>The web root path for writing static data files.</summary>
    private readonly string WebRootPath;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public static JobOptions Options { get; } = new("update mod dataset", Cron.Hourly());


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="modDatasetRepo"><inheritdoc cref="ModDatasetRepo" path="/summary"/></param>
    /// <param name="hostEnvironment">The web host environment metadata.</param>
    public ModDatasetJob(IModDatasetRepository modDatasetRepo, IWebHostEnvironment hostEnvironment)
    {
        this.ModDatasetRepo = modDatasetRepo;
        this.WebRootPath = hostEnvironment.WebRootPath;
    }

    /// <inheritdoc />
    public async Task RunAsync(PerformContext? context)
    {
        // update repo
        context.WriteLine("Updating mod dataset repo...");
        DatasetDownload result = await this.ModDatasetRepo.UpdateAsync(context.WriteLine);

        // copy files
        context.WriteLine("Copying data files for script use...");
        this.CopyToData("stats", "mods by type.jsonl");
        this.CopyToData("stats", "Content Patcher packs by format version.jsonl", "content-packs-by-format.jsonl");
        this.CopyToData("reference-data", "SMAPI costs.jsonl");
        this.CopyToData("reference-data", "SMAPI DNS queries.json");

        Program.ModDatasetCacheBustValue = result.ETag ?? $"epoch-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        context.WriteLine("Done!");
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Copy a fetched file into the web content data directory.</summary>
    /// <param name="fromRelativeDir">The relative path to the directory containing the file to copy within the fetched repo.</param>
    /// <param name="fromFileName">The file name within the fetched repo to copy.</param>
    /// <param name="toFileName">The file name to save it to within the web content data directory, or <c>null</c> to use a lowercase spaces-to-hyphenated version of <paramref name="fromFileName"/>.</param>
    private void CopyToData(string fromRelativeDir, string fromFileName, string? toFileName = null)
    {
        string fromRelativePath = Path.Combine(fromRelativeDir, fromFileName);

        toFileName ??= fromFileName.Replace(' ', '-').ToLowerInvariant();
        string toAbsolutePath = Path.Combine(this.WebRootPath, "Content", "data", toFileName);

        Directory.CreateDirectory(Path.GetDirectoryName(toAbsolutePath)!);

        File.Copy(this.ModDatasetRepo.GetFilePath(fromRelativePath), toAbsolutePath, overwrite: true);
    }
}
