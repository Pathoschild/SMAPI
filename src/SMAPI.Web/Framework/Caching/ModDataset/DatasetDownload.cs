namespace StardewModdingAPI.Web.Framework.Caching.ModDataset;

/// <summary>The metadata about a dataset download.</summary>
/// <param name="FolderName">The folder name within the root folder.</param>
/// <param name="RelativePathToDataset">The relative path to the 'dataset' folder within the <see cref="FolderName"/>.</param>
/// <param name="ETag">The ETag value for the downloaded archive, if available.</param>
internal record DatasetDownload(string FolderName, string RelativePathToDataset, string? ETag);
