using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;

#pragma warning disable 809 // obsolete override of non-obsolete method (this is deliberate)
namespace StardewModdingAPI.Framework.Input;

/// <summary>Manages the game's input state.</summary>
internal sealed class SInputState : InputState
{
    /*********
    ** Accessors
    *********/
    /// <summary>The cursor position on the screen adjusted for the zoom level.</summary>
    private CursorPosition CursorPositionImpl = new(Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero);

    /// <summary>The player's last known tile position.</summary>
    private Vector2? LastPlayerTile;

    /// <summary>The buttons to press until the game next handles input.</summary>
    private readonly HashSet<SButton> CustomPressedKeys = new();

    /// <summary>The buttons to consider released until the actual button is released.</summary>
    private readonly HashSet<SButton> CustomReleasedKeys = new();

    /// <summary>Whether there are new overrides in <see cref="CustomPressedKeys"/> or <see cref="CustomReleasedKeys"/> that haven't been applied to the previous state.</summary>
    private bool HasNewOverrides;


    /*********
    ** Accessors
    *********/
    /// <summary>The controller state as of the last update, with overrides applied.</summary>
    public GamePadState ControllerState { get; private set; }

    /// <summary>The keyboard state as of the last update, with overrides applied.</summary>
    public KeyboardState KeyboardState { get; private set; }

    /// <summary>The mouse state as of the last update, with overrides applied.</summary>
    public MouseState MouseState { get; private set; }

    /// <summary>The buttons which were pressed, held, or released as of the last update.</summary>
    public IDictionary<SButton, SButtonState> ButtonStates { get; private set; } = new Dictionary<SButton, SButtonState>();

    /// <summary>The cursor position on the screen adjusted for the zoom level.</summary>
    public ICursorPosition CursorPosition => this.CursorPositionImpl;


    /*********
    ** Public methods
    *********/
    /// <summary>This method is called by the game, and does nothing since SMAPI will already have updated by that point.</summary>
    [Obsolete("This method should only be called by the game itself.")]
    public override void Update() { }

    /// <summary>Update the current button states for the given tick.</summary>
    public void TrueUpdate()
    {
        // update base state
        base.Update();

        // update SMAPI extended data
        // note: Stardew Valley is *not* in UI mode when this code runs
        try
        {
            float zoomMultiplier = (1f / Game1.options.zoomLevel);

            // get real values
            var controller = new GamePadStateBuilder(base.GetGamePadState());
            var keyboard = new KeyboardStateBuilder(base.GetKeyboardState());
            var mouse = new MouseStateBuilder(base.GetMouseState());
            Vector2 cursorAbsolutePos = new((mouse.X * zoomMultiplier) + Game1.viewport.X, (mouse.Y * zoomMultiplier) + Game1.viewport.Y);
            Vector2? playerTilePos = Context.IsPlayerFree ? Game1.player.getTileLocation() : null;
            HashSet<SButton> reallyDown = new HashSet<SButton>(this.GetPressedButtons(keyboard, mouse, controller));

            // apply overrides
            bool hasOverrides = false;
            if (this.CustomPressedKeys.Count > 0 || this.CustomReleasedKeys.Count > 0)
            {
                // reset overrides that no longer apply
                this.CustomPressedKeys.RemoveWhere(key => reallyDown.Contains(key));
                this.CustomReleasedKeys.RemoveWhere(key => !reallyDown.Contains(key));

                // apply overrides
                if (this.ApplyOverrides(this.CustomPressedKeys, this.CustomReleasedKeys, controller, keyboard, mouse))
                    hasOverrides = true;

                // remove pressed keys
                this.CustomPressedKeys.Clear();
            }

            // get button states
            var pressedButtons = hasOverrides
                ? new HashSet<SButton>(this.GetPressedButtons(keyboard, mouse, controller))
                : reallyDown;
            var activeButtons = this.DeriveStates(this.ButtonStates, pressedButtons);

            // update
            this.HasNewOverrides = false;
            this.ControllerState = controller.GetState();
            this.KeyboardState = keyboard.GetState();
            this.MouseState = mouse.GetState();
            this.ButtonStates = activeButtons;
            if (cursorAbsolutePos != this.CursorPositionImpl.AbsolutePixels || playerTilePos != this.LastPlayerTile)
            {
                this.LastPlayerTile = playerTilePos;
                this.CursorPositionImpl = this.GetCursorPosition(this.MouseState, cursorAbsolutePos, zoomMultiplier);
            }
        }
        catch (InvalidOperationException)
        {
            // GetState() may crash for some players if window doesn't have focus but game1.IsActive == true
        }
    }

