namespace StardewModdingAPI.Web.Framework.Clients.ModDrop.ResponseModels;

/// <summary>Metadata about a mod from the ModDrop API.</summary>
public class ModDataModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod's unique ID on ModDrop.</summary>
    public int ID { get; set; }

    /// <summary>The mod name.</summary>
    public string Title { get; set; }

    /// <summary>The error code, if any.</summary>
    public int? ErrorCode { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="id">The mod's unique ID on ModDrop.</param>
    /// <param name="title">The mod name.</param>
    /// <param name="errorCode">The error code, if any.</param>
    public ModDataModel(int id, string title, int? errorCode)
    {
        this.ID = id;
        this.Title = title;
        this.ErrorCode = errorCode;
    }
}
