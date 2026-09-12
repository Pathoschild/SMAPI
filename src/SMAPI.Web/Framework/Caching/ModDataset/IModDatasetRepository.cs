using System;
using System.Threading.Tasks;

namespace StardewModdingAPI.Web.Framework.Caching.ModDataset;

/// <summary>Manages the Stardew mod dataset repo.</summary>
internal interface IModDatasetRepository
{
    /*********
    ** Methods
    *********/
    /// <summary>Fetch the latest mod dataset, if it changed or hasn't been fetched yet.</summary>
    /// <param name="log">A callback which should receive progress messages for logging.</param>
    /// <exception cref="InvalidOperationException">The downloaded archive isn't a valid mod dataset.</exception>
    Task<DatasetDownload> UpdateAsync(Action<string>? log = null);

    /// <summary>Get the full path to a file in the mod dataset.</summary>
    /// <param name="relativePath">The file path relative to the <c>dataset</c> folder in the mod dataset repo.</param>
    string GetFilePath(string relativePath);
}
