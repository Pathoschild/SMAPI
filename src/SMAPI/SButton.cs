using System;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace StardewModdingAPI;

/// <summary>A unified button constant which includes all controller, keyboard, and mouse buttons.</summary>
/// <remarks>Derived from <see cref="Keys"/>, <see cref="Buttons"/>, and <c>System.Windows.Forms.MouseButtons</c>.</remarks>
public enum SButton
{
    /// <summary>No valid key.</summary>
    None = 0,

    /*********
    ** Mouse
    *********/
    /// <summary>The left mouse button.</summary>
    MouseLeft = 1000,

    /// <summary>The right mouse button.</summary>
    MouseRight = 1001,

    /// <summary>The middle mouse button.</summary>
    MouseMiddle = 1002,

    /// <summary>The first mouse XButton.</summary>
    MouseX1 = 1003,

    /// <summary>The second mouse XButton.</summary>
    MouseX2 = 1004,

    /*********
    ** Controller
    *********/
    /// <summary>The 'A' button on a controller.</summary>
    ControllerA = SButtonExtensions.ControllerOffset + Buttons.A,

    /// <summary>The 'B' button on a controller.</summary>
    ControllerB = SButtonExtensions.ControllerOffset + Buttons.B,

    /// <summary>The 'X' button on a controller.</summary>
    ControllerX = SButtonExtensions.ControllerOffset + Buttons.X,

    /// <summary>The 'Y' button on a controller.</summary>
    ControllerY = SButtonExtensions.ControllerOffset + Buttons.Y,

    /// <summary>The back button on a controller.</summary>
    ControllerBack = SButtonExtensions.ControllerOffset + Buttons.Back,

    /// <summary>The start button on a controller.</summary>
    ControllerStart = SButtonExtensions.ControllerOffset + Buttons.Start,

    /// <summary>The up button on the directional pad of a controller.</summary>
    DPadUp = SButtonExtensions.ControllerOffset + Buttons.DPadUp,

    /// <summary>The down button on the directional pad of a controller.</summary>
    DPadDown = SButtonExtensions.ControllerOffset + Buttons.DPadDown,

    /// <summary>The left button on the directional pad of a controller.</summary>
    DPadLeft = SButtonExtensions.ControllerOffset + Buttons.DPadLeft,

    /// <summary>The right button on the directional pad of a controller.</summary>
    DPadRight = SButtonExtensions.ControllerOffset + Buttons.DPadRight,

    /// <summary>The left bumper (shoulder) button on a controller.</summary>
    LeftShoulder = SButtonExtensions.ControllerOffset + Buttons.LeftShoulder,

    /// <summary>The right bumper (shoulder) button on a controller.</summary>
    RightShoulder = SButtonExtensions.ControllerOffset + Buttons.RightShoulder,

    /// <summary>The left trigger on a controller.</summary>
    LeftTrigger = SButtonExtensions.ControllerOffset + Buttons.LeftTrigger,

    /// <summary>The right trigger on a controller.</summary>
    RightTrigger = SButtonExtensions.ControllerOffset + Buttons.RightTrigger,

    /// <summary>The left analog stick on a controller (when pressed).</summary>
    LeftStick = SButtonExtensions.ControllerOffset + Buttons.LeftStick,

    /// <summary>The right analog stick on a controller (when pressed).</summary>
    RightStick = SButtonExtensions.ControllerOffset + Buttons.RightStick,

    /// <summary>The 'big button' on a controller.</summary>
    BigButton = SButtonExtensions.ControllerOffset + Buttons.BigButton,

    /// <summary>The left analog stick on a controller (when pushed left).</summary>
    LeftThumbstickLeft = SButtonExtensions.ControllerOffset + Buttons.LeftThumbstickLeft,

    /// <summary>The left analog stick on a controller (when pushed right).</summary>
    LeftThumbstickRight = SButtonExtensions.ControllerOffset + Buttons.LeftThumbstickRight,

    /// <summary>The left analog stick on a controller (when pushed down).</summary>
    LeftThumbstickDown = SButtonExtensions.ControllerOffset + Buttons.LeftThumbstickDown,

    /// <summary>The left analog stick on a controller (when pushed up).</summary>
    LeftThumbstickUp = SButtonExtensions.ControllerOffset + Buttons.LeftThumbstickUp,

