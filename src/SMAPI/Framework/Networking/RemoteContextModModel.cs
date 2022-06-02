namespace StardewModdingAPI.Framework.Networking;

/// <summary>Metadata about an installed mod exchanged with connected computers.</summary>
public class RemoteContextModModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique mod ID.</summary>
    public string ID { get; }

    /// <summary>The mod's display name.</summary>
    public string Name { get; }

    /// <summary>The mod version.</summary>
    public ISemanticVersion Version { get; }


    /*********
    ** Accessors
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="id">The unique mod ID.</param>
    /// <param name="name">The mod's display name.</param>
    /// <param name="version">The mod version.</param>
    public RemoteContextModModel(string id, string name, ISemanticVersion version)
    {
        this.ID = id;
        this.Name = name;
        this.Version = version;
    }
}
