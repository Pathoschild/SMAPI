using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace StardewModdingAPI.Framework.Content;

/// <summary>Encapsulates access and changes to image content being read from a data file.</summary>
internal class AssetDataForImage : AssetData<Texture2D>, IAssetDataForImage
{
    /*********
    ** Fields
    *********/
    /// <summary>The minimum value to consider non-transparent.</summary>
    /// <remarks>On Linux/macOS, fully transparent pixels may have an alpha up to 4 for some reason.</remarks>
    private const byte MinOpacity = 5;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="locale">The content's locale code, if the content is localized.</param>
    /// <param name="assetName">The asset name being read.</param>
    /// <param name="data">The content data being read.</param>
    /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
    /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
    public AssetDataForImage(string? locale, IAssetName assetName, Texture2D data, Func<string, string> getNormalizedPath, Action<Texture2D> onDataReplaced)
        : base(locale, assetName, data, getNormalizedPath, onDataReplaced) { }

    /// <inheritdoc />
    public void PatchImage(IRawTextureData source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMode patchMode = PatchMode.Replace)
    {
        this.GetPatchBounds(ref sourceArea, ref targetArea, source.Width, source.Height);

        // validate source data
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Can't patch from null source data.");

        // get the pixels for the source area
        Color[] sourceData;
        {
            int areaX = sourceArea.Value.X;
            int areaY = sourceArea.Value.Y;
            int areaWidth = sourceArea.Value.Width;
            int areaHeight = sourceArea.Value.Height;

            if (areaX == 0 && areaY == 0 && areaWidth == source.Width && areaHeight == source.Height)
                sourceData = source.Data;
            else
            {
                sourceData = new Color[areaWidth * areaHeight];
                int i = 0;
                for (int y = areaY, maxY = areaY + areaHeight - 1; y <= maxY; y++)
                {
                    for (int x = areaX, maxX = areaX + areaWidth - 1; x <= maxX; x++)
                    {
                        int targetIndex = (y * source.Width) + x;
                        sourceData[i++] = source.Data[targetIndex];
                    }
                }
            }
        }

        // apply
        this.PatchImageImpl(sourceData, source.Width, source.Height, sourceArea.Value, targetArea.Value, patchMode);
    }

    /// <inheritdoc />
    public void PatchImage(Texture2D source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMode patchMode = PatchMode.Replace)
    {
        this.GetPatchBounds(ref sourceArea, ref targetArea, source.Width, source.Height);

        // validate source texture
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Can't patch from a null source texture.");
        if (!source.Bounds.Contains(sourceArea.Value))
            throw new ArgumentOutOfRangeException(nameof(sourceArea), "The source area is outside the bounds of the source texture.");

        // get source data
        int pixelCount = sourceArea.Value.Width * sourceArea.Value.Height;
        Color[] sourceData = GC.AllocateUninitializedArray<Color>(pixelCount);
        source.GetData(0, sourceArea, sourceData, 0, pixelCount);

        // apply
        this.PatchImageImpl(sourceData, source.Width, source.Height, sourceArea.Value, targetArea.Value, patchMode);
    }

    /// <inheritdoc />
    public bool ExtendImage(int minWidth, int minHeight)
    {
        if (this.Data.Width >= minWidth && this.Data.Height >= minHeight)
            return false;

        Texture2D original = this.Data;
        Texture2D texture = new(Game1.graphics.GraphicsDevice, Math.Max(original.Width, minWidth), Math.Max(original.Height, minHeight));
        this.ReplaceWith(texture);
        this.PatchImage(original);
        return true;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the bounds for an image patch.</summary>
    /// <param name="sourceArea">The source area to set if needed.</param>
    /// <param name="targetArea">The target area to set if needed.</param>
    /// <param name="sourceWidth">The width of the full source image.</param>
    /// <param name="sourceHeight">The height of the full source image.</param>
    private void GetPatchBounds([NotNull] ref Rectangle? sourceArea, [NotNull] ref Rectangle? targetArea, int sourceWidth, int sourceHeight)
    {
        sourceArea ??= new Rectangle(0, 0, sourceWidth, sourceHeight);
        targetArea ??= new Rectangle(0, 0, Math.Min(sourceArea.Value.Width, this.Data.Width), Math.Min(sourceArea.Value.Height, this.Data.Height));
    }

    /// <summary>Overwrite part of the image.</summary>
    /// <param name="sourceData">The image data to patch into the content.</param>
    /// <param name="sourceWidth">The pixel width of the source image.</param>
    /// <param name="sourceHeight">The pixel height of the source image.</param>
    /// <param name="sourceArea">The part of the <paramref name="sourceData"/> to copy (or <c>null</c> to take the whole texture). This must be within the bounds of the <paramref name="sourceData"/> texture.</param>
    /// <param name="targetArea">The part of the content to patch (or <c>null</c> to patch the whole texture). The original content within this area will be erased. This must be within the bounds of the existing spritesheet.</param>
    /// <param name="patchMode">Indicates how an image should be patched.</param>
    /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="targetArea"/> is outside the bounds of the spritesheet.</exception>
    /// <exception cref="InvalidOperationException">The content being read isn't an image.</exception>
    private void PatchImageImpl(Color[] sourceData, int sourceWidth, int sourceHeight, Rectangle sourceArea, Rectangle targetArea, PatchMode patchMode)
    {
        // get texture
        Texture2D target = this.Data;
        int pixelCount = sourceArea.Width * sourceArea.Height;

        // validate
        if (sourceArea.X < 0 || sourceArea.Y < 0 || sourceArea.Right > sourceWidth || sourceArea.Bottom > sourceHeight)
            throw new ArgumentOutOfRangeException(nameof(sourceArea), "The source area is outside the bounds of the source texture.");
        if (!target.Bounds.Contains(targetArea))
            throw new ArgumentOutOfRangeException(nameof(targetArea), "The target area is outside the bounds of the target texture.");
        if (sourceArea.Size != targetArea.Size)
            throw new InvalidOperationException("The source and target areas must be the same size.");

        // merge data
        if (patchMode == PatchMode.Overlay)
        {
            // get target data
            Color[] mergedData = GC.AllocateUninitializedArray<Color>(pixelCount);
            target.GetData(0, targetArea, mergedData, 0, pixelCount);

            // merge pixels
            for (int i = 0; i < pixelCount; i++)
            {
                Color above = sourceData[i];
                Color below = mergedData[i];

                // shortcut transparency
                if (above.A < MinOpacity)
                    continue;
                if (below.A < MinOpacity)
                    mergedData[i] = above;

                // merge pixels
                else
                {
                    // This performs a conventional alpha blend for the pixels, which are already
                    // premultiplied by the content pipeline. The formula is derived from
                    // https://blogs.msdn.microsoft.com/shawnhar/2009/11/06/premultiplied-alpha/.
                    float alphaBelow = 1 - (above.A / 255f);
                    mergedData[i] = new Color(
                        r: (int)(above.R + (below.R * alphaBelow)),
                        g: (int)(above.G + (below.G * alphaBelow)),
                        b: (int)(above.B + (below.B * alphaBelow)),
                        alpha: Math.Max(above.A, below.A)
                    );
                }
            }

            target.SetData(0, targetArea, mergedData, 0, pixelCount);
        }
        else
            target.SetData(0, targetArea, sourceData, 0, pixelCount);
    }
}