    /// <summary>The right analog stick on a controller (when pushed left).</summary>
    RightThumbstickLeft = SButtonExtensions.ControllerOffset + Buttons.RightThumbstickLeft,

    /// <summary>The right analog stick on a controller (when pushed right).</summary>
    RightThumbstickRight = SButtonExtensions.ControllerOffset + Buttons.RightThumbstickRight,

    /// <summary>The right analog stick on a controller (when pushed down).</summary>
    RightThumbstickDown = SButtonExtensions.ControllerOffset + Buttons.RightThumbstickDown,

    /// <summary>The right analog stick on a controller (when pushed up).</summary>
    RightThumbstickUp = SButtonExtensions.ControllerOffset + Buttons.RightThumbstickUp,

    /*********
    ** Keyboard
    *********/
    /// <summary>The A button on a keyboard.</summary>
    A = Keys.A,

    /// <summary>The Add button on a keyboard.</summary>
    Add = Keys.Add,

    /// <summary>The Applications button on a keyboard.</summary>
    Apps = Keys.Apps,

    /// <summary>The Attn button on a keyboard.</summary>
    Attn = Keys.Attn,

    /// <summary>The B button on a keyboard.</summary>
    B = Keys.B,

    /// <summary>The Backspace button on a keyboard.</summary>
    Back = Keys.Back,

    /// <summary>The Browser Back button on a keyboard in Windows 2000/XP.</summary>
    BrowserBack = Keys.BrowserBack,

    /// <summary>The Browser Favorites button on a keyboard in Windows 2000/XP.</summary>
    BrowserFavorites = Keys.BrowserFavorites,

    /// <summary>The Browser Favorites button on a keyboard in Windows 2000/XP.</summary>
    BrowserForward = Keys.BrowserForward,

    /// <summary>The Browser Home button on a keyboard in Windows 2000/XP.</summary>
    BrowserHome = Keys.BrowserHome,

    /// <summary>The Browser Refresh button on a keyboard in Windows 2000/XP.</summary>
    BrowserRefresh = Keys.BrowserRefresh,

    /// <summary>The Browser Search button on a keyboard in Windows 2000/XP.</summary>
    BrowserSearch = Keys.BrowserSearch,

    /// <summary>The Browser Stop button on a keyboard in Windows 2000/XP.</summary>
    BrowserStop = Keys.BrowserStop,

    /// <summary>The C button on a keyboard.</summary>
    C = Keys.C,

    /// <summary>The Caps Lock button on a keyboard.</summary>
    CapsLock = Keys.CapsLock,

    /// <summary>The Green ChatPad button on a keyboard.</summary>
    ChatPadGreen = Keys.ChatPadGreen,

    /// <summary>The Orange ChatPad button on a keyboard.</summary>
    ChatPadOrange = Keys.ChatPadOrange,

    /// <summary>The CrSel button on a keyboard.</summary>
    Crsel = Keys.Crsel,

    /// <summary>The D button on a keyboard.</summary>
    D = Keys.D,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D0 = Keys.D0,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D1 = Keys.D1,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D2 = Keys.D2,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D3 = Keys.D3,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D4 = Keys.D4,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D5 = Keys.D5,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D6 = Keys.D6,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D7 = Keys.D7,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D8 = Keys.D8,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    D9 = Keys.D9,

    /// <summary>The Decimal button on a keyboard.</summary>
    Decimal = Keys.Decimal,

    /// <summary>The Delete button on a keyboard.</summary>
    Delete = Keys.Delete,

    /// <summary>The Divide button on a keyboard.</summary>
    Divide = Keys.Divide,

    /// <summary>The Down arrow button on a keyboard.</summary>
    Down = Keys.Down,

    /// <summary>The E button on a keyboard.</summary>
    E = Keys.E,

    /// <summary>The End button on a keyboard.</summary>
    End = Keys.End,

    /// <summary>The Enter button on a keyboard.</summary>
    Enter = Keys.Enter,

    /// <summary>The Erase EOF button on a keyboard.</summary>
    EraseEof = Keys.EraseEof,

    /// <summary>The Escape button on a keyboard.</summary>
    Escape = Keys.Escape,

