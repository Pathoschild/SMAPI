namespace StardewModdingAPI;

/// <summary>Specifies a source containing content that can be loaded.</summary>
public enum ContentSource
{
    /// <summary>Assets in the game's content manager (i.e. XNBs in the game's content folder).</summary>
    GameContent,

    /// <summary>XNB files in the current mod's folder.</summary>
    ModFolder
}
