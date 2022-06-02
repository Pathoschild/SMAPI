using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;

namespace SMAPI.Tests.Core;

/// <summary>Unit tests for <see cref="AssetName"/>.</summary>
[TestFixture]
internal class AssetNameTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** Constructor
    ****/
    [Test(Description = $"Assert that the {nameof(AssetName)} constructor creates an instance with the expected values.")]
    [TestCase("SimpleName", "SimpleName", null, null)]
    [TestCase("Data/Achievements", "Data/Achievements", null, null)]
    [TestCase("Characters/Dialogue/Abigail", "Characters/Dialogue/Abigail", null, null)]
    [TestCase("Characters/Dialogue/Abigail.fr-FR", "Characters/Dialogue/Abigail", "fr-FR", LocalizedContentManager.LanguageCode.fr)]
    [TestCase("Characters/Dialogue\\Abigail.fr-FR", "Characters/Dialogue/Abigail.fr-FR", null, null)]
    [TestCase("Characters/Dialogue/Abigail.fr-FR", "Characters/Dialogue/Abigail", "fr-FR", LocalizedContentManager.LanguageCode.fr)]
    public void Constructor_Valid(string name, string expectedBaseName, string? expectedLocale, LocalizedContentManager.LanguageCode? expectedLanguageCode)
    {
        // arrange
        name = PathUtilities.NormalizeAssetName(name);

        // act
        IAssetName assetName = AssetName.Parse(name, parseLocale: _ => expectedLanguageCode);

        // assert
        assetName.Name.Should()
            .NotBeNull()
            .And.Be(name.Replace("\\", "/"));
        assetName.BaseName.Should()
            .NotBeNull()
            .And.Be(expectedBaseName);
        assetName.LocaleCode.Should()
            .Be(expectedLocale);
        assetName.LanguageCode.Should()
            .Be(expectedLanguageCode);
    }

    [Test(Description = $"Assert that the {nameof(AssetName)} constructor throws an exception if the value is invalid.")]
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    [TestCase("  \t   ")]
    public void Constructor_NullOrWhitespace(string? name)
    {
        // act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => _ = AssetName.Parse(name!, _ => null))!;

        // assert
        exception.ParamName.Should().Be("rawName");
        exception.Message.Should().Be("The asset name can't be null or empty. (Parameter 'rawName')");
    }


    /****
    ** IsEquivalentTo
    ****/
    [Test(Description = $"Assert that {nameof(AssetName.IsEquivalentTo)} compares names as expected when the locale is included.")]

    // exact match (ignore case)
    [TestCase("Data/Achievements", "Data/Achievements", ExpectedResult = true)]
    [TestCase("DATA/achievements", "data/ACHIEVEMENTS", ExpectedResult = true)]

    // exact match (ignore formatting)
    [TestCase("Data/Achievements", "Data\\Achievements", ExpectedResult = true)]
    [TestCase("DATA\\achievements", "data/ACHIEVEMENTS", ExpectedResult = true)]
    [TestCase("DATA\\\\achievements", "data////ACHIEVEMENTS", ExpectedResult = true)]

    // whitespace-insensitive
    [TestCase("Data/Achievements", " Data/Achievements ", ExpectedResult = true)]
    [TestCase(" Data/Achievements ", "Data/Achievements", ExpectedResult = true)]

    // other is null or whitespace
    [TestCase("Data/Achievements", null, ExpectedResult = false)]
    [TestCase("Data/Achievements", "", ExpectedResult = false)]
    [TestCase("Data/Achievements", "   ", ExpectedResult = false)]

    // with locale codes
    [TestCase("Data/Achievements", "Data/Achievements.fr-FR", ExpectedResult = false)]
    [TestCase("Data/Achievements.fr-FR", "Data/Achievements", ExpectedResult = false)]
    [TestCase("Data/Achievements.fr-FR", "Data/Achievements.fr-FR", ExpectedResult = true)]
    public bool IsEquivalentTo_Name(string mainAssetName, string otherAssetName)
    {
        // arrange
        mainAssetName = PathUtilities.NormalizeAssetName(mainAssetName);

        // act
        AssetName name = AssetName.Parse(mainAssetName, _ => LocalizedContentManager.LanguageCode.fr);

        // assert
        return name.IsEquivalentTo(otherAssetName);
    }

    [Test(Description = $"Assert that {nameof(AssetName.IsEquivalentTo)} compares names as expected when the locale is excluded.")]

    // a few samples from previous test to make sure
    [TestCase("Data/Achievements", "Data/Achievements", ExpectedResult = true)]
    [TestCase("DATA/achievements", "data/ACHIEVEMENTS", ExpectedResult = true)]
    [TestCase("DATA\\\\achievements", "data////ACHIEVEMENTS", ExpectedResult = true)]
    [TestCase(" Data/Achievements ", "Data/Achievements", ExpectedResult = true)]
    [TestCase("Data/Achievements", "   ", ExpectedResult = false)]

    // with locale codes
    [TestCase("Data/Achievements", "Data/Achievements.fr-FR", ExpectedResult = false)]
    [TestCase("Data/Achievements.fr-FR", "Data/Achievements", ExpectedResult = true)]
    [TestCase("Data/Achievements.fr-FR", "Data/Achievements.fr-FR", ExpectedResult = false)]
    public bool IsEquivalentTo_BaseName(string mainAssetName, string otherAssetName)
    {
        // arrange
        mainAssetName = PathUtilities.NormalizeAssetName(mainAssetName);

        // act
        AssetName name = AssetName.Parse(mainAssetName, _ => LocalizedContentManager.LanguageCode.fr);

        // assert
        return name.IsEquivalentTo(otherAssetName, useBaseName: true);
    }


    /****
    ** StartsWith
    ****/
    [Test(Description = $"Assert that {nameof(AssetName.StartsWith)} compares names as expected for inputs that aren't affected by the input options.")]

    // exact match (ignore case and formatting)
    [TestCase("Data/Achievements", "Data/Achievements", ExpectedResult = true)]
    [TestCase("DATA/achievements", "data/ACHIEVEMENTS", ExpectedResult = true)]
    [TestCase("Data/Achievements", "Data\\Achievements", ExpectedResult = true)]
    [TestCase("DATA\\achievements", "data/ACHIEVEMENTS", ExpectedResult = true)]
    [TestCase("DATA\\\\achievements", "data////ACHIEVEMENTS", ExpectedResult = true)]

    // whitespace-insensitive
    [TestCase("Data/Achievements", " Data/Achievements", ExpectedResult = true)]
    [TestCase(" Data/Achievements ", "Data/Achievements", ExpectedResult = true)]
    [TestCase("Data/Achievements", "   ", ExpectedResult = true)]

    // invalid prefixes
    [TestCase("Data/Achievements", null, ExpectedResult = false)]

    // with locale codes
    [TestCase("Data/Achievements.fr-FR", "Data/Achievements", ExpectedResult = true)]
    public bool StartsWith_SimpleCases(string mainAssetName, string prefix)
    {
        // arrange
        mainAssetName = PathUtilities.NormalizeAssetName(mainAssetName);

        // act
        AssetName name = AssetName.Parse(mainAssetName, _ => null);

        // assert value is the same for any combination of options
        bool result = name.StartsWith(prefix);
        foreach (bool allowPartialWord in new[] { true, false })
        {
            foreach (bool allowSubfolder in new[] { true, true })
            {
                if (allowPartialWord && allowSubfolder)
                    continue;

                name.StartsWith(prefix, allowPartialWord, allowSubfolder)
                    .Should().Be(result, $"the value returned for options ({nameof(allowPartialWord)}: {allowPartialWord}, {nameof(allowSubfolder)}: {allowSubfolder}) should match the base case");
            }
        }

        // assert value
        return result;
    }

    [Test(Description = $"Assert that {nameof(AssetName.StartsWith)} compares names as expected for the 'allowPartialWord' option.")]
    [TestCase("Data/AchievementsToIgnore", "Data/Achievements", true, ExpectedResult = true)]
    [TestCase("Data/AchievementsToIgnore", "Data/Achievements", false, ExpectedResult = false)]
    [TestCase("Data/Achievements X", "Data/Achievements", true, ExpectedResult = true)]
    [TestCase("Data/Achievements X", "Data/Achievements", false, ExpectedResult = true)]
    [TestCase("Data/Achievements.X", "Data/Achievements", true, ExpectedResult = true)]
    [TestCase("Data/Achievements.X", "Data/Achievements", false, ExpectedResult = true)]

    // with locale codes
    [TestCase("Data/Achievements.fr-FR", "Data/Achievements", true, ExpectedResult = true)]
    [TestCase("Data/Achievements.fr-FR", "Data/Achievements", false, ExpectedResult = true)]
    public bool StartsWith_PartialWord(string mainAssetName, string prefix, bool allowPartialWord)
    {
        // arrange
        mainAssetName = PathUtilities.NormalizeAssetName(mainAssetName);

        // act
        AssetName name = AssetName.Parse(mainAssetName, _ => null);

        // assert value is the same for any combination of options
        bool result = name.StartsWith(prefix, allowPartialWord: allowPartialWord, allowSubfolder: true);
        name.StartsWith(prefix, allowPartialWord, allowSubfolder: false)
            .Should().Be(result, "specifying allowSubfolder should have no effect for these inputs");

        // assert value
        return result;
    }

    [Test(Description = $"Assert that {nameof(AssetName.StartsWith)} compares names as expected for the 'allowSubfolder' option.")]

    // simple cases
    [TestCase("Data/Achievements/Path", "Data/Achievements", true, ExpectedResult = true)]
    [TestCase("Data/Achievements/Path", "Data/Achievements", false, ExpectedResult = false)]
    [TestCase("Data/Achievements/Path", "Data\\Achievements", true, ExpectedResult = true)]
    [TestCase("Data/Achievements/Path", "Data\\Achievements", false, ExpectedResult = false)]

    // trailing slash
    [TestCase("Data/Achievements/Path", "Data/", true, ExpectedResult = true)]
    [TestCase("Data/Achievements/Path", "Data/", false, ExpectedResult = false)]

    // normalize slash style
    [TestCase("Data/Achievements/Path", "Data\\", true, ExpectedResult = true)]
    [TestCase("Data/Achievements/Path", "Data\\", false, ExpectedResult = false)]
    [TestCase("Data/Achievements/Path", "Data/\\/", true, ExpectedResult = true)]
    [TestCase("Data/Achievements/Path", "Data/\\/", false, ExpectedResult = false)]

    // with locale code
    [TestCase("Data/Achievements/Path.fr-FR", "Data/Achievements", true, ExpectedResult = true)]
    [TestCase("Data/Achievements/Path.fr-FR", "Data/Achievements", false, ExpectedResult = false)]
    public bool StartsWith_Subfolder(string mainAssetName, string otherAssetName, bool allowSubfolder)
    {
        // arrange
        mainAssetName = PathUtilities.NormalizeAssetName(mainAssetName);

        // act
        AssetName name = AssetName.Parse(mainAssetName, _ => null);

        // assert value is the same for any combination of options
        bool result = name.StartsWith(otherAssetName, allowPartialWord: true, allowSubfolder: allowSubfolder);
        name.StartsWith(otherAssetName, allowPartialWord: false, allowSubfolder: allowSubfolder)
            .Should().Be(result, "specifying allowPartialWord should have no effect for these inputs");

        // assert value
        return result;
    }


    /****
    ** GetHashCode
    ****/
    [Test(Description = $"Assert that {nameof(AssetName.GetHashCode)} generates the same hash code for two asset names which differ only by capitalization.")]
    public void GetHashCode_IsCaseInsensitive()
    {
        // arrange
        string left = "data/ACHIEVEMENTS";
        string right = "DATA/achievements";

        // act
        int leftHash = AssetName.Parse(left, _ => null).GetHashCode();
        int rightHash = AssetName.Parse(right, _ => null).GetHashCode();

        // assert
        leftHash.Should().Be(rightHash, "two asset names which differ only by capitalization should produce the same hash code");
    }

    [Test(Description = $"Assert that {nameof(AssetName.GetHashCode)} generates few hash code collisions for an arbitrary set of asset names.")]
    public void GetHashCode_HasFewCollisions()
    {
        // generate list of names
        List<string> names = new();
        {
            Random random = new();
            string characters = "abcdefghijklmnopqrstuvwxyz1234567890/";

            while (names.Count < 1000)
            {
                char[] name = new char[random.Next(5, 20)];
                for (int i = 0; i < name.Length; i++)
                    name[i] = characters[random.Next(0, characters.Length)];

                names.Add(new string(name));
            }
        }

        // get distinct hash codes
        HashSet<int> hashCodes = new();
        foreach (string name in names)
            hashCodes.Add(AssetName.Parse(name, _ => null).GetHashCode());

        // assert a collision frequency under 0.1%
        float collisionFrequency = 1 - (hashCodes.Count / (names.Count * 1f));
        collisionFrequency.Should().BeLessOrEqualTo(0.001f, "hash codes should be relatively distinct with a collision rate under 0.1% for a small sample set");
    }
}