    /// <summary>The Execute button on a keyboard.</summary>
    Execute = Keys.Execute,

    /// <summary>The ExSel button on a keyboard.</summary>
    Exsel = Keys.Exsel,

    /// <summary>The F button on a keyboard.</summary>
    F = Keys.F,

    /// <summary>The F1 button on a keyboard.</summary>
    F1 = Keys.F1,

    /// <summary>The F10 button on a keyboard.</summary>
    F10 = Keys.F10,

    /// <summary>The F11 button on a keyboard.</summary>
    F11 = Keys.F11,

    /// <summary>The F12 button on a keyboard.</summary>
    F12 = Keys.F12,

    /// <summary>The F13 button on a keyboard.</summary>
    F13 = Keys.F13,

    /// <summary>The F14 button on a keyboard.</summary>
    F14 = Keys.F14,

    /// <summary>The F15 button on a keyboard.</summary>
    F15 = Keys.F15,

    /// <summary>The F16 button on a keyboard.</summary>
    F16 = Keys.F16,

    /// <summary>The F17 button on a keyboard.</summary>
    F17 = Keys.F17,

    /// <summary>The F18 button on a keyboard.</summary>
    F18 = Keys.F18,

    /// <summary>The F19 button on a keyboard.</summary>
    F19 = Keys.F19,

    /// <summary>The F2 button on a keyboard.</summary>
    F2 = Keys.F2,

    /// <summary>The F20 button on a keyboard.</summary>
    F20 = Keys.F20,

    /// <summary>The F21 button on a keyboard.</summary>
    F21 = Keys.F21,

    /// <summary>The F22 button on a keyboard.</summary>
    F22 = Keys.F22,

    /// <summary>The F23 button on a keyboard.</summary>
    F23 = Keys.F23,

    /// <summary>The F24 button on a keyboard.</summary>
    F24 = Keys.F24,

    /// <summary>The F3 button on a keyboard.</summary>
    F3 = Keys.F3,

    /// <summary>The F4 button on a keyboard.</summary>
    F4 = Keys.F4,

    /// <summary>The F5 button on a keyboard.</summary>
    F5 = Keys.F5,

    /// <summary>The F6 button on a keyboard.</summary>
    F6 = Keys.F6,

    /// <summary>The F7 button on a keyboard.</summary>
    F7 = Keys.F7,

    /// <summary>The F8 button on a keyboard.</summary>
    F8 = Keys.F8,

    /// <summary>The F9 button on a keyboard.</summary>
    F9 = Keys.F9,

    /// <summary>The G button on a keyboard.</summary>
    G = Keys.G,

    /// <summary>The H button on a keyboard.</summary>
    H = Keys.H,

    /// <summary>The Help button on a keyboard.</summary>
    Help = Keys.Help,

    /// <summary>The Home button on a keyboard.</summary>
    Home = Keys.Home,

    /// <summary>The I button on a keyboard.</summary>
    I = Keys.I,

    /// <summary>The IME Convert button on a keyboard.</summary>
    ImeConvert = Keys.ImeConvert,

    /// <summary>The IME NoConvert button on a keyboard.</summary>
    ImeNoConvert = Keys.ImeNoConvert,

    /// <summary>The INS button on a keyboard.</summary>
    Insert = Keys.Insert,

    /// <summary>The J button on a keyboard.</summary>
    J = Keys.J,

    /// <summary>The K button on a keyboard.</summary>
    K = Keys.K,

    /// <summary>The Kana button on a Japanese keyboard.</summary>
    Kana = Keys.Kana,

    /// <summary>The Kanji button on a Japanese keyboard.</summary>
    Kanji = Keys.Kanji,

    /// <summary>The L button on a keyboard.</summary>
    L = Keys.L,

    /// <summary>The Start Applications 1 button on a keyboard in Windows 2000/XP.</summary>
    LaunchApplication1 = Keys.LaunchApplication1,

    /// <summary>The Start Applications 2 button on a keyboard in Windows 2000/XP.</summary>
    LaunchApplication2 = Keys.LaunchApplication2,

    /// <summary>The Start Mail button on a keyboard in Windows 2000/XP.</summary>
    LaunchMail = Keys.LaunchMail,

    /// <summary>The Left arrow button on a keyboard.</summary>
    Left = Keys.Left,

