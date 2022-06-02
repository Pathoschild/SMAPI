using System;
using System.Collections.Generic;
using System.IO;

namespace StardewModdingAPI.Toolkit.Utilities.PathLookups;

/// <summary>An API for case-insensitive file lookups within a root directory.</summary>
internal class CaseInsensitiveFileLookup : IFileLookup
{
    /*********
    ** Fields
    *********/
    /// <summary>The root directory path for relative paths.</summary>
    private readonly string RootPath;

    /// <summary>A case-insensitive lookup of file paths within the <see cref="RootPath"/>. Each path is listed in both file path and asset name format, so it's usable in both contexts without needing to re-parse paths.</summary>
    private readonly Lazy<Dictionary<string, string>> RelativePathCache;

    /// <summary>The case-insensitive file lookups by root path.</summary>
    private static readonly Dictionary<string, CaseInsensitiveFileLookup> CachedRoots = new(StringComparer.OrdinalIgnoreCase);


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="rootPath">The root directory path for relative paths.</param>
    /// <param name="searchOption">Which directories to scan from the root.</param>
    public CaseInsensitiveFileLookup(string rootPath, SearchOption searchOption = SearchOption.AllDirectories)
    {
        this.RootPath = PathUtilities.NormalizePath(rootPath);
        this.RelativePathCache = new(() => this.GetRelativePathCache(searchOption));
    }

    /// <inheritdoc />
    public FileInfo GetFile(string relativePath)
    {
        // invalid path
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new InvalidOperationException("Can't get a file from an empty relative path.");

        // already cached
        if (this.RelativePathCache.Value.TryGetValue(relativePath, out string? resolved))
            return new(Path.Combine(this.RootPath, resolved));

        // keep capitalization as-is
        FileInfo file = new(Path.Combine(this.RootPath, relativePath));
        if (file.Exists)
            this.RelativePathCache.Value[relativePath] = relativePath;
        return file;
    }

    /// <inheritdoc />
    public void Add(string relativePath)
    {
        // skip if cache isn't created yet (no need to add files manually in that case)
        if (!this.RelativePathCache.IsValueCreated)
            return;

        // skip if already cached
        if (this.RelativePathCache.Value.ContainsKey(relativePath))
            return;

        // make sure path exists
        relativePath = PathUtilities.NormalizePath(relativePath);
        if (!File.Exists(Path.Combine(this.RootPath, relativePath)))
            throw new InvalidOperationException($"Can't add relative path '{relativePath}' to the case-insensitive cache for '{this.RootPath}' because that file doesn't exist.");

        // cache path
        this.RelativePathCache.Value[relativePath] = relativePath;
    }

    /// <summary>Get a cached dictionary of relative paths within a root path, for case-insensitive file lookups.</summary>
    /// <param name="rootPath">The root path to scan.</param>
    public static CaseInsensitiveFileLookup GetCachedFor(string rootPath)
    {
        rootPath = PathUtilities.NormalizePath(rootPath);

        if (!CaseInsensitiveFileLookup.CachedRoots.TryGetValue(rootPath, out CaseInsensitiveFileLookup? cache))
            CaseInsensitiveFileLookup.CachedRoots[rootPath] = cache = new CaseInsensitiveFileLookup(rootPath);

        return cache;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a case-insensitive lookup of file paths (see <see cref="RelativePathCache"/>).</summary>
    /// <param name="searchOption">Which directories to scan from the root.</param>
    private Dictionary<string, string> GetRelativePathCache(SearchOption searchOption)
    {
        Dictionary<string, string> cache = new(StringComparer.OrdinalIgnoreCase);

        foreach (string path in Directory.EnumerateFiles(this.RootPath, "*", searchOption))
        {
            string relativePath = path.Substring(this.RootPath.Length + 1);
            cache[relativePath] = relativePath;
        }

        return cache;
    }
}
