using System;
using System.Collections.Generic;
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
    private readonly HashSet<SButton> CustomPressedKeys = [];

    /// <summary>The buttons to consider released until the actual button is released.</summary>
    private readonly HashSet<SButton> CustomReleasedKeys = [];

    /// <summary>Whether there are new overrides in <see cref="CustomPressedKeys"/> or <see cref="CustomReleasedKeys"/> that haven't been applied to the previous state.</summary>
    private bool HasNewOverrides;

    /// <summary>The builder which reads the game pad state and applies overrides.</summary>
    private readonly GamePadStateBuilder ControllerStateBuilder = new();

    /// <summary>The builder which reads the keyboard state and applies overrides.</summary>
    private readonly KeyboardStateBuilder KeyboardStateBuilder = new();

    /// <summary>The builder which reads the mouse state and applies overrides.</summary>
    private readonly MouseStateBuilder MouseStateBuilder = new();

    /// <summary>The pooled cache set for <see cref="FillPressedButtons"/> in <see cref="TrueUpdate"/>.</summary>
    private readonly HashSet<SButton> PooledPressedButtons = [];

    /// <summary>The buttons which were active (pressed, held, or newly released) as of the last update.</summary>
    /// <remarks>A released button is considered inactive if it's been released for two consecutive ticks, at which point it's no longer in this dictionary.</remarks>
    private readonly Dictionary<SButton, SButtonState> ButtonStates = [];


    /*********
    ** Accessors
    *********/
    /// <summary>The controller state as of the last update, with overrides applied.</summary>
    public GamePadState ControllerState { get; private set; }

    /// <summary>The keyboard state as of the last update, with overrides applied.</summary>
    public KeyboardState KeyboardState { get; private set; }

    /// <summary>The mouse state as of the last update, with overrides applied.</summary>
    public MouseState MouseState { get; private set; }

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

#if SMAPI_FOR_ANDROID
        // Touch and gamepad state have to be folded into the mouse/gamepad snapshots before SMAPI reads them.
        base.UpdateStates();
