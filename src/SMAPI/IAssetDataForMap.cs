using Microsoft.Xna.Framework;
using xTile;

namespace StardewModdingAPI;

/// <summary>Encapsulates access and changes to map content being read from a data file.</summary>
public interface IAssetDataForMap : IAssetData<Map>
{
    /*********
    ** Public methods
    *********/
    /// <summary>Copy layers, tiles, and tilesheets from another map onto the asset.</summary>
    /// <param name="source">The map from which to copy.</param>
    /// <param name="sourceArea">The tile area within the source map to copy, or <c>null</c> for the entire source map size. This must be within the bounds of the <paramref name="source"/> map.</param>
    /// <param name="targetArea">The tile area within the target map to overwrite, or <c>null</c> to patch the whole map. The original content within this area will be erased. This must be within the bounds of the existing map.</param>
    /// <param name="patchMode">Indicates how the map should be patched.</param>
    void PatchMap(Map source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMapMode patchMode = PatchMapMode.Overlay);

    /// <summary>Extend the map if needed to fit the given size. Note that this is an expensive operation and resizes the map in-place.</summary>
    /// <param name="minWidth">The minimum map width in tiles.</param>
    /// <param name="minHeight">The minimum map height in tiles.</param>
    /// <returns>Whether the map was resized.</returns>
    bool ExtendMap(int minWidth = 0, int minHeight = 0);
}
