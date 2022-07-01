using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Framework.Input;

/// <summary>Manages mouse state.</summary>
internal class MouseStateBuilder : IInputStateBuilder<MouseStateBuilder, MouseState>
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying mouse state.</summary>
    private MouseState? State;

    /// <summary>The current button states.</summary>
    private readonly IDictionary<SButton, ButtonState> ButtonStates;

    /// <summary>The mouse wheel scroll value.</summary>
    private readonly int ScrollWheelValue;


    /*********
    ** Accessors
    *********/
    /// <summary>The X cursor position.</summary>
    public int X { get; }

    /// <summary>The Y cursor position.</summary>
    public int Y { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="state">The initial state.</param>
    public MouseStateBuilder(MouseState state)
    {
        this.State = state;

        this.ButtonStates = new Dictionary<SButton, ButtonState>
        {
            [SButton.MouseLeft] = state.LeftButton,
            [SButton.MouseMiddle] = state.MiddleButton,
            [SButton.MouseRight] = state.RightButton,
            [SButton.MouseX1] = state.XButton1,
            [SButton.MouseX2] = state.XButton2
        };
        this.X = state.X;
        this.Y = state.Y;
        this.ScrollWheelValue = state.ScrollWheelValue;
    }

    /// <inheritdoc />
    public MouseStateBuilder OverrideButtons(IDictionary<SButton, SButtonState> overrides)
    {
        foreach (var pair in overrides)
        {
            if (this.ButtonStates.ContainsKey(pair.Key))
            {
                this.State = null;
                this.ButtonStates[pair.Key] = pair.Value.IsDown() ? ButtonState.Pressed : ButtonState.Released;
            }
        }

        return this;
    }

    /// <inheritdoc />
    public IEnumerable<SButton> GetPressedButtons()
    {
        foreach (var pair in this.ButtonStates)
        {
            if (pair.Value == ButtonState.Pressed)
                yield return pair.Key;
        }
    }

    /// <inheritdoc />
    public MouseState GetState()
    {
        this.State ??= new MouseState(
            x: this.X,
            y: this.Y,
            scrollWheel: this.ScrollWheelValue,
            leftButton: this.ButtonStates[SButton.MouseLeft],
            middleButton: this.ButtonStates[SButton.MouseMiddle],
            rightButton: this.ButtonStates[SButton.MouseRight],
            xButton1: this.ButtonStates[SButton.MouseX1],
            xButton2: this.ButtonStates[SButton.MouseX2]
        );

        return this.State.Value;
    }
}