#endif

        // update SMAPI extended data
        // note: Stardew Valley is *not* in UI mode when this code runs
        try
        {
            float zoomMultiplier = (1f / Game1.options.zoomLevel);

            // get builders
            GamePadStateBuilder controller = this.ControllerStateBuilder;
            KeyboardStateBuilder keyboard = this.KeyboardStateBuilder;
            MouseStateBuilder mouse = this.MouseStateBuilder;

            // get pooled button set
            HashSet<SButton> pressedButtons = this.PooledPressedButtons;

            // get real values
            controller.Reset(base.GetGamePadState());
            keyboard.Reset(base.GetKeyboardState());
            mouse.Reset(base.GetMouseState());
            Vector2 cursorAbsolutePos = new((mouse.X * zoomMultiplier) + Game1.viewport.X, (mouse.Y * zoomMultiplier) + Game1.viewport.Y);
            Vector2? playerTilePos = Context.IsPlayerFree ? Game1.player.Tile : null;

            pressedButtons.Clear();
            SInputState.FillPressedButtons(pressedButtons, keyboard, mouse, controller);

            // apply overrides
            if (this.CustomPressedKeys.Count > 0 || this.CustomReleasedKeys.Count > 0)
            {
                // reset overrides that no longer apply
                this.CustomPressedKeys.ExceptWith(pressedButtons);
                this.CustomReleasedKeys.IntersectWith(pressedButtons);

                // apply overrides
                if (this.ApplyOverrides(this.CustomPressedKeys, this.CustomReleasedKeys, controller, keyboard, mouse))
                {
                    pressedButtons.Clear();
                    SInputState.FillPressedButtons(pressedButtons, keyboard, mouse, controller);
                }
                this.CustomPressedKeys.Clear();
            }

            // update
            this.HasNewOverrides = false;
            this.ControllerState = controller.GetState();
            this.KeyboardState = keyboard.GetState();
            this.MouseState = mouse.GetState();
            if (cursorAbsolutePos != this.CursorPositionImpl.AbsolutePixels || playerTilePos != this.LastPlayerTile)
            {
                this.LastPlayerTile = playerTilePos;
                this.CursorPositionImpl = this.GetCursorPosition(this.MouseState, cursorAbsolutePos, zoomMultiplier);
            }
            SInputState.UpdateActiveStates(this.ButtonStates, pressedButtons);
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

    /// <summary>Get the buttons which were active (pressed, held, or newly released) as of the last update.</summary>
    /// <remarks>A released button is considered inactive if it's been released for two consecutive ticks, at which point it's no longer in this dictionary.</remarks>
    public IReadOnlyDictionary<SButton, SButtonState> GetActiveButtonStates()
    {
        return this.ButtonStates;
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
            GamePadStateBuilder controller = this.ControllerStateBuilder;
            KeyboardStateBuilder keyboard = this.KeyboardStateBuilder;
            MouseStateBuilder mouse = this.MouseStateBuilder;

            controller.Reset(this.ControllerState);
            keyboard.Reset(this.KeyboardState);
            mouse.Reset(this.MouseState);

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
        return SInputState.GetState(this.ButtonStates, button).IsDown();
    }

    /// <summary>Get whether any of the given buttons were pressed or held.</summary>
    /// <param name="buttons">The buttons to check.</param>
    public bool IsAnyDown(InputButton[] buttons)
    {
        foreach (InputButton button in buttons)
        {
            if (this.IsDown(button.ToSButton()))
                return true;
        }

        return false;
    }

    /// <summary>Get the state of a button.</summary>
    /// <param name="button">The button to check.</param>
    public SButtonState GetState(SButton button)
    {
        return SInputState.GetState(this.ButtonStates, button);
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
#if SMAPI_FOR_ANDROID
        if (Game1.player == null)
            return new CursorPosition(absolutePixels, screenPixels, tile, Vector2.Zero);
#endif
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

        foreach (SButton button in pressed)
            Apply(button, this.GetState(button), isDown: true, controller, keyboard, mouse);
        foreach (SButton button in released)
            Apply(button, this.GetState(button), isDown: pressed.Contains(button), controller, keyboard, mouse);

        return true;

        static void Apply(SButton button, SButtonState oldState, bool isDown, GamePadStateBuilder controller, KeyboardStateBuilder keyboard, MouseStateBuilder mouse)
        {
            SButtonState newState = SInputState.DeriveState(oldState, isDown);

            if (button is SButton.MouseLeft or SButton.MouseMiddle or SButton.MouseRight or SButton.MouseX1 or SButton.MouseX2)
                mouse.OverrideButton(button, newState);
            else if (button.TryGetKeyboard(out Keys key))
                keyboard.OverrideButton(key, newState);
            else if (button.TryGetController(out Buttons controllerButton))
                controller.OverrideButton(controllerButton, newState);
        }
    }

    /// <summary>Update active button states based on the currently pressed buttons.</summary>
    /// <param name="buttonStates">The button states from the previous update tick.</param>
    /// <param name="pressedButtons">The currently pressed buttons.</param>
    private static void UpdateActiveStates(Dictionary<SButton, SButtonState> buttonStates, HashSet<SButton> pressedButtons)
    {
        // update previously active keys
        foreach ((SButton button, SButtonState oldState) in buttonStates)
        {
            SButtonState newState = SInputState.DeriveState(oldState, isDown: pressedButtons.Contains(button));

            if (oldState == SButtonState.Released && newState == SButtonState.Released)
                buttonStates.Remove(button);
            else if (oldState != newState)
                buttonStates[button] = newState;
        }

        // add newly pressed keys
        foreach (SButton button in pressedButtons)
            buttonStates.TryAdd(button, SButtonState.Pressed);
    }

    /// <summary>Get the state of a button relative to its previous state.</summary>
    /// <param name="oldState">The previous button state.</param>
    /// <param name="isDown">Whether the button is currently down.</param>
    private static SButtonState DeriveState(SButtonState oldState, bool isDown)
    {
        if (isDown)
        {
            return oldState.IsDown()
                ? SButtonState.Held
                : SButtonState.Pressed;
        }

        return SButtonState.Released;
    }

    /// <summary>Get the state of a button.</summary>
    /// <param name="activeButtons">The current button states to check.</param>
    /// <param name="button">The button to check.</param>
    private static SButtonState GetState(IDictionary<SButton, SButtonState> activeButtons, SButton button)
    {
        return activeButtons.TryGetValue(button, out SButtonState state)
            ? state
            : SButtonState.None;
    }

    /// <summary>Get the buttons pressed in the given stats.</summary>
    /// <param name="set">The set to populate with pressed buttons.</param>
    /// <param name="keyboard">The keyboard state.</param>
    /// <param name="mouse">The mouse state.</param>
    /// <param name="controller">The controller state.</param>
    /// <remarks>Thumbstick direction logic derived from <see cref="ButtonCollection"/>.</remarks>
    private static void FillPressedButtons(HashSet<SButton> set, KeyboardStateBuilder keyboard, MouseStateBuilder mouse, GamePadStateBuilder controller)
    {
        keyboard.FillPressedButtons(set);
        mouse.FillPressedButtons(set);
        controller.FillPressedButtons(set);
    }
}
