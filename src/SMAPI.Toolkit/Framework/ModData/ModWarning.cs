using System;

namespace StardewModdingAPI.Toolkit.Framework.ModData;

/// <summary>Indicates a detected non-error mod issue.</summary>
[Flags]
public enum ModWarning
{
    /// <summary>No issues detected.</summary>
    None = 0,

    /// <summary>SMAPI detected incompatible code in the mod, but was configured to load it anyway.</summary>
    BrokenCodeLoaded = 1,

    /// <summary>The mod affects the save serializer in a way that may make saves unloadable without the mod.</summary>
    ChangesSaveSerializer = 2,

    /// <summary>The mod patches the game in a way that may impact stability.</summary>
    PatchesGame = 4,

#if SMAPI_DEPRECATED
    /// <summary>The mod uses the <c>dynamic</c> keyword which won't work on Linux/macOS.</summary>
    [Obsolete("This value is no longer used by SMAPI and will be removed in the upcoming SMAPI 4.0.0.")]
    UsesDynamic = 8,
#endif

    /// <summary>The mod references specialized 'unvalidated update tick' events which may impact stability.</summary>
    UsesUnvalidatedUpdateTick = 16,

    /// <summary>The mod has no update keys set.</summary>
    NoUpdateKeys = 32,

    /// <summary>Uses .NET APIs for reading and writing to the console.</summary>
    AccessesConsole = 64,

    /// <summary>Uses .NET APIs for filesystem access.</summary>
    AccessesFilesystem = 128,

    /// <summary>Uses .NET APIs for shell or process access.</summary>
    AccessesShell = 256,

#if SMAPI_DEPRECATED
    /// <summary>References the legacy <c>System.Configuration.ConfigurationManager</c> assembly and doesn't include a copy in the mod folder, so it'll break in SMAPI 4.0.0.</summary>
    DetectedLegacyConfigurationDll = 512,

    /// <summary>References the legacy <c>System.Runtime.Caching</c> assembly and doesn't include a copy in the mod folder, so it'll break in SMAPI 4.0.0.</summary>
    DetectedLegacyCachingDll = 1024,

    /// <summary>References the legacy <c>System.Security.Permissions</c> assembly and doesn't include a copy in the mod folder, so it'll break in SMAPI 4.0.0.</summary>
    DetectedLegacyPermissionsDll = 2048
#endif
}
