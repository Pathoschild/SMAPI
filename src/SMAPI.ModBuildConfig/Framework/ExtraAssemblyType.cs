using System;

namespace StardewModdingAPI.ModBuildConfig.Framework;

/// <summary>An extra assembly type for the <see cref="DeployModTask.BundleExtraAssemblies"/> field.</summary>
[Flags]
internal enum ExtraAssemblyTypes
{
    /// <summary>Don't include extra assemblies.</summary>
    None = 0,

    /// <summary>Assembly files which are part of MonoGame, SMAPI, or Stardew Valley.</summary>
    Game = 1,

    /// <summary>Assembly files whose names start with <c>Microsoft.*</c> or <c>System.*</c>.</summary>
    System = 2,

    /// <summary>Assembly files which don't match any other category.</summary>
    ThirdParty = 4
}
