using System.IO;

namespace StardewModdingAPI.Toolkit.Utilities.PathLookups;

/// <summary>An API for file lookups within a root directory.</summary>
internal interface IFileLookup
{
    /// <summary>Get the file for a given relative file path, if it exists.</summary>
    /// <param name="relativePath">The relative path.</param>
    FileInfo GetFile(string relativePath);

    /// <summary>Add a relative path that was just created by a SMAPI API.</summary>
    /// <param name="relativePath">The relative path.</param>
    void Add(string relativePath);
}
