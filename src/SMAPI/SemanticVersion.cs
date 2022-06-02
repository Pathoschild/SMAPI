using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace StardewModdingAPI;

/// <summary>A semantic version with an optional release tag.</summary>
public class SemanticVersion : ISemanticVersion
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying semantic version implementation.</summary>
    private readonly ISemanticVersion Version;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public int MajorVersion => this.Version.MajorVersion;

    /// <inheritdoc />
    public int MinorVersion => this.Version.MinorVersion;

    /// <inheritdoc />
    public int PatchVersion => this.Version.PatchVersion;

    /// <inheritdoc />
    public string? PrereleaseTag => this.Version.PrereleaseTag;

    /// <inheritdoc />
    public string? BuildMetadata => this.Version.BuildMetadata;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="majorVersion">The major version incremented for major API changes.</param>
    /// <param name="minorVersion">The minor version incremented for backwards-compatible changes.</param>
    /// <param name="patchVersion">The patch version for backwards-compatible bug fixes.</param>
    /// <param name="prereleaseTag">An optional prerelease tag.</param>
    /// <param name="buildMetadata">Optional build metadata. This is ignored when determining version precedence.</param>
    public SemanticVersion(int majorVersion, int minorVersion, int patchVersion, string? prereleaseTag = null, string? buildMetadata = null)
        : this(majorVersion, minorVersion, patchVersion, 0, prereleaseTag, buildMetadata) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="majorVersion">The major version incremented for major API changes.</param>
    /// <param name="minorVersion">The minor version incremented for backwards-compatible changes.</param>
    /// <param name="patchVersion">The patch version for backwards-compatible bug fixes.</param>
    /// <param name="prereleaseTag">An optional prerelease tag.</param>
    /// <param name="platformRelease">The platform-specific version (if applicable).</param>
    /// <param name="buildMetadata">Optional build metadata. This is ignored when determining version precedence.</param>
    [JsonConstructor]
    internal SemanticVersion(int majorVersion, int minorVersion, int patchVersion, int platformRelease, string? prereleaseTag = null, string? buildMetadata = null)
        : this(new Toolkit.SemanticVersion(majorVersion, minorVersion, patchVersion, platformRelease, prereleaseTag, buildMetadata)) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="version">The semantic version string.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="version"/> is null.</exception>
    /// <exception cref="FormatException">The <paramref name="version"/> is not a valid semantic version.</exception>
    public SemanticVersion(string version)
        : this(version, allowNonStandard: false) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="version">The semantic version string.</param>
    /// <param name="allowNonStandard">Whether to recognize non-standard semver extensions.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="version"/> is null.</exception>
    /// <exception cref="FormatException">The <paramref name="version"/> is not a valid semantic version.</exception>
    internal SemanticVersion(string version, bool allowNonStandard)
        : this(new Toolkit.SemanticVersion(version, allowNonStandard)) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="version">The assembly version.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="version"/> is null.</exception>
    public SemanticVersion(Version version)
        : this(new Toolkit.SemanticVersion(version)) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="version">The underlying semantic version implementation.</param>
    internal SemanticVersion(ISemanticVersion version)
    {
        this.Version = version;
    }

    /// <inheritdoc />
#if NET5_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(SemanticVersion.PrereleaseTag))]
#endif
    public bool IsPrerelease()
    {
        return this.Version.IsPrerelease();
    }

    /// <inheritdoc />
    /// <remarks>The implementation is defined by Semantic Version 2.0 (https://semver.org/).</remarks>
    public int CompareTo(ISemanticVersion? other)
    {
        return this.Version.CompareTo(other);
    }

    /// <inheritdoc />
    public bool IsOlderThan(ISemanticVersion? other)
    {
        return this.Version.IsOlderThan(other);
    }

    /// <inheritdoc />
    public bool IsOlderThan(string? other)
    {
        return this.Version.IsOlderThan(other);
    }

    /// <inheritdoc />
    public bool IsNewerThan(ISemanticVersion? other)
    {
        return this.Version.IsNewerThan(other);
    }

    /// <inheritdoc />
    public bool IsNewerThan(string? other)
    {
        return this.Version.IsNewerThan(other);
    }

    /// <inheritdoc />
    public bool IsBetween(ISemanticVersion? min, ISemanticVersion? max)
    {
        return this.Version.IsBetween(min, max);
    }

    /// <inheritdoc />
    public bool IsBetween(string? min, string? max)
    {
        return this.Version.IsBetween(min, max);
    }

    /// <inheritdoc />
    public bool Equals(ISemanticVersion? other)
    {
        return other != null && this.CompareTo(other) == 0;
    }

    /// <inheritdoc cref="ISemanticVersion.ToString" />
    public override string ToString()
    {
        return this.Version.ToString();
    }

    /// <inheritdoc />
    public bool IsNonStandard()
    {
        return this.Version.IsNonStandard();
    }

    /// <summary>Parse a version string without throwing an exception if it fails.</summary>
    /// <param name="version">The version string.</param>
    /// <param name="parsed">The parsed representation.</param>
    /// <returns>Returns whether parsing the version succeeded.</returns>
    public static bool TryParse(string version, [NotNullWhen(true)] out ISemanticVersion? parsed)
    {
        if (Toolkit.SemanticVersion.TryParse(version, out ISemanticVersion? versionImpl))
        {
            parsed = new SemanticVersion(versionImpl);
            return true;
        }

        parsed = null;
        return false;
    }
}