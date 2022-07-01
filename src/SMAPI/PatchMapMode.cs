namespace StardewModdingAPI;

/// <summary>Indicates how a map should be patched.</summary>
public enum PatchMapMode
{
    /// <summary>Replace matching tiles. Target tiles missing in the source area are kept as-is.</summary>
    Overlay,

    /// <summary>Replace all tiles on layers that exist in the source map.</summary>
    ReplaceByLayer,

    /// <summary>Replace all tiles with the source map.</summary>
    Replace
}
