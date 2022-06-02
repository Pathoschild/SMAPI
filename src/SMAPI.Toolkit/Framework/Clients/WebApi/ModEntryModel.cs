using System;

namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi;

/// <summary>Metadata about a mod.</summary>
public class ModEntryModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod's unique ID (if known).</summary>
    public string ID { get; }

    /// <summary>The update version recommended by the web API based on its version update and mapping rules.</summary>
    public ModEntryVersionModel? SuggestedUpdate { get; set; }

    /// <summary>Optional extended data which isn't needed for update checks.</summary>
    public ModExtendedMetadataModel? Metadata { get; set; }

    /// <summary>The errors that occurred while fetching update data.</summary>
    public string[] Errors { get; set; } = Array.Empty<string>();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="id">The mod's unique ID (if known).</param>
    public ModEntryModel(string id)
    {
        this.ID = id;
    }
}
