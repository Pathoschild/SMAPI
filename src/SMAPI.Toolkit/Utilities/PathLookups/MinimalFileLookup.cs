using System.Collections.Generic;
using System.IO;

namespace StardewModdingAPI.Toolkit.Utilities.PathLookups;

/// <summary>An API for file lookups within a root directory with minimal preprocessing.</summary>
internal class MinimalFileLookup : IFileLookup
{
    /*********
    ** Accessors
    *********/
    /// <summary>The file lookups by root path.</summary>
    private static readonly Dictionary<string, MinimalFileLookup> CachedRoots = new();

    /// <summary>The root directory path for relative paths.</summary>
    private readonly string RootPath;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="rootPath">The root directory path for relative paths.</param>
    public MinimalFileLookup(string rootPath)
    {
        this.RootPath = rootPath;
    }

    /// <inheritdoc />
    public FileInfo GetFile(string relativePath)
    {
        return new(
            Path.Combine(this.RootPath, PathUtilities.NormalizePath(relativePath))
        );
    }

    /// <inheritdoc />
    public void Add(string relativePath) { }

    /// <summary>Get a cached dictionary of relative paths within a root path, for case-insensitive file lookups.</summary>
    /// <param name="rootPath">The root path to scan.</param>
    public static MinimalFileLookup GetCachedFor(string rootPath)
    {
        rootPath = PathUtilities.NormalizePath(rootPath);

        if (!MinimalFileLookup.CachedRoots.TryGetValue(rootPath, out MinimalFileLookup? lookup))
            MinimalFileLookup.CachedRoots[rootPath] = lookup = new MinimalFileLookup(rootPath);

        return lookup;
    }
}
