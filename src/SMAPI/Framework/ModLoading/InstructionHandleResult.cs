using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>Indicates how an instruction was handled.</summary>
internal enum InstructionHandleResult
{
    /// <summary>No special handling is needed.</summary>
    None,

    /// <summary>The instruction was successfully rewritten for compatibility.</summary>
    Rewritten,

    /// <summary>The instruction is not compatible and can't be rewritten for compatibility.</summary>
    NotCompatible,

    /// <summary>The instruction is compatible, but patches the game in a way that may impact stability.</summary>
    DetectedGamePatch,

    /// <summary>The instruction is compatible, but affects the save serializer in a way that may make saves unloadable without the mod.</summary>
    DetectedSaveSerializer,

    /// <summary>The instruction is compatible, but references <see cref="ISpecializedEvents.UnvalidatedUpdateTicking"/> or <see cref="ISpecializedEvents.UnvalidatedUpdateTicked"/> which may impact stability.</summary>
    DetectedUnvalidatedUpdateTick,

    /// <summary>The instruction accesses the SMAPI console directly.</summary>
    DetectedConsoleAccess,

    /// <summary>The instruction accesses the filesystem directly.</summary>
    DetectedFilesystemAccess,

    /// <summary>The instruction accesses the OS shell or processes directly.</summary>
    DetectedShellAccess,

#if SMAPI_DEPRECATED
    /// <summary>The module references the legacy <c>System.Configuration.ConfigurationManager</c> assembly and doesn't include a copy in the mod folder, so it'll break in SMAPI 4.0.0.</summary>
    DetectedLegacyConfigurationDll,

    /// <summary>The module references the legacy <c>System.Runtime.Caching</c> assembly and doesn't include a copy in the mod folder, so it'll break in SMAPI 4.0.0.</summary>
    DetectedLegacyCachingDll,

    /// <summary>The module references the legacy <c>System.Security.Permissions</c> assembly and doesn't include a copy in the mod folder, so it'll break in SMAPI 4.0.0.</summary>
    DetectedLegacyPermissionsDll
#endif
}
