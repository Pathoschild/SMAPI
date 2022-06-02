using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework;

/// <summary>An extension of <see cref="ISemanticVersion"/> that correctly handles non-semantic versions used by Stardew Valley.</summary>
internal class GameVersion : Toolkit.SemanticVersion
{
    /*********
    ** Private methods
    *********/
    /// <summary>A mapping of game to semantic versions.</summary>
    private static readonly IDictionary<string, string> VersionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["1.0"] = "1.0.0",
        ["1.01"] = "1.0.1",
        ["1.02"] = "1.0.2",
        ["1.03"] = "1.0.3",
        ["1.04"] = "1.0.4",
        ["1.05"] = "1.0.5",
        ["1.051"] = "1.0.5.1",
        ["1.051b"] = "1.0.5.2",
        ["1.06"] = "1.0.6",
        ["1.07"] = "1.0.7",
        ["1.07a"] = "1.0.7.1",
        ["1.08"] = "1.0.8",
        ["1.1"] = "1.1.0",
        ["1.2"] = "1.2.0",
        ["1.11"] = "1.1.1"
    };


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="version">The game version string.</param>
    public GameVersion(string version)
        : base(GameVersion.GetSemanticVersionString(version), allowNonStandard: true) { }

    /// <inheritdoc />
    public override string ToString()
    {
        return GameVersion.GetGameVersionString(base.ToString());
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Convert a game version string to a semantic version string.</summary>
    /// <param name="gameVersion">The game version string.</param>
    private static string GetSemanticVersionString(string gameVersion)
    {
        // mapped version
        return GameVersion.VersionMap.TryGetValue(gameVersion, out string? semanticVersion)
            ? semanticVersion
            : gameVersion;
    }

    /// <summary>Convert a semantic version string to the equivalent game version string.</summary>
    /// <param name="semanticVersion">The semantic version string.</param>
    private static string GetGameVersionString(string semanticVersion)
    {
        foreach ((string gameVersion, string equivalentSemanticVersion) in GameVersion.VersionMap)
        {
            if (equivalentSemanticVersion.Equals(semanticVersion, StringComparison.OrdinalIgnoreCase))
                return gameVersion;
        }

        return semanticVersion;
    }
}
