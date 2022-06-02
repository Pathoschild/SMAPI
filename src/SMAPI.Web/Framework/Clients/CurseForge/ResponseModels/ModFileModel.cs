namespace StardewModdingAPI.Web.Framework.Clients.CurseForge.ResponseModels;

/// <summary>Metadata from the CurseForge API about a mod file.</summary>
public class ModFileModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The file name as downloaded.</summary>
    public string FileName { get; }

    /// <summary>The file display name.</summary>
    public string? DisplayName { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fileName">The file name as downloaded.</param>
    /// <param name="displayName">The file display name.</param>
    public ModFileModel(string fileName, string? displayName)
    {
        this.FileName = fileName;
        this.DisplayName = displayName;
    }
}
