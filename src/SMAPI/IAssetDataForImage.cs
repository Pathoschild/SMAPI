using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewModdingAPI;

/// <summary>Encapsulates access and changes to image content being read from a data file.</summary>
public interface IAssetDataForImage : IAssetData<Texture2D>
{
    /*********
    ** Public methods
    *********/
    /// <summary>Overwrite part of the image.</summary>
    /// <param name="source">The image to patch into the content.</param>
    /// <param name="sourceArea">The part of the <paramref name="source"/> to copy (or <c>null</c> to take the whole texture). This must be within the bounds of the <paramref name="source"/> texture.</param>
    /// <param name="targetArea">The part of the content to patch (or <c>null</c> to patch the whole texture). The original content within this area will be erased. This must be within the bounds of the existing spritesheet.</param>
    /// <param name="patchMode">Indicates how an image should be patched.</param>
    /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="targetArea"/> is outside the bounds of the spritesheet.</exception>
    /// <exception cref="InvalidOperationException">The content being read isn't an image.</exception>
    void PatchImage(IRawTextureData source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMode patchMode = PatchMode.Replace);

    /// <summary>Overwrite part of the image.</summary>
    /// <param name="source">The image to patch into the content.</param>
    /// <param name="sourceArea">The part of the <paramref name="source"/> to copy (or <c>null</c> to take the whole texture). This must be within the bounds of the <paramref name="source"/> texture.</param>
    /// <param name="targetArea">The part of the content to patch (or <c>null</c> to patch the whole texture). The original content within this area will be erased. This must be within the bounds of the existing spritesheet.</param>
    /// <param name="patchMode">Indicates how an image should be patched.</param>
    /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="targetArea"/> is outside the bounds of the spritesheet.</exception>
    /// <exception cref="InvalidOperationException">The content being read isn't an image.</exception>
    void PatchImage(Texture2D source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMode patchMode = PatchMode.Replace);

    /// <summary>Extend the image if needed to fit the given size. Note that this is an expensive operation, creates a new texture instance, and that extending a spritesheet horizontally may cause game errors or bugs.</summary>
    /// <param name="minWidth">The minimum texture width in pixels.</param>
    /// <param name="minHeight">The minimum texture height in pixels.</param>
    /// <returns>Whether the texture was resized.</returns>
    bool ExtendImage(int minWidth, int minHeight);
}
