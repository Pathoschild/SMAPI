using System.Threading.Tasks;

namespace StardewModdingAPI.Web.Framework.Storage;

/// <summary>Provides access to raw data storage.</summary>
internal interface IStorageProvider
{
    /// <summary>Save a text file to storage.</summary>
    /// <param name="content">The content to upload.</param>
    /// <param name="compress">Whether to gzip the text.</param>
    /// <returns>Returns metadata about the save attempt.</returns>
    Task<UploadResult> SaveAsync(string content, bool compress = true);

    /// <summary>Fetch raw text from storage.</summary>
    /// <param name="id">The storage ID returned by <see cref="SaveAsync"/>.</param>
    /// <param name="renew">Whether to reset the file expiry.</param>
    Task<StoredFileInfo> GetAsync(string id, bool renew);
}