namespace StardewModdingAPI.Framework.Events;

/// <summary>An internal base class for event API classes.</summary>
internal abstract class ModEventsBase
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying event manager.</summary>
    protected readonly EventManager EventManager;

    /// <summary>The mod which uses this instance.</summary>
    protected readonly IModMetadata Mod;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    internal ModEventsBase(IModMetadata mod, EventManager eventManager)
    {
        this.Mod = mod;
        this.EventManager = eventManager;
    }
}
