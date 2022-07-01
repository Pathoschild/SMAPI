namespace StardewModdingAPI.Web.ViewModels;

/// <summary>Metadata about a link.</summary>
public class ModLinkModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The URL of the linked page.</summary>
    public string Url { get; }

    /// <summary>The suggested link text.</summary>
    public string Text { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="url">The URL of the linked page.</param>
    /// <param name="text">The suggested link text.</param>
    public ModLinkModel(string url, string text)
    {
        this.Url = url;
        this.Text = text;
    }
}
