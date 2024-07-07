using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using ToolkitPathUtilities = StardewModdingAPI.Toolkit.Utilities.PathUtilities;

namespace StardewModdingAPI.Utilities
{
    /// <summary>Provides utilities for normalizing file paths.</summary>
    public static class PathUtilities
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The preferred directory separator character in an asset key.</summary>
        public static char PreferredAssetSeparator { get; } = ToolkitPathUtilities.PreferredAssetSeparator;


        /*********
        ** Public methods
        *********/
        /// <summary>Get the segments from a path (e.g. <c>/usr/bin/example</c> => <c>usr</c>, <c>bin</c>, and <c>example</c>).</summary>
        /// <param name="path">The path to split.</param>
        /// <param name="limit">The number of segments to match. Any additional segments will be merged into the last returned part.</param>
        [Pure]
        public static string[] GetSegments(string? path, int? limit = null)
        {
            return ToolkitPathUtilities.GetSegments(path, limit);
        }

        /// <summary>Normalize an asset name to match how MonoGame's content APIs would normalize and cache it.</summary>
        /// <param name="assetName">The asset name to normalize.</param>
        [Pure]
        [return: NotNullIfNotNull("assetName")]
        public static string? NormalizeAssetName(string? assetName)
        {
            return ToolkitPathUtilities.NormalizeAssetName(assetName);
        }

        /// <summary>Normalize separators in a file path for the current platform.</summary>
        /// <param name="path">The file path to normalize.</param>
        /// <remarks>This should only be used for file paths. For asset names, use <see cref="NormalizeAssetName"/> instead.</remarks>
        [Pure]
        [return: NotNullIfNotNull("path")]
        public static string? NormalizePath(string? path)
        {
            return ToolkitPathUtilities.NormalizePath(path);
        }

        /// <summary>Get a path with the home directory path replaced with <c>~</c> (like <c>C:\Users\Admin\Game</c> to <c>~\Game</c>), if applicable.</summary>
        /// <param name="path">The path to anonymize.</param>
        [Pure]
        public static string AnonymizePathForDisplay(string path)
        {
            return ToolkitPathUtilities.AnonymizePathForDisplay(path);
        }

        /// <summary>Get whether a path is relative and doesn't try to climb out of its containing folder (e.g. doesn't contain <c>../</c>).</summary>
        /// <param name="path">The path to check.</param>
        [Pure]
        public static bool IsSafeRelativePath(string? path)
        {
            return ToolkitPathUtilities.IsSafeRelativePath(path);
        }

        /// <summary>Get whether a string is a valid 'slug', containing only basic characters that are safe in all contexts (e.g. filenames, URLs, etc).</summary>
        /// <param name="str">The string to check.</param>
        [Pure]
        public static bool IsSlug(string? str)
        {
            return ToolkitPathUtilities.IsSlug(str);
        }
    }
}
