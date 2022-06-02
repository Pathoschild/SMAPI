using System;
using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI;

/// <summary>A semantic version with an optional release tag.</summary>
public interface ISemanticVersion : IComparable<ISemanticVersion>, IEquatable<ISemanticVersion>
{
    /*********
    ** Accessors
    *********/
    /// <summary>The major version incremented for major API changes.</summary>
    int MajorVersion { get; }

    /// <summary>The minor version incremented for backwards-compatible changes.</summary>
    int MinorVersion { get; }

    /// <summary>The patch version for backwards-compatible bug fixes.</summary>
    int PatchVersion { get; }

    /// <summary>An optional prerelease tag.</summary>
    string? PrereleaseTag { get; }

    /// <summary>Optional build metadata. This is ignored when determining version precedence.</summary>
    string? BuildMetadata { get; }


    /*********
    ** Accessors
    *********/
    /// <summary>Whether this is a prerelease version.</summary>
#if NET5_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(ISemanticVersion.PrereleaseTag))]
#endif
    bool IsPrerelease();

    /// <summary>Get whether this version is older than the specified version.</summary>
    /// <param name="other">The version to compare with this instance.</param>
    /// <remarks>Although the <paramref name="other"/> parameter is nullable, it isn't optional. A <c>null</c> version is considered earlier than every possible valid version, so passing <c>null</c> to <paramref name="other"/> will always return false.</remarks>
    bool IsOlderThan(ISemanticVersion? other);

    /// <summary>Get whether this version is older than the specified version.</summary>
    /// <param name="other">The version to compare with this instance.  A null value is never older.</param>
    /// <exception cref="FormatException">The specified version is not a valid semantic version.</exception>
    /// <remarks>Although the <paramref name="other"/> parameter is nullable, it isn't optional. A <c>null</c> version is considered earlier than every possible valid version, so passing <c>null</c> to <paramref name="other"/> will always return false.</remarks>
    bool IsOlderThan(string? other);

    /// <summary>Get whether this version is newer than the specified version.</summary>
    /// <param name="other">The version to compare with this instance. A null value is always older.</param>
    /// <remarks>Although the <paramref name="other"/> parameter is nullable, it isn't optional. A <c>null</c> version is considered earlier than every possible valid version, so passing <c>null</c> to <paramref name="other"/> will always return true.</remarks>
    bool IsNewerThan(ISemanticVersion? other);

    /// <summary>Get whether this version is newer than the specified version.</summary>
    /// <param name="other">The version to compare with this instance. A null value is always older.</param>
    /// <exception cref="FormatException">The specified version is not a valid semantic version.</exception>
    /// <remarks>Although the <paramref name="other"/> parameter is nullable, it isn't optional. A <c>null</c> version is considered earlier than every possible valid version, so passing <c>null</c> to <paramref name="other"/> will always return true.</remarks>
    bool IsNewerThan(string? other);

    /// <summary>Get whether this version is between two specified versions (inclusively).</summary>
    /// <param name="min">The minimum version. A null value is always older.</param>
    /// <param name="max">The maximum version. A null value is never newer.</param>
    /// <remarks>Although the <paramref name="min"/> and <paramref name="max"/> parameters are nullable, they are not optional. A <c>null</c> version is considered earlier than every possible valid version. For example, passing <c>null</c> to <paramref name="max"/> will always return false, since no valid version can be earlier than <c>null</c>.</remarks>
    bool IsBetween(ISemanticVersion? min, ISemanticVersion? max);

    /// <summary>Get whether this version is between two specified versions (inclusively).</summary>
    /// <param name="min">The minimum version. A null value is always older.</param>
    /// <param name="max">The maximum version. A null value is never newer.</param>
    /// <exception cref="FormatException">One of the specified versions is not a valid semantic version.</exception>
    /// <remarks>Although the <paramref name="min"/> and <paramref name="max"/> parameters are nullable, they are not optional. A <c>null</c> version is considered earlier than every possible valid version. For example, passing <c>null</c> to <paramref name="max"/> will always return false, since no valid version can be earlier than <c>null</c>.</remarks>
    bool IsBetween(string? min, string? max);

    /// <summary>Get a string representation of the version.</summary>
    string ToString();

    /// <summary>Whether the version uses non-standard extensions, like four-part game versions on some platforms.</summary>
    bool IsNonStandard();
}
