using System.Diagnostics.CodeAnalysis;
using System.IO;
using NUnit.Framework;
using StardewModdingAPI.Toolkit.Utilities;

namespace SMAPI.Tests.Utilities;

/// <summary>Unit tests for <see cref="PathUtilities"/>.</summary>
[TestFixture]
[SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "These are standard game install paths.")]
internal class PathUtilitiesTests
{
    /*********
    ** Sample data
    *********/
    /// <summary>Sample paths used in unit tests.</summary>
    public static readonly SamplePath[] SamplePaths = {
        // Windows absolute path
        new(
            OriginalPath: @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley",

            Segments: new[] { "C:", "Program Files (x86)", "Steam", "steamapps", "common", "Stardew Valley" },
            SegmentsLimit3: new [] { "C:", "Program Files (x86)", @"Steam\steamapps\common\Stardew Valley" },

            NormalizedOnWindows: @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley",
            NormalizedOnUnix: @"C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley"
        ),

        // Windows absolute path (with trailing slash)
        new(
            OriginalPath: @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\",

            Segments: new[] { "C:", "Program Files (x86)", "Steam", "steamapps", "common", "Stardew Valley" },
            SegmentsLimit3: new [] { "C:", "Program Files (x86)", @"Steam\steamapps\common\Stardew Valley\" },

            NormalizedOnWindows: @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\",
            NormalizedOnUnix: @"C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley/"
        ),

        // Windows relative path
        new(
            OriginalPath: @"Content\Characters\Dialogue\Abigail",

            Segments: new [] { "Content", "Characters", "Dialogue", "Abigail" },
            SegmentsLimit3: new [] { "Content", "Characters", @"Dialogue\Abigail" },

            NormalizedOnWindows: @"Content\Characters\Dialogue\Abigail",
            NormalizedOnUnix: @"Content/Characters/Dialogue/Abigail"
        ),

        // Windows relative path (with directory climbing)
        new(
            OriginalPath: @"..\..\Content",

            Segments: new [] { "..", "..", "Content" },
            SegmentsLimit3: new [] { "..", "..", "Content" },

            NormalizedOnWindows: @"..\..\Content",
            NormalizedOnUnix: @"../../Content"
        ),

        // Windows UNC path
        new(
            OriginalPath: @"\\unc\path",

            Segments: new [] { "unc", "path" },
            SegmentsLimit3: new [] { "unc", "path" },

            NormalizedOnWindows: @"\\unc\path",
            NormalizedOnUnix: "/unc/path" // there's no good way to normalize this on Unix since UNC paths aren't supported; path normalization is meant for asset names anyway, so this test only ensures it returns some sort of sane value
        ),

        // Linux absolute path
        new(
            OriginalPath: @"/home/.steam/steam/steamapps/common/Stardew Valley",

            Segments: new [] { "home", ".steam", "steam", "steamapps", "common", "Stardew Valley" },
            SegmentsLimit3: new [] { "home", ".steam", "steam/steamapps/common/Stardew Valley" },

            NormalizedOnWindows: @"\home\.steam\steam\steamapps\common\Stardew Valley",
            NormalizedOnUnix: @"/home/.steam/steam/steamapps/common/Stardew Valley"
        ),

        // Linux absolute path (with trailing slash)
        new(
            OriginalPath: @"/home/.steam/steam/steamapps/common/Stardew Valley/",

            Segments: new [] { "home", ".steam", "steam", "steamapps", "common", "Stardew Valley" },
            SegmentsLimit3: new [] { "home", ".steam", "steam/steamapps/common/Stardew Valley/" },

            NormalizedOnWindows: @"\home\.steam\steam\steamapps\common\Stardew Valley\",
            NormalizedOnUnix: @"/home/.steam/steam/steamapps/common/Stardew Valley/"
        ),

        // Linux absolute path (with ~)
        new(
            OriginalPath: @"~/.steam/steam/steamapps/common/Stardew Valley",

            Segments: new [] { "~", ".steam", "steam", "steamapps", "common", "Stardew Valley" },
            SegmentsLimit3: new [] { "~", ".steam", "steam/steamapps/common/Stardew Valley" },

            NormalizedOnWindows: @"~\.steam\steam\steamapps\common\Stardew Valley",
            NormalizedOnUnix: @"~/.steam/steam/steamapps/common/Stardew Valley"
        ),

        // Linux relative path
        new(
            OriginalPath: @"Content/Characters/Dialogue/Abigail",

            Segments: new [] { "Content", "Characters", "Dialogue", "Abigail" },
            SegmentsLimit3: new [] { "Content", "Characters", "Dialogue/Abigail" },

            NormalizedOnWindows: @"Content\Characters\Dialogue\Abigail",
            NormalizedOnUnix: @"Content/Characters/Dialogue/Abigail"
        ),

        // Linux relative path (with directory climbing)
        new(
            OriginalPath: @"../../Content",

            Segments: new [] { "..", "..", "Content" },
            SegmentsLimit3: new [] { "..", "..", "Content" },

            NormalizedOnWindows: @"..\..\Content",
            NormalizedOnUnix: @"../../Content"
        ),

        // Mixed directory separators
        new(
            OriginalPath: @"C:\some/mixed\path/separators",

            Segments: new [] { "C:", "some", "mixed", "path", "separators" },
            SegmentsLimit3: new [] { "C:", "some", @"mixed\path/separators" },

            NormalizedOnWindows: @"C:\some\mixed\path\separators",
            NormalizedOnUnix: @"C:/some/mixed/path/separators"
        )
    };


    /*********
    ** Unit tests
    *********/
    /****
    ** GetSegments
    ****/
    [Test(Description = "Assert that PathUtilities.GetSegments splits paths correctly.")]
    [TestCaseSource(nameof(PathUtilitiesTests.SamplePaths))]
    public void GetSegments(SamplePath path)
    {
        // act
        string[] segments = PathUtilities.GetSegments(path.OriginalPath);

        // assert
        Assert.AreEqual(path.Segments, segments);
    }

    [Test(Description = "Assert that PathUtilities.GetSegments splits paths correctly when given a limit.")]
    [TestCaseSource(nameof(PathUtilitiesTests.SamplePaths))]
    public void GetSegments_WithLimit(SamplePath path)
    {
        // act
        string[] segments = PathUtilities.GetSegments(path.OriginalPath, 3);

        // assert
        Assert.AreEqual(path.SegmentsLimit3, segments);
    }

    /****
    ** NormalizeAssetName
    ****/
    [Test(Description = "Assert that PathUtilities.NormalizeAssetName normalizes paths correctly.")]
    [TestCaseSource(nameof(PathUtilitiesTests.SamplePaths))]
    public void NormalizeAssetName(SamplePath path)
    {
        if (Path.IsPathRooted(path.OriginalPath) || path.OriginalPath.StartsWith('/') || path.OriginalPath.StartsWith('\\'))
            Assert.Ignore("Absolute paths can't be used as asset names.");

        // act
        string normalized = PathUtilities.NormalizeAssetName(path.OriginalPath);

        // assert
        Assert.AreEqual(path.NormalizedOnUnix, normalized); // MonoGame uses the Linux format
    }

    /****
    ** NormalizePath
    ****/
    [Test(Description = "Assert that PathUtilities.NormalizePath normalizes paths correctly.")]
    [TestCaseSource(nameof(PathUtilitiesTests.SamplePaths))]
    public void NormalizePath(SamplePath path)
    {
        // act
        string normalized = PathUtilities.NormalizePath(path.OriginalPath);

        // assert
#if SMAPI_FOR_WINDOWS
        Assert.AreEqual(path.NormalizedOnWindows, normalized);
#else
            Assert.AreEqual(path.NormalizedOnUnix, normalized);
#endif
    }

    /****
    ** GetRelativePath
    ****/
    [Test(Description = "Assert that PathUtilities.GetRelativePath returns the expected values.")]
#if SMAPI_FOR_WINDOWS
    [TestCase(
        @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley",
        @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\Automate",
        ExpectedResult = @"Mods\Automate"
    )]
    [TestCase(
        @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\Automate",
        @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Content",
        ExpectedResult = @"..\..\Content"
    )]
    [TestCase(
        @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\Automate",
        @"D:\another-drive",
        ExpectedResult = @"D:\another-drive"
    )]
    [TestCase(
        @"\\parent\unc",
        @"\\parent\unc\path\to\child",
        ExpectedResult = @"path\to\child"
    )]
    [TestCase(
        @"C:\same\path",
        @"C:\same\path",
        ExpectedResult = @"."
    )]
    [TestCase(
        @"C:\parent",
        @"C:\PARENT\child",
        ExpectedResult = @"child"
    )]
#else
        [TestCase(
            @"~/.steam/steam/steamapps/common/Stardew Valley",
            @"~/.steam/steam/steamapps/common/Stardew Valley/Mods/Automate",
            ExpectedResult = @"Mods/Automate"
        )]
        [TestCase(
            @"~/.steam/steam/steamapps/common/Stardew Valley/Mods/Automate",
            @"~/.steam/steam/steamapps/common/Stardew Valley/Content",
            ExpectedResult = @"../../Content"
        )]
        [TestCase(
            @"~/.steam/steam/steamapps/common/Stardew Valley/Mods/Automate",
            @"/mnt/another-drive",
            ExpectedResult = @"/mnt/another-drive"
        )]
        [TestCase(
            @"~/same/path",
            @"~/same/path",
            ExpectedResult = @"."
        )]
        [TestCase(
            @"~/parent",
            @"~/PARENT/child",
            ExpectedResult = @"child" // note: incorrect on Linux and sometimes macOS, but not worth the complexity of detecting whether the filesystem is case-sensitive for SMAPI's purposes
        )]
#endif
    public string GetRelativePath(string sourceDir, string targetPath)
    {
        return PathUtilities.GetRelativePath(sourceDir, targetPath);
    }


    /*********
    ** Private classes
    *********/
    /// <summary>A sample path in multiple formats.</summary>
    /// <param name="OriginalPath">The original path to pass to the <see cref="PathUtilities"/>.</param>
    /// <param name="Segments">The normalized path segments.</param>
    /// <param name="SegmentsLimit3">The normalized path segments, if we stop segmenting after the second one.</param>
    /// <param name="NormalizedOnWindows">The normalized form on Windows.</param>
    /// <param name="NormalizedOnUnix">The normalized form on Linux or macOS.</param>
    public record SamplePath(string OriginalPath, string[] Segments, string[] SegmentsLimit3, string NormalizedOnWindows, string NormalizedOnUnix)
    {
        public override string ToString()
        {
            return this.OriginalPath;
        }
    }
}
