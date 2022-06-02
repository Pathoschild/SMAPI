using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using StardewModdingAPI.Toolkit.Framework;

namespace StardewModdingAPI.Toolkit;

/// <summary>A semantic version with an optional release tag.</summary>
/// <remarks>
/// The implementation is defined by Semantic Version 2.0 (https://semver.org/), with a few deviations:
/// - short-form "x.y" versions are supported (equivalent to "x.y.0");
/// - hyphens are synonymous with dots in prerelease tags and build metadata (like "-unofficial.3-pathoschild");
/// - and "-unofficial" in prerelease tags is always lower-precedence (e.g. "1.0-beta" is newer than "1.0-unofficial").
///
/// This optionally also supports four-part versions, a non-standard extension used by Stardew Valley on ported platforms to represent platform-specific patches to a ported version, represented as a fourth number in the version string.
/// </remarks>
public class SemanticVersion : ISemanticVersion
{
    /*********
    ** Fields
    *********/
    /// <summary>A regex pattern matching a valid prerelease or build metadata tag.</summary>
    private const string TagPattern = @"(?>[a-z0-9]+[\-\.]?)+";


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public int MajorVersion { get; }

    /// <inheritdoc />
    public int MinorVersion { get; }

    /// <inheritdoc />
    public int PatchVersion { get; }

    /// <summary>The platform release. This is a non-standard semver extension used by Stardew Valley on ported platforms to represent platform-specific patches to a ported version, represented as a fourth number in the version string.</summary>
    public int PlatformRelease { get; }

    /// <inheritdoc />
    public string? PrereleaseTag { get; }

