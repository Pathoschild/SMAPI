using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events;

/// <inheritdoc />
internal class ModEvents : IModEvents
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public IContentEvents Content { get; }

    /// <inheritdoc />
    public IDisplayEvents Display { get; }

    /// <inheritdoc />
    public IGameLoopEvents GameLoop { get; }

    /// <inheritdoc />
    public IInputEvents Input { get; }

    /// <inheritdoc />
    public IMultiplayerEvents Multiplayer { get; }

    /// <inheritdoc />
    public IPlayerEvents Player { get; }

    /// <inheritdoc />
    public IWorldEvents World { get; }

    /// <inheritdoc />
    public ISpecializedEvents Specialized { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    public ModEvents(IModMetadata mod, EventManager eventManager)
    {
        this.Content = new ModContentEvents(mod, eventManager);
        this.Display = new ModDisplayEvents(mod, eventManager);
        this.GameLoop = new ModGameLoopEvents(mod, eventManager);
        this.Input = new ModInputEvents(mod, eventManager);
        this.Multiplayer = new ModMultiplayerEvents(mod, eventManager);
        this.Player = new ModPlayerEvents(mod, eventManager);
        this.World = new ModWorldEvents(mod, eventManager);
        this.Specialized = new ModSpecializedEvents(mod, eventManager);
    }
}
