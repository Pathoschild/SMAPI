using System;
using System.Linq;

namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi;

/// <summary>Specifies the identifiers for a mod to match.</summary>
public class ModSearchEntryModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique mod ID.</summary>
    public string ID { get; }

    /// <summary>The namespaced mod update keys (if available).</summary>
    public string[] UpdateKeys { get; private set; }

    /// <summary>The mod version installed by the local player. This is used for version mapping in some cases.</summary>
    public ISemanticVersion? InstalledVersion { get; }

    /// <summary>Whether the installed version is broken or could not be loaded.</summary>
    public bool IsBroken { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="id">The unique mod ID.</param>
    /// <param name="installedVersion">The version installed by the local player. This is used for version mapping in some cases.</param>
    /// <param name="updateKeys">The namespaced mod update keys (if available).</param>
    /// <param name="isBroken">Whether the installed version is broken or could not be loaded.</param>
    public ModSearchEntryModel(string id, ISemanticVersion? installedVersion, string[]? updateKeys, bool isBroken = false)
    {
        this.ID = id;
        this.InstalledVersion = installedVersion;
        this.UpdateKeys = updateKeys ?? Array.Empty<string>();
        this.IsBroken = isBroken;
    }

    /// <summary>Add update keys for the mod.</summary>
    /// <param name="updateKeys">The update keys to add.</param>
    public void AddUpdateKeys(params string[] updateKeys)
    {
        this.UpdateKeys = this.UpdateKeys.Concat(updateKeys).ToArray();
    }
}
