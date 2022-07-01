namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>Indicates the result of an assembly load.</summary>
internal enum AssemblyLoadStatus
{
    /// <summary>The assembly was loaded successfully.</summary>
    Okay = 1,

    /// <summary>The assembly could not be loaded.</summary>
    Failed = 2,

    /// <summary>The assembly is already loaded.</summary>
    AlreadyLoaded = 3
}
