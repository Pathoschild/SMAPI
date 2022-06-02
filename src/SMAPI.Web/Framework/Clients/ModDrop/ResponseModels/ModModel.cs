namespace StardewModdingAPI.Web.Framework.Clients.ModDrop.ResponseModels;

/// <summary>An entry in a mod list from the ModDrop API.</summary>
public class ModModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The available file downloads.</summary>
    public FileDataModel[] Files { get; }

    /// <summary>The mod metadata.</summary>
    public ModDataModel Mod { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="files">The available file downloads.</param>
    /// <param name="mod">The mod metadata.</param>
    public ModModel(FileDataModel[] files, ModDataModel mod)
    {
        this.Files = files;
        this.Mod = mod;
    }
}
