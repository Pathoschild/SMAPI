using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Framework;

namespace StardewModdingAPI.Utilities;

/// <summary>A single multi-key binding which can be triggered by the player.</summary>
/// <remarks>NOTE: this is part of <see cref="KeybindList"/>, and usually shouldn't be used directly.</remarks>
public class Keybind
{
    /*********
    ** Fields
    *********/
    /// <summary>Get the current input state for a button.</summary>
    [Obsolete("This property should only be used for unit tests.")]
    internal Func<SButton, SButtonState> GetButtonState { get; set; } = SGame.GetInputState;


    /*********
    ** Accessors
    *********/
    /// <summary>The buttons that must be down to activate the keybind.</summary>
    public SButton[] Buttons { get; }

    /// <summary>Whether any keys are bound.</summary>
    public bool IsBound { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="buttons">The buttons that must be down to activate the keybind.</param>
    public Keybind(params SButton[] buttons)
    {
        this.Buttons = buttons;
        this.IsBound = buttons.Any(p => p != SButton.None);
    }

    /// <summary>Parse a keybind string, if it's valid.</summary>
    /// <param name="input">The keybind string. See remarks on <see cref="ToString"/> for format details.</param>
    /// <param name="parsed">The parsed keybind, if valid.</param>
    /// <param name="errors">The parse errors, if any.</param>
    public static bool TryParse(string input, [NotNullWhen(true)] out Keybind? parsed, out string[] errors)
    {
        // empty input
        if (string.IsNullOrWhiteSpace(input))
        {
            parsed = new Keybind(SButton.None);
            errors = Array.Empty<string>();
            return true;
        }

        // parse buttons
        string[] rawButtons = input.Split('+');
        SButton[] buttons = new SButton[rawButtons.Length];
        List<string> rawErrors = new List<string>();
        for (int i = 0; i < buttons.Length; i++)
        {
            string rawButton = rawButtons[i].Trim();
            if (string.IsNullOrWhiteSpace(rawButton))
                rawErrors.Add("Invalid empty button value");
            else if (!Enum.TryParse(rawButton, ignoreCase: true, out SButton button))
            {
                string error = $"Invalid button value '{rawButton}'";

                switch (rawButton.ToLower())
                {
                    case "shift":
                        error += $" (did you mean {SButton.LeftShift}?)";
                        break;

                    case "ctrl":
                    case "control":
                        error += $" (did you mean {SButton.LeftControl}?)";
                        break;

                    case "alt":
                        error += $" (did you mean {SButton.LeftAlt}?)";
                        break;
                }

                rawErrors.Add(error);
            }
            else
                buttons[i] = button;
        }

        // build result
        if (rawErrors.Any())
        {
            parsed = null;
            errors = rawErrors.ToArray();
            return false;
        }
        else
        {
            parsed = new Keybind(buttons);
            errors = Array.Empty<string>();
            return true;
        }
    }

    /// <summary>Get the keybind state relative to the previous tick.</summary>
    public SButtonState GetState()
    {
#pragma warning disable CS0618 // Type or member is obsolete: deliberate call to GetButtonState() for unit tests
        SButtonState[] states = this.Buttons.Select(this.GetButtonState).Distinct().ToArray();
#pragma warning restore CS0618

        // single state
        if (states.Length == 1)
            return states[0];

        // if any key has no state, the whole set wasn't enabled last tick
        if (states.Contains(SButtonState.None))
            return SButtonState.None;

        // mix of held + pressed => pressed
        if (states.All(p => p is SButtonState.Pressed or SButtonState.Held))
            return SButtonState.Pressed;

        // mix of held + released => released
        if (states.All(p => p is SButtonState.Held or SButtonState.Released))
            return SButtonState.Released;

        // not down last tick or now
        return SButtonState.None;
    }

    /// <summary>Get a string representation of the keybind.</summary>
    /// <remarks>A keybind is serialized to a string like <c>LeftControl + S</c>, where each key is separated with <c>+</c>. The key order is commutative, so <c>LeftControl + S</c> and <c>S + LeftControl</c> are identical.</remarks>
    public override string ToString()
    {
        return this.Buttons.Length > 0
            ? string.Join(" + ", this.Buttons)
            : SButton.None.ToString();
    }
}
