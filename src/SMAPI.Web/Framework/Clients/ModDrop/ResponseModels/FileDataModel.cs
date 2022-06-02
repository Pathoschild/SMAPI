using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.ModDrop.ResponseModels;

/// <summary>Metadata from the ModDrop API about a mod file.</summary>
public class FileDataModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The file title.</summary>
    [JsonProperty("title")]
    public string Name { get; }

    /// <summary>The file description.</summary>
    [JsonProperty("desc")]
    public string Description { get; }

    /// <summary>The file version.</summary>
    public string Version { get; }

    /// <summary>Whether the file is deleted.</summary>
    public bool IsDeleted { get; }

    /// <summary>Whether the file is hidden from users.</summary>
    public bool IsHidden { get; }

    /// <summary>Whether this is the default file for the mod.</summary>
    public bool IsDefault { get; }

    /// <summary>Whether this is an archived file.</summary>
    public bool IsOld { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">The file title.</param>
    /// <param name="description">The file description.</param>
    /// <param name="version">The file version.</param>
    /// <param name="isDeleted">Whether the file is deleted.</param>
    /// <param name="isHidden">Whether the file is hidden from users.</param>
    /// <param name="isDefault">Whether this is the default file for the mod.</param>
    /// <param name="isOld">Whether this is an archived file.</param>
    public FileDataModel(string name, string description, string version, bool isDeleted, bool isHidden, bool isDefault, bool isOld)
    {
        this.Name = name;
        this.Description = description;
        this.Version = version;
        this.IsDeleted = isDeleted;
        this.IsHidden = isHidden;
        this.IsDefault = isDefault;
        this.IsOld = isOld;
    }
}