    /// <summary>Get the gamepad state visible to the game.</summary>
    public override GamePadState GetGamePadState()
    {
        return this.ControllerState;
    }

    /// <summary>Get the keyboard state visible to the game.</summary>
    public override KeyboardState GetKeyboardState()
    {
        return this.KeyboardState;
    }

    /// <summary>Get the keyboard state visible to the game.</summary>
    public override MouseState GetMouseState()
    {
        return this.MouseState;
    }

    /// <summary>Override the state for a button.</summary>
    /// <param name="button">The button to override.</param>
    /// <param name="setDown">Whether to mark it pressed; else mark it released.</param>
    public void OverrideButton(SButton button, bool setDown)
    {
        bool changed = setDown
            ? this.CustomPressedKeys.Add(button) | this.CustomReleasedKeys.Remove(button)
            : this.CustomPressedKeys.Remove(button) | this.CustomReleasedKeys.Add(button);

        if (changed)
            this.HasNewOverrides = true;
    }

    /// <summary>Get whether a mod has indicated the key was already handled, so the game shouldn't handle it.</summary>
    /// <param name="button">The button to check.</param>
    public bool IsSuppressed(SButton button)
    {
        return this.CustomReleasedKeys.Contains(button);
    }

    /// <summary>Apply input overrides to the current state.</summary>
    public void ApplyOverrides()
    {
        if (this.HasNewOverrides)
        {
            var controller = new GamePadStateBuilder(this.ControllerState);
            var keyboard = new KeyboardStateBuilder(this.KeyboardState);
            var mouse = new MouseStateBuilder(this.MouseState);

            if (this.ApplyOverrides(pressed: this.CustomPressedKeys, released: this.CustomReleasedKeys, controller, keyboard, mouse))
            {
                this.ControllerState = controller.GetState();
                this.KeyboardState = keyboard.GetState();
                this.MouseState = mouse.GetState();
            }
        }
    }

    /// <summary>Get whether a given button was pressed or held.</summary>
    /// <param name="button">The button to check.</param>
    public bool IsDown(SButton button)
    {
        return this.GetState(this.ButtonStates, button).IsDown();
    }

    /// <summary>Get whether any of the given buttons were pressed or held.</summary>
    /// <param name="buttons">The buttons to check.</param>
    public bool IsAnyDown(InputButton[] buttons)
    {
        return buttons.Any(button => this.IsDown(button.ToSButton()));
    }

