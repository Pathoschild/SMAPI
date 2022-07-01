using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Toolkit.Framework;

/// <summary>Reads strings into a semantic version.</summary>
internal static class SemanticVersionReader
{
    /*********
    ** Public methods
    *********/
    /// <summary>Parse a semantic version string.</summary>
    /// <param name="versionStr">The version string to parse.</param>
    /// <param name="allowNonStandard">Whether to recognize non-standard semver extensions.</param>
    /// <param name="major">The major version incremented for major API changes.</param>
    /// <param name="minor">The minor version incremented for backwards-compatible changes.</param>
    /// <param name="patch">The patch version for backwards-compatible fixes.</param>
    /// <param name="platformRelease">The platform-specific version (if applicable).</param>
    /// <param name="prereleaseTag">An optional prerelease tag.</param>
    /// <param name="buildMetadata">Optional build metadata. This is ignored when determining version precedence.</param>
    /// <returns>Returns whether the version was successfully parsed.</returns>
    public static bool TryParse(string? versionStr, bool allowNonStandard, out int major, out int minor, out int patch, out int platformRelease, out string? prereleaseTag, out string? buildMetadata)
    {
        // init
        major = 0;
        minor = 0;
        patch = 0;
        platformRelease = 0;
        prereleaseTag = null;
        buildMetadata = null;

        // normalize
        versionStr = versionStr?.Trim();
        if (string.IsNullOrWhiteSpace(versionStr))
            return false;
        char[] raw = versionStr.ToCharArray();

        // read major/minor version
        int i = 0;
        if (!TryParseVersionPart(raw, ref i, out major) || !TryParseLiteral(raw, ref i, '.') || !TryParseVersionPart(raw, ref i, out minor))
            return false;

        // read optional patch version
        if (TryParseLiteral(raw, ref i, '.') && !TryParseVersionPart(raw, ref i, out patch))
            return false;

        // read optional non-standard platform release version
        if (allowNonStandard && TryParseLiteral(raw, ref i, '.') && !TryParseVersionPart(raw, ref i, out platformRelease))
            return false;

        // read optional prerelease tag
        if (TryParseLiteral(raw, ref i, '-') && !TryParseTag(raw, ref i, out prereleaseTag))
            return false;

        // read optional build tag
        if (TryParseLiteral(raw, ref i, '+') && !TryParseTag(raw, ref i, out buildMetadata))
            return false;

        // validate
        return i == versionStr.Length; // valid if we're at the end
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Try to parse the next characters in a queue as a numeric part.</summary>
    /// <param name="raw">The raw characters to parse.</param>
    /// <param name="index">The index of the next character to read.</param>
    /// <param name="part">The parsed part.</param>
    private static bool TryParseVersionPart(char[] raw, ref int index, out int part)
    {
        part = 0;

        // take digits
        string str = "";
        for (int i = index; i < raw.Length && char.IsDigit(raw[i]); i++)
            str += raw[i];

        // validate
        if (str.Length == 0)
            return false;
        if (str.Length > 1 && str[0] == '0')
            return false; // can't have leading zeros

        // parse
        part = int.Parse(str);
        index += str.Length;
        return true;
    }

    /// <summary>Try to parse a literal character.</summary>
    /// <param name="raw">The raw characters to parse.</param>
    /// <param name="index">The index of the next character to read.</param>
    /// <param name="ch">The expected character.</param>
    private static bool TryParseLiteral(char[] raw, ref int index, char ch)
    {
        if (index >= raw.Length || raw[index] != ch)
            return false;

        index++;
        return true;
    }

    /// <summary>Try to parse a tag.</summary>
    /// <param name="raw">The raw characters to parse.</param>
    /// <param name="index">The index of the next character to read.</param>
    /// <param name="tag">The parsed tag.</param>
    private static bool TryParseTag(char[] raw, ref int index,
#if NET5_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        out string? tag
    )
    {
        // read tag length
        int length = 0;
        for (int i = index; i < raw.Length && (char.IsLetterOrDigit(raw[i]) || raw[i] == '-' || raw[i] == '.'); i++)
            length++;

        // validate
        if (length == 0)
        {
            tag = null;
            return false;
        }

        // parse
        tag = new string(raw, index, length);
        index += length;
        return true;
    }
}
