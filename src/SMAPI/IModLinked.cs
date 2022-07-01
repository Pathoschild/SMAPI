namespace StardewModdingAPI;

/// <summary>An instance linked to a mod.</summary>
public interface IModLinked
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique ID of the mod for which the instance was created.</summary>
    string ModID { get; }
}
