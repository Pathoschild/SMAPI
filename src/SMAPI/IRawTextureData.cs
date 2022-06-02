using Microsoft.Xna.Framework;

namespace StardewModdingAPI;

/// <summary>The raw data for an image read from the filesystem.</summary>
public interface IRawTextureData
{
    /// <summary>The image width.</summary>
    int Width { get; }

    /// <summary>The image height.</summary>
    int Height { get; }

    /// <summary>The loaded image data.</summary>
    Color[] Data { get; }
}
