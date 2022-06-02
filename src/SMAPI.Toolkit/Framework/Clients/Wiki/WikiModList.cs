namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

/// <summary>Metadata from the wiki's mod compatibility list.</summary>
public class WikiModList
{
    /*********
    ** Accessors
    *********/
    /// <summary>The stable game version.</summary>
    public string? StableVersion { get; }

    /// <summary>The beta game version (if any).</summary>
    public string? BetaVersion { get; }

    /// <summary>The mods on the wiki.</summary>
    public WikiModEntry[] Mods { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="stableVersion">The stable game version.</param>
    /// <param name="betaVersion">The beta game version (if any).</param>
    /// <param name="mods">The mods on the wiki.</param>
    public WikiModList(string? stableVersion, string? betaVersion, WikiModEntry[] mods)
    {
        this.StableVersion = stableVersion;
        this.BetaVersion = betaVersion;
        this.Mods = mods;
    }
}
