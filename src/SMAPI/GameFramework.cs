using System;

namespace StardewModdingAPI
{
    /// <summary>The game framework running the game.</summary>
    public enum GameFramework
    {
        /// <summary>The XNA Framework, previously used on Windows.</summary>
        [Obsolete("Stardew Valley no longer uses XNA Framework on any supported platform.")]
        Xna,

        /// <summary>The MonoGame framework.</summary>
        MonoGame
    }
}
