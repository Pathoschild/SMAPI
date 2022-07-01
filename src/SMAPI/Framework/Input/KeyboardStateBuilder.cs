using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Framework.Input;

/// <summary>Manages keyboard state.</summary>
internal class KeyboardStateBuilder : IInputStateBuilder<KeyboardStateBuilder, KeyboardState>
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying keyboard state.</summary>
    private KeyboardState? State;

    /// <summary>The pressed buttons.</summary>
    private readonly HashSet<Keys> PressedButtons = new();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="state">The initial state.</param>
    public KeyboardStateBuilder(KeyboardState state)
    {
        this.State = state;

        this.PressedButtons.Clear();
        foreach (Keys button in state.GetPressedKeys())
            this.PressedButtons.Add(button);
    }

    /// <inheritdoc />
    public KeyboardStateBuilder OverrideButtons(IDictionary<SButton, SButtonState> overrides)
    {
        foreach (var pair in overrides)
        {
            if (pair.Key.TryGetKeyboard(out Keys key))
            {
                this.State = null;

                if (pair.Value.IsDown())
                    this.PressedButtons.Add(key);
                else
                    this.PressedButtons.Remove(key);
            }
        }

        return this;
    }

    /// <inheritdoc />
    public IEnumerable<SButton> GetPressedButtons()
    {
        foreach (Keys key in this.PressedButtons)
            yield return key.ToSButton();
    }

    /// <inheritdoc />
    public KeyboardState GetState()
    {
        return
            this.State
            ?? (this.State = new KeyboardState(this.PressedButtons.ToArray())).Value;
    }
}
