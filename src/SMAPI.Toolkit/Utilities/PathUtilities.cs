using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StardewModdingAPI.Toolkit.Utilities
{
    /// <summary>Provides utilities for normalizing file paths.</summary>
    public static class PathUtilities
    {
        /*********
        ** Fields
        *********/
        /// <summary>The root prefix for a Windows UNC path.</summary>
        private const string WindowsUncRoot = @"\\";


        /*********
        ** Accessors
        *********/
        /// <summary>The possible directory separator characters in a file path.</summary>
        public static readonly char[] PossiblePathSeparators = new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.Distinct().ToArray();

        /// <summary>The preferred directory separator character in a file path.</summary>
        public static readonly char PreferredPathSeparator = Path.DirectorySeparatorChar;

        /// <summary>The preferred directory separator character in an asset key.</summary>
        public static readonly char PreferredAssetSeparator = '/';


        /*********
        ** Public methods
        *********/
        /// <summary>Get the segments from a path (e.g. <c>/usr/bin/example</c> => <c>usr</c>, <c>bin</c>, and <c>example</c>).</summary>
        /// <param name="path">The path to split.</param>
        /// <param name="limit">The number of segments to match. Any additional segments will be merged into the last returned part.</param>
        [Pure]
        public static string[] GetSegments(string path, int? limit = null)
        {
            return limit.HasValue
                ? path.Split(PathUtilities.PossiblePathSeparators, limit.Value, StringSplitOptions.RemoveEmptyEntries)
                : path.Split(PathUtilities.PossiblePathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Normalize an asset name to match how MonoGame's content APIs would normalize and cache it.</summary>
        /// <param name="assetName">The asset name to normalize.</param>
        public static string NormalizeAssetName(string assetName)
        {
            return string.Join(PathUtilities.PreferredAssetSeparator.ToString(), PathUtilities.GetSegments(assetName)); // based on MonoGame's ContentManager.Load<T> logic
        }

        /// <summary>Normalize separators in a file path for the current platform.</summary>
        /// <param name="path">The file path to normalize.</param>
        /// <remarks>This should only be used for file paths. For asset names, use <see cref="NormalizeAssetName"/> instead.</remarks>
        [Pure]
        public static string NormalizePath(string path)
        {
            path = path?.Trim();
            if (string.IsNullOrEmpty(path))
                return path;

            // get basic path format (e.g. /some/asset\\path/ => some\asset\path)
            string[] segments = PathUtilities.GetSegments(path);
            string newPath = string.Join(PathUtilities.PreferredPathSeparator.ToString(), segments);

            // keep root prefix
            bool hasRoot = false;
            if (path.StartsWith(PathUtilities.WindowsUncRoot))
            {
                newPath = PathUtilities.WindowsUncRoot + newPath;
                hasRoot = true;
            }
            else if (PathUtilities.PossiblePathSeparators.Contains(path[0]))
            {
                newPath = PathUtilities.PreferredPathSeparator + newPath;
                hasRoot = true;
            }

            // keep trailing separator
            if ((!hasRoot || segments.Any()) && PathUtilities.PossiblePathSeparators.Contains(path[path.Length - 1]))
                newPath += PathUtilities.PreferredPathSeparator;

            return newPath;
        }

        /// <summary>Get a directory or file path relative to a given source path. If no relative path is possible (e.g. the paths are on different drives), an absolute path is returned.</summary>
        /// <param name="sourceDir">The source folder path.</param>
        /// <param name="targetPath">The target folder or file path.</param>
        [Pure]
        public static string GetRelativePath(string sourceDir, string targetPath)
        {
#if NET5_0
            return Path.GetRelativePath(sourceDir, targetPath);
#else
            // NOTE:
            // this is a heuristic implementation that works in the cases SMAPI needs it for, but it
            // doesn't handle all edge cases (e.g. case-sensitivity on Linux, or traversing between
            // UNC paths on Windows). SMAPI and mods will use the more robust .NET 5 version anyway
            // though, this is only for compatibility with the mod build package.

            // convert to URIs
            Uri from = new Uri(sourceDir.TrimEnd(PathUtilities.PossiblePathSeparators) + "/");
            Uri to = new Uri(targetPath.TrimEnd(PathUtilities.PossiblePathSeparators) + "/");
            if (from.Scheme != to.Scheme)
                throw new InvalidOperationException($"Can't get path for '{targetPath}' relative to '{sourceDir}'.");

            // get relative path
            string rawUrl = Uri.UnescapeDataString(from.MakeRelativeUri(to).ToString());
            if (rawUrl.StartsWith("file://"))
                rawUrl = PathUtilities.WindowsUncRoot + rawUrl.Substring("file://".Length);
            string relative = PathUtilities.NormalizePath(rawUrl);

            // normalize
            if (relative == "")
                relative = ".";
            else
            {
                // trim trailing slash from URL
                if (relative.EndsWith(PathUtilities.PreferredPathSeparator.ToString()))
                    relative = relative.Substring(0, relative.Length - 1);

                // fix root
                if (relative.StartsWith("file:") && !targetPath.Contains("file:"))
                    relative = relative.Substring("file:".Length);
            }

            return relative;
#endif
        }

        /// <summary>Get whether a path is relative and doesn't try to climb out of its containing folder (e.g. doesn't contain <c>../</c>).</summary>
        /// <param name="path">The path to check.</param>
        [Pure]
        public static bool IsSafeRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            return
                !Path.IsPathRooted(path)
                && PathUtilities.GetSegments(path).All(segment => segment.Trim() != "..");
        }

        /// <summary>Get whether a string is a valid 'slug', containing only basic characters that are safe in all contexts (e.g. filenames, URLs, etc).</summary>
        /// <param name="str">The string to check.</param>
        [Pure]
        public static bool IsSlug(string str)
        {
            return !Regex.IsMatch(str, "[^a-z0-9_.-]", RegexOptions.IgnoreCase);
        }

        /// <summary>Get the paths which exceed the OS length limit.</summary>
        /// <param name="rootPath">The root path to search.</param>
        internal static IEnumerable<string> GetTooLongPaths(string rootPath)
        {
            if (!Directory.Exists(rootPath))
                return new string[0];

            return Directory
                .EnumerateFileSystemEntries(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(PathUtilities.IsPathTooLong);
        }

        /// <summary>Get whether a file or directory path exceeds the OS path length limit.</summary>
        /// <param name="path">The path to test.</param>
        internal static bool IsPathTooLong(string path)
        {
            try
            {
                _ = Path.GetFullPath(path);
                return false;
            }
            catch (PathTooLongException)
            {
                return true;
            }
        }
    }
}
