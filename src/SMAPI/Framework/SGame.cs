using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Rendering;
using StardewModdingAPI.Framework.StateTracking.Snapshots;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Logging;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace StardewModdingAPI.Framework
{
    /// <summary>SMAPI's extension of the game's core <see cref="Game1"/>, used to inject events.</summary>
    internal class SGame : Game1
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging for SMAPI.</summary>
        private readonly Monitor Monitor;

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from a draw error.</summary>
        private readonly Countdown DrawCrashTimer = new(60); // 60 ticks = roughly one second

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        private readonly Action<string> ExitGameImmediately;

        /// <summary>The initial override for <see cref="Input"/>. This value is null after initialization.</summary>
        private SInputState? InitialInput;

        /// <summary>The initial override for <see cref="Multiplayer"/>. This value is null after initialization.</summary>
        private SMultiplayer? InitialMultiplayer;

        /// <summary>Raised when the instance is updating its state (roughly 60 times per second).</summary>
        private readonly Action<SGame, GameTime, Action> OnUpdating;

        /// <summary>Raised after the instance finishes loading its initial content.</summary>
        private readonly Action OnContentLoaded;

        /// <summary>Raised invoke when the load stage changes through a method like <see cref="Game1.CleanupReturningToTitle"/>.</summary>
        private readonly Action<LoadStage> OnLoadStageChanged;

        /// <summary>Raised after the instance finishes a draw loop.</summary>
        private readonly Action<RenderTarget2D> OnRendered;


        /*********
        ** Accessors
        *********/
        /// <summary>Manages input visible to the game.</summary>
        public SInputState Input => (SInputState)Game1.input;

        /// <summary>Monitors the entire game state for changes.</summary>
        public WatcherCore Watchers { get; private set; } = null!; // initialized on first update tick

        /// <summary>A snapshot of the current <see cref="Watchers"/> state.</summary>
        public WatcherSnapshot WatcherSnapshot { get; } = new();

        /// <summary>Whether the current update tick is the first one for this instance.</summary>
        public bool IsFirstTick = true;

        /// <summary>The number of ticks until SMAPI should notify mods that the game has loaded.</summary>
        /// <remarks>Skipping a few frames ensures the game finishes initializing the world before mods try to change it.</remarks>
        public Countdown AfterLoadTimer { get; } = new(5);

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        public bool IsBetweenSaveEvents { get; set; }

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        public bool IsBetweenCreateEvents { get; set; }

        /// <summary>The cached <see cref="Farmer.UniqueMultiplayerID"/> value for this instance's player.</summary>
        public long? PlayerId { get; private set; }

        /// <summary>Construct a content manager to read game content files.</summary>
        /// <remarks>This must be static because the game accesses it before the <see cref="SGame"/> constructor is called.</remarks>
        [NonInstancedStatic]
        public static Func<IServiceProvider, string, LocalizedContentManager>? CreateContentManagerImpl;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="instanceIndex">The instance index.</param>
        /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="input">Manages the game's input state.</param>
        /// <param name="modHooks">Handles mod hooks provided by the game.</param>
        /// <param name="gameLogger">The game log output handler.</param>
        /// <param name="multiplayer">The core multiplayer logic.</param>
        /// <param name="exitGameImmediately">Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</param>
        /// <param name="onUpdating">Raised when the instance is updating its state (roughly 60 times per second).</param>
        /// <param name="onContentLoaded">Raised after the game finishes loading its initial content.</param>
        /// <param name="onLoadStageChanged">Raised invoke when the load stage changes through a method like <see cref="Game1.CleanupReturningToTitle"/>.</param>
        /// <param name="onRendered">Raised after the instance finishes a draw loop.</param>
        public SGame(PlayerIndex playerIndex, int instanceIndex, Monitor monitor, Reflector reflection, SInputState input, SModHooks modHooks, IGameLogger gameLogger, SMultiplayer multiplayer, Action<string> exitGameImmediately, Action<SGame, GameTime, Action> onUpdating, Action onContentLoaded, Action<LoadStage> onLoadStageChanged, Action<RenderTarget2D> onRendered)
            : base(playerIndex, instanceIndex)
        {
            // init XNA
            Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // hook into game
            Game1.input = this.InitialInput = input;
            Game1.log = gameLogger;
            Game1.multiplayer = this.InitialMultiplayer = multiplayer;
            Game1.hooks = modHooks;
            this._locations = new ObservableCollection<GameLocation>();

            // init SMAPI
            this.Monitor = monitor;
            this.Reflection = reflection;
            this.ExitGameImmediately = exitGameImmediately;
            this.OnUpdating = onUpdating;
            this.OnContentLoaded = onContentLoaded;
            this.OnLoadStageChanged = onLoadStageChanged;
            this.OnRendered = onRendered;
        }

        /// <summary>Get the current input state for a button.</summary>
        /// <param name="button">The button to check.</param>
        /// <remarks>This is intended for use by <see cref="Keybind"/> and shouldn't be used directly in most cases.</remarks>
        internal static SButtonState GetInputState(SButton button)
        {
            if (Game1.input is not SInputState inputHandler)
                throw new InvalidOperationException("SMAPI's input state is not in a ready state yet.");

            return inputHandler.GetState(button);
        }

        /// <inheritdoc />
        protected override void LoadContent()
        {
            base.LoadContent();

            this.OnContentLoaded();
        }

        /*********
        ** Protected methods
        *********/
        /// <summary>Construct a content manager to read game content files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        protected internal override LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            if (SGame.CreateContentManagerImpl == null)
                throw new InvalidOperationException($"The {nameof(SGame)}.{nameof(SGame.CreateContentManagerImpl)} must be set.");

            return SGame.CreateContentManagerImpl(serviceProvider, rootDirectory);
        }

        /// <summary>Initialize the instance when the game starts.</summary>
        protected override void Initialize()
        {
            base.Initialize();

            // The game resets public static fields after the class is constructed (see GameRunner.SetInstanceDefaults), so SMAPI needs to re-override them here.
            Game1.input = this.InitialInput;
            Game1.multiplayer = this.InitialMultiplayer;
            if (this.IsMainInstance)
                TitleMenu.OnCreatedNewCharacter += () => this.OnLoadStageChanged(LoadStage.CreatedBasicInfo); // event is static and shared between screens

            // The Initial* fields should no longer be used after this point, since mods may further override them after initialization.
            this.InitialInput = null;
            this.InitialMultiplayer = null;
        }

        /// <summary>The method called when loading or creating a save.</summary>
        /// <param name="loadedGame">Whether this is being called from the game's load enumerator.</param>
        public override void loadForNewGame(bool loadedGame = false)
        {
            base.loadForNewGame(loadedGame);

            bool isCreating =
                (Game1.currentMinigame is Intro) // creating save with intro
                || (Game1.activeClickableMenu is TitleMenu menu && menu.transitioningCharacterCreationMenu); // creating save, skipped intro

            if (isCreating)
                this.OnLoadStageChanged(LoadStage.CreatedLocations);
        }

        /// <summary>The method called when the instance is updating its state (roughly 60 times per second).</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        protected override void Update(GameTime gameTime)
        {
            // set initial state
            if (this.IsFirstTick)
            {
                this.Input.TrueUpdate();
                this.Watchers = new WatcherCore(this.Input, (ObservableCollection<GameLocation>)this._locations);
            }

            // crosscheck rendering device.
            if (Game1.mapDisplayDevice is SDisplayDevice sDisplayDevice
                && sDisplayDevice.IsDirty == true && Game1.currentLocation is { } loc)
            {
                sDisplayDevice.PopulateTilesheets(loc.Map);
            }

            // update
            try
            {
                this.OnUpdating(this, gameTime, () => base.Update(gameTime));
                this.PlayerId = Game1.player?.UniqueMultiplayerID;
            }
            finally
            {
                this.IsFirstTick = false;
            }
        }

        /// <summary>The method called to draw everything to the screen.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="target_screen">The render target, if any.</param>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "copied from game code as-is")]
        protected override void _draw(GameTime gameTime, RenderTarget2D target_screen)
        {
            Context.IsInDrawLoop = true;
            try
            {
                base._draw(gameTime, target_screen);
                this.OnRendered(target_screen);
                this.DrawCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occurred in the game's draw loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.DrawCrashTimer.Decrement())
                {
                    this.ExitGameImmediately("The game crashed when drawing, and SMAPI was unable to recover the game.");
                    return;
                }

                // recover draw state
                try
                {
                    if (Game1.spriteBatch.IsOpen(this.Reflection))
                    {
                        this.Monitor.Log("Recovering sprite batch from error...");
                        Game1.spriteBatch.End();
                    }

                    Game1.uiMode = false;
                    Game1.uiModeCount = 0;
                    Game1.nonUIRenderTarget = null;
                }
                catch (Exception innerEx)
                {
                    this.Monitor.Log($"Could not recover game draw state: {innerEx.GetLogSummary()}", LogLevel.Error);
                }
            }
            Context.IsInDrawLoop = false;
        }

        /// <summary>The method called when the game is returning to the title screen from a loaded save.</summary>
        public override void CleanupReturningToTitle()
        {
            this.OnLoadStageChanged(LoadStage.ReturningToTitle);

            base.CleanupReturningToTitle();
        }
    }
}
