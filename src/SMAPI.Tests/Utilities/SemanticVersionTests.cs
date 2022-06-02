using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Framework;

namespace SMAPI.Tests.Utilities;

/// <summary>Unit tests for <see cref="SemanticVersion"/>.</summary>
[TestFixture]
internal class SemanticVersionTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** Constructor
    ****/
    /// <summary>Assert the parsed version when constructed from a standard string.</summary>
    /// <param name="input">The version string to parse.</param>
    [TestCase("1.0", ExpectedResult = "1.0.0")]
    [TestCase("1.0.0", ExpectedResult = "1.0.0")]
    [TestCase("3000.4000.5000", ExpectedResult = "3000.4000.5000")]
    [TestCase("1.2-some-tag.4", ExpectedResult = "1.2.0-some-tag.4")]
    [TestCase("1.2.3-some-tag.4", ExpectedResult = "1.2.3-some-tag.4")]
    [TestCase("1.2.3-SoME-tAg.4", ExpectedResult = "1.2.3-SoME-tAg.4")]
    [TestCase("1.2.3-some-tag.4      ", ExpectedResult = "1.2.3-some-tag.4")]
    [TestCase("1.2.3-some-tag.4+build.004", ExpectedResult = "1.2.3-some-tag.4+build.004")]
    [TestCase("1.2+3.4.5-build.004", ExpectedResult = "1.2.0+3.4.5-build.004")]
    public string Constructor_FromString(string input)
    {
        // act
        ISemanticVersion version = new SemanticVersion(input);

        // assert
        return version.ToString();
    }


    /// <summary>Assert that the constructor rejects invalid values when constructed from a string.</summary>
    /// <param name="input">The version string to parse.</param>
    [Test(Description = "Assert that the constructor throws the expected exception for invalid versions.")]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("1")]
    [TestCase("01.0")]
    [TestCase("1.05")]
    [TestCase("1.5.06")] // leading zeros specifically prohibited by spec
    [TestCase("1.2.3.4")]
    [TestCase("1.apple")]
    [TestCase("1.2.apple")]
    [TestCase("1.2.3.apple")]
    [TestCase("1..2..3")]
    [TestCase("1.2.3-")]
    [TestCase("1.2.3--some-tag")]
    [TestCase("1.2.3-some-tag...")]
    [TestCase("1.2.3-some-tag...4")]
    [TestCase("1.2.3-some-tag.4+build...4")]
    [TestCase("apple")]
    [TestCase("-apple")]
    [TestCase("-5")]
    public void Constructor_FromString_WithInvalidValues(string? input)
    {
        if (input == null)
            this.AssertAndLogException<ArgumentNullException>(() => new SemanticVersion(input!));
        else
            this.AssertAndLogException<FormatException>(() => new SemanticVersion(input));
    }

    /// <summary>Assert the parsed version when constructed from a non-standard string.</summary>
    /// <param name="input">The version string to parse.</param>
    [TestCase("1.2.3", ExpectedResult = "1.2.3")]
    [TestCase("1.0.0.0", ExpectedResult = "1.0.0")]
    [TestCase("1.0.0.5", ExpectedResult = "1.0.0.5")]
    [TestCase("1.2.3.4-some-tag.4      ", ExpectedResult = "1.2.3.4-some-tag.4")]
    public string Constructor_FromString_NonStandard(string input)
    {
        // act
        ISemanticVersion version = new SemanticVersion(input, allowNonStandard: true);

        // assert
        return version.ToString();
    }

    /// <summary>Assert that the constructor rejects a non-standard string when the non-standard flag isn't set.</summary>
    /// <param name="input">The version string to parse.</param>
    [TestCase("1.0.0.0")]
    [TestCase("1.0.0.5")]
    [TestCase("1.2.3.4-some-tag.4      ")]
    public void Constructor_FromString_Standard_DisallowsNonStandardVersion(string input)
    {
        Assert.Throws<FormatException>(() => _ = new SemanticVersion(input));
    }

    /// <summary>Assert the parsed version when constructed from standard parts.</summary>
    /// <param name="major">The major number.</param>
    /// <param name="minor">The minor number.</param>
    /// <param name="patch">The patch number.</param>
    /// <param name="prerelease">The prerelease tag.</param>
    /// <param name="build">The build metadata.</param>
    [TestCase(1, 0, 0, null, null, ExpectedResult = "1.0.0")]
    [TestCase(3000, 4000, 5000, null, null, ExpectedResult = "3000.4000.5000")]
    [TestCase(1, 2, 3, "", null, ExpectedResult = "1.2.3")]
    [TestCase(1, 2, 3, "    ", null, ExpectedResult = "1.2.3")]
    [TestCase(1, 2, 3, "0", null, ExpectedResult = "1.2.3-0")]
    [TestCase(1, 2, 3, "some-tag.4", null, ExpectedResult = "1.2.3-some-tag.4")]
    [TestCase(1, 2, 3, "sOMe-TaG.4", null, ExpectedResult = "1.2.3-sOMe-TaG.4")]
    [TestCase(1, 2, 3, "some-tag.4   ", null, ExpectedResult = "1.2.3-some-tag.4")]
    [TestCase(1, 2, 3, "some-tag.4   ", "build.004", ExpectedResult = "1.2.3-some-tag.4+build.004")]
    [TestCase(1, 2, 0, null, "3.4.5-build.004", ExpectedResult = "1.2.0+3.4.5-build.004")]
    public string Constructor_FromParts(int major, int minor, int patch, string? prerelease, string? build)
    {
        // act
        ISemanticVersion version = new SemanticVersion(major, minor, patch, prerelease, build);

        // assert
        this.AssertParts(version, major, minor, patch, prerelease, build, nonStandard: false);
        return version.ToString();
    }

    /// <summary>Assert the parsed version when constructed from parts including non-standard fields.</summary>
    /// <param name="major">The major number.</param>
    /// <param name="minor">The minor number.</param>
    /// <param name="patch">The patch number.</param>
    /// <param name="platformRelease">The non-standard platform release number.</param>
    /// <param name="prerelease">The prerelease tag.</param>
    /// <param name="build">The build metadata.</param>
    [TestCase(1, 0, 0, 0, null, null, ExpectedResult = "1.0.0")]
    [TestCase(3000, 4000, 5000, 6000, null, null, ExpectedResult = "3000.4000.5000.6000")]
    [TestCase(1, 2, 3, 4, "", null, ExpectedResult = "1.2.3.4")]
    [TestCase(1, 2, 3, 4, "    ", null, ExpectedResult = "1.2.3.4")]
    [TestCase(1, 2, 3, 4, "0", null, ExpectedResult = "1.2.3.4-0")]
    [TestCase(1, 2, 3, 4, "some-tag.4", null, ExpectedResult = "1.2.3.4-some-tag.4")]
    [TestCase(1, 2, 3, 4, "sOMe-TaG.4", null, ExpectedResult = "1.2.3.4-sOMe-TaG.4")]
    [TestCase(1, 2, 3, 4, "some-tag.4   ", null, ExpectedResult = "1.2.3.4-some-tag.4")]
    [TestCase(1, 2, 3, 4, "some-tag.4   ", "build.004", ExpectedResult = "1.2.3.4-some-tag.4+build.004")]
    [TestCase(1, 2, 0, 4, null, "3.4.5-build.004", ExpectedResult = "1.2.0.4+3.4.5-build.004")]
    public string Constructor_FromParts_NonStandard(int major, int minor, int patch, int platformRelease, string prerelease, string build)
    {
        // act
        ISemanticVersion version = new SemanticVersion(major, minor, patch, platformRelease, prerelease, build);

        // assert
        this.AssertParts(version, major, minor, patch, prerelease, build, nonStandard: platformRelease != 0);
        return version.ToString();
    }

    /// <summary>Assert that the constructor rejects invalid values when constructed from the individual numbers.</summary>
    /// <param name="major">The major number.</param>
    /// <param name="minor">The minor number.</param>
    /// <param name="patch">The patch number.</param>
    /// <param name="prerelease">The prerelease tag.</param>
    /// <param name="build">The build metadata.</param>
    [TestCase(0, 0, 0, null, null)]
    [TestCase(-1, 0, 0, null, null)]
    [TestCase(0, -1, 0, null, null)]
    [TestCase(0, 0, -1, null, null)]
    [TestCase(1, 0, 0, "-tag", null)]
    [TestCase(1, 0, 0, "tag spaces", null)]
    [TestCase(1, 0, 0, "tag~", null)]
    [TestCase(1, 0, 0, null, "build~")]
    public void Constructor_FromParts_WithInvalidValues(int major, int minor, int patch, string prerelease, string build)
    {
        this.AssertAndLogException<FormatException>(() => new SemanticVersion(major, minor, patch, prerelease, build));
    }

    /// <summary>Assert the parsed version when constructed from an assembly version.</summary>
    /// <param name="major">The major number.</param>
    /// <param name="minor">The minor number.</param>
    /// <param name="patch">The patch number.</param>
    [Test(Description = "Assert that the constructor sets the expected values for all valid versions when constructed from an assembly version.")]
    [TestCase(1, 0, 0, ExpectedResult = "1.0.0")]
    [TestCase(1, 2, 3, ExpectedResult = "1.2.3")]
    [TestCase(3000, 4000, 5000, ExpectedResult = "3000.4000.5000")]
    public string Constructor_FromAssemblyVersion(int major, int minor, int patch)
    {
        // act
        ISemanticVersion version = new SemanticVersion(new Version(major, minor, patch));

        // assert
        this.AssertParts(version, major, minor, patch, null, null, nonStandard: false);
        return version.ToString();
    }

    /****
    ** CompareTo
    ****/
    /// <summary>Assert that <see cref="ISemanticVersion.CompareTo"/> returns the expected value.</summary>
    /// <param name="versionStrA">The left version.</param>
    /// <param name="versionStrB">The right version.</param>
    // equal
    [TestCase("0.5.7", "0.5.7", ExpectedResult = 0)]
    [TestCase("1.0", "1.0", ExpectedResult = 0)]
    [TestCase("1.0-beta", "1.0-beta", ExpectedResult = 0)]
    [TestCase("1.0-beta.10", "1.0-beta.10", ExpectedResult = 0)]
    [TestCase("1.0-beta", "1.0-beta   ", ExpectedResult = 0)]
    [TestCase("1.0-beta+build.001", "1.0-beta+build.001", ExpectedResult = 0)]
    [TestCase("1.0-beta+build.001", "1.0-beta+build.006", ExpectedResult = 0)] // build metadata must not affect precedence

    // less than
    [TestCase("0.5.7", "0.5.8", ExpectedResult = -1)]
    [TestCase("1.0", "1.1", ExpectedResult = -1)]
    [TestCase("1.0-beta", "1.0", ExpectedResult = -1)]
    [TestCase("1.0-beta", "1.0-beta.2", ExpectedResult = -1)]
    [TestCase("1.0-beta.1", "1.0-beta.2", ExpectedResult = -1)]
    [TestCase("1.0-beta.2", "1.0-beta.10", ExpectedResult = -1)]
    [TestCase("1.0-beta-2", "1.0-beta-10", ExpectedResult = -1)]
    [TestCase("1.0-unofficial.1", "1.0-beta.1", ExpectedResult = -1)] // special case: 'unofficial' has lower priority than official releases

    // more than
    [TestCase("0.5.8", "0.5.7", ExpectedResult = 1)]
    [TestCase("1.1", "1.0", ExpectedResult = 1)]
    [TestCase("1.0", "1.0-beta", ExpectedResult = 1)]
    [TestCase("1.0-beta.2", "1.0-beta", ExpectedResult = 1)]
    [TestCase("1.0-beta.2", "1.0-beta.1", ExpectedResult = 1)]
    [TestCase("1.0-beta.10", "1.0-beta.2", ExpectedResult = 1)]
    [TestCase("1.0-beta-10", "1.0-beta-2", ExpectedResult = 1)]

    // null
    [TestCase("1.0.0", null, ExpectedResult = 1)] // null is always less than any value per CompareTo remarks
    public int CompareTo(string versionStrA, string? versionStrB)
    {
        // arrange
        ISemanticVersion versionA = new SemanticVersion(versionStrA);
        ISemanticVersion? versionB = versionStrB != null
            ? new SemanticVersion(versionStrB)
            : null;

        // assert
        return versionA.CompareTo(versionB);
    }

    /****
    ** IsOlderThan
    ****/
    /// <summary>Assert that <see cref="ISemanticVersion.IsOlderThan(string)"/> and <see cref="ISemanticVersion.IsOlderThan(ISemanticVersion)"/> return the expected value.</summary>
    /// <param name="versionStrA">The left version.</param>
    /// <param name="versionStrB">The right version.</param>
    // keep test cases in sync with CompareTo for simplicity.
    // equal
    [TestCase("0.5.7", "0.5.7", ExpectedResult = false)]
    [TestCase("1.0", "1.0", ExpectedResult = false)]
    [TestCase("1.0-beta", "1.0-beta", ExpectedResult = false)]
    [TestCase("1.0-beta.10", "1.0-beta.10", ExpectedResult = false)]
    [TestCase("1.0-beta", "1.0-beta   ", ExpectedResult = false)]
    [TestCase("1.0-beta+build.001", "1.0-beta+build.001", ExpectedResult = false)] // build metadata must not affect precedence
    [TestCase("1.0-beta+build.001", "1.0-beta+build.006", ExpectedResult = false)] // build metadata must not affect precedence

    // less than
    [TestCase("0.5.7", "0.5.8", ExpectedResult = true)]
    [TestCase("1.0", "1.1", ExpectedResult = true)]
    [TestCase("1.0-beta", "1.0", ExpectedResult = true)]
    [TestCase("1.0-beta", "1.0-beta.2", ExpectedResult = true)]
    [TestCase("1.0-beta.1", "1.0-beta.2", ExpectedResult = true)]
    [TestCase("1.0-beta.2", "1.0-beta.10", ExpectedResult = true)]
    [TestCase("1.0-beta-2", "1.0-beta-10", ExpectedResult = true)]

    // more than
    [TestCase("0.5.8", "0.5.7", ExpectedResult = false)]
    [TestCase("1.1", "1.0", ExpectedResult = false)]
    [TestCase("1.0", "1.0-beta", ExpectedResult = false)]
    [TestCase("1.0-beta.2", "1.0-beta", ExpectedResult = false)]
    [TestCase("1.0-beta.2", "1.0-beta.1", ExpectedResult = false)]
    [TestCase("1.0-beta.10", "1.0-beta.2", ExpectedResult = false)]
    [TestCase("1.0-beta-10", "1.0-beta-2", ExpectedResult = false)]

    // null
    [TestCase("1.0.0", null, ExpectedResult = false)] // null is always less than any value per CompareTo remarks
    public bool IsOlderThan(string versionStrA, string? versionStrB)
    {
        // arrange
        ISemanticVersion versionA = new SemanticVersion(versionStrA);
        ISemanticVersion? versionB = versionStrB != null
            ? new SemanticVersion(versionStrB)
            : null;

        // assert
        Assert.AreEqual(versionA.IsOlderThan(versionB), versionA.IsOlderThan(versionB?.ToString()), "The two signatures returned different results.");
        return versionA.IsOlderThan(versionB);
    }

    /****
    ** IsNewerThan
    ****/
    /// <summary>Assert that <see cref="ISemanticVersion.IsNewerThan(string)"/> and <see cref="ISemanticVersion.IsNewerThan(ISemanticVersion)"/> return the expected value.</summary>
    /// <param name="versionStrA">The left version.</param>
    /// <param name="versionStrB">The right version.</param>
    // keep test cases in sync with CompareTo for simplicity.
    // equal
    [TestCase("0.5.7", "0.5.7", ExpectedResult = false)]
    [TestCase("1.0", "1.0", ExpectedResult = false)]
    [TestCase("1.0-beta", "1.0-beta", ExpectedResult = false)]
    [TestCase("1.0-beta.10", "1.0-beta.10", ExpectedResult = false)]
    [TestCase("1.0-beta", "1.0-beta   ", ExpectedResult = false)]
    [TestCase("1.0-beta+build.001", "1.0-beta+build.001", ExpectedResult = false)] // build metadata must not affect precedence
    [TestCase("1.0-beta+build.001", "1.0-beta+build.006", ExpectedResult = false)] // build metadata must not affect precedence

    // less than
    [TestCase("0.5.7", "0.5.8", ExpectedResult = false)]
    [TestCase("1.0", "1.1", ExpectedResult = false)]
    [TestCase("1.0-beta", "1.0", ExpectedResult = false)]
    [TestCase("1.0-beta", "1.0-beta.2", ExpectedResult = false)]
    [TestCase("1.0-beta.1", "1.0-beta.2", ExpectedResult = false)]
    [TestCase("1.0-beta.2", "1.0-beta.10", ExpectedResult = false)]
    [TestCase("1.0-beta-2", "1.0-beta-10", ExpectedResult = false)]

    // more than
    [TestCase("0.5.8", "0.5.7", ExpectedResult = true)]
    [TestCase("1.1", "1.0", ExpectedResult = true)]
    [TestCase("1.0", "1.0-beta", ExpectedResult = true)]
    [TestCase("1.0-beta.2", "1.0-beta", ExpectedResult = true)]
    [TestCase("1.0-beta.2", "1.0-beta.1", ExpectedResult = true)]
    [TestCase("1.0-beta.10", "1.0-beta.2", ExpectedResult = true)]
    [TestCase("1.0-beta-10", "1.0-beta-2", ExpectedResult = true)]

    // null
    [TestCase("1.0.0", null, ExpectedResult = true)] // null is always less than any value per CompareTo remarks
    public bool IsNewerThan(string versionStrA, string? versionStrB)
    {
        // arrange
        ISemanticVersion versionA = new SemanticVersion(versionStrA);
        ISemanticVersion? versionB = versionStrB != null
            ? new SemanticVersion(versionStrB)
            : null;

        // assert
        Assert.AreEqual(versionA.IsNewerThan(versionB), versionA.IsNewerThan(versionB?.ToString()), "The two signatures returned different results.");
        return versionA.IsNewerThan(versionB);
    }

    /****
    ** IsBetween
    ****/
    /// <summary>Assert that <see cref="ISemanticVersion.IsBetween(string, string)"/> and <see cref="ISemanticVersion.IsBetween(ISemanticVersion, ISemanticVersion)"/> return the expected value.</summary>
    /// <param name="versionStr">The main version.</param>
    /// <param name="lowerStr">The lower version number.</param>
    /// <param name="upperStr">The upper version number.</param>
    [Test(Description = "Assert that version.IsBetween returns the expected value.")]
    // is between
    [TestCase("0.5.7-beta.3", "0.5.7-beta.3", "0.5.7-beta.3", ExpectedResult = true)]
    [TestCase("1.0", "1.0", "1.1", ExpectedResult = true)]
    [TestCase("1.0", "1.0-beta", "1.1", ExpectedResult = true)]
    [TestCase("1.0", "0.5", "1.1", ExpectedResult = true)]
    [TestCase("1.0-beta.2", "1.0-beta.1", "1.0-beta.3", ExpectedResult = true)]
    [TestCase("1.0-beta-2", "1.0-beta-1", "1.0-beta-3", ExpectedResult = true)]
    [TestCase("1.0.0", null, "1.0.0", ExpectedResult = true)] // null is always less than any value per CompareTo remarks

    // is not between
    [TestCase("1.0-beta", "1.0", "1.1", ExpectedResult = false)]
    [TestCase("1.0", "1.1", "1.0", ExpectedResult = false)]
    [TestCase("1.0-beta.2", "1.1", "1.0", ExpectedResult = false)]
    [TestCase("1.0-beta.2", "1.0-beta.10", "1.0-beta.3", ExpectedResult = false)]
    [TestCase("1.0-beta-2", "1.0-beta-10", "1.0-beta-3", ExpectedResult = false)]
    [TestCase("1.0.0", "1.0.0", null, ExpectedResult = false)] // null is always less than any value per CompareTo remarks
    public bool IsBetween(string versionStr, string? lowerStr, string? upperStr)
    {
        // arrange
        ISemanticVersion? lower = lowerStr != null
            ? new SemanticVersion(lowerStr)
            : null;
        ISemanticVersion? upper = upperStr != null
            ? new SemanticVersion(upperStr)
            : null;
        ISemanticVersion version = new SemanticVersion(versionStr);

        // assert
        Assert.AreEqual(version.IsBetween(lower, upper), version.IsBetween(lower?.ToString(), upper?.ToString()), "The two signatures returned different results.");
        return version.IsBetween(lower, upper);
    }

    /****
    ** Serializable
    ****/
    /// <summary>Assert that the version can be round-tripped through JSON with no special configuration.</summary>
    /// <param name="versionStr">The semantic version.</param>
    [TestCase("1.0.0")]
    [TestCase("1.0.0-beta.400")]
    [TestCase("1.0.0-beta.400+build")]
    public void Serializable(string versionStr)
    {
        // act
        string json = JsonConvert.SerializeObject(new SemanticVersion(versionStr));
        SemanticVersion after = JsonConvert.DeserializeObject<SemanticVersion>(json);

        // assert
        Assert.IsNotNull(after, "The semantic version after deserialization is unexpectedly null.");
        Assert.AreEqual(versionStr, after.ToString(), "The semantic version after deserialization doesn't match the input version.");
    }


    /****
    ** GameVersion
    ****/
    /// <summary>Assert that the GameVersion subclass correctly parses non-standard game versions.</summary>
    /// <param name="versionStr">The raw version.</param>
    [TestCase("1.0")]
    [TestCase("1.01")]
    [TestCase("1.02")]
    [TestCase("1.03")]
    [TestCase("1.04")]
    [TestCase("1.05")]
    [TestCase("1.051")]
    [TestCase("1.051b")]
    [TestCase("1.06")]
    [TestCase("1.07")]
    [TestCase("1.07a")]
    [TestCase("1.08")]
    [TestCase("1.1")]
    [TestCase("1.11")]
    [TestCase("1.2")]
    [TestCase("1.2.15")]
    [TestCase("1.4.0.1")]
    [TestCase("1.4.0.6")]
    public void GameVersion(string versionStr)
    {
        // act
        GameVersion version = new(versionStr);

        // assert
        Assert.AreEqual(versionStr, version.ToString(), "The game version did not round-trip to the same value.");
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Assert that the version matches the expected parts.</summary>
    /// <param name="version">The version number.</param>
    /// <param name="major">The major number.</param>
    /// <param name="minor">The minor number.</param>
    /// <param name="patch">The patch number.</param>
    /// <param name="prerelease">The prerelease tag.</param>
    /// <param name="build">The build metadata.</param>
    /// <param name="nonStandard">Whether the version should be marked as non-standard.</param>
    private void AssertParts(ISemanticVersion version, int major, int minor, int patch, string? prerelease, string? build, bool nonStandard)
    {
        Assert.AreEqual(major, version.MajorVersion, "The major version doesn't match.");
        Assert.AreEqual(minor, version.MinorVersion, "The minor version doesn't match.");
        Assert.AreEqual(patch, version.PatchVersion, "The patch version doesn't match.");
        Assert.AreEqual(string.IsNullOrWhiteSpace(prerelease) ? null : prerelease.Trim(), version.PrereleaseTag, "The prerelease tag doesn't match.");
        Assert.AreEqual(string.IsNullOrWhiteSpace(build) ? null : build.Trim(), version.BuildMetadata, "The build metadata doesn't match.");
        Assert.AreEqual(nonStandard, version.IsNonStandard(), $"The version is incorrectly marked {(nonStandard ? "standard" : "non-standard")}.");
    }

    /// <summary>Assert that the expected exception type is thrown, and log the action output and thrown exception.</summary>
    /// <typeparam name="T">The expected exception type.</typeparam>
    /// <param name="action">The action which may throw the exception.</param>
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "The message argument is deliberately only used in precondition checks since this is an assertion method.")]
    private void AssertAndLogException<T>(Func<object> action)
        where T : Exception
    {
        this.AssertAndLogException<T>(() =>
        {
            object result = action();
            TestContext.WriteLine($"Func result: {result}");
        });
    }

    /// <summary>Assert that the expected exception type is thrown, and log the thrown exception.</summary>
    /// <typeparam name="T">The expected exception type.</typeparam>
    /// <param name="action">The action which may throw the exception.</param>
    /// <param name="message">The message to log if the expected exception isn't thrown.</param>
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "The message argument is deliberately only used in precondition checks since this is an assertion method.")]
    private void AssertAndLogException<T>(Action action, string? message = null)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex)
        {
            TestContext.WriteLine($"Exception thrown:\n{ex}");
            return;
        }
        catch (Exception ex) when (ex is not AssertionException)
        {
            TestContext.WriteLine($"Exception thrown:\n{ex}");
            Assert.Fail(message ?? $"Didn't throw the expected exception; expected {typeof(T).FullName}, got {ex.GetType().FullName}.");
        }

        // no exception thrown
        Assert.Fail(message ?? "Didn't throw an exception.");
    }
}