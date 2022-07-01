namespace StardewModdingAPI.Events;

/// <summary>Manages access to events raised by SMAPI.</summary>
public interface IModEvents
{
    /// <summary>Events related to assets loaded from the content pipeline (including data, maps, and textures).</summary>
    IContentEvents Content { get; }

    /// <summary>Events related to UI and drawing to the screen.</summary>
    IDisplayEvents Display { get; }

    /// <summary>Events linked to the game's update loop. The update loop runs roughly ≈60 times/second to run game logic like state changes, action handling, etc. These can be useful, but you should consider more semantic events like <see cref="Input"/> if possible.</summary>
    IGameLoopEvents GameLoop { get; }

    /// <summary>Events raised when the player provides input using a controller, keyboard, or mouse.</summary>
    IInputEvents Input { get; }

    /// <summary>Events raised for multiplayer messages and connections.</summary>
    IMultiplayerEvents Multiplayer { get; }

    /// <summary>Events raised when the player data changes.</summary>
    IPlayerEvents Player { get; }

    /// <summary>Events raised when something changes in the world.</summary>
    IWorldEvents World { get; }

    /// <summary>Events serving specialized edge cases that shouldn't be used by most mods.</summary>
    ISpecializedEvents Specialized { get; }
}
