using System;
using System.Collections.Generic;
using NUnit.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace SMAPI.Tests.Utilities;

/// <summary>Unit tests for <see cref="KeybindList"/>.</summary>
[TestFixture]
internal class KeybindListTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** TryParse
    ****/
    /// <summary>Assert the parsed fields when constructed from a simple single-key string.</summary>
    [TestCaseSource(nameof(KeybindListTests.GetAllButtons))]
    public void TryParse_SimpleValue(SButton button)
    {
        // act
        bool success = KeybindList.TryParse($"{button}", out KeybindList? parsed, out string[] errors);

        // assert
        Assert.IsTrue(success, "Parsing unexpectedly failed.");
        Assert.IsNotNull(parsed, "The parsed result should not be null.");
        Assert.AreEqual(parsed!.ToString(), $"{button}");
        Assert.IsNotNull(errors, message: "The errors should never be null.");
        Assert.IsEmpty(errors, message: "The input bindings incorrectly reported errors.");
    }

    /// <summary>Assert the parsed fields when constructed from multi-key values.</summary>
    [TestCase("", ExpectedResult = "None")]
    [TestCase("    ", ExpectedResult = "None")]
    [TestCase(null, ExpectedResult = "None")]
    [TestCase("A + B", ExpectedResult = "A + B")]
    [TestCase("A+B", ExpectedResult = "A + B")]
    [TestCase("      A+     B    ", ExpectedResult = "A + B")]
    [TestCase("a +b", ExpectedResult = "A + B")]
    [TestCase("a +b, LEFTcontrol + leftALT + LeftSHifT + delete", ExpectedResult = "A + B, LeftControl + LeftAlt + LeftShift + Delete")]

    [TestCase(",", ExpectedResult = "None")]
    [TestCase("A,", ExpectedResult = "A")]
    [TestCase(",A", ExpectedResult = "A")]
    public string TryParse_MultiValues(string? input)
    {
        // act
        bool success = KeybindList.TryParse(input, out KeybindList? parsed, out string[] errors);

        // assert
        Assert.IsTrue(success, "Parsing unexpectedly failed.");
        Assert.IsNotNull(parsed, "The parsed result should not be null.");
        Assert.IsNotNull(errors, message: "The errors should never be null.");
        Assert.IsEmpty(errors, message: "The input bindings incorrectly reported errors.");
        return parsed!.ToString();
    }

    /// <summary>Assert invalid values are rejected.</summary>
    [TestCase("+", "Invalid empty button value")]
    [TestCase("A+", "Invalid empty button value")]
    [TestCase("+C", "Invalid empty button value")]
    [TestCase("A + B +, C", "Invalid empty button value")]
    [TestCase("A, TotallyInvalid", "Invalid button value 'TotallyInvalid'")]
    [TestCase("A + TotallyInvalid", "Invalid button value 'TotallyInvalid'")]
    public void TryParse_InvalidValues(string input, string expectedError)
    {
        // act
        bool success = KeybindList.TryParse(input, out KeybindList? parsed, out string[] errors);

        // assert
        Assert.IsFalse(success, "Parsing unexpectedly succeeded.");
        Assert.IsNull(parsed, "The parsed result should be null.");
        Assert.IsNotNull(errors, message: "The errors should never be null.");
        Assert.AreEqual(expectedError, string.Join("; ", errors), "The errors don't match the expected ones.");
    }


    /****
    ** GetState
    ****/
    /// <summary>Assert that <see cref="KeybindList.GetState"/> returns the expected result for a given input state.</summary>
    // single value
    [TestCase("A", "A:Held", ExpectedResult = SButtonState.Held)]
    [TestCase("A", "A:Pressed", ExpectedResult = SButtonState.Pressed)]
    [TestCase("A", "A:Released", ExpectedResult = SButtonState.Released)]
    [TestCase("A", "A:None", ExpectedResult = SButtonState.None)]

    // multiple values
    [TestCase("A + B + C, D", "A:Released, B:None, C:None, D:Pressed", ExpectedResult = SButtonState.Pressed)] // right pressed => pressed
    [TestCase("A + B + C, D", "A:Pressed, B:Held, C:Pressed, D:None", ExpectedResult = SButtonState.Pressed)] // left pressed => pressed
    [TestCase("A + B + C, D", "A:Pressed, B:Pressed, C:Released, D:None", ExpectedResult = SButtonState.None)] // one key released but other keys weren't down last tick => none
    [TestCase("A + B + C, D", "A:Held, B:Held, C:Released, D:None", ExpectedResult = SButtonState.Released)] // all three keys were down last tick and now one is released => released

    // transitive
    [TestCase("A, B", "A: Released, B: Pressed", ExpectedResult = SButtonState.Held)]
    public SButtonState GetState(string input, string stateMap)
    {
        // act
        bool success = KeybindList.TryParse(input, out KeybindList? parsed, out string[] errors);
        if (success && parsed?.Keybinds != null)
        {
            foreach (Keybind? keybind in parsed.Keybinds)
            {
#pragma warning disable 618 // method is marked obsolete because it should only be used in unit tests
                keybind.GetButtonState = key => this.GetStateFromMap(key, stateMap);
#pragma warning restore 618
            }
        }

        // assert
        Assert.IsTrue(success, "Parsing unexpected failed");
        Assert.IsNotNull(parsed, "The parsed result should not be null.");
        Assert.IsNotNull(errors, message: "The errors should never be null.");
        Assert.IsEmpty(errors, message: "The input bindings incorrectly reported errors.");
        return parsed!.GetState();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get all defined buttons.</summary>
    private static IEnumerable<SButton> GetAllButtons()
    {
        foreach (SButton button in Enum.GetValues(typeof(SButton)))
            yield return button;
    }

    /// <summary>Get the button state defined by a mapping string.</summary>
    /// <param name="button">The button to check.</param>
    /// <param name="stateMap">The state map.</param>
    private SButtonState GetStateFromMap(SButton button, string stateMap)
    {
        foreach (string rawPair in stateMap.Split(','))
        {
            // parse values
            string[] parts = rawPair.Split(new[] { ':' }, 2);
            if (!Enum.TryParse(parts[0], ignoreCase: true, out SButton curButton))
                Assert.Fail($"The state map is invalid: unknown button value '{parts[0].Trim()}'");
            if (!Enum.TryParse(parts[1], ignoreCase: true, out SButtonState state))
                Assert.Fail($"The state map is invalid: unknown state value '{parts[1].Trim()}'");

            // get state
            if (curButton == button)
                return state;
        }

        Assert.Fail($"The state map doesn't define button value '{button}'.");
        return SButtonState.None;
    }
}
