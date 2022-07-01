namespace StardewModdingAPI.Framework.ModHelpers;

/// <summary>The common base class for mod helpers.</summary>
internal abstract class BaseHelper : IModLinked
{
    /*********
    ** Fields
    *********/
    /// <summary>The mod using this instance.</summary>
    protected readonly IModMetadata Mod;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string ModID => this.Mod.Manifest.UniqueID;


    /*********
    ** Protected methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod using this instance.</param>
    protected BaseHelper(IModMetadata mod)
    {
        this.Mod = mod;
    }
}
