namespace StardewModdingAPI;

/// <summary>The input state for a button during an update frame.</summary>
public enum SButtonState
{
    /// <summary>The button was neither pressed, held, nor released.</summary>
    None,

    /// <summary>The button was pressed in this frame.</summary>
    Pressed,

    /// <summary>The button has been held since the last frame.</summary>
    Held,

    /// <summary>The button was released in this frame.</summary>
    Released
}

/// <summary>Extension methods for <see cref="SButtonState"/>.</summary>
public static class InputStatusExtensions
{
    /// <summary>Whether the button was pressed or held.</summary>
    /// <param name="state">The button state.</param>
    public static bool IsDown(this SButtonState state)
    {
        return state is SButtonState.Held or SButtonState.Pressed;
    }
}
