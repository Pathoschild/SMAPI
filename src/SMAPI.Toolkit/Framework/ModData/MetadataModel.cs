using System.Collections.Generic;

namespace StardewModdingAPI.Toolkit.Framework.ModData;

/// <summary>The SMAPI predefined metadata.</summary>
internal class MetadataModel
{
    /********
    ** Accessors
    ********/
    /// <summary>Extra metadata about mods.</summary>
    public IDictionary<string, ModDataModel> ModData { get; } = new Dictionary<string, ModDataModel>();
}
