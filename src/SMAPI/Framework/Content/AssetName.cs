using System;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;

namespace StardewModdingAPI.Framework.Content;

/// <summary>An asset name that can be loaded through the content pipeline.</summary>
internal class AssetName : IAssetName
{
    /*********
    ** Fields
    *********/
    /// <summary>A lowercase version of <see cref="Name"/> used for consistent hash codes and equality checks.</summary>
    private readonly string ComparableName;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string BaseName { get; }

    /// <inheritdoc />
    public string? LocaleCode { get; }

    /// <inheritdoc />
    public LocalizedContentManager.LanguageCode? LanguageCode { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="baseName">The base asset name without the locale code.</param>
    /// <param name="localeCode">The locale code specified in the <see cref="Name"/>, if it's a valid code recognized by the game content.</param>
    /// <param name="languageCode">The language code matching the <see cref="LocaleCode"/>, if applicable.</param>
    public AssetName(string baseName, string? localeCode, LocalizedContentManager.LanguageCode? languageCode)
    {
        // validate
        if (string.IsNullOrWhiteSpace(baseName))
            throw new ArgumentException("The asset name can't be null or empty.", nameof(baseName));
        if (string.IsNullOrWhiteSpace(localeCode))
            localeCode = null;

        // set base values
        this.BaseName = PathUtilities.NormalizeAssetName(baseName);
        this.LocaleCode = localeCode;
        this.LanguageCode = languageCode;

        // set derived values
        this.Name = localeCode != null
            ? string.Concat(this.BaseName, '.', this.LocaleCode)
            : this.BaseName;
        this.ComparableName = this.Name.ToLowerInvariant();
    }

    /// <summary>Parse a raw asset name into an instance.</summary>
    /// <param name="rawName">The raw asset name to parse.</param>
    /// <param name="parseLocale">Get the language code for a given locale, if it's valid.</param>
    /// <exception cref="ArgumentException">The <paramref name="rawName"/> is null or empty.</exception>
    public static AssetName Parse(string rawName, Func<string, LocalizedContentManager.LanguageCode?> parseLocale)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            throw new ArgumentException("The asset name can't be null or empty.", nameof(rawName));

        string baseName = rawName;
        string? localeCode = null;
        LocalizedContentManager.LanguageCode? languageCode = null;

        int lastPeriodIndex = rawName.LastIndexOf('.');
        if (lastPeriodIndex > 0 && rawName.Length > lastPeriodIndex + 1)
        {
            string possibleLocaleCode = rawName[(lastPeriodIndex + 1)..];
            LocalizedContentManager.LanguageCode? possibleLanguageCode = parseLocale(possibleLocaleCode);

            if (possibleLanguageCode != null)
            {
                baseName = rawName[..lastPeriodIndex];
                localeCode = possibleLocaleCode;
                languageCode = possibleLanguageCode;
            }
        }

        return new AssetName(baseName, localeCode, languageCode);
    }

    /// <inheritdoc />
    public bool IsEquivalentTo(string? assetName, bool useBaseName = false)
    {
        // empty asset key is never equivalent
        if (string.IsNullOrWhiteSpace(assetName))
            return false;

        assetName = PathUtilities.NormalizeAssetName(assetName);

        string compareTo = useBaseName ? this.BaseName : this.Name;
        return compareTo.Equals(assetName, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool IsEquivalentTo(IAssetName? assetName, bool useBaseName = false)
    {
        if (useBaseName)
            return this.BaseName.Equals(assetName?.BaseName, StringComparison.OrdinalIgnoreCase);

        if (assetName is AssetName impl)
            return this.ComparableName == impl.ComparableName;

        return this.Name.Equals(assetName?.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool StartsWith(string? prefix, bool allowPartialWord = true, bool allowSubfolder = true)
    {
        // asset keys never start with null
        if (prefix is null)
            return false;

        string rawTrimmed = prefix.Trim();

        // asset keys can't have a leading slash, but NormalizeAssetName will trim them
        if (rawTrimmed.StartsWith('/') || rawTrimmed.StartsWith('\\'))
            return false;

        // normalize prefix
        {
            string normalized = PathUtilities.NormalizeAssetName(prefix);

            // keep trailing slash
            if (rawTrimmed.EndsWith('/') || rawTrimmed.EndsWith('\\'))
                normalized += PathUtilities.PreferredAssetSeparator;

            prefix = normalized;
        }

        // compare
        if (prefix.Length == 0)
            return true;

        return
            this.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && (
                allowPartialWord
                || this.Name.Length == prefix.Length
                || !char.IsLetterOrDigit(prefix[^1]) // last character in suffix is word separator
                || !char.IsLetterOrDigit(this.Name[prefix.Length]) // or first character after it is
            )
            && (
                allowSubfolder
                || this.Name.Length == prefix.Length
                || !this.Name[prefix.Length..].Contains(PathUtilities.PreferredAssetSeparator)
            );
    }


    /// <inheritdoc />
    public bool IsDirectlyUnderPath(string? assetFolder)
    {
        if (assetFolder is null)
            return false;

        return this.StartsWith(assetFolder + "/", allowPartialWord: false, allowSubfolder: false);
    }

    /// <inheritdoc />
    IAssetName IAssetName.GetBaseAssetName()
    {
        return this.LocaleCode == null
            ? this
            : new AssetName(this.BaseName, null, null);
    }

    /// <inheritdoc />
    public bool Equals(IAssetName? other)
    {
        return other switch
        {
            null => false,
            AssetName otherImpl => this.ComparableName == otherImpl.ComparableName,
            _ => StringComparer.OrdinalIgnoreCase.Equals(this.Name, other.Name)
        };
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return this.ComparableName.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Name;
    }
}