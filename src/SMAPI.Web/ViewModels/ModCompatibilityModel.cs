using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

namespace StardewModdingAPI.Web.ViewModels;

/// <summary>Metadata about a mod's compatibility with the latest versions of SMAPI and Stardew Valley.</summary>
public class ModCompatibilityModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The compatibility status, as a string like <c>"Broken"</c>.</summary>
    public string Status { get; }

    /// <summary>The human-readable summary, as an HTML block.</summary>
    public string? Summary { get; }

    /// <summary>The game or SMAPI version which broke this mod (if applicable).</summary>
    public string? BrokeIn { get; }

    /// <summary>A link to the unofficial version which fixes compatibility, if any.</summary>
    public ModLinkModel? UnofficialVersion { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="status">The compatibility status, as a string like <c>"Broken"</c>.</param>
    /// <param name="summary">The human-readable summary, as an HTML block.</param>
    /// <param name="brokeIn">The game or SMAPI version which broke this mod (if applicable).</param>
    /// <param name="unofficialVersion">A link to the unofficial version which fixes compatibility, if any.</param>
    [JsonConstructor]
    public ModCompatibilityModel(string status, string? summary, string? brokeIn, ModLinkModel? unofficialVersion)
    {
        this.Status = status;
        this.Summary = summary;
        this.BrokeIn = brokeIn;
        this.UnofficialVersion = unofficialVersion;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="info">The mod metadata.</param>
    public ModCompatibilityModel(WikiCompatibilityInfo info)
    {
        this.Status = info.Status.ToString();
        this.Status = this.Status.Substring(0, 1).ToLower() + this.Status.Substring(1);

        this.Summary = info.Summary;
        this.BrokeIn = info.BrokeIn;
        if (info.UnofficialVersion != null)
            this.UnofficialVersion = new ModLinkModel(info.UnofficialUrl!, info.UnofficialVersion.ToString());
    }
}
