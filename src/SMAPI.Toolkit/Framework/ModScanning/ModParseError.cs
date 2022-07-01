namespace StardewModdingAPI.Toolkit.Framework.ModScanning;

/// <summary>Indicates why a mod could not be parsed.</summary>
public enum ModParseError
{
    /// <summary>No parse error.</summary>
    None,

    /// <summary>The folder is empty or contains only ignored files.</summary>
    EmptyFolder,

    /// <summary>The folder is an empty folder managed by Vortex.</summary>
    EmptyVortexFolder,

    /// <summary>The folder is ignored by convention.</summary>
    IgnoredFolder,

    /// <summary>The mod's <c>manifest.json</c> could not be parsed.</summary>
    ManifestInvalid,

    /// <summary>The folder contains non-ignored and non-XNB files, but none of them are <c>manifest.json</c>.</summary>
    ManifestMissing,

    /// <summary>The folder is an XNB mod, which can't be loaded through SMAPI.</summary>
    XnbMod
}
