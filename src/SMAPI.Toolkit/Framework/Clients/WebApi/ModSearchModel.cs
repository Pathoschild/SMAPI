using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi;

/// <summary>Specifies mods whose update-check info to fetch.</summary>
public class ModSearchModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mods for which to find data.</summary>
    public ModSearchEntryModel[] Mods { get; set; }

    /// <summary>Whether to include extended metadata for each mod.</summary>
    public bool IncludeExtendedMetadata { get; set; }

    /// <summary>The SMAPI version installed by the player. This is used for version mapping in some cases.</summary>
    public ISemanticVersion ApiVersion { get; set; }

    /// <summary>The Stardew Valley version installed by the player.</summary>
    public ISemanticVersion GameVersion { get; set; }

    /// <summary>The OS on which the player plays.</summary>
    public Platform Platform { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an empty instance.</summary>
    [Obsolete("This constructor only exists to support ASP.NET model binding, and shouldn't be used directly.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used by ASP.NET model binding.")]
    public ModSearchModel()
    {
        // ASP.NET Web API needs a public empty constructor for top-level request models, and
        // it'll fail if the other constructor is marked with [JsonConstructor]. Apparently
        // it's fine with non-empty constructors in nested models like ModSearchEntryModel.
        this.Mods = Array.Empty<ModSearchEntryModel>();
        this.ApiVersion = null!;
        this.GameVersion = null!;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="mods">The mods to search.</param>
    /// <param name="apiVersion">The SMAPI version installed by the player. If this is null, the API won't provide a recommended update.</param>
    /// <param name="gameVersion">The Stardew Valley version installed by the player.</param>
    /// <param name="platform">The OS on which the player plays.</param>
    /// <param name="includeExtendedMetadata">Whether to include extended metadata for each mod.</param>
    public ModSearchModel(ModSearchEntryModel[] mods, ISemanticVersion apiVersion, ISemanticVersion gameVersion, Platform platform, bool includeExtendedMetadata)
    {
        this.Mods = mods.ToArray();
        this.ApiVersion = apiVersion;
        this.GameVersion = gameVersion;
        this.Platform = platform;
        this.IncludeExtendedMetadata = includeExtendedMetadata;
    }
}
