using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Web.ViewModels;

/// <summary>Metadata for the mod list page.</summary>
public class ModListModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The current stable version of the game.</summary>
    public string? StableVersion { get; }

    /// <summary>The current beta version of the game (if any).</summary>
    public string? BetaVersion { get; }

    /// <summary>The mods to display.</summary>
    public ModModel[] Mods { get; }

    /// <summary>When the data was last updated.</summary>
    public DateTimeOffset LastUpdated { get; }

    /// <summary>Whether the data hasn't been updated in a while.</summary>
    public bool IsStale { get; }

    /// <summary>Whether the mod metadata is available.</summary>
    public bool HasData => this.Mods.Any();


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="stableVersion">The current stable version of the game.</param>
    /// <param name="betaVersion">The current beta version of the game (if any).</param>
    /// <param name="mods">The mods to display.</param>
    /// <param name="lastUpdated">When the data was last updated.</param>
    /// <param name="isStale">Whether the data hasn't been updated in a while.</param>
    public ModListModel(string? stableVersion, string? betaVersion, IEnumerable<ModModel> mods, DateTimeOffset lastUpdated, bool isStale)
    {
        this.StableVersion = stableVersion;
        this.BetaVersion = betaVersion;
        this.Mods = mods.ToArray();
        this.LastUpdated = lastUpdated;
        this.IsStale = isStale;
    }
}
