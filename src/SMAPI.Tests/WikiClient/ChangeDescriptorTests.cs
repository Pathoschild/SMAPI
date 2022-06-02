using System.Collections.Generic;
using NUnit.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

namespace SMAPI.Tests.WikiClient;

/// <summary>Unit tests for <see cref="ChangeDescriptor"/>.</summary>
[TestFixture]
internal class ChangeDescriptorTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** Constructor
    ****/
    [Test(Description = "Assert that Parse sets the expected values for valid and invalid descriptors.")]
    public void Parse_SetsExpectedValues_Raw()
    {
        // arrange
        string rawDescriptor = "-Nexus:2400,    -B, XX → YY, Nexus:451,+A, XXX → YYY, invalidA →, → invalidB";
        string[] expectedAdd = { "Nexus:451", "A" };
        string[] expectedRemove = { "Nexus:2400", "B" };
        IDictionary<string, string> expectedReplace = new Dictionary<string, string>
        {
            ["XX"] = "YY",
            ["XXX"] = "YYY"
        };
        string[] expectedErrors = {
            "Failed parsing ' invalidA →': can't map to a blank value. Use the '-value' format to remove a value.",
            "Failed parsing ' → invalidB': can't map from a blank old value. Use the '+value' format to add a value."
        };

        // act
        ChangeDescriptor parsed = ChangeDescriptor.Parse(rawDescriptor, out string[] errors);

        // assert
        Assert.That(parsed.Add, Is.EquivalentTo(expectedAdd), $"{nameof(parsed.Add)} doesn't match the expected value.");
        Assert.That(parsed.Remove, Is.EquivalentTo(expectedRemove), $"{nameof(parsed.Replace)} doesn't match the expected value.");
        Assert.That(parsed.Replace, Is.EquivalentTo(expectedReplace), $"{nameof(parsed.Replace)} doesn't match the expected value.");
        Assert.That(errors, Is.EquivalentTo(expectedErrors), $"{nameof(errors)} doesn't match the expected value.");
    }

    [Test(Description = "Assert that Parse sets the expected values for descriptors when a format callback is specified.")]
    public void Parse_SetsExpectedValues_Formatted()
    {
        // arrange
        string rawDescriptor = "-1.0.1,    -2.0-beta, 1.00 → 1.0, 1.0.0,+2.0-beta.15, 2.0 → 2.0-beta, invalidA →, → invalidB";
        string[] expectedAdd = { "1.0.0", "2.0.0-beta.15" };
        string[] expectedRemove = { "1.0.1", "2.0.0-beta" };
        IDictionary<string, string> expectedReplace = new Dictionary<string, string>
        {
            ["1.00"] = "1.0.0",
            ["2.0.0"] = "2.0.0-beta"
        };
        string[] expectedErrors = {
            "Failed parsing ' invalidA →': can't map to a blank value. Use the '-value' format to remove a value.",
            "Failed parsing ' → invalidB': can't map from a blank old value. Use the '+value' format to add a value."
        };

        // act
        ChangeDescriptor parsed = ChangeDescriptor.Parse(
            rawDescriptor,
            out string[] errors,
            formatValue: raw => SemanticVersion.TryParse(raw, out ISemanticVersion? version)
                ? version.ToString()
                : raw
        );

        // assert
        Assert.That(parsed.Add, Is.EquivalentTo(expectedAdd), $"{nameof(parsed.Add)} doesn't match the expected value.");
        Assert.That(parsed.Remove, Is.EquivalentTo(expectedRemove), $"{nameof(parsed.Replace)} doesn't match the expected value.");
        Assert.That(parsed.Replace, Is.EquivalentTo(expectedReplace), $"{nameof(parsed.Replace)} doesn't match the expected value.");
        Assert.That(errors, Is.EquivalentTo(expectedErrors), $"{nameof(errors)} doesn't match the expected value.");
    }

    [Test(Description = "Assert that Apply returns the expected value for the given descriptor.")]

    // null input
    [TestCase(null, "", ExpectedResult = null)]
    [TestCase(null, "+Nexus:2400", ExpectedResult = "Nexus:2400")]
    [TestCase(null, "-Nexus:2400", ExpectedResult = null)]

    // blank input
    [TestCase("", null, ExpectedResult = "")]
    [TestCase("", "", ExpectedResult = "")]

    // add value
    [TestCase("", "+Nexus:2400", ExpectedResult = "Nexus:2400")]
    [TestCase("Nexus:2400", "+Nexus:2400", ExpectedResult = "Nexus:2400")]
    [TestCase("Nexus:2400", "Nexus:2400", ExpectedResult = "Nexus:2400")]
    [TestCase("Nexus:2400", "+Nexus:2401", ExpectedResult = "Nexus:2400, Nexus:2401")]
    [TestCase("Nexus:2400", "Nexus:2401", ExpectedResult = "Nexus:2400, Nexus:2401")]

    // remove value
    [TestCase("", "-Nexus:2400", ExpectedResult = "")]
    [TestCase("Nexus:2400", "-Nexus:2400", ExpectedResult = "")]
    [TestCase("Nexus:2400", "-Nexus:2401", ExpectedResult = "Nexus:2400")]

    // replace value
    [TestCase("", "Nexus:2400 → Nexus:2401", ExpectedResult = "")]
    [TestCase("Nexus:2400", "Nexus:2400 → Nexus:2401", ExpectedResult = "Nexus:2401")]
    [TestCase("Nexus:1", "Nexus: 2400 → Nexus: 2401", ExpectedResult = "Nexus:1")]

    // complex strings
    [TestCase("", "+Nexus:A, Nexus:B, -Chucklefish:14, Nexus:2400 → Nexus:2401, Nexus:A→Nexus:B", ExpectedResult = "Nexus:A, Nexus:B")]
    [TestCase("Nexus:2400", "+Nexus:A, Nexus:B, -Chucklefish:14, Nexus:2400 → Nexus:2401, Nexus:A→Nexus:B", ExpectedResult = "Nexus:2401, Nexus:A, Nexus:B")]
    [TestCase("Nexus:2400, Nexus:2401, Nexus:B,Chucklefish:14", "+Nexus:A, Nexus:B, -Chucklefish:14, Nexus:2400 → Nexus:2401, Nexus:A→Nexus:B", ExpectedResult = "Nexus:2401, Nexus:2401, Nexus:B, Nexus:A")]
    public string Apply_Raw(string input, string? descriptor)
    {
        ChangeDescriptor parsed = ChangeDescriptor.Parse(descriptor, out string[] errors);

        Assert.IsEmpty(errors, "Parsing the descriptor failed.");

        return parsed.ApplyToCopy(input);
    }

    [Test(Description = "Assert that ToString returns the expected normalized descriptors.")]
    [TestCase(null, ExpectedResult = "")]
    [TestCase("", ExpectedResult = "")]
    [TestCase("+   Nexus:2400", ExpectedResult = "+Nexus:2400")]
    [TestCase("  Nexus:2400  ", ExpectedResult = "+Nexus:2400")]
    [TestCase("-Nexus:2400", ExpectedResult = "-Nexus:2400")]
    [TestCase("  Nexus:2400   →Nexus:2401  ", ExpectedResult = "Nexus:2400 → Nexus:2401")]
    [TestCase("+Nexus:A, Nexus:B, -Chucklefish:14, Nexus:2400 → Nexus:2401, Nexus:A→Nexus:B", ExpectedResult = "+Nexus:A, +Nexus:B, -Chucklefish:14, Nexus:2400 → Nexus:2401, Nexus:A → Nexus:B")]
    public string ToString(string? descriptor)
    {
        var parsed = ChangeDescriptor.Parse(descriptor, out string[] errors);

        Assert.IsEmpty(errors, "Parsing the descriptor failed.");

        return parsed.ToString();
    }
}