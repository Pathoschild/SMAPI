using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModHelpers;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Toolkit.Serialization.Models;
using StardewValley;

namespace SMAPI.Tests.Core;

/// <summary>Unit tests for <see cref="TranslationHelper"/> and <see cref="Translation"/>.</summary>
[TestFixture]
public class TranslationTests
{
    /*********
    ** Data
    *********/
    /// <summary>Sample translation text for unit tests.</summary>
    public static string?[] Samples = { null, "", "  ", "boop", "  boop  " };


    /*********
    ** Unit tests
    *********/
    /****
    ** Translation helper
    ****/
    [Test(Description = "Assert that the translation helper correctly handles no translations.")]
    public void Helper_HandlesNoTranslations()
    {
        // arrange
        var data = new Dictionary<string, IDictionary<string, string>>();

        // act
        ITranslationHelper helper = new TranslationHelper(this.CreateModMetadata(), "en", LocalizedContentManager.LanguageCode.en).SetTranslations(data);
        Translation translation = helper.Get("key");
        Translation[]? translationList = helper.GetTranslations()?.ToArray();

        // assert
        Assert.AreEqual("en", helper.Locale, "The locale doesn't match the input value.");
        Assert.AreEqual(LocalizedContentManager.LanguageCode.en, helper.LocaleEnum, "The locale enum doesn't match the input value.");
        Assert.IsNotNull(translationList, "The full list of translations is unexpectedly null.");
        Assert.AreEqual(0, translationList!.Length, "The full list of translations is unexpectedly not empty.");

        Assert.IsNotNull(translation, "The translation helper unexpectedly returned a null translation.");
        Assert.AreEqual(this.GetPlaceholderText("key"), translation.ToString(), "The translation returned an unexpected value.");
    }

    [Test(Description = "Assert that the translation helper returns the expected translations correctly.")]
    public void Helper_GetTranslations_ReturnsExpectedText()
    {
        // arrange
        var data = this.GetSampleData();
        var expected = this.GetExpectedTranslations();

        // act
        var actual = new Dictionary<string, Translation[]?>();
        TranslationHelper helper = new TranslationHelper(this.CreateModMetadata(), "en", LocalizedContentManager.LanguageCode.en).SetTranslations(data);
        foreach (string locale in expected.Keys)
        {
            this.AssertSetLocale(helper, locale, LocalizedContentManager.LanguageCode.en);
            actual[locale] = helper.GetTranslations()?.ToArray();
        }

        // assert
        foreach (string locale in expected.Keys)
        {
            Assert.IsNotNull(actual[locale], $"The translations for {locale} is unexpectedly null.");
            Assert.That(actual[locale], Is.EquivalentTo(expected[locale]).Using<Translation, Translation>(this.CompareEquality), $"The translations for {locale} don't match the expected values.");
        }
    }

    [Test(Description = "Assert that the translations returned by the helper has the expected text.")]
    public void Helper_Get_ReturnsExpectedText()
    {
        // arrange
        var data = this.GetSampleData();
        var expected = this.GetExpectedTranslations();

        // act
        var actual = new Dictionary<string, Translation[]>();
        TranslationHelper helper = new TranslationHelper(this.CreateModMetadata(), "en", LocalizedContentManager.LanguageCode.en).SetTranslations(data);
        foreach (string locale in expected.Keys)
        {
            this.AssertSetLocale(helper, locale, LocalizedContentManager.LanguageCode.en);

            List<Translation> translations = new List<Translation>();
            foreach (Translation translation in expected[locale])
                translations.Add(helper.Get(translation.Key));
            actual[locale] = translations.ToArray();
        }

        // assert
        foreach (string locale in expected.Keys)
        {
            Assert.IsNotNull(actual[locale], $"The translations for {locale} is unexpectedly null.");
            Assert.That(actual[locale], Is.EquivalentTo(expected[locale]).Using<Translation, Translation>(this.CompareEquality), $"The translations for {locale} don't match the expected values.");
        }
    }

