namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

/// <summary>Compatibility info for a mod.</summary>
public class WikiCompatibilityInfo
{
    /*********
    ** Accessors
    *********/
    /// <summary>The compatibility status.</summary>
    public WikiCompatibilityStatus Status { get; }

    /// <summary>The human-readable summary of the compatibility status or workaround, without HTML formatting.</summary>
    public string? Summary { get; }

    /// <summary>The game or SMAPI version which broke this mod, if applicable.</summary>
    public string? BrokeIn { get; }

    /// <summary>The version of the latest unofficial update, if applicable.</summary>
    public ISemanticVersion? UnofficialVersion { get; }

    /// <summary>The URL to the latest unofficial update, if applicable.</summary>
    public string? UnofficialUrl { get; }


    /*********
    ** Accessors
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="status">The compatibility status.</param>
    /// <param name="summary">The human-readable summary of the compatibility status or workaround, without HTML formatting.</param>
    /// <param name="brokeIn">The game or SMAPI version which broke this mod, if applicable.</param>
    /// <param name="unofficialVersion">The version of the latest unofficial update, if applicable.</param>
    /// <param name="unofficialUrl">The URL to the latest unofficial update, if applicable.</param>
    public WikiCompatibilityInfo(WikiCompatibilityStatus status, string? summary, string? brokeIn, ISemanticVersion? unofficialVersion, string? unofficialUrl)
    {
        this.Status = status;
        this.Summary = summary;
        this.BrokeIn = brokeIn;
        this.UnofficialVersion = unofficialVersion;
        this.UnofficialUrl = unofficialUrl;
    }
}
