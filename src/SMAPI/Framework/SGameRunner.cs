using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Framework;

/// <summary>SMAPI's extension of the game's core <see cref="GameRunner"/>, used to inject SMAPI components.</summary>
internal class SGameRunner : GameRunner
{
    /*********
    ** Fields
    *********/
    /// <summary>Encapsulates monitoring and logging for SMAPI.</summary>
    private readonly Monitor Monitor;

    /// <summary>Manages SMAPI events for mods.</summary>
    private readonly EventManager Events;

    /// <summary>Simplifies access to private game code.</summary>
    private readonly Reflector Reflection;

    /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
    private readonly Action<string> ExitGameImmediately;

    /// <summary>The core SMAPI mod hooks.</summary>
    private readonly SModHooks ModHooks;

    /// <summary>The core multiplayer logic.</summary>
    private readonly SMultiplayer Multiplayer;

    /// <summary>Raised after the game finishes loading its initial content.</summary>
    private readonly Action OnGameContentLoaded;

    /// <summary>Raised when XNA is updating (roughly 60 times per second).</summary>
    private readonly Action<GameTime, Action> OnGameUpdating;

    /// <summary>Raised when the game instance for a local split-screen player is updating (once per <see cref="OnGameUpdating"/> per player).</summary>
    private readonly Action<SGame, GameTime, Action> OnPlayerInstanceUpdating;

    /// <summary>Raised before the game exits.</summary>
    private readonly Action OnGameExiting;


    /*********
    ** Public methods
    *********/
    /// <summary>The singleton instance.</summary>
    public static SGameRunner Instance => (SGameRunner)GameRunner.instance;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
    /// <param name="reflection">Simplifies access to private game code.</param>
    /// <param name="eventManager">Manages SMAPI events for mods.</param>
    /// <param name="modHooks">Handles mod hooks provided by the game.</param>
    /// <param name="multiplayer">The core multiplayer logic.</param>
    /// <param name="exitGameImmediately">Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</param>
    /// <param name="onGameContentLoaded">Raised after the game finishes loading its initial content.</param>
    /// <param name="onGameUpdating">Raised when XNA is updating its state (roughly 60 times per second).</param>
    /// <param name="onPlayerInstanceUpdating">Raised when the game instance for a local split-screen player is updating (once per <see cref="OnGameUpdating"/> per player).</param>
    /// <param name="onGameExiting">Raised before the game exits.</param>
    public SGameRunner(Monitor monitor, Reflector reflection, EventManager eventManager, SModHooks modHooks, SMultiplayer multiplayer, Action<string> exitGameImmediately, Action onGameContentLoaded, Action<GameTime, Action> onGameUpdating, Action<SGame, GameTime, Action> onPlayerInstanceUpdating, Action onGameExiting)
    {
        // init XNA
        Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

        // hook into game
        this.ModHooks = modHooks;

        // init SMAPI
        this.Monitor = monitor;
        this.Events = eventManager;
        this.Reflection = reflection;
        this.Multiplayer = multiplayer;
        this.ExitGameImmediately = exitGameImmediately;
        this.OnGameContentLoaded = onGameContentLoaded;
        this.OnGameUpdating = onGameUpdating;
        this.OnPlayerInstanceUpdating = onPlayerInstanceUpdating;
        this.OnGameExiting = onGameExiting;
    }

    /// <summary>Create a game instance for a local player.</summary>
    /// <param name="playerIndex">The player index.</param>
    /// <param name="instanceIndex">The instance index.</param>
    public override Game1 CreateGameInstance(PlayerIndex playerIndex = PlayerIndex.One, int instanceIndex = 0)
    {
        SInputState inputState = new();
        return new SGame(playerIndex, instanceIndex, this.Monitor, this.Reflection, this.Events, inputState, this.ModHooks, this.Multiplayer, this.ExitGameImmediately, this.OnPlayerInstanceUpdating, this.OnGameContentLoaded);
    }

    /// <inheritdoc />
    public override void AddGameInstance(PlayerIndex playerIndex)
    {
        base.AddGameInstance(playerIndex);

        EarlyConstants.LogScreenId = Context.ScreenId;
        this.UpdateForSplitScreenChanges();
    }

    /// <inheritdoc />
    public override void RemoveGameInstance(Game1 gameInstance)
    {
        base.RemoveGameInstance(gameInstance);

        if (this.gameInstances.Count <= 1)
            EarlyConstants.LogScreenId = null;
        this.UpdateForSplitScreenChanges();
    }

    /// <summary>Get the screen ID for a given player ID, if the player is local.</summary>
    /// <param name="playerId">The player ID to check.</param>
    public int? GetScreenId(long playerId)
    {
        return this.gameInstances
            .FirstOrDefault(p => ((SGame)p).PlayerId == playerId)
            ?.instanceId;
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Perform cleanup logic when the game exits.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The event args.</param>
    /// <remarks>This overrides the logic in <see cref="Game1.exitEvent"/> to let SMAPI clean up before exit.</remarks>
    protected override void OnExiting(object sender, EventArgs args)
    {
        this.OnGameExiting();
    }

    /// <summary>The method called when the game is updating its state (roughly 60 times per second).</summary>
    /// <param name="gameTime">A snapshot of the game timing state.</param>
    protected override void Update(GameTime gameTime)
    {
        this.OnGameUpdating(gameTime, () => base.Update(gameTime));
    }

    /// <summary>Update metadata when a split screen is added or removed.</summary>
    private void UpdateForSplitScreenChanges()
    {
        HashSet<int> oldScreenIds = new(Context.ActiveScreenIds);

        // track active screens
        Context.ActiveScreenIds.Clear();
        foreach (var screen in this.gameInstances)
            Context.ActiveScreenIds.Add(screen.instanceId);

        // remember last removed screen
        foreach (int id in oldScreenIds)
        {
            if (!Context.ActiveScreenIds.Contains(id))
                Context.LastRemovedScreenId = id;
        }
    }
}