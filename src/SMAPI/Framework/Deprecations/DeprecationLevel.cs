namespace StardewModdingAPI.Framework.Deprecations;

/// <summary>Indicates how deprecated something is.</summary>
internal enum DeprecationLevel
{
    /// <summary>It's deprecated but won't be removed soon. Mod authors have some time to update their mods. Deprecation warnings should be logged, but not written to the console.</summary>
    Notice,

    /// <summary>Mods should no longer be using it. Deprecation messages should be debug entries in the console.</summary>
    Info,

    /// <summary>The code will be removed soon. Deprecation messages should be warnings in the console.</summary>
    PendingRemoval
}
