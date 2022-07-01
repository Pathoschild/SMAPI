using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI;

/// <summary>The game's platform version.</summary>
public enum GamePlatform
{
    /// <summary>The Android version of the game.</summary>
    Android = Platform.Android,

    /// <summary>The Linux version of the game.</summary>
    Linux = Platform.Linux,

    /// <summary>The macOS version of the game.</summary>
    Mac = Platform.Mac,

    /// <summary>The Windows version of the game.</summary>
    Windows = Platform.Windows
}