    /// <summary>The Left Alt button on a keyboard.</summary>
    LeftAlt = Keys.LeftAlt,

    /// <summary>The Left Control button on a keyboard.</summary>
    LeftControl = Keys.LeftControl,

    /// <summary>The Left Shift button on a keyboard.</summary>
    LeftShift = Keys.LeftShift,

    /// <summary>The Left Windows button on a keyboard.</summary>
    LeftWindows = Keys.LeftWindows,

    /// <summary>The M button on a keyboard.</summary>
    M = Keys.M,

    /// <summary>The MediaNextTrack button on a keyboard in Windows 2000/XP.</summary>
    MediaNextTrack = Keys.MediaNextTrack,

    /// <summary>The MediaPlayPause button on a keyboard in Windows 2000/XP.</summary>
    MediaPlayPause = Keys.MediaPlayPause,

    /// <summary>The MediaPreviousTrack button on a keyboard in Windows 2000/XP.</summary>
    MediaPreviousTrack = Keys.MediaPreviousTrack,

    /// <summary>The MediaStop button on a keyboard in Windows 2000/XP.</summary>
    MediaStop = Keys.MediaStop,

    /// <summary>The Multiply button on a keyboard.</summary>
    Multiply = Keys.Multiply,

    /// <summary>The N button on a keyboard.</summary>
    N = Keys.N,

    /// <summary>The Num Lock button on a keyboard.</summary>
    NumLock = Keys.NumLock,

    /// <summary>The Numeric keypad 0 button on a keyboard.</summary>
    NumPad0 = Keys.NumPad0,

    /// <summary>The Numeric keypad 1 button on a keyboard.</summary>
    NumPad1 = Keys.NumPad1,

    /// <summary>The Numeric keypad 2 button on a keyboard.</summary>
    NumPad2 = Keys.NumPad2,

    /// <summary>The Numeric keypad 3 button on a keyboard.</summary>
    NumPad3 = Keys.NumPad3,

    /// <summary>The Numeric keypad 4 button on a keyboard.</summary>
    NumPad4 = Keys.NumPad4,

    /// <summary>The Numeric keypad 5 button on a keyboard.</summary>
    NumPad5 = Keys.NumPad5,

    /// <summary>The Numeric keypad 6 button on a keyboard.</summary>
    NumPad6 = Keys.NumPad6,

    /// <summary>The Numeric keypad 7 button on a keyboard.</summary>
    NumPad7 = Keys.NumPad7,

    /// <summary>The Numeric keypad 8 button on a keyboard.</summary>
    NumPad8 = Keys.NumPad8,

    /// <summary>The Numeric keypad 9 button on a keyboard.</summary>
    NumPad9 = Keys.NumPad9,

    /// <summary>The O button on a keyboard.</summary>
    O = Keys.O,

    /// <summary>A miscellaneous button on a keyboard; can vary by keyboard.</summary>
    Oem8 = Keys.Oem8,

    /// <summary>The OEM Auto button on a keyboard.</summary>
    OemAuto = Keys.OemAuto,

    /// <summary>The OEM Angle Bracket or Backslash button on the RT 102 keyboard in Windows 2000/XP.</summary>
    OemBackslash = Keys.OemBackslash,

    /// <summary>The Clear button on a keyboard.</summary>
    OemClear = Keys.OemClear,

    /// <summary>The OEM Close Bracket button on a US standard keyboard in Windows 2000/XP.</summary>
    OemCloseBrackets = Keys.OemCloseBrackets,

    /// <summary>The ',' button on a keyboard in any country/region in Windows 2000/XP.</summary>
    OemComma = Keys.OemComma,

    /// <summary>The OEM Copy button on a keyboard.</summary>
    OemCopy = Keys.OemCopy,

    /// <summary>The OEM Enlarge Window button on a keyboard.</summary>
    OemEnlW = Keys.OemEnlW,

    /// <summary>The '-' button on a keyboard in any country/region in Windows 2000/XP.</summary>
    OemMinus = Keys.OemMinus,

    /// <summary>The OEM Open Bracket button on a US standard keyboard in Windows 2000/XP.</summary>
    OemOpenBrackets = Keys.OemOpenBrackets,

