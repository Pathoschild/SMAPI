namespace StardewModdingAPI;

/// <summary>Indicates how an image should be patched.</summary>
public enum PatchMode
{
    /// <summary>Erase the original content within the area before drawing the new content.</summary>
    Replace,

    /// <summary>Draw the new content over the original content, so the original content shows through any transparent pixels.</summary>
    Overlay
}