    /// <inheritdoc />
    public string? BuildMetadata { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="major">The major version incremented for major API changes.</param>
    /// <param name="minor">The minor version incremented for backwards-compatible changes.</param>
    /// <param name="patch">The patch version for backwards-compatible fixes.</param>
    /// <param name="platformRelease">The platform-specific version (if applicable).</param>
    /// <param name="prereleaseTag">An optional prerelease tag.</param>
    /// <param name="buildMetadata">Optional build metadata. This is ignored when determining version precedence.</param>
    public SemanticVersion(int major, int minor, int patch, int platformRelease = 0, string? prereleaseTag = null, string? buildMetadata = null)
    {
        this.MajorVersion = major;
        this.MinorVersion = minor;
        this.PatchVersion = patch;
        this.PlatformRelease = platformRelease;
        this.PrereleaseTag = this.GetNormalizedTag(prereleaseTag);
        this.BuildMetadata = this.GetNormalizedTag(buildMetadata);

        this.AssertValid();
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="version">The assembly version.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="version"/> is null.</exception>
    public SemanticVersion(Version version)
    {
        if (version == null)
            throw new ArgumentNullException(nameof(version), "The input version can't be null.");

        this.MajorVersion = version.Major;
        this.MinorVersion = version.Minor;
        this.PatchVersion = version.Build;

        this.AssertValid();
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="version">The semantic version string.</param>
    /// <param name="allowNonStandard">Whether to recognize non-standard semver extensions.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="version"/> is null.</exception>
    /// <exception cref="FormatException">The <paramref name="version"/> is not a valid semantic version.</exception>
    public SemanticVersion(string version, bool allowNonStandard = false)
    {
        if (version == null)
            throw new ArgumentNullException(nameof(version), "The input version string can't be null.");
        if (!SemanticVersionReader.TryParse(version, allowNonStandard, out int major, out int minor, out int patch, out int platformRelease, out string? prereleaseTag, out string? buildMetadata) || (!allowNonStandard && platformRelease != 0))
            throw new FormatException($"The input '{version}' isn't a valid semantic version.");

        this.MajorVersion = major;
        this.MinorVersion = minor;
        this.PatchVersion = patch;
        this.PlatformRelease = platformRelease;
        this.PrereleaseTag = prereleaseTag;
        this.BuildMetadata = buildMetadata;

        this.AssertValid();
    }

    /// <inheritdoc />
    public int CompareTo(ISemanticVersion? other)
    {
        return other == null
            ? 1
            : this.CompareTo(other.MajorVersion, other.MinorVersion, other.PatchVersion, (other as SemanticVersion)?.PlatformRelease ?? 0, other.PrereleaseTag);
    }

    /// <inheritdoc />
    public bool Equals(ISemanticVersion? other)
    {
        return other != null && this.CompareTo(other) == 0;
    }

    /// <inheritdoc />
#if NET5_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(SemanticVersion.PrereleaseTag))]
#endif
    public bool IsPrerelease()
    {
        return !string.IsNullOrWhiteSpace(this.PrereleaseTag);
    }

    /// <inheritdoc />
    public bool IsOlderThan(ISemanticVersion? other)
    {
        return this.CompareTo(other) < 0;
    }

    /// <inheritdoc />
    public bool IsOlderThan(string? other)
    {
        ISemanticVersion? otherVersion = other != null
            ? new SemanticVersion(other, allowNonStandard: true)
            : null;

        return this.IsOlderThan(otherVersion);
    }

    /// <inheritdoc />
    public bool IsNewerThan(ISemanticVersion? other)
    {
        return this.CompareTo(other) > 0;
    }

    /// <inheritdoc />
    public bool IsNewerThan(string? other)
    {
        ISemanticVersion? otherVersion = other != null
            ? new SemanticVersion(other, allowNonStandard: true)
            : null;

        return this.IsNewerThan(otherVersion);
    }

    /// <inheritdoc />
    public bool IsBetween(ISemanticVersion? min, ISemanticVersion? max)
    {
        return this.CompareTo(min) >= 0 && this.CompareTo(max) <= 0;
    }

    /// <inheritdoc />
    public bool IsBetween(string? min, string? max)
    {
        ISemanticVersion? minVersion = min != null
            ? new SemanticVersion(min, allowNonStandard: true)
            : null;
        ISemanticVersion? maxVersion = max != null
            ? new SemanticVersion(max, allowNonStandard: true)
            : null;

        return this.IsBetween(minVersion, maxVersion);
    }

    /// <inheritdoc cref="ISemanticVersion.ToString" />
    public override string ToString()
    {
        string version = $"{this.MajorVersion}.{this.MinorVersion}.{this.PatchVersion}";
        if (this.PlatformRelease != 0)
            version += $".{this.PlatformRelease}";
        if (this.PrereleaseTag != null)
            version += $"-{this.PrereleaseTag}";
        if (this.BuildMetadata != null)
            version += $"+{this.BuildMetadata}";
        return version;
    }

    /// <inheritdoc />
    public bool IsNonStandard()
    {
        return this.PlatformRelease != 0;
    }

    /// <summary>Parse a version string without throwing an exception if it fails.</summary>
    /// <param name="version">The version string.</param>
    /// <param name="parsed">The parsed representation.</param>
    /// <returns>Returns whether parsing the version succeeded.</returns>
    public static bool TryParse(string? version,
#if NET5_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        out ISemanticVersion? parsed
    )
    {
        return SemanticVersion.TryParse(version, allowNonStandard: false, out parsed);
    }

    /// <summary>Parse a version string without throwing an exception if it fails.</summary>
    /// <param name="version">The version string.</param>
    /// <param name="allowNonStandard">Whether to allow non-standard extensions to semantic versioning.</param>
    /// <param name="parsed">The parsed representation.</param>
    /// <returns>Returns whether parsing the version succeeded.</returns>
    public static bool TryParse(string? version, bool allowNonStandard,
#if NET5_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        out ISemanticVersion? parsed
    )
    {
        if (version == null)
        {
            parsed = null;
            return false;
        }

        try
        {
            parsed = new SemanticVersion(version, allowNonStandard);
            return true;
        }
        catch
        {
            parsed = null;
            return false;
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get a normalized prerelease or build tag.</summary>
    /// <param name="tag">The tag to normalize.</param>
    private string? GetNormalizedTag(string? tag)
    {
        tag = tag?.Trim();
        return !string.IsNullOrWhiteSpace(tag) ? tag : null;
    }

    /// <summary>Get an integer indicating whether this version precedes (less than 0), supersedes (more than 0), or is equivalent to (0) the specified version.</summary>
    /// <param name="otherMajor">The major version to compare with this instance.</param>
    /// <param name="otherMinor">The minor version to compare with this instance.</param>
    /// <param name="otherPatch">The patch version to compare with this instance.</param>
    /// <param name="otherPlatformRelease">The non-standard platform release to compare with this instance.</param>
    /// <param name="otherTag">The prerelease tag to compare with this instance.</param>
    private int CompareTo(int otherMajor, int otherMinor, int otherPatch, int otherPlatformRelease, string? otherTag)
    {
        const int same = 0;
        const int curNewer = 1;
        const int curOlder = -1;

        int CompareToRaw()
        {
            // compare stable versions
            if (this.MajorVersion != otherMajor)
                return this.MajorVersion.CompareTo(otherMajor);
            if (this.MinorVersion != otherMinor)
                return this.MinorVersion.CompareTo(otherMinor);
            if (this.PatchVersion != otherPatch)
                return this.PatchVersion.CompareTo(otherPatch);
            if (this.PlatformRelease != otherPlatformRelease)
                return this.PlatformRelease.CompareTo(otherPlatformRelease);
            if (this.PrereleaseTag == otherTag)
                return same;

            // stable supersedes prerelease
            bool curIsStable = string.IsNullOrWhiteSpace(this.PrereleaseTag);
            bool otherIsStable = string.IsNullOrWhiteSpace(otherTag);
            if (curIsStable)
                return curNewer;
            if (otherIsStable)
                return curOlder;

            // compare two prerelease tag values
            string[] curParts = this.PrereleaseTag?.Split('.', '-') ?? Array.Empty<string>();
            string[] otherParts = otherTag?.Split('.', '-') ?? Array.Empty<string>();
            int length = Math.Max(curParts.Length, otherParts.Length);
            for (int i = 0; i < length; i++)
            {
                // longer prerelease tag supersedes if otherwise equal
                if (curParts.Length <= i)
                    return curOlder;
                if (otherParts.Length <= i)
                    return curNewer;

                // skip if same value, unless we've reached the end
                if (curParts[i] == otherParts[i])
                {
                    if (i == length - 1)
                        return same;

                    continue;
                }

                // unofficial is always lower-precedence
                if (otherParts[i].Equals("unofficial", StringComparison.OrdinalIgnoreCase))
                    return curNewer;
                if (curParts[i].Equals("unofficial", StringComparison.OrdinalIgnoreCase))
                    return curOlder;

                // compare numerically if possible
                {
                    if (int.TryParse(curParts[i], out int curNum) && int.TryParse(otherParts[i], out int otherNum))
                        return curNum.CompareTo(otherNum);
                }

                // else compare lexically
                return string.Compare(curParts[i], otherParts[i], StringComparison.OrdinalIgnoreCase);
            }

            // fallback (this should never happen)
            return string.Compare(this.ToString(), new SemanticVersion(otherMajor, otherMinor, otherPatch, otherPlatformRelease, otherTag).ToString(), StringComparison.OrdinalIgnoreCase);
        }

        int result = CompareToRaw();
        if (result < 0)
            return curOlder;
        if (result > 0)
            return curNewer;
        return same;
    }

    /// <summary>Assert that the current version is valid.</summary>
    private void AssertValid()
    {
        if (this.MajorVersion < 0 || this.MinorVersion < 0 || this.PatchVersion < 0)
            throw new FormatException($"{this} isn't a valid semantic version. The major, minor, and patch numbers can't be negative.");
        if (this.MajorVersion == 0 && this.MinorVersion == 0 && this.PatchVersion == 0)
            throw new FormatException($"{this} isn't a valid semantic version. At least one of the major, minor, and patch numbers must be more than zero.");

        if (this.PrereleaseTag != null)
        {
            if (this.PrereleaseTag.Trim() == "")
                throw new FormatException($"{this} isn't a valid semantic version. The prerelease tag cannot be a blank string (but may be omitted).");
            if (!Regex.IsMatch(this.PrereleaseTag, $"^{SemanticVersion.TagPattern}$", RegexOptions.IgnoreCase))
                throw new FormatException($"{this} isn't a valid semantic version. The prerelease tag is invalid.");
        }

        if (this.BuildMetadata != null)
        {
            if (this.BuildMetadata.Trim() == "")
                throw new FormatException($"{this} isn't a valid semantic version. The build metadata cannot be a blank string (but may be omitted).");
            if (!Regex.IsMatch(this.BuildMetadata, $"^{SemanticVersion.TagPattern}$", RegexOptions.IgnoreCase))
                throw new FormatException($"{this} isn't a valid semantic version. The build metadata is invalid.");
        }
    }
}