    /// <summary>The '.' button on a keyboard in any country/region.</summary>
    OemPeriod = Keys.OemPeriod,

    /// <summary>The OEM Pipe button on a US standard keyboard.</summary>
    OemPipe = Keys.OemPipe,

    /// <summary>The '+' button on a keyboard in Windows 2000/XP.</summary>
    OemPlus = Keys.OemPlus,

    /// <summary>The OEM Question Mark button on a US standard keyboard.</summary>
    OemQuestion = Keys.OemQuestion,

    /// <summary>The OEM Single/Double Quote button on a US standard keyboard.</summary>
    OemQuotes = Keys.OemQuotes,

    /// <summary>The OEM Semicolon button on a US standard keyboard.</summary>
    OemSemicolon = Keys.OemSemicolon,

    /// <summary>The OEM Tilde button on a US standard keyboard.</summary>
    OemTilde = Keys.OemTilde,

    /// <summary>The P button on a keyboard.</summary>
    P = Keys.P,

    /// <summary>The PA1 button on a keyboard.</summary>
    Pa1 = Keys.Pa1,

    /// <summary>The Page Down button on a keyboard.</summary>
    PageDown = Keys.PageDown,

    /// <summary>The Page Up button on a keyboard.</summary>
    PageUp = Keys.PageUp,

    /// <summary>The Pause button on a keyboard.</summary>
    Pause = Keys.Pause,

    /// <summary>The Play button on a keyboard.</summary>
    Play = Keys.Play,

    /// <summary>The Print button on a keyboard.</summary>
    Print = Keys.Print,

    /// <summary>The Print Screen button on a keyboard.</summary>
    PrintScreen = Keys.PrintScreen,

    /// <summary>The IME Process button on a keyboard in Windows 95/98/ME/NT 4.0/2000/XP.</summary>
    ProcessKey = Keys.ProcessKey,

    /// <summary>The Q button on a keyboard.</summary>
    Q = Keys.Q,

    /// <summary>The R button on a keyboard.</summary>
    R = Keys.R,

    /// <summary>The Right Arrow button on a keyboard.</summary>
    Right = Keys.Right,

    /// <summary>The Right Alt button on a keyboard.</summary>
    RightAlt = Keys.RightAlt,

    /// <summary>The Right Control button on a keyboard.</summary>
    RightControl = Keys.RightControl,

    /// <summary>The Right Shift button on a keyboard.</summary>
    RightShift = Keys.RightShift,

    /// <summary>The Right Windows button on a keyboard.</summary>
    RightWindows = Keys.RightWindows,

    /// <summary>The S button on a keyboard.</summary>
    S = Keys.S,

    /// <summary>The Scroll Lock button on a keyboard.</summary>
    Scroll = Keys.Scroll,

    /// <summary>The Select button on a keyboard.</summary>
    Select = Keys.Select,

    /// <summary>The Select Media button on a keyboard in Windows 2000/XP.</summary>
    SelectMedia = Keys.SelectMedia,

    /// <summary>The Separator button on a keyboard.</summary>
    Separator = Keys.Separator,

    /// <summary>The Computer Sleep button on a keyboard.</summary>
    Sleep = Keys.Sleep,

    /// <summary>The Space bar on a keyboard.</summary>
    Space = Keys.Space,

    /// <summary>The Subtract button on a keyboard.</summary>
    Subtract = Keys.Subtract,

    /// <summary>The T button on a keyboard.</summary>
    T = Keys.T,

    /// <summary>The Tab button on a keyboard.</summary>
    Tab = Keys.Tab,

    /// <summary>The U button on a keyboard.</summary>
    U = Keys.U,

    /// <summary>The Up Arrow button on a keyboard.</summary>
    Up = Keys.Up,

    /// <summary>The V button on a keyboard.</summary>
    V = Keys.V,

    /// <summary>The Volume Down button on a keyboard in Windows 2000/XP.</summary>
    VolumeDown = Keys.VolumeDown,

    /// <summary>The Volume Mute button on a keyboard in Windows 2000/XP.</summary>
    VolumeMute = Keys.VolumeMute,

    /// <summary>The Volume Up button on a keyboard in Windows 2000/XP.</summary>
    VolumeUp = Keys.VolumeUp,

