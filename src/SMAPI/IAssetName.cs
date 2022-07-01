using System;
using StardewValley;

namespace StardewModdingAPI;

/// <summary>The name for an asset loaded through the content pipeline.</summary>
public interface IAssetName : IEquatable<IAssetName>
{
    /*********
    ** Accessors
    *********/
    /// <summary>The full normalized asset name, including the locale if applicable (like <c>Data/Achievements.fr-FR</c>).</summary>
    string Name { get; }

    /// <summary>The base asset name without the locale code.</summary>
    string BaseName { get; }

    /// <summary>The locale code specified in the <see cref="Name"/>, if it's a valid code recognized by the game content.</summary>
    string? LocaleCode { get; }

    /// <summary>The language code matching the <see cref="LocaleCode"/>, if applicable.</summary>
    LocalizedContentManager.LanguageCode? LanguageCode { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Get whether the given asset name is equivalent, ignoring capitalization and formatting.</summary>
    /// <param name="assetName">The asset name to compare this instance to.</param>
    /// <param name="useBaseName">Whether to compare the given name with the <see cref="BaseName"/> (if true) or <see cref="Name"/> (if false). This has no effect on any locale included in the given <paramref name="assetName"/>.</param>
    bool IsEquivalentTo(string? assetName, bool useBaseName = false);

    /// <summary>Get whether the given asset name is equivalent, ignoring capitalization and formatting.</summary>
    /// <param name="assetName">The asset name to compare this instance to.</param>
    /// <param name="useBaseName">Whether to compare the given name with the <see cref="BaseName"/> (if true) or <see cref="Name"/> (if false).</param>
    bool IsEquivalentTo(IAssetName? assetName, bool useBaseName = false);

    /// <summary>Get whether the asset name starts with the given value, ignoring capitalization and formatting. This can be used with a trailing slash to test for an asset folder, like <c>Data/</c>.</summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <param name="allowPartialWord">Whether to match if the prefix occurs mid-word, so <c>Data/AchievementsToIgnore</c> matches prefix <c>Data/Achievements</c>. If this is false, the prefix only matches if the asset name starts with the prefix followed by a non-alphanumeric character (including <c>.</c>, <c>/</c>, or <c>\\</c>) or the end of string.</param>
    /// <param name="allowSubfolder">Whether to match the prefix if there's a subfolder path after it, so <c>Data/Achievements/Example</c> matches prefix <c>Data/Achievements</c>. If this is false, the prefix only matches if the asset name has no <c>/</c> or <c>\\</c> characters after the prefix.</param>
    bool StartsWith(string? prefix, bool allowPartialWord = true, bool allowSubfolder = true);

    /// <summary>Get whether the asset is directly within the given asset path.</summary>
    /// <remarks>For example, <c>Characters/Dialogue/Abigail</c> is directly under <c>Characters/Dialogue</c> but not <c>Characters</c> or <c>Characters/Dialogue/Ab</c>. To allow sub-paths, use <see cref="StartsWith"/> instead.</remarks>
    /// <param name="assetFolder">The asset path to check. This doesn't need a trailing slash.</param>
    bool IsDirectlyUnderPath(string? assetFolder);

    /// <summary>Get an asset name representing the <see cref="BaseName"/> without locale.</summary>
    internal IAssetName GetBaseAssetName();
}
