namespace StardewModdingAPI.Framework;

/// <summary>A generic tuple which links something to a mod.</summary>
/// <typeparam name="T">The interceptor type.</typeparam>
internal class ModLinked<T>
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod metadata.</summary>
    public IModMetadata Mod { get; }

    /// <summary>The instance linked to the mod.</summary>
    public T Data { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod metadata.</param>
    /// <param name="data">The instance linked to the mod.</param>
    public ModLinked(IModMetadata mod, T data)
    {
        this.Mod = mod;
        this.Data = data;
    }
}
