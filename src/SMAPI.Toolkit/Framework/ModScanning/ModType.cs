namespace StardewModdingAPI.Toolkit.Framework.ModScanning;

/// <summary>A general mod type.</summary>
public enum ModType
{
    /// <summary>The mod is invalid and its type could not be determined.</summary>
    Invalid,

    /// <summary>The folder is ignored by convention.</summary>
    Ignored,

    /// <summary>A mod which uses SMAPI directly.</summary>
    Smapi,

    /// <summary>A mod which contains files loaded by a SMAPI mod.</summary>
    ContentPack,

    /// <summary>A legacy mod which replaces game files directly.</summary>
    Xnb
}
