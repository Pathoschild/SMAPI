using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Framework.Input;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments when any buttons were pressed or released.</summary>
public class ButtonsChangedEventArgs : EventArgs
{
    /*********
    ** Fields
    *********/
    /// <summary>The buttons that were pressed, held, or released since the previous tick.</summary>
    private readonly Lazy<Dictionary<SButtonState, SButton[]>> ButtonsByState;


    /*********
    ** Accessors
    *********/
    /// <summary>The current cursor position.</summary>
    public ICursorPosition Cursor { get; }

    /// <summary>The buttons which were pressed since the previous tick.</summary>
    public IEnumerable<SButton> Pressed => this.ButtonsByState.Value[SButtonState.Pressed];

    /// <summary>The buttons which were held since the previous tick.</summary>
    public IEnumerable<SButton> Held => this.ButtonsByState.Value[SButtonState.Held];

    /// <summary>The buttons which were released since the previous tick.</summary>
    public IEnumerable<SButton> Released => this.ButtonsByState.Value[SButtonState.Released];


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="cursor">The cursor position.</param>
    /// <param name="inputState">The game's current input state.</param>
    internal ButtonsChangedEventArgs(ICursorPosition cursor, SInputState inputState)
    {
        this.Cursor = cursor;
        this.ButtonsByState = new Lazy<Dictionary<SButtonState, SButton[]>>(() => this.GetButtonsByState(inputState));
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the buttons that were pressed, held, or released since the previous tick.</summary>
    /// <param name="inputState">The game's current input state.</param>
    private Dictionary<SButtonState, SButton[]> GetButtonsByState(SInputState inputState)
    {
        Dictionary<SButtonState, SButton[]> lookup = inputState.ButtonStates
            .GroupBy(p => p.Value)
            .ToDictionary(p => p.Key, p => p.Select(p => p.Key).ToArray());

        foreach (var state in new[] { SButtonState.Pressed, SButtonState.Held, SButtonState.Released })
        {
            if (!lookup.ContainsKey(state))
                lookup[state] = Array.Empty<SButton>();
        }

        return lookup;
    }
}
