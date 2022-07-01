namespace StardewModdingAPI.Internal.ConsoleWriting;

/// <summary>A monitor color scheme to use.</summary>
internal enum MonitorColorScheme
{
    /// <summary>Choose a color scheme automatically.</summary>
    AutoDetect,

    /// <summary>Use lighter text colors that look better on a black or dark background.</summary>
    DarkBackground,

    /// <summary>Use darker text colors that look better on a white or light background.</summary>
    LightBackground,

    /// <summary>Disable console color.</summary>
    None
}
