using StardewModdingAPI.Utilities;

namespace StardewModdingAPI;

/// <summary>Provides an API for checking and changing input state.</summary>
public interface IInputHelper : IModLinked
{
    /// <summary>Get the current cursor position.</summary>
    ICursorPosition GetCursorPosition();

    /// <summary>Get whether a button is currently pressed.</summary>
    /// <param name="button">The button.</param>
    bool IsDown(SButton button);

    /// <summary>Get whether a button is currently suppressed, so the game won't see it.</summary>
    /// <param name="button">The button.</param>
    bool IsSuppressed(SButton button);

    /// <summary>Prevent the game from handling a button press. This doesn't prevent other mods from receiving the event.</summary>
    /// <param name="button">The button to suppress.</param>
    void Suppress(SButton button);

    /// <summary>Suppress the keybinds which are currently down.</summary>
    /// <param name="keybindList">The keybind list whose active keybinds to suppress.</param>
    void SuppressActiveKeybinds(KeybindList keybindList);

    /// <summary>Get the state of a button.</summary>
    /// <param name="button">The button to check.</param>
    SButtonState GetState(SButton button);
}