    /// <summary>Get the state of a button.</summary>
    /// <param name="button">The button to check.</param>
    public SButtonState GetState(SButton button)
    {
        return this.GetState(this.ButtonStates, button);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the current cursor position.</summary>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="absolutePixels">The absolute pixel position relative to the map, adjusted for pixel zoom.</param>
    /// <param name="zoomMultiplier">The multiplier applied to pixel coordinates to adjust them for pixel zoom.</param>
    private CursorPosition GetCursorPosition(MouseState mouseState, Vector2 absolutePixels, float zoomMultiplier)
    {
        Vector2 screenPixels = new(mouseState.X * zoomMultiplier, mouseState.Y * zoomMultiplier);
        Vector2 tile = new((int)((Game1.viewport.X + screenPixels.X) / Game1.tileSize), (int)((Game1.viewport.Y + screenPixels.Y) / Game1.tileSize));
        Vector2 grabTile = (Game1.mouseCursorTransparency > 0 && Utility.tileWithinRadiusOfPlayer((int)tile.X, (int)tile.Y, 1, Game1.player)) // derived from Game1.pressActionButton
            ? tile
            : Game1.player.GetGrabTile();
        return new CursorPosition(absolutePixels, screenPixels, tile, grabTile);
    }

    /// <summary>Apply input overrides to the given states.</summary>
    /// <param name="pressed">The buttons to mark pressed.</param>
    /// <param name="released">The buttons to mark released.</param>
    /// <param name="controller">The game's controller state for the current tick.</param>
    /// <param name="keyboard">The game's keyboard state for the current tick.</param>
    /// <param name="mouse">The game's mouse state for the current tick.</param>
    /// <returns>Returns whether any overrides were applied.</returns>
    private bool ApplyOverrides(ISet<SButton> pressed, ISet<SButton> released, GamePadStateBuilder controller, KeyboardStateBuilder keyboard, MouseStateBuilder mouse)
    {
        if (pressed.Count == 0 && released.Count == 0)
            return false;

        // group keys by type
        IDictionary<SButton, SButtonState> keyboardOverrides = new Dictionary<SButton, SButtonState>();
        IDictionary<SButton, SButtonState> controllerOverrides = new Dictionary<SButton, SButtonState>();
        IDictionary<SButton, SButtonState> mouseOverrides = new Dictionary<SButton, SButtonState>();
        foreach (var button in pressed.Concat(released))
        {
            var newState = this.DeriveState(
                oldState: this.GetState(button),
                isDown: pressed.Contains(button)
            );

            if (button is SButton.MouseLeft or SButton.MouseMiddle or SButton.MouseRight or SButton.MouseX1 or SButton.MouseX2)
                mouseOverrides[button] = newState;
            else if (button.TryGetKeyboard(out Keys _))
                keyboardOverrides[button] = newState;
            else if (controller.IsConnected && button.TryGetController(out Buttons _))
                controllerOverrides[button] = newState;
        }

        // override states
        if (keyboardOverrides.Any())
            keyboard.OverrideButtons(keyboardOverrides);
        if (controller.IsConnected && controllerOverrides.Any())
            controller.OverrideButtons(controllerOverrides);
        if (mouseOverrides.Any())
            mouse.OverrideButtons(mouseOverrides);

        return true;
    }

    /// <summary>Get the state of all pressed or released buttons relative to their previous state.</summary>
    /// <param name="previousStates">The previous button states.</param>
    /// <param name="pressedButtons">The currently pressed buttons.</param>
    private IDictionary<SButton, SButtonState> DeriveStates(IDictionary<SButton, SButtonState> previousStates, HashSet<SButton> pressedButtons)
    {
        IDictionary<SButton, SButtonState> activeButtons = new Dictionary<SButton, SButtonState>();

        // handle pressed keys
        foreach (SButton button in pressedButtons)
            activeButtons[button] = this.DeriveState(this.GetState(previousStates, button), isDown: true);

        // handle released keys
        foreach (KeyValuePair<SButton, SButtonState> prev in previousStates)
        {
            if (prev.Value.IsDown() && !activeButtons.ContainsKey(prev.Key))
                activeButtons[prev.Key] = SButtonState.Released;
        }

        return activeButtons;
    }

    /// <summary>Get the state of a button relative to its previous state.</summary>
    /// <param name="oldState">The previous button state.</param>
    /// <param name="isDown">Whether the button is currently down.</param>
    private SButtonState DeriveState(SButtonState oldState, bool isDown)
    {
        if (isDown && oldState.IsDown())
            return SButtonState.Held;
        if (isDown)
            return SButtonState.Pressed;
        return SButtonState.Released;
    }

    /// <summary>Get the state of a button.</summary>
    /// <param name="activeButtons">The current button states to check.</param>
    /// <param name="button">The button to check.</param>
    private SButtonState GetState(IDictionary<SButton, SButtonState> activeButtons, SButton button)
    {
        return activeButtons.TryGetValue(button, out SButtonState state)
            ? state
            : SButtonState.None;
    }

    /// <summary>Get the buttons pressed in the given stats.</summary>
    /// <param name="keyboard">The keyboard state.</param>
    /// <param name="mouse">The mouse state.</param>
    /// <param name="controller">The controller state.</param>
    /// <remarks>Thumbstick direction logic derived from <see cref="ButtonCollection"/>.</remarks>
    private IEnumerable<SButton> GetPressedButtons(KeyboardStateBuilder keyboard, MouseStateBuilder mouse, GamePadStateBuilder controller)
    {
        return keyboard
            .GetPressedButtons()
            .Concat(mouse.GetPressedButtons())
            .Concat(controller.GetPressedButtons());
    }
}
