using Microsoft.Xna.Framework;

namespace StardewModdingAPI.Framework.Content;

/// <summary>The raw data for an image read from the filesystem.</summary>
/// <param name="Width">The image width.</param>
/// <param name="Height">The image height.</param>
/// <param name="Data">The loaded image data.</param>
internal record RawTextureData(int Width, int Height, Color[] Data) : IRawTextureData;
