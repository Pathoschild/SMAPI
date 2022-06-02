namespace StardewModdingAPI.Web.Framework.ConfigModels;

/// <summary>The site config settings.</summary>
public class SiteConfig // must be public to pass into views
{
    /*********
    ** Accessors
    *********/
    /// <summary>A message to show below the download button (e.g. for details on downloading a beta version), in Markdown format.</summary>
    public string? OtherBlurb { get; set; }

    /// <summary>A list of supports to credit on the main page, in Markdown format.</summary>
    public string? SupporterList { get; set; }
}