    /// <summary>The W button on a keyboard.</summary>
    W = Keys.W,

    /// <summary>The X button on a keyboard.</summary>
    X = Keys.X,

    /// <summary>The Y button on a keyboard.</summary>
    Y = Keys.Y,

    /// <summary>The Z button on a keyboard.</summary>
    Z = Keys.Z,

    /// <summary>The Zoom button on a keyboard.</summary>
    Zoom = Keys.Zoom
}

/// <summary>Provides extension methods for <see cref="SButton"/>.</summary>
public static class SButtonExtensions
{
    /*********
    ** Accessors
    *********/
    /// <summary>The offset added to <see cref="Buttons"/> values when converting them to <see cref="SButton"/> to avoid collisions with <see cref="Keys"/> values.</summary>
    internal const int ControllerOffset = 2000;


    /*********
    ** Public methods
    *********/
    /// <summary>Get the <see cref="SButton"/> equivalent for the given button.</summary>
    /// <param name="key">The keyboard button to convert.</param>
    public static SButton ToSButton(this Keys key)
    {
        return (SButton)key;
    }

    /// <summary>Get the <see cref="SButton"/> equivalent for the given button.</summary>
    /// <param name="key">The controller button to convert.</param>
    public static SButton ToSButton(this Buttons key)
    {
        return (SButton)(SButtonExtensions.ControllerOffset + key);
    }

    /// <summary>Get the <see cref="SButton"/> equivalent for the given button.</summary>
    /// <param name="input">The Stardew Valley button to convert.</param>
    public static SButton ToSButton(this InputButton input)
    {
        // derived from InputButton constructors
        if (input.mouseLeft)
            return SButton.MouseLeft;
        if (input.mouseRight)
            return SButton.MouseRight;
        return input.key.ToSButton();
    }

    /// <summary>Get the <see cref="Keys"/> equivalent for the given button.</summary>
    /// <param name="input">The button to convert.</param>
    /// <param name="key">The keyboard equivalent.</param>
    /// <returns>Returns whether the value was converted successfully.</returns>
    public static bool TryGetKeyboard(this SButton input, out Keys key)
    {
        if (Enum.IsDefined(typeof(Keys), (int)input))
        {
            key = (Keys)input;
            return true;
        }

        key = Keys.None;
        return false;
    }

    /// <summary>Get the <see cref="Buttons"/> equivalent for the given button.</summary>
    /// <param name="input">The button to convert.</param>
    /// <param name="button">The controller equivalent.</param>
    /// <returns>Returns whether the value was converted successfully.</returns>
    public static bool TryGetController(this SButton input, out Buttons button)
    {
        if (Enum.IsDefined(typeof(Buttons), (int)input - SButtonExtensions.ControllerOffset))
        {
            button = (Buttons)(input - SButtonExtensions.ControllerOffset);
            return true;
        }

        button = 0;
        return false;
    }

    /// <summary>Get the <see cref="InputButton"/> equivalent for the given button.</summary>
    /// <param name="input">The button to convert.</param>
    /// <param name="button">The Stardew Valley input button equivalent.</param>
    /// <returns>Returns whether the value was converted successfully.</returns>
    public static bool TryGetStardewInput(this SButton input, out InputButton button)
    {
        // keyboard
        if (input.TryGetKeyboard(out Keys key))
        {
            button = new InputButton(key);
            return true;
        }

        // mouse
        if (input is SButton.MouseLeft or SButton.MouseRight)
        {
            button = new InputButton(mouseLeft: input == SButton.MouseLeft);
            return true;
        }

        // not valid
        button = default;
        return false;
    }

    /// <summary>Get whether the given button is equivalent to <see cref="Options.useToolButton"/>.</summary>
    /// <param name="input">The button.</param>
    public static bool IsUseToolButton(this SButton input)
    {
        return input == SButton.ControllerX || Game1.options.useToolButton.Any(p => p.ToSButton() == input);
    }

    /// <summary>Get whether the given button is equivalent to <see cref="Options.actionButton"/>.</summary>
    /// <param name="input">The button.</param>
    public static bool IsActionButton(this SButton input)
    {
        return input == SButton.ControllerA || Game1.options.actionButton.Any(p => p.ToSButton() == input);
    }
}
