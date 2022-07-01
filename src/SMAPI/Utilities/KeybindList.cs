using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Toolkit.Serialization;

namespace StardewModdingAPI.Utilities;

/// <summary>A set of multi-key bindings which can be triggered by the player.</summary>
public class KeybindList
{
    /*********
    ** Accessors
    *********/
    /// <summary>The individual keybinds.</summary>
    public Keybind[] Keybinds { get; }

    /// <summary>Whether any keys are bound.</summary>
    public bool IsBound { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="keybinds">The underlying keybinds.</param>
    /// <remarks>See <see cref="Parse"/> or <see cref="TryParse"/> to parse it from a string representation. You can also use this type directly in your config or JSON data models, and it'll be parsed by SMAPI.</remarks>
    public KeybindList(params Keybind[] keybinds)
    {
        this.Keybinds = keybinds.Where(p => p.IsBound).ToArray();
        this.IsBound = this.Keybinds.Any();
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="singleKey">A single-key binding.</param>
    public KeybindList(SButton singleKey)
        : this(new Keybind(singleKey)) { }

    /// <summary>Parse a keybind list from a string, and throw an exception if it's not valid.</summary>
    /// <param name="input">The keybind string. See remarks on <see cref="ToString"/> for format details.</param>
    /// <exception cref="FormatException">The <paramref name="input"/> format is invalid.</exception>
    public static KeybindList Parse(string input)
    {
        return KeybindList.TryParse(input, out KeybindList? parsed, out string[] errors)
            ? parsed
            : throw new SParseException($"Can't parse {nameof(Keybind)} from invalid value '{input}'.\n{string.Join("\n", errors)}");
    }

    /// <summary>Try to parse a keybind list from a string.</summary>
    /// <param name="input">The keybind string. See remarks on <see cref="ToString"/> for format details.</param>
    /// <param name="parsed">The parsed keybind list, if valid.</param>
    /// <param name="errors">The errors that occurred while parsing the input, if any.</param>
    public static bool TryParse(string? input, [NotNullWhen(true)] out KeybindList? parsed, out string[] errors)
    {
        // empty input
        if (string.IsNullOrWhiteSpace(input))
        {
            parsed = new KeybindList();
            errors = Array.Empty<string>();
            return true;
        }

        // parse buttons
        var rawErrors = new List<string>();
        var keybinds = new List<Keybind>();
        foreach (string rawSet in input.Split(','))
        {
            if (string.IsNullOrWhiteSpace(rawSet))
                continue;

            if (!Keybind.TryParse(rawSet, out Keybind? keybind, out string[] curErrors))
                rawErrors.AddRange(curErrors);
            else
                keybinds.Add(keybind);
        }

        // build result
        if (rawErrors.Any())
        {
            parsed = null;
            errors = rawErrors.Distinct().ToArray();
            return false;
        }
        else
        {
            parsed = new KeybindList(keybinds.ToArray());
            errors = Array.Empty<string>();
            return true;
        }
    }

    /// <summary>Get a keybind list for a single keybind.</summary>
    /// <param name="buttons">The buttons that must be down to activate the keybind.</param>
    public static KeybindList ForSingle(params SButton[] buttons)
    {
        return new KeybindList(
            new Keybind(buttons)
        );
    }

    /// <summary>Get the overall keybind list state relative to the previous tick.</summary>
    /// <remarks>States are transitive across keybind. For example, if one keybind is 'released' and another is 'pressed', the state of the keybind list is 'held'.</remarks>
    public SButtonState GetState()
    {
        bool wasPressed = false;
        bool isPressed = false;

        foreach (Keybind keybind in this.Keybinds)
        {
            switch (keybind.GetState())
            {
                case SButtonState.Pressed:
                    isPressed = true;
                    break;

                case SButtonState.Held:
                    wasPressed = true;
                    isPressed = true;
                    break;

                case SButtonState.Released:
                    wasPressed = true;
                    break;
            }
        }

        if (wasPressed == isPressed)
        {
            return wasPressed
                ? SButtonState.Held
                : SButtonState.None;
        }

        return wasPressed
            ? SButtonState.Released
            : SButtonState.Pressed;
    }

    /// <summary>Get whether any of the button sets are pressed.</summary>
    public bool IsDown()
    {
        SButtonState state = this.GetState();
        return state is SButtonState.Pressed or SButtonState.Held;
    }

    /// <summary>Get whether the input binding was just pressed this tick.</summary>
    public bool JustPressed()
    {
        return this.GetState() == SButtonState.Pressed;
    }

    /// <summary>Get the keybind which is currently down, if any. If there are multiple keybinds down, the first one is returned.</summary>
    public Keybind? GetKeybindCurrentlyDown()
    {
        return this.Keybinds.FirstOrDefault(p => p.GetState().IsDown());
    }

    /// <summary>Get a string representation of the input binding.</summary>
    /// <remarks>A keybind list is serialized to a string like <c>LeftControl + S, LeftAlt + S</c>, where each multi-key binding is separated with <c>,</c> and the keys within each keybind are separated with <c>+</c>. The key order is commutative, so <c>LeftControl + S</c> and <c>S + LeftControl</c> are identical.</remarks>
    public override string ToString()
    {
        return this.Keybinds.Length > 0
            ? string.Join(", ", this.Keybinds.Select(p => p.ToString()))
            : SButton.None.ToString();
    }
}
