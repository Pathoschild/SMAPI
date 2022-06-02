using System.Collections.Generic;

namespace StardewModdingAPI.Framework.Input;

/// <summary>Manages input state.</summary>
/// <typeparam name="THandler">The handler type.</typeparam>
/// <typeparam name="TState">The state type.</typeparam>
internal interface IInputStateBuilder<out THandler, TState>
    where TState : struct
    where THandler : IInputStateBuilder<THandler, TState>
{
    /*********
    ** Methods
    *********/
    /// <summary>Override the states for a set of buttons.</summary>
    /// <param name="overrides">The button state overrides.</param>
    THandler OverrideButtons(IDictionary<SButton, SButtonState> overrides);

    /// <summary>Get the currently pressed buttons.</summary>
    IEnumerable<SButton> GetPressedButtons();

    /// <summary>Get the equivalent state.</summary>
    TState GetState();
}