    /****
    ** Translation
    ****/
    [Test(Description = "Assert that HasValue returns the expected result for various inputs.")]
    [TestCase(null, ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    [TestCase("  ", ExpectedResult = true)]
    [TestCase("boop", ExpectedResult = true)]
    [TestCase("  boop  ", ExpectedResult = true)]
    public bool Translation_HasValue(string? text)
    {
        return new Translation("pt-BR", "key", text).HasValue();
    }

    [Test(Description = "Assert that the translation's ToString method returns the expected text for various inputs.")]
    public void Translation_ToString([ValueSource(nameof(TranslationTests.Samples))] string? text)
    {
        // act
        Translation translation = new("pt-BR", "key", text);

        // assert
        if (translation.HasValue())
            Assert.AreEqual(text, translation.ToString(), "The translation returned an unexpected value given a valid input.");
        else
            Assert.AreEqual(this.GetPlaceholderText("key"), translation.ToString(), "The translation returned an unexpected value given a null or empty input.");
    }

    [Test(Description = "Assert that the translation's implicit string conversion returns the expected text for various inputs.")]
    public void Translation_ImplicitStringConversion([ValueSource(nameof(TranslationTests.Samples))] string? text)
    {
        // act
        Translation translation = new("pt-BR", "key", text);

        // assert
        if (translation.HasValue())
            Assert.AreEqual(text, (string?)translation, "The translation returned an unexpected value given a valid input.");
        else
            Assert.AreEqual(this.GetPlaceholderText("key"), (string?)translation, "The translation returned an unexpected value given a null or empty input.");
    }

    [Test(Description = "Assert that the translation returns the expected text given a use-placeholder setting.")]
    public void Translation_UsePlaceholder([Values(true, false)] bool value, [ValueSource(nameof(TranslationTests.Samples))] string? text)
    {
        // act
        Translation translation = new Translation("pt-BR", "key", text).UsePlaceholder(value);

        // assert
        if (translation.HasValue())
            Assert.AreEqual(text, translation.ToString(), "The translation returned an unexpected value given a valid input.");
        else if (!value)
            Assert.AreEqual(text, translation.ToString(), "The translation returned an unexpected value given a null or empty input with the placeholder disabled.");
        else
            Assert.AreEqual(this.GetPlaceholderText("key"), translation.ToString(), "The translation returned an unexpected value given a null or empty input with the placeholder enabled.");
    }

    [Test(Description = "Assert that the translation returns the expected text after setting the default.")]
    public void Translation_Default([ValueSource(nameof(TranslationTests.Samples))] string? text, [ValueSource(nameof(TranslationTests.Samples))] string? @default)
    {
        // act
        Translation translation = new Translation("pt-BR", "key", text).Default(@default);

        // assert
        if (!string.IsNullOrEmpty(text))
            Assert.AreEqual(text, translation.ToString(), "The translation returned an unexpected value given a valid base text.");
        else if (!string.IsNullOrEmpty(@default))
            Assert.AreEqual(@default, translation.ToString(), "The translation returned an unexpected value given a null or empty base text, but valid default.");
        else
            Assert.AreEqual(this.GetPlaceholderText("key"), translation.ToString(), "The translation returned an unexpected value given a null or empty base and default text.");
    }

    /****
    ** Translation tokens
    ****/
    [Test(Description = "Assert that multiple translation tokens are replaced correctly regardless of the token structure.")]
    public void Translation_Tokens([Values("anonymous object", "class", "IDictionary<string, object>", "IDictionary<string, string>")] string structure)
    {
        // arrange
        string start = Guid.NewGuid().ToString("N");
        string middle = Guid.NewGuid().ToString("N");
        string end = Guid.NewGuid().ToString("N");
        const string input = "{{start}} tokens are properly replaced (including {{middle}} {{  MIDdlE}}) {{end}}";
        string expected = $"{start} tokens are properly replaced (including {middle} {middle}) {end}";

        // act
        Translation translation = new("pt-BR", "key", input);
        switch (structure)
        {
            case "anonymous object":
                translation = translation.Tokens(new { start, middle, end });
                break;

            case "class":
                translation = translation.Tokens(new TokenModel(start, middle, end));
                break;

            case "IDictionary<string, object>":
                translation = translation.Tokens(new Dictionary<string, object> { ["start"] = start, ["middle"] = middle, ["end"] = end });
                break;

            case "IDictionary<string, string>":
                translation = translation.Tokens(new Dictionary<string, string> { ["start"] = start, ["middle"] = middle, ["end"] = end });
                break;

            default:
                throw new NotSupportedException($"Unknown structure '{structure}'.");
        }

        // assert
        Assert.AreEqual(expected, translation.ToString(), "The translation returned an unexpected text.");
    }

    [Test(Description = "Assert that the translation can replace tokens in all valid formats.")]
    [TestCase("{{value}}", "value")]
    [TestCase("{{ value }}", "value")]
    [TestCase("{{value       }}", "value")]
    [TestCase("{{ the_value }}", "the_value")]
    [TestCase("{{ the.value_here }}", "the.value_here")]
    [TestCase("{{ the_value-here.... }}", "the_value-here....")]
    [TestCase("{{ tHe_vALuE-HEre.... }}", "tHe_vALuE-HEre....")]
    public void Translation_Tokens_ValidFormats(string text, string key)
    {
        // arrange
        string value = Guid.NewGuid().ToString("N");

        // act
        Translation translation = new Translation("pt-BR", "key", text).Tokens(new Dictionary<string, object> { [key] = value });

        // assert
        Assert.AreEqual(value, translation.ToString(), "The translation returned an unexpected value given a valid base text.");
    }

    [Test(Description = "Assert that translation tokens are case-insensitive and surrounding-whitespace-insensitive.")]
    [TestCase("{{value}}", "value")]
    [TestCase("{{VaLuE}}", "vAlUe")]
    [TestCase("{{VaLuE   }}", "   vAlUe")]
    public void Translation_Tokens_KeysAreNormalized(string text, string key)
    {
        // arrange
        string value = Guid.NewGuid().ToString("N");

        // act
        Translation translation = new Translation("pt-BR", "key", text).Tokens(new Dictionary<string, object> { [key] = value });

        // assert
        Assert.AreEqual(value, translation.ToString(), "The translation returned an unexpected value given a valid base text.");
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Set a translation helper's locale and assert that it was set correctly.</summary>
    /// <param name="helper">The translation helper to change.</param>
    /// <param name="locale">The expected locale.</param>
    /// <param name="localeEnum">The expected game language code.</param>
    private void AssertSetLocale(TranslationHelper helper, string locale, LocalizedContentManager.LanguageCode localeEnum)
    {
        helper.SetLocale(locale, localeEnum);
        Assert.AreEqual(locale, helper.Locale, "The locale doesn't match the input value.");
        Assert.AreEqual(localeEnum, helper.LocaleEnum, "The locale enum doesn't match the input value.");
    }

    /// <summary>Get sample raw translations to input.</summary>
    private IDictionary<string, IDictionary<string, string>> GetSampleData()
    {
        return new Dictionary<string, IDictionary<string, string>>
        {
            ["default"] = new Dictionary<string, string>
            {
                ["key A"] = "default A",
                ["key C"] = "default C"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["key A"] = "en A",
                ["key B"] = "en B"
            },
            ["en-US"] = new Dictionary<string, string>(),
            ["zzz"] = new Dictionary<string, string>
            {
                ["key A"] = "zzz A"
            }
        };
    }

    /// <summary>Get the expected translation output given <see cref="TranslationTests.GetSampleData"/>, based on the expected locale fallback.</summary>
    private IDictionary<string, Translation[]> GetExpectedTranslations()
    {
        var expected = new Dictionary<string, Translation[]>
        {
            ["default"] = new[]
            {
                new Translation("default", "key A", "default A"),
                new Translation("default", "key C", "default C")
            },
            ["en"] = new[]
            {
                new Translation("en", "key A", "en A"),
                new Translation("en", "key B", "en B"),
                new Translation("en", "key C", "default C")
            },
            ["zzz"] = new[]
            {
                new Translation("zzz", "key A", "zzz A"),
                new Translation("zzz", "key C", "default C")
            }
        };
        expected["en-us"] = expected["en"].ToArray();
        return expected;
    }

    /// <summary>Get whether two translations have the same public values.</summary>
    /// <param name="a">The first translation to compare.</param>
    /// <param name="b">The second translation to compare.</param>
    private bool CompareEquality(Translation a, Translation b)
    {
        return a.Key == b.Key && a.ToString() == b.ToString();
    }

    /// <summary>Get the default placeholder text when a translation is missing.</summary>
    /// <param name="key">The translation key.</param>
    private string GetPlaceholderText(string key)
    {
        return string.Format(Translation.PlaceholderText, key);
    }

    /// <summary>Create a fake mod manifest.</summary>
    private IModMetadata CreateModMetadata()
    {
        string id = $"smapi.unit-tests.fake-mod-{Guid.NewGuid():N}";

        string tempPath = Path.Combine(Path.GetTempPath(), id);
        return new ModMetadata(
            displayName: "Mod Display Name",
            directoryPath: tempPath,
            rootPath: tempPath,
            manifest: new Manifest(
                uniqueID: id,
                name: "Mod Name",
                author: "Mod Author",
                description: "Mod Description",
                version: new SemanticVersion(1, 0, 0)
            ),
            dataRecord: null,
            isIgnored: false
        );
    }


    /*********
    ** Test models
    *********/
    /// <summary>A model used to test token support.</summary>
    [SuppressMessage("ReSharper", "NotAccessedField.Local", Justification = "Used dynamically via translation helper.")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Used dynamically via translation helper.")]
    private class TokenModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A sample token property.</summary>
        public string Start { get; }

        /// <summary>A sample token property.</summary>
        public string Middle { get; }

        /// <summary>A sample token field.</summary>
        public string End;


        /*********
        ** public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="start">A sample token property.</param>
        /// <param name="middle">A sample token field.</param>
        /// <param name="end">A sample token property.</param>
        public TokenModel(string start, string middle, string end)
        {
            this.Start = start;
            this.Middle = middle;
            this.End = end;
        }
    }
}
