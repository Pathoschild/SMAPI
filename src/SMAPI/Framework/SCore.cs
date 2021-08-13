using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
#if SMAPI_FOR_WINDOWS
using Microsoft.Win32;
#endif
using Newtonsoft.Json;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.Logging;
using StardewModdingAPI.Framework.Models;
using StardewModdingAPI.Framework.ModHelpers;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Framework.Networking;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Rendering;
using StardewModdingAPI.Framework.Serialization;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewModdingAPI.Framework.StateTracking.Snapshots;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Internal.Patching;
using StardewModdingAPI.Patches;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;
using StardewModdingAPI.Utilities;
using StardewValley;
using xTile.Display;
using MiniMonoModHotfix = MonoMod.Utils.MiniMonoModHotfix;
using PathUtilities = StardewModdingAPI.Toolkit.Utilities.PathUtilities;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Framework
{
    /// <summary>The core class which initializes and manages SMAPI.</summary>
    internal class SCore : IDisposable
    {
        /*********
        ** Fields
        *********/
        /****
        ** Low-level components
        ****/
        /// <summary>Tracks whether the game should exit immediately and any pending initialization should be cancelled.</summary>
        private readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();

        /// <summary>Manages the SMAPI console window and log file.</summary>
        private readonly LogManager LogManager;

        /// <summary>The core logger and monitor for SMAPI.</summary>
        private Monitor Monitor => this.LogManager.Monitor;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection = new Reflector();

        /// <summary>Encapsulates access to SMAPI core translations.</summary>
        private readonly Translator Translator = new Translator();

        /// <summary>The SMAPI configuration settings.</summary>
        private readonly SConfig Settings;

        /// <summary>The mod toolkit used for generic mod interactions.</summary>
        private readonly ModToolkit Toolkit = new ModToolkit();

        /****
        ** Higher-level components
        ****/
        /// <summary>Manages console commands.</summary>
        private readonly CommandManager CommandManager;

        /// <summary>The underlying game instance.</summary>
        private SGameRunner Game;

        /// <summary>SMAPI's content manager.</summary>
        private ContentCoordinator ContentCore;

        /// <summary>The game's core multiplayer utility for the main player.</summary>
        private SMultiplayer Multiplayer;

        /// <summary>Tracks the installed mods.</summary>
        /// <remarks>This is initialized after the game starts.</remarks>
        private readonly ModRegistry ModRegistry = new ModRegistry();

        /// <summary>Manages SMAPI events for mods.</summary>
        private readonly EventManager EventManager;


        /****
        ** State
        ****/
        /// <summary>The path to search for mods.</summary>
        private string ModsPath => Constants.ModsPath;

        /// <summary>Whether the game is currently running.</summary>
        private bool IsGameRunning;

        /// <summary>Whether the program has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;

        /// <summary>Whether post-game-startup initialization has been performed.</summary>
        private bool IsInitialized;

        /// <summary>Whether the player just returned to the title screen.</summary>
        public bool JustReturnedToTitle { get; set; }

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from an update error.</summary>
        private readonly Countdown UpdateCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>Asset interceptors added or removed since the last tick.</summary>
        private readonly List<AssetInterceptorChange> ReloadAssetInterceptorsQueue = new List<AssetInterceptorChange>();

        /// <summary>A list of queued commands to parse and execute.</summary>
        /// <remarks>This property must be thread-safe, since it's accessed from a separate console input thread.</remarks>
        private readonly ConcurrentQueue<string> RawCommandQueue = new ConcurrentQueue<string>();

        /// <summary>A list of commands to execute on each screen.</summary>
        private readonly PerScreen<List<Tuple<Command, string, string[]>>> ScreenCommandQueue = new PerScreen<List<Tuple<Command, string, string[]>>>(() => new List<Tuple<Command, string, string[]>>());


        /*********
        ** Accessors
        *********/
        /// <summary>Manages deprecation warnings.</summary>
        /// <remarks>This is initialized after the game starts. This is accessed directly because it's not part of the normal class model.</remarks>
        internal static DeprecationManager DeprecationManager { get; private set; }

        /// <summary>The singleton instance.</summary>
        /// <remarks>This is only intended for use by external code like the Error Handler mod.</remarks>
        internal static SCore Instance { get; private set; }

        /// <summary>The number of update ticks which have already executed. This is similar to <see cref="Game1.ticks"/>, but incremented more consistently for every tick.</summary>
        internal static uint TicksElapsed { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modsPath">The path to search for mods.</param>
        /// <param name="writeToConsole">Whether to output log messages to the console.</param>
        public SCore(string modsPath, bool writeToConsole)
        {
            SCore.Instance = this;

            // init paths
            this.VerifyPath(modsPath);
            this.VerifyPath(Constants.LogDir);
            Constants.ModsPath = modsPath;

            // init log file
            this.PurgeNormalLogs();
            string logPath = this.GetLogPath();

            // init basics
            this.Settings = JsonConvert.DeserializeObject<SConfig>(File.ReadAllText(Constants.ApiConfigPath));
            if (File.Exists(Constants.ApiUserConfigPath))
                JsonConvert.PopulateObject(File.ReadAllText(Constants.ApiUserConfigPath), this.Settings);

            this.LogManager = new LogManager(logPath: logPath, colorConfig: this.Settings.ConsoleColors, writeToConsole: writeToConsole, isVerbose: this.Settings.VerboseLogging, isDeveloperMode: this.Settings.DeveloperMode, getScreenIdForLog: this.GetScreenIdForLog);
            this.CommandManager = new CommandManager(this.Monitor);
            this.EventManager = new EventManager(this.ModRegistry);
            SCore.DeprecationManager = new DeprecationManager(this.Monitor, this.ModRegistry);
            SDate.Translations = this.Translator;

            // log SMAPI/OS info
            this.LogManager.LogIntro(modsPath, this.Settings.GetCustomSettings());

            // validate platform
#if SMAPI_FOR_WINDOWS
            if (Constants.Platform != Platform.Windows)
            {
                this.Monitor.Log("Oops! You're running Windows, but this version of SMAPI is for Linux or macOS. Please reinstall SMAPI to fix this.", LogLevel.Error);
                this.LogManager.PressAnyKeyToExit();
            }
#else
            if (Constants.Platform == Platform.Windows)
            {
                this.Monitor.Log($"Oops! You're running {Constants.Platform}, but this version of SMAPI is for Windows. Please reinstall SMAPI to fix this.", LogLevel.Error);
                this.LogManager.PressAnyKeyToExit();
            }
#endif
        }

        /// <summary>Launch SMAPI.</summary>
        [HandleProcessCorruptedStateExceptions, SecurityCritical] // let try..catch handle corrupted state exceptions
        public void RunInteractively()
        {
            // initialize SMAPI
            try
            {
                JsonConverter[] converters = {
                    new ColorConverter(),
                    new KeybindConverter(),
                    new PointConverter(),
                    new Vector2Converter(),
                    new RectangleConverter()
                };
                foreach (JsonConverter converter in converters)
                    this.Toolkit.JsonHelper.JsonSettings.Converters.Add(converter);

                // add error handlers
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => this.Monitor.Log($"Critical app domain exception: {e.ExceptionObject}", LogLevel.Error);

                // add more lenient assembly resolver
                AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => AssemblyLoader.ResolveAssembly(e.Name);

                // hook locale event
                LocalizedContentManager.OnLanguageChange += locale => this.OnLocaleChanged();

                // override game
                this.Multiplayer = new SMultiplayer(this.Monitor, this.EventManager, this.Toolkit.JsonHelper, this.ModRegistry, this.Reflection, this.OnModMessageReceived, this.Settings.LogNetworkTraffic);
                SGame.CreateContentManagerImpl = this.CreateContentManager; // must be static since the game accesses it before the SGame constructor is called
                this.Game = new SGameRunner(
                    monitor: this.Monitor,
                    reflection: this.Reflection,
                    eventManager: this.EventManager,
                    modHooks: new SModHooks(this.OnNewDayAfterFade, this.Monitor),
                    multiplayer: this.Multiplayer,
                    exitGameImmediately: this.ExitGameImmediately,

                    onGameContentLoaded: this.OnInstanceContentLoaded,
                    onGameUpdating: this.OnGameUpdating,
                    onPlayerInstanceUpdating: this.OnPlayerInstanceUpdating,
                    onGameExiting: this.OnGameExiting
                );
                StardewValley.GameRunner.instance = this.Game;

                // apply game patches
                MiniMonoModHotfix.Apply();
                HarmonyPatcher.Apply("SMAPI", this.Monitor,
                    new Game1Patcher(this.Reflection, this.OnLoadStageChanged),
                    new TitleMenuPatcher(this.OnLoadStageChanged)
                );

                // add exit handler
                this.CancellationToken.Token.Register(() =>
                {
                    if (this.IsGameRunning)
                    {
                        this.LogManager.WriteCrashLog();
                        this.Game.Exit();
                    }
                });

                // set window titles
                this.UpdateWindowTitles();
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"SMAPI failed to initialize: {ex.GetLogSummary()}", LogLevel.Error);
                this.LogManager.PressAnyKeyToExit();
                return;
            }

            // log basic info
            this.LogManager.HandleMarkerFiles();
            this.LogManager.LogSettingsHeader(this.Settings);

            // set window titles
            this.UpdateWindowTitles();

            // start game
            this.Monitor.Log("Waiting for game to launch...", LogLevel.Debug);
            try
            {
                this.IsGameRunning = true;
                StardewValley.Program.releaseBuild = true; // game's debug logic interferes with SMAPI opening the game window
                this.Game.Run();
            }
            catch (Exception ex)
            {
                this.LogManager.LogFatalLaunchError(ex);
                this.LogManager.PressAnyKeyToExit();
            }
            finally
            {
                try
                {
                    this.Dispose();
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"The game ended, but SMAPI wasn't able to dispose correctly. Technical details: {ex}", LogLevel.Error);
                }
            }
        }

        /// <summary>Get the core logger and monitor on behalf of the game.</summary>
        /// <remarks>This method is called using reflection by the ErrorHandler mod to log game errors.</remarks>
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used via reflection")]
        public IMonitor GetMonitorForGame()
        {
            return this.LogManager.MonitorForGame;
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            // skip if already disposed
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;
            this.Monitor.Log("Disposing...");

            // dispose mod data
            foreach (IModMetadata mod in this.ModRegistry.GetAll())
            {
                try
                {
                    (mod.Mod as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    mod.LogAsMod($"Mod failed during disposal: {ex.GetLogSummary()}.", LogLevel.Warn);
                }
            }

            // dispose core components
            this.IsGameRunning = false;
            this.ContentCore?.Dispose();
            this.CancellationToken?.Dispose();
            this.Game?.Dispose();
            this.LogManager?.Dispose(); // dispose last to allow for any last-second log messages

            // end game (moved from Game1.OnExiting to let us clean up first)
            Process.GetCurrentProcess().Kill();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Initialize mods before the first game asset is loaded. At this point the core content managers are loaded (so mods can load their own assets), but the game is mostly uninitialized.</summary>
        private void InitializeBeforeFirstAssetLoaded()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                this.Monitor.Log("SMAPI shutting down: aborting initialization.", LogLevel.Warn);
                return;
            }

            // init TMX support
            xTile.Format.FormatManager.Instance.RegisterMapFormat(new TMXTile.TMXFormat(Game1.tileSize / Game1.pixelZoom, Game1.tileSize / Game1.pixelZoom, Game1.pixelZoom, Game1.pixelZoom));

            // load mod data
            ModToolkit toolkit = new ModToolkit();
            ModDatabase modDatabase = toolkit.GetModDatabase(Constants.ApiMetadataPath);

            // load mods
            {
                this.Monitor.Log("Loading mod metadata...", LogLevel.Debug);
                ModResolver resolver = new ModResolver();

                // log loose files
                {
                    string[] looseFiles = new DirectoryInfo(this.ModsPath).GetFiles().Select(p => p.Name).ToArray();
                    if (looseFiles.Any())
                        this.Monitor.Log($"  Ignored loose files: {string.Join(", ", looseFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))}");
                }

                // load manifests
                IModMetadata[] mods = resolver.ReadManifests(toolkit, this.ModsPath, modDatabase).ToArray();

                // filter out ignored mods
                foreach (IModMetadata mod in mods.Where(p => p.IsIgnored))
                    this.Monitor.Log($"  Skipped {mod.GetRelativePathWithRoot()} (folder name starts with a dot).");
                mods = mods.Where(p => !p.IsIgnored).ToArray();

                // load mods
                resolver.ValidateManifests(mods, Constants.ApiVersion, toolkit.GetUpdateUrl);
                mods = resolver.ProcessDependencies(mods, modDatabase).ToArray();
                this.LoadMods(mods, this.Toolkit.JsonHelper, this.ContentCore, modDatabase);

                // check for software likely to cause issues
                this.CheckForSoftwareConflicts();

                // check for updates
                this.CheckForUpdatesAsync(mods);
            }

            // update window titles
            this.UpdateWindowTitles();
        }

        /// <summary>Raised after the game finishes initializing.</summary>
        private void OnGameInitialized()
        {
            // validate XNB integrity
            if (!this.ValidateContentIntegrity())
                this.Monitor.Log("SMAPI found problems in your game's content files which are likely to cause errors or crashes. Consider uninstalling XNB mods or reinstalling the game.", LogLevel.Error);

            // start SMAPI console
            new Thread(
                () => this.LogManager.RunConsoleInputLoop(
                    commandManager: this.CommandManager,
                    reloadTranslations: this.ReloadTranslations,
                    handleInput: input => this.RawCommandQueue.Enqueue(input),
                    continueWhile: () => this.IsGameRunning && !this.CancellationToken.IsCancellationRequested
                )
            ).Start();
        }

        /// <summary>Raised after an instance finishes loading its initial content.</summary>
        private void OnInstanceContentLoaded()
        {
            // override map display device
            Game1.mapDisplayDevice = new SDisplayDevice(Game1.content, Game1.game1.GraphicsDevice);

            // log GPU info
#if SMAPI_FOR_WINDOWS
            this.Monitor.Log($"Running on GPU: {Game1.game1.GraphicsDevice?.Adapter?.Description ?? "<unknown>"}");
#endif
        }

        /// <summary>Raised when the game is updating its state (roughly 60 times per second).</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="runGameUpdate">Invoke the game's update logic.</param>
        private void OnGameUpdating(GameTime gameTime, Action runGameUpdate)
        {
            try
            {
                /*********
                ** Safe queued work
                *********/
                // print warnings/alerts
                SCore.DeprecationManager.PrintQueued();

                /*********
                ** First-tick initialization
                *********/
                if (!this.IsInitialized)
                {
                    this.IsInitialized = true;
                    this.OnGameInitialized();
                }

                /*********
                ** Special cases
                *********/
                // Abort if SMAPI is exiting.
                if (this.CancellationToken.IsCancellationRequested)
                {
                    this.Monitor.Log("SMAPI shutting down: aborting update.");
                    return;
                }

                /*********
                ** Reload assets when interceptors are added/removed
                *********/
                if (this.ReloadAssetInterceptorsQueue.Any())
                {
                    // get unique interceptors
                    AssetInterceptorChange[] interceptors = this.ReloadAssetInterceptorsQueue
                        .GroupBy(p => p.Instance, new ObjectReferenceComparer<object>())
                        .Select(p => p.First())
                        .ToArray();
                    this.ReloadAssetInterceptorsQueue.Clear();

                    // log summary
                    this.Monitor.Log("Invalidating cached assets for new editors & loaders...");
                    this.Monitor.Log(
                        "   changed: "
                        + string.Join(", ",
                            interceptors
                                .GroupBy(p => p.Mod)
                                .OrderBy(p => p.Key.DisplayName)
                                .Select(modGroup =>
                                    $"{modGroup.Key.DisplayName} ("
                                    + string.Join(", ", modGroup.GroupBy(p => p.WasAdded).ToDictionary(p => p.Key, p => p.Count()).Select(p => $"{(p.Key ? "added" : "removed")} {p.Value}"))
                                    + ")"
                                )
                        )
                        + "."
                    );

                    // reload affected assets
                    this.ContentCore.InvalidateCache(asset => interceptors.Any(p => p.CanIntercept(asset)));
                }

                /*********
                ** Parse commands
                *********/
                while (this.RawCommandQueue.TryDequeue(out string rawInput))
                {
                    // parse command
                    string name;
                    string[] args;
                    Command command;
                    int screenId;
                    try
                    {
                        if (!this.CommandManager.TryParse(rawInput, out name, out args, out command, out screenId))
                        {
                            this.Monitor.Log("Unknown command; type 'help' for a list of available commands.", LogLevel.Error);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"Failed parsing that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                        continue;
                    }

                    // queue command for screen
                    this.ScreenCommandQueue.GetValueForScreen(screenId).Add(Tuple.Create(command, name, args));
                }


                /*********
                ** Run game update
                *********/
                runGameUpdate();

                /*********
                ** Reset crash timer
                *********/
                this.UpdateCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occured in the overridden update loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.UpdateCrashTimer.Decrement())
                    this.ExitGameImmediately("The game crashed when updating, and SMAPI was unable to recover the game.");
            }
            finally
            {
                SCore.TicksElapsed++;
            }
        }

        /// <summary>Raised when the game instance for a local player is updating (once per <see cref="OnGameUpdating"/> per player).</summary>
        /// <param name="instance">The game instance being updated.</param>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="runUpdate">Invoke the game's update logic.</param>
        private void OnPlayerInstanceUpdating(SGame instance, GameTime gameTime, Action runUpdate)
        {
            var events = this.EventManager;

            try
            {
                /*********
                ** Reapply overrides
                *********/
                if (this.JustReturnedToTitle)
                {
                    if (!(Game1.mapDisplayDevice is SDisplayDevice))
                        Game1.mapDisplayDevice = this.GetMapDisplayDevice();

                    this.JustReturnedToTitle = false;
                }

                /*********
                ** Execute commands
                *********/
                {
                    var commandQueue = this.ScreenCommandQueue.Value;
                    foreach (var entry in commandQueue)
                    {
                        Command command = entry.Item1;
                        string name = entry.Item2;
                        string[] args = entry.Item3;

                        try
                        {
                            command.Callback.Invoke(name, args);
                        }
                        catch (Exception ex)
                        {
                            if (command.Mod != null)
                                command.Mod.LogAsMod($"Mod failed handling that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                            else
                                this.Monitor.Log($"Failed handling that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                        }
                    }
                    commandQueue.Clear();
                }


                /*********
                ** Update input
                *********/
                // This should *always* run, even when suppressing mod events, since the game uses
                // this too. For example, doing this after mod event suppression would prevent the
                // user from doing anything on the overnight shipping screen.
                SInputState inputState = instance.Input;
                if (this.Game.IsActive)
                    inputState.TrueUpdate();

                /*********
                ** Special cases
                *********/
                // Run async tasks synchronously to avoid issues due to mod events triggering
                // concurrently with game code.
                bool saveParsed = false;
                if (Game1.currentLoader != null)
                {
                    this.Monitor.Log("Game loader synchronizing...");
                    this.Reflection.GetMethod(Game1.game1, "UpdateTitleScreen").Invoke(Game1.currentGameTime); // run game logic to change music on load, etc
                    while (Game1.currentLoader?.MoveNext() == true)
                    {
                        // raise load stage changed
                        switch (Game1.currentLoader.Current)
                        {
                            case 20 when (!saveParsed && SaveGame.loaded != null):
                                saveParsed = true;
                                this.OnLoadStageChanged(LoadStage.SaveParsed);
                                break;

                            case 36:
                                this.OnLoadStageChanged(LoadStage.SaveLoadedBasicInfo);
                                break;

                            case 50:
                                this.OnLoadStageChanged(LoadStage.SaveLoadedLocations);
                                break;

                            default:
                                if (Game1.gameMode == Game1.playingGameMode)
                                    this.OnLoadStageChanged(LoadStage.Preloaded);
                                break;
                        }
                    }

                    Game1.currentLoader = null;
                    this.Monitor.Log("Game loader done.");
                }

                // While a background task is in progress, the game may make changes to the game
                // state while mods are running their code. This is risky, because data changes can
                // conflict (e.g. collection changed during enumeration errors) and data may change
                // unexpectedly from one mod instruction to the next.
                //
                // Therefore we can just run Game1.Update here without raising any SMAPI events. There's
                // a small chance that the task will finish after we defer but before the game checks,
                // which means technically events should be raised, but the effects of missing one
                // update tick are negligible and not worth the complications of bypassing Game1.Update.
                if (Game1.gameMode == Game1.loadingMode)
                {
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    runUpdate();
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    return;
                }

                // Raise minimal events while saving.
                // While the game is writing to the save file in the background, mods can unexpectedly
                // fail since they don't have exclusive access to resources (e.g. collection changed
                // during enumeration errors). To avoid problems, events are not invoked while a save
                // is in progress. It's safe to raise SaveEvents.BeforeSave as soon as the menu is
                // opened (since the save hasn't started yet), but all other events should be suppressed.
                if (Context.IsSaving)
                {
                    // raise before-create
                    if (!Context.IsWorldReady && !instance.IsBetweenCreateEvents)
                    {
                        instance.IsBetweenCreateEvents = true;
                        this.Monitor.Log("Context: before save creation.");
                        events.SaveCreating.RaiseEmpty();
                    }

                    // raise before-save
                    if (Context.IsWorldReady && !instance.IsBetweenSaveEvents)
                    {
                        instance.IsBetweenSaveEvents = true;
                        this.Monitor.Log("Context: before save.");
                        events.Saving.RaiseEmpty();
                    }

                    // suppress non-save events
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    runUpdate();
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    return;
                }

                /*********
                ** Update context
                *********/
                bool wasWorldReady = Context.IsWorldReady;
                if ((Context.IsWorldReady && !Context.IsSaveLoaded) || Game1.exitToTitle)
                {
                    Context.IsWorldReady = false;
                    instance.AfterLoadTimer.Reset();
                }
                else if (Context.IsSaveLoaded && instance.AfterLoadTimer.Current > 0 && Game1.currentLocation != null)
                {
                    if (Game1.dayOfMonth != 0) // wait until new-game intro finishes (world not fully initialized yet)
                        instance.AfterLoadTimer.Decrement();
                    Context.IsWorldReady = instance.AfterLoadTimer.Current == 0;
                }

                /*********
                ** Update watchers
                **   (Watchers need to be updated, checked, and reset in one go so we can detect any changes mods make in event handlers.)
                *********/
                instance.Watchers.Update();
                instance.WatcherSnapshot.Update(instance.Watchers);
                instance.Watchers.Reset();
                WatcherSnapshot state = instance.WatcherSnapshot;

                /*********
                ** Pre-update events
                *********/
                {
                    /*********
                    ** Save created/loaded events
                    *********/
                    if (instance.IsBetweenCreateEvents)
                    {
                        // raise after-create
                        instance.IsBetweenCreateEvents = false;
                        this.Monitor.Log($"Context: after save creation, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.");
                        this.OnLoadStageChanged(LoadStage.CreatedSaveFile);
                        events.SaveCreated.RaiseEmpty();
                    }

                    if (instance.IsBetweenSaveEvents)
                    {
                        // raise after-save
                        instance.IsBetweenSaveEvents = false;
                        this.Monitor.Log($"Context: after save, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.");
                        events.Saved.RaiseEmpty();
                        events.DayStarted.RaiseEmpty();
                    }

                    /*********
                    ** Locale changed events
                    *********/
                    if (state.Locale.IsChanged)
                        this.Monitor.Log($"Context: locale set to {state.Locale.New}.");

                    /*********
                    ** Load / return-to-title events
                    *********/
                    if (wasWorldReady && !Context.IsWorldReady)
                        this.OnLoadStageChanged(LoadStage.None);
                    else if (Context.IsWorldReady && Context.LoadStage != LoadStage.Ready)
                    {
                        // print context
                        string context = $"Context: loaded save '{Constants.SaveFolderName}', starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}, locale set to {this.ContentCore.Language}.";
                        if (Context.IsMultiplayer)
                        {
                            int onlineCount = Game1.getOnlineFarmers().Count();
                            context += $" {(Context.IsMainPlayer ? "Main player" : "Farmhand")} with {onlineCount} {(onlineCount == 1 ? "player" : "players")} online.";
                        }
                        else
                            context += " Single-player.";

                        this.Monitor.Log(context);

                        // raise events
                        this.OnLoadStageChanged(LoadStage.Ready);
                        events.SaveLoaded.RaiseEmpty();
                        events.DayStarted.RaiseEmpty();
                    }

                    /*********
                    ** Window events
                    *********/
                    // Here we depend on the game's viewport instead of listening to the Window.Resize
                    // event because we need to notify mods after the game handles the resize, so the
                    // game's metadata (like Game1.viewport) are updated. That's a bit complicated
                    // since the game adds & removes its own handler on the fly.
                    if (state.WindowSize.IsChanged)
                    {
                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Events: window size changed to {state.WindowSize.New}.");

                        events.WindowResized.Raise(new WindowResizedEventArgs(state.WindowSize.Old, state.WindowSize.New));
                    }

                    /*********
                    ** Input events (if window has focus)
                    *********/
                    if (this.Game.IsActive)
                    {
                        // raise events
                        bool isChatInput = Game1.IsChatting || (Context.IsMultiplayer && Context.IsWorldReady && Game1.activeClickableMenu == null && Game1.currentMinigame == null && inputState.IsAnyDown(Game1.options.chatButton));
                        if (!isChatInput)
                        {
                            ICursorPosition cursor = instance.Input.CursorPosition;

                            // raise cursor moved event
                            if (state.Cursor.IsChanged)
                                events.CursorMoved.Raise(new CursorMovedEventArgs(state.Cursor.Old, state.Cursor.New));

                            // raise mouse wheel scrolled
                            if (state.MouseWheelScroll.IsChanged)
                            {
                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Events: mouse wheel scrolled to {state.MouseWheelScroll.New}.");
                                events.MouseWheelScrolled.Raise(new MouseWheelScrolledEventArgs(cursor, state.MouseWheelScroll.Old, state.MouseWheelScroll.New));
                            }

                            // raise input button events
                            if (inputState.ButtonStates.Count > 0)
                            {
                                events.ButtonsChanged.Raise(new ButtonsChangedEventArgs(cursor, inputState));

                                foreach (var pair in inputState.ButtonStates)
                                {
                                    SButton button = pair.Key;
                                    SButtonState status = pair.Value;

                                    if (status == SButtonState.Pressed)
                                    {
                                        if (this.Monitor.IsVerbose)
                                            this.Monitor.Log($"Events: button {button} pressed.");

                                        events.ButtonPressed.Raise(new ButtonPressedEventArgs(button, cursor, inputState));
                                    }
                                    else if (status == SButtonState.Released)
                                    {
                                        if (this.Monitor.IsVerbose)
                                            this.Monitor.Log($"Events: button {button} released.");

                                        events.ButtonReleased.Raise(new ButtonReleasedEventArgs(button, cursor, inputState));
                                    }
                                }
                            }
                        }
                    }

                    /*********
                    ** Menu events
                    *********/
                    if (state.ActiveMenu.IsChanged)
                    {
                        var was = state.ActiveMenu.Old;
                        var now = state.ActiveMenu.New;

                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Context: menu changed from {was?.GetType().FullName ?? "none"} to {now?.GetType().FullName ?? "none"}.");

                        // raise menu events
                        events.MenuChanged.Raise(new MenuChangedEventArgs(was, now));
                    }

                    /*********
                    ** World & player events
                    *********/
                    if (Context.IsWorldReady)
                    {
                        bool raiseWorldEvents = !state.SaveID.IsChanged; // don't report changes from unloaded => loaded

                        // location list changes
                        if (state.Locations.LocationList.IsChanged && (events.LocationListChanged.HasListeners() || this.Monitor.IsVerbose))
                        {
                            var added = state.Locations.LocationList.Added.ToArray();
                            var removed = state.Locations.LocationList.Removed.ToArray();

                            if (this.Monitor.IsVerbose)
                            {
                                string addedText = added.Any() ? string.Join(", ", added.Select(p => p.Name)) : "none";
                                string removedText = removed.Any() ? string.Join(", ", removed.Select(p => p.Name)) : "none";
                                this.Monitor.Log($"Context: location list changed (added {addedText}; removed {removedText}).");
                            }

                            events.LocationListChanged.Raise(new LocationListChangedEventArgs(added, removed));
                        }

                        // raise location contents changed
                        if (raiseWorldEvents)
                        {
                            foreach (LocationSnapshot locState in state.Locations.Locations)
                            {
                                var location = locState.Location;

                                // buildings changed
                                if (locState.Buildings.IsChanged)
                                    events.BuildingListChanged.Raise(new BuildingListChangedEventArgs(location, locState.Buildings.Added, locState.Buildings.Removed));

                                // debris changed
                                if (locState.Debris.IsChanged)
                                    events.DebrisListChanged.Raise(new DebrisListChangedEventArgs(location, locState.Debris.Added, locState.Debris.Removed));

                                // large terrain features changed
                                if (locState.LargeTerrainFeatures.IsChanged)
                                    events.LargeTerrainFeatureListChanged.Raise(new LargeTerrainFeatureListChangedEventArgs(location, locState.LargeTerrainFeatures.Added, locState.LargeTerrainFeatures.Removed));

                                // NPCs changed
                                if (locState.Npcs.IsChanged)
                                    events.NpcListChanged.Raise(new NpcListChangedEventArgs(location, locState.Npcs.Added, locState.Npcs.Removed));

                                // objects changed
                                if (locState.Objects.IsChanged)
                                    events.ObjectListChanged.Raise(new ObjectListChangedEventArgs(location, locState.Objects.Added, locState.Objects.Removed));

                                // chest items changed
                                if (events.ChestInventoryChanged.HasListeners())
                                {
                                    foreach (var pair in locState.ChestItems)
                                    {
                                        SnapshotItemListDiff diff = pair.Value;
                                        events.ChestInventoryChanged.Raise(new ChestInventoryChangedEventArgs(pair.Key, location, added: diff.Added, removed: diff.Removed, quantityChanged: diff.QuantityChanged));
                                    }
                                }

                                // terrain features changed
                                if (locState.TerrainFeatures.IsChanged)
                                    events.TerrainFeatureListChanged.Raise(new TerrainFeatureListChangedEventArgs(location, locState.TerrainFeatures.Added, locState.TerrainFeatures.Removed));

                                // furniture changed
                                if (locState.Furniture.IsChanged)
                                    events.FurnitureListChanged.Raise(new FurnitureListChangedEventArgs(location, locState.Furniture.Added, locState.Furniture.Removed));
                            }
                        }

                        // raise time changed
                        if (raiseWorldEvents && state.Time.IsChanged)
                            events.TimeChanged.Raise(new TimeChangedEventArgs(state.Time.Old, state.Time.New));

                        // raise player events
                        if (raiseWorldEvents)
                        {
                            PlayerSnapshot playerState = state.CurrentPlayer;
                            Farmer player = playerState.Player;

                            // raise current location changed
                            if (playerState.Location.IsChanged)
                            {
                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Context: set location to {playerState.Location.New}.");

                                events.Warped.Raise(new WarpedEventArgs(player, playerState.Location.Old, playerState.Location.New));
                            }

                            // raise player leveled up a skill
                            foreach (var pair in playerState.Skills)
                            {
                                if (!pair.Value.IsChanged)
                                    continue;

                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Events: player skill '{pair.Key}' changed from {pair.Value.Old} to {pair.Value.New}.");

                                events.LevelChanged.Raise(new LevelChangedEventArgs(player, pair.Key, pair.Value.Old, pair.Value.New));
                            }

                            // raise player inventory changed
                            if (playerState.Inventory.IsChanged)
                            {
                                var inventory = playerState.Inventory;

                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log("Events: player inventory changed.");
                                events.InventoryChanged.Raise(new InventoryChangedEventArgs(player, added: inventory.Added, removed: inventory.Removed, quantityChanged: inventory.QuantityChanged));
                            }
                        }
                    }

                    /*********
                    ** Game update
                    *********/
                    // game launched (not raised for secondary players in split-screen mode)
                    if (instance.IsFirstTick && !Context.IsGameLaunched)
                    {
                        Context.IsGameLaunched = true;
                        events.GameLaunched.Raise(new GameLaunchedEventArgs());
                    }

                    // preloaded
                    if (Context.IsSaveLoaded && Context.LoadStage != LoadStage.Loaded && Context.LoadStage != LoadStage.Ready && Game1.dayOfMonth != 0)
                        this.OnLoadStageChanged(LoadStage.Loaded);
                }

                /*********
                ** Game update tick
                *********/
                {
                    bool isOneSecond = SCore.TicksElapsed % 60 == 0;
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    events.UpdateTicking.RaiseEmpty();
                    if (isOneSecond)
                        events.OneSecondUpdateTicking.RaiseEmpty();
                    try
                    {
                        instance.Input.ApplyOverrides(); // if mods added any new overrides since the update, process them now
                        runUpdate();
                    }
                    catch (Exception ex)
                    {
                        this.LogManager.MonitorForGame.Log($"An error occurred in the base update loop: {ex.GetLogSummary()}", LogLevel.Error);
                    }

                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    events.UpdateTicked.RaiseEmpty();
                    if (isOneSecond)
                        events.OneSecondUpdateTicked.RaiseEmpty();
                }

                /*********
                ** Update events
                *********/
                this.UpdateCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occurred in the overridden update loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.UpdateCrashTimer.Decrement())
                    this.ExitGameImmediately("The game crashed when updating, and SMAPI was unable to recover the game.");
            }
        }

        /// <summary>Handle the game changing locale.</summary>
        private void OnLocaleChanged()
        {
            this.ContentCore.OnLocaleChanged();

            // get locale
            string locale = this.ContentCore.GetLocale();
            LocalizedContentManager.LanguageCode languageCode = this.ContentCore.Language;

            // update core translations
            this.Translator.SetLocale(locale, languageCode);

            // update mod translation helpers
            foreach (IModMetadata mod in this.ModRegistry.GetAll())
            {
                mod.Translations.SetLocale(locale, languageCode);

                foreach (ContentPack contentPack in mod.GetFakeContentPacks())
                    contentPack.TranslationImpl.SetLocale(locale, languageCode);
            }
        }

        /// <summary>Raised when the low-level stage while loading a save changes.</summary>
        /// <param name="newStage">The new load stage.</param>
        internal void OnLoadStageChanged(LoadStage newStage)
        {
            // nothing to do
            if (newStage == Context.LoadStage)
                return;

            // update data
            LoadStage oldStage = Context.LoadStage;
            Context.LoadStage = newStage;
            this.Monitor.VerboseLog($"Context: load stage changed to {newStage}");

            // handle stages
            switch (newStage)
            {
                case LoadStage.ReturningToTitle:
                    this.Monitor.Log("Context: returning to title");
                    this.OnReturningToTitle();
                    break;

                case LoadStage.None:
                    this.JustReturnedToTitle = true;
                    break;

                case LoadStage.Loaded:
                    // override chatbox
                    Game1.onScreenMenus.Remove(Game1.chatBox);
                    Game1.onScreenMenus.Add(Game1.chatBox = new SChatBox(this.LogManager.MonitorForGame));
                    break;
            }

            // raise events
            this.EventManager.LoadStageChanged.Raise(new LoadStageChangedEventArgs(oldStage, newStage));
            if (newStage == LoadStage.None)
                this.EventManager.ReturnedToTitle.RaiseEmpty();
        }

        /// <summary>A callback invoked before <see cref="Game1.newDayAfterFade"/> runs.</summary>
        protected void OnNewDayAfterFade()
        {
            this.EventManager.DayEnding.RaiseEmpty();
        }

        /// <summary>Raised immediately before the player returns to the title screen.</summary>
        private void OnReturningToTitle()
        {
            // perform cleanup
            this.Multiplayer.CleanupOnMultiplayerExit();
            this.ContentCore.OnReturningToTitleScreen();
        }

        /// <summary>Raised before the game exits.</summary>
        private void OnGameExiting()
        {
            this.Multiplayer.Disconnect(StardewValley.Multiplayer.DisconnectType.ClosedGame);
            this.Dispose();
        }

        /// <summary>Raised when a mod network message is received.</summary>
        /// <param name="message">The message to deliver to applicable mods.</param>
        private void OnModMessageReceived(ModMessageModel message)
        {
            // get mod IDs to notify
            HashSet<string> modIDs = new HashSet<string>(message.ToModIDs ?? this.ModRegistry.GetAll().Select(p => p.Manifest.UniqueID), StringComparer.OrdinalIgnoreCase);
            if (message.FromPlayerID == Game1.player?.UniqueMultiplayerID)
                modIDs.Remove(message.FromModID); // don't send a broadcast back to the sender

            // raise events
            this.EventManager.ModMessageReceived.Raise(new ModMessageReceivedEventArgs(message, this.Toolkit.JsonHelper), mod => mod != null && modIDs.Contains(mod.Manifest.UniqueID));
        }

        /// <summary>Constructor a content manager to read game content files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        private LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            // Game1._temporaryContent initializing from SGame constructor
            if (this.ContentCore == null)
            {
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, this.Monitor, this.Reflection, this.Toolkit.JsonHelper, this.InitializeBeforeFirstAssetLoaded, this.Settings.AggressiveMemoryOptimizations);
                if (this.ContentCore.Language != this.Translator.LocaleEnum)
                    this.Translator.SetLocale(this.ContentCore.GetLocale(), this.ContentCore.Language);

                this.NextContentManagerIsMain = true;
                return this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
            }

            // Game1.content initializing from LoadContent
            if (this.NextContentManagerIsMain)
            {
                this.NextContentManagerIsMain = false;
                return this.ContentCore.MainContentManager;
            }

            // any other content manager
            return this.ContentCore.CreateGameContentManager("(generated)");
        }

        /// <summary>Get the current game instance. This may not be the main player if playing in split-screen.</summary>
        private SGame GetCurrentGameInstance()
        {
            return Game1.game1 as SGame
                ?? throw new InvalidOperationException("The current game instance wasn't created by SMAPI.");
        }

        /// <summary>Look for common issues with the game's XNB content, and log warnings if anything looks broken or outdated.</summary>
        /// <returns>Returns whether all integrity checks passed.</returns>
        private bool ValidateContentIntegrity()
        {
            this.Monitor.Log("Detecting common issues...");
            bool issuesFound = false;

            // object format (commonly broken by outdated files)
            {
                // detect issues
                bool hasObjectIssues = false;
                void LogIssue(int id, string issue) => this.Monitor.Log($@"Detected issue: item #{id} in Content\Data\ObjectInformation.xnb is invalid ({issue}).");
                foreach (KeyValuePair<int, string> entry in Game1.objectInformation)
                {
                    // must not be empty
                    if (string.IsNullOrWhiteSpace(entry.Value))
                    {
                        LogIssue(entry.Key, "entry is empty");
                        hasObjectIssues = true;
                        continue;
                    }

                    // require core fields
                    string[] fields = entry.Value.Split('/');
                    if (fields.Length < SObject.objectInfoDescriptionIndex + 1)
                    {
                        LogIssue(entry.Key, "too few fields for an object");
                        hasObjectIssues = true;
                        continue;
                    }

                    // check min length for specific types
                    switch (fields[SObject.objectInfoTypeIndex].Split(new[] { ' ' }, 2)[0])
                    {
                        case "Cooking":
                            if (fields.Length < SObject.objectInfoBuffDurationIndex + 1)
                            {
                                LogIssue(entry.Key, "too few fields for a cooking item");
                                hasObjectIssues = true;
                            }
                            break;
                    }
                }

                // log error
                if (hasObjectIssues)
                {
                    issuesFound = true;
                    this.Monitor.Log(@"Your Content\Data\ObjectInformation.xnb file seems to be broken or outdated.", LogLevel.Warn);
                }
            }

            return !issuesFound;
        }

        /// <summary>Set the titles for the game and console windows.</summary>
        private void UpdateWindowTitles()
        {
            string consoleTitle = $"SMAPI {Constants.ApiVersion} - running Stardew Valley {Constants.GameVersion}";
            string gameTitle = $"Stardew Valley {Constants.GameVersion} - running SMAPI {Constants.ApiVersion}";

            if (this.ModRegistry.AreAllModsLoaded)
            {
                int modsLoaded = this.ModRegistry.GetAll().Count();
                consoleTitle += $" with {modsLoaded} mods";
                gameTitle += $" with {modsLoaded} mods";
            }

            this.Game.Window.Title = gameTitle;
            this.LogManager.SetConsoleTitle(consoleTitle);
        }

        /// <summary>Log a warning if software known to cause issues is installed.</summary>
        private void CheckForSoftwareConflicts()
        {
#if SMAPI_FOR_WINDOWS
            this.Monitor.Log("Checking for known software conflicts...");

            try
            {
                string[] registryKeys = { @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" };

                string[] installedNames = registryKeys
                    .SelectMany(registryKey =>
                    {
                        using RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey);
                        if (key == null)
                            return new string[0];

                        return key
                            .GetSubKeyNames()
                            .Select(subkeyName =>
                            {
                                using RegistryKey subkey = key.OpenSubKey(subkeyName);
                                string displayName = (string)subkey?.GetValue("DisplayName");
                                string displayVersion = (string)subkey?.GetValue("DisplayVersion");

                                if (displayName != null && displayVersion != null && displayName.EndsWith($" {displayVersion}"))
                                    displayName = displayName.Substring(0, displayName.Length - displayVersion.Length - 1);

                                return displayName;
                            })
                            .ToArray();
                    })
                    .Where(name => name != null && (name.Contains("MSI Afterburner") || name.Contains("RivaTuner")))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToArray();

                if (installedNames.Any())
                    this.Monitor.Log($"Found {string.Join(" and ", installedNames)} installed, which may conflict with SMAPI. If you experience errors or crashes, try disabling that software or adding an exception for SMAPI and Stardew Valley.", LogLevel.Warn);
                else
                    this.Monitor.Log("   None found!");
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Failed when checking for conflicting software. Technical details:\n{ex}");
            }
#endif
        }

        /// <summary>Asynchronously check for a new version of SMAPI and any installed mods, and print alerts to the console if an update is available.</summary>
        /// <param name="mods">The mods to include in the update check (if eligible).</param>
        private void CheckForUpdatesAsync(IModMetadata[] mods)
        {
            if (!this.Settings.CheckForUpdates)
                return;

            new Thread(() =>
            {
                // create client
                string url = this.Settings.WebApiBaseUrl;
                WebApiClient client = new WebApiClient(url, Constants.ApiVersion);
                this.Monitor.Log("Checking for updates...");

                // check SMAPI version
                {
                    ISemanticVersion updateFound = null;
                    string updateUrl = null;
                    try
                    {
                        // fetch update check
                        ModEntryModel response = client.GetModInfo(new[] { new ModSearchEntryModel("Pathoschild.SMAPI", Constants.ApiVersion, new[] { $"GitHub:{this.Settings.GitHubProjectName}" }) }, apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform).Single().Value;
                        updateFound = response.SuggestedUpdate?.Version;
                        updateUrl = response.SuggestedUpdate?.Url ?? Constants.HomePageUrl;

                        // log message
                        if (updateFound != null)
                            this.Monitor.Log($"You can update SMAPI to {updateFound}: {updateUrl}", LogLevel.Alert);
                        else
                            this.Monitor.Log("   SMAPI okay.");

                        // show errors
                        if (response.Errors.Any())
                        {
                            this.Monitor.Log("Couldn't check for a new version of SMAPI. This won't affect your game, but you may not be notified of new versions if this keeps happening.", LogLevel.Warn);
                            this.Monitor.Log($"Error: {string.Join("\n", response.Errors)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log("Couldn't check for a new version of SMAPI. This won't affect your game, but you won't be notified of new versions if this keeps happening.", LogLevel.Warn);
                        this.Monitor.Log(ex is WebException && ex.InnerException == null
                            ? $"Error: {ex.Message}"
                            : $"Error: {ex.GetLogSummary()}"
                        );
                    }

                    // show update message on next launch
                    if (updateFound != null)
                        this.LogManager.WriteUpdateMarker(updateFound.ToString(), updateUrl);
                }

                // check Stardew64Installer version
                if (Constants.IsPatchedByStardew64Installer(out ISemanticVersion patchedByVersion))
                {
                    ISemanticVersion updateFound = null;
                    string updateUrl = null;
                    try
                    {
                        // fetch update check
                        ModEntryModel response = client.GetModInfo(new[] { new ModSearchEntryModel("Steviegt6.Stardew64Installer", patchedByVersion, new[] { $"GitHub:{this.Settings.Stardew64InstallerGitHubProjectName}" }) }, apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform).Single().Value;
                        updateFound = response.SuggestedUpdate?.Version;
                        updateUrl = response.SuggestedUpdate?.Url ?? Constants.HomePageUrl;

                        // log message
                        if (updateFound != null)
                            this.Monitor.Log($"You can update Stardew64Installer to {updateFound}: {updateUrl}", LogLevel.Alert);
                        else
                            this.Monitor.Log("   Stardew64Installer okay.");

                        // show errors
                        if (response.Errors.Any())
                        {
                            this.Monitor.Log("Couldn't check for a new version of Stardew64Installer. This won't affect your game, but you may not be notified of new versions if this keeps happening.", LogLevel.Warn);
                            this.Monitor.Log($"Error: {string.Join("\n", response.Errors)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log("Couldn't check for a new version of Stardew64Installer. This won't affect your game, but you won't be notified of new versions if this keeps happening.", LogLevel.Warn);
                        this.Monitor.Log(ex is WebException && ex.InnerException == null
                            ? $"Error: {ex.Message}"
                            : $"Error: {ex.GetLogSummary()}"
                        );
                    }
                }

                // check mod versions
                if (mods.Any())
                {
                    try
                    {
                        HashSet<string> suppressUpdateChecks = new HashSet<string>(this.Settings.SuppressUpdateChecks, StringComparer.OrdinalIgnoreCase);

                        // prepare search model
                        List<ModSearchEntryModel> searchMods = new List<ModSearchEntryModel>();
                        foreach (IModMetadata mod in mods)
                        {
                            if (!mod.HasID() || suppressUpdateChecks.Contains(mod.Manifest.UniqueID))
                                continue;

                            string[] updateKeys = mod
                                .GetUpdateKeys(validOnly: true)
                                .Select(p => p.ToString())
                                .ToArray();
                            searchMods.Add(new ModSearchEntryModel(mod.Manifest.UniqueID, mod.Manifest.Version, updateKeys.ToArray(), isBroken: mod.Status == ModMetadataStatus.Failed));
                        }

                        // fetch results
                        this.Monitor.Log($"   Checking for updates to {searchMods.Count} mods...");
                        IDictionary<string, ModEntryModel> results = client.GetModInfo(searchMods.ToArray(), apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform);

                        // extract update alerts & errors
                        var updates = new List<Tuple<IModMetadata, ISemanticVersion, string>>();
                        var errors = new StringBuilder();
                        foreach (IModMetadata mod in mods.OrderBy(p => p.DisplayName))
                        {
                            // link to update-check data
                            if (!mod.HasID() || !results.TryGetValue(mod.Manifest.UniqueID, out ModEntryModel result))
                                continue;
                            mod.SetUpdateData(result);

                            // handle errors
                            if (result.Errors != null && result.Errors.Any())
                            {
                                errors.AppendLine(result.Errors.Length == 1
                                    ? $"   {mod.DisplayName}: {result.Errors[0]}"
                                    : $"   {mod.DisplayName}:\n      - {string.Join("\n      - ", result.Errors)}"
                                );
                            }

                            // handle update
                            if (result.SuggestedUpdate != null)
                                updates.Add(Tuple.Create(mod, result.SuggestedUpdate.Version, result.SuggestedUpdate.Url));
                        }

                        // show update errors
                        if (errors.Length != 0)
                            this.Monitor.Log("Got update-check errors for some mods:\n" + errors.ToString().TrimEnd());

                        // show update alerts
                        if (updates.Any())
                        {
                            this.Monitor.Newline();
                            this.Monitor.Log($"You can update {updates.Count} mod{(updates.Count != 1 ? "s" : "")}:", LogLevel.Alert);
                            foreach (var entry in updates)
                            {
                                IModMetadata mod = entry.Item1;
                                ISemanticVersion newVersion = entry.Item2;
                                string newUrl = entry.Item3;
                                this.Monitor.Log($"   {mod.DisplayName} {newVersion}: {newUrl}", LogLevel.Alert);
                            }
                        }
                        else
                            this.Monitor.Log("   All mods up to date.");
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log("Couldn't check for new mod versions. This won't affect your game, but you won't be notified of mod updates if this keeps happening.", LogLevel.Warn);
                        this.Monitor.Log(ex is WebException && ex.InnerException == null
                            ? ex.Message
                            : ex.ToString()
                        );
                    }
                }
            }).Start();
        }

        /// <summary>Create a directory path if it doesn't exist.</summary>
        /// <param name="path">The directory path.</param>
        private void VerifyPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                // note: this happens before this.Monitor is initialized
                Console.WriteLine($"Couldn't create a path: {path}\n\n{ex.GetLogSummary()}");
            }
        }

        /// <summary>Load and hook up the given mods.</summary>
        /// <param name="mods">The mods to load.</param>
        /// <param name="jsonHelper">The JSON helper with which to read mods' JSON files.</param>
        /// <param name="contentCore">The content manager to use for mod content.</param>
        /// <param name="modDatabase">Handles access to SMAPI's internal mod metadata list.</param>
        private void LoadMods(IModMetadata[] mods, JsonHelper jsonHelper, ContentCoordinator contentCore, ModDatabase modDatabase)
        {
            this.Monitor.Log("Loading mods...", LogLevel.Debug);

            // load mods
            IList<IModMetadata> skippedMods = new List<IModMetadata>();
            using (AssemblyLoader modAssemblyLoader = new AssemblyLoader(Constants.Platform, Constants.GameFramework, this.Monitor, this.Settings.ParanoidWarnings, this.Settings.RewriteMods))
            {
                // init
                HashSet<string> suppressUpdateChecks = new HashSet<string>(this.Settings.SuppressUpdateChecks, StringComparer.OrdinalIgnoreCase);
                InterfaceProxyFactory proxyFactory = new InterfaceProxyFactory();

                // load mods
                foreach (IModMetadata mod in mods)
                {
                    if (!this.TryLoadMod(mod, mods, modAssemblyLoader, proxyFactory, jsonHelper, contentCore, modDatabase, suppressUpdateChecks, out ModFailReason? failReason, out string errorPhrase, out string errorDetails))
                    {
                        failReason ??= ModFailReason.LoadFailed;
                        mod.SetStatus(ModMetadataStatus.Failed, failReason.Value, errorPhrase, errorDetails);
                        skippedMods.Add(mod);
                    }
                }
            }

            IModMetadata[] loaded = this.ModRegistry.GetAll().ToArray();
            IModMetadata[] loadedContentPacks = loaded.Where(p => p.IsContentPack).ToArray();
            IModMetadata[] loadedMods = loaded.Where(p => !p.IsContentPack).ToArray();

            // unlock content packs
            this.ModRegistry.AreAllModsLoaded = true;

            // log mod info
            this.LogManager.LogModInfo(loaded, loadedContentPacks, loadedMods, skippedMods.ToArray(), this.Settings.ParanoidWarnings);

            // initialize translations
            this.ReloadTranslations(loaded);

            // initialize loaded non-content-pack mods
            this.Monitor.Log("Launching mods...", LogLevel.Debug);
            foreach (IModMetadata metadata in loadedMods)
            {
                // add interceptors
                if (metadata.Mod.Helper.Content is ContentHelper helper)
                {
                    // ReSharper disable SuspiciousTypeConversion.Global
                    if (metadata.Mod is IAssetEditor editor)
                        this.ContentCore.Editors.Add(new ModLinked<IAssetEditor>(metadata, editor));
                    if (metadata.Mod is IAssetLoader loader)
                        this.ContentCore.Loaders.Add(new ModLinked<IAssetLoader>(metadata, loader));
                    // ReSharper restore SuspiciousTypeConversion.Global

                    helper.ObservableAssetEditors.CollectionChanged += (sender, e) => this.OnAssetInterceptorsChanged(metadata, e.NewItems?.Cast<IAssetEditor>(), e.OldItems?.Cast<IAssetEditor>(), this.ContentCore.Editors);
                    helper.ObservableAssetLoaders.CollectionChanged += (sender, e) => this.OnAssetInterceptorsChanged(metadata, e.NewItems?.Cast<IAssetLoader>(), e.OldItems?.Cast<IAssetLoader>(), this.ContentCore.Loaders);
                }

                // call entry method
                try
                {
                    IMod mod = metadata.Mod;
                    mod.Entry(mod.Helper);
                }
                catch (Exception ex)
                {
                    metadata.LogAsMod($"Mod crashed on entry and might not work correctly. Technical details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }

                // get mod API
                try
                {
                    object api = metadata.Mod.GetApi();
                    if (api != null && !api.GetType().IsPublic)
                    {
                        api = null;
                        this.Monitor.Log($"{metadata.DisplayName} provides an API instance with a non-public type. This isn't currently supported, so the API won't be available to other mods.", LogLevel.Warn);
                    }

                    if (api != null)
                        this.Monitor.Log($"   Found mod-provided API ({api.GetType().FullName}).");
                    metadata.SetApi(api);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Failed loading mod-provided API for {metadata.DisplayName}. Integrations with other mods may not work. Error: {ex.GetLogSummary()}", LogLevel.Error);
                }
            }

            // unlock mod integrations
            this.ModRegistry.AreAllModsInitialized = true;

            this.Monitor.Log("Mods loaded and ready!", LogLevel.Debug);
        }

        /// <summary>Raised after a mod adds or removes asset interceptors.</summary>
        /// <typeparam name="T">The asset interceptor type (one of <see cref="IAssetEditor"/> or <see cref="IAssetLoader"/>).</typeparam>
        /// <param name="mod">The mod metadata.</param>
        /// <param name="added">The interceptors that were added.</param>
        /// <param name="removed">The interceptors that were removed.</param>
        /// <param name="list">A list of interceptors to update for the change.</param>
        private void OnAssetInterceptorsChanged<T>(IModMetadata mod, IEnumerable<T> added, IEnumerable<T> removed, IList<ModLinked<T>> list)
        {
            foreach (T interceptor in added ?? new T[0])
            {
                this.ReloadAssetInterceptorsQueue.Add(new AssetInterceptorChange(mod, interceptor, wasAdded: true));
                list.Add(new ModLinked<T>(mod, interceptor));
            }

            foreach (T interceptor in removed ?? new T[0])
            {
                this.ReloadAssetInterceptorsQueue.Add(new AssetInterceptorChange(mod, interceptor, wasAdded: false));
                foreach (ModLinked<T> entry in list.Where(p => p.Mod == mod && object.ReferenceEquals(p.Data, interceptor)).ToArray())
                    list.Remove(entry);
            }
        }

        /// <summary>Load a given mod.</summary>
        /// <param name="mod">The mod to load.</param>
        /// <param name="mods">The mods being loaded.</param>
        /// <param name="assemblyLoader">Preprocesses and loads mod assemblies.</param>
        /// <param name="proxyFactory">Generates proxy classes to access mod APIs through an arbitrary interface.</param>
        /// <param name="jsonHelper">The JSON helper with which to read mods' JSON files.</param>
        /// <param name="contentCore">The content manager to use for mod content.</param>
        /// <param name="modDatabase">Handles access to SMAPI's internal mod metadata list.</param>
        /// <param name="suppressUpdateChecks">The mod IDs to ignore when validating update keys.</param>
        /// <param name="failReason">The reason the mod couldn't be loaded, if applicable.</param>
        /// <param name="errorReasonPhrase">The user-facing reason phrase explaining why the mod couldn't be loaded (if applicable).</param>
        /// <param name="errorDetails">More detailed details about the error intended for developers (if any).</param>
        /// <returns>Returns whether the mod was successfully loaded.</returns>
        private bool TryLoadMod(IModMetadata mod, IModMetadata[] mods, AssemblyLoader assemblyLoader, InterfaceProxyFactory proxyFactory, JsonHelper jsonHelper, ContentCoordinator contentCore, ModDatabase modDatabase, HashSet<string> suppressUpdateChecks, out ModFailReason? failReason, out string errorReasonPhrase, out string errorDetails)
        {
            errorDetails = null;

            // log entry
            {
                string relativePath = mod.GetRelativePathWithRoot();
                if (mod.IsContentPack)
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}) [content pack]...");
                else if (mod.Manifest?.EntryDll != null)
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}{Path.DirectorySeparatorChar}{mod.Manifest.EntryDll})..."); // don't use Path.Combine here, since EntryDLL might not be valid
                else
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath})...");
            }

            // add warning for missing update key
            if (mod.HasID() && !suppressUpdateChecks.Contains(mod.Manifest.UniqueID) && !mod.HasValidUpdateKeys())
                mod.SetWarning(ModWarning.NoUpdateKeys);

            // validate status
            if (mod.Status == ModMetadataStatus.Failed)
            {
                this.Monitor.Log($"      Failed: {mod.ErrorDetails ?? mod.Error}");
                failReason = mod.FailReason;
                errorReasonPhrase = mod.Error;
                return false;
            }

            // validate dependencies
            // Although dependencies are validated before mods are loaded, a dependency may have failed to load.
            foreach (IManifestDependency dependency in mod.Manifest.Dependencies.Where(p => p.IsRequired))
            {
                if (this.ModRegistry.Get(dependency.UniqueID) == null)
                {
                    string dependencyName = mods
                        .FirstOrDefault(otherMod => otherMod.HasID(dependency.UniqueID))
                        ?.DisplayName ?? dependency.UniqueID;
                    errorReasonPhrase = $"it needs the '{dependencyName}' mod, which couldn't be loaded.";
                    failReason = ModFailReason.MissingDependencies;
                    return false;
                }
            }

            // load as content pack
            if (mod.IsContentPack)
            {
                IManifest manifest = mod.Manifest;
                IMonitor monitor = this.LogManager.GetMonitor(mod.DisplayName);
                IContentHelper contentHelper = new ContentHelper(this.ContentCore, mod.DirectoryPath, manifest.UniqueID, mod.DisplayName, monitor);
                TranslationHelper translationHelper = new TranslationHelper(manifest.UniqueID, contentCore.GetLocale(), contentCore.Language);
                IContentPack contentPack = new ContentPack(mod.DirectoryPath, manifest, contentHelper, translationHelper, jsonHelper);
                mod.SetMod(contentPack, monitor, translationHelper);
                this.ModRegistry.Add(mod);

                errorReasonPhrase = null;
                failReason = null;
                return true;
            }

            // load as mod
            else
            {
                // get mod info
                IManifest manifest = mod.Manifest;
                string assemblyPath = Path.Combine(mod.DirectoryPath, manifest.EntryDll);

                // load mod
                Assembly modAssembly;
                try
                {
                    modAssembly = assemblyLoader.Load(mod, assemblyPath, assumeCompatible: mod.DataRecord?.Status == ModStatus.AssumeCompatible);
                    this.ModRegistry.TrackAssemblies(mod, modAssembly);
                }
                catch (IncompatibleInstructionException) // details already in trace logs
                {
                    string[] updateUrls = new[] { modDatabase.GetModPageUrlFor(manifest.UniqueID), "https://smapi.io/mods" }.Where(p => p != null).ToArray();
                    errorReasonPhrase = $"it's no longer compatible. Please check for a new version at {string.Join(" or ", updateUrls)}";
                    failReason = ModFailReason.Incompatible;
                    return false;
                }
                catch (SAssemblyLoadFailedException ex)
                {
                    errorReasonPhrase = $"its DLL couldn't be loaded: {ex.Message}";
                    failReason = ModFailReason.LoadFailed;
                    return false;
                }
                catch (Exception ex)
                {
                    errorReasonPhrase = "its DLL couldn't be loaded.";
                    if (ex is BadImageFormatException && !EnvironmentUtility.Is64BitAssembly(assemblyPath))
                        errorReasonPhrase = "it needs to be updated for 64-bit mode.";

                    errorDetails = $"Error: {ex.GetLogSummary()}";
                    failReason = ModFailReason.LoadFailed;
                    return false;
                }

                // initialize mod
                try
                {
                    // get mod instance
                    if (!this.TryLoadModEntry(modAssembly, out Mod modEntry, out errorReasonPhrase))
                    {
                        failReason = ModFailReason.LoadFailed;
                        return false;
                    }

                    // get content packs
                    IContentPack[] GetContentPacks()
                    {
                        if (!this.ModRegistry.AreAllModsLoaded)
                            throw new InvalidOperationException("Can't access content packs before SMAPI finishes loading mods.");

                        return this.ModRegistry
                            .GetAll(assemblyMods: false)
                            .Where(p => p.IsContentPack && mod.HasID(p.Manifest.ContentPackFor.UniqueID))
                            .Select(p => p.ContentPack)
                            .ToArray();
                    }

                    // init mod helpers
                    IMonitor monitor = this.LogManager.GetMonitor(mod.DisplayName);
                    TranslationHelper translationHelper = new TranslationHelper(manifest.UniqueID, contentCore.GetLocale(), contentCore.Language);
                    IModHelper modHelper;
                    {
                        IContentPack CreateFakeContentPack(string packDirPath, IManifest packManifest)
                        {
                            IMonitor packMonitor = this.LogManager.GetMonitor(packManifest.Name);
                            IContentHelper packContentHelper = new ContentHelper(contentCore, packDirPath, packManifest.UniqueID, packManifest.Name, packMonitor);
                            TranslationHelper packTranslationHelper = new TranslationHelper(packManifest.UniqueID, contentCore.GetLocale(), contentCore.Language);

                            ContentPack contentPack = new ContentPack(packDirPath, packManifest, packContentHelper, packTranslationHelper, this.Toolkit.JsonHelper);
                            this.ReloadTranslationsForTemporaryContentPack(mod, contentPack);
                            mod.FakeContentPacks.Add(new WeakReference<ContentPack>(contentPack));
                            return contentPack;
                        }

                        IModEvents events = new ModEvents(mod, this.EventManager);
                        ICommandHelper commandHelper = new CommandHelper(mod, this.CommandManager);
                        IContentHelper contentHelper = new ContentHelper(contentCore, mod.DirectoryPath, manifest.UniqueID, mod.DisplayName, monitor);
                        IContentPackHelper contentPackHelper = new ContentPackHelper(manifest.UniqueID, new Lazy<IContentPack[]>(GetContentPacks), CreateFakeContentPack);
                        IDataHelper dataHelper = new DataHelper(manifest.UniqueID, mod.DirectoryPath, jsonHelper);
                        IReflectionHelper reflectionHelper = new ReflectionHelper(manifest.UniqueID, mod.DisplayName, this.Reflection);
                        IModRegistry modRegistryHelper = new ModRegistryHelper(manifest.UniqueID, this.ModRegistry, proxyFactory, monitor);
                        IMultiplayerHelper multiplayerHelper = new MultiplayerHelper(manifest.UniqueID, this.Multiplayer);

                        modHelper = new ModHelper(manifest.UniqueID, mod.DirectoryPath, () => this.GetCurrentGameInstance().Input, events, contentHelper, contentPackHelper, commandHelper, dataHelper, modRegistryHelper, reflectionHelper, multiplayerHelper, translationHelper);
                    }

                    // init mod
                    modEntry.ModManifest = manifest;
                    modEntry.Helper = modHelper;
                    modEntry.Monitor = monitor;

                    // track mod
                    mod.SetMod(modEntry, translationHelper);
                    this.ModRegistry.Add(mod);
                    failReason = null;
                    return true;
                }
                catch (Exception ex)
                {
                    errorReasonPhrase = $"initialization failed:\n{ex.GetLogSummary()}";
                    failReason = ModFailReason.LoadFailed;
                    return false;
                }
            }
        }

        /// <summary>Load a mod's entry class.</summary>
        /// <param name="modAssembly">The mod assembly.</param>
        /// <param name="mod">The loaded instance.</param>
        /// <param name="error">The error indicating why loading failed (if applicable).</param>
        /// <returns>Returns whether the mod entry class was successfully loaded.</returns>
        private bool TryLoadModEntry(Assembly modAssembly, out Mod mod, out string error)
        {
            mod = null;

            // find type
            TypeInfo[] modEntries = modAssembly.DefinedTypes.Where(type => typeof(Mod).IsAssignableFrom(type) && !type.IsAbstract).Take(2).ToArray();
            if (modEntries.Length == 0)
            {
                error = $"its DLL has no '{nameof(Mod)}' subclass.";
                return false;
            }
            if (modEntries.Length > 1)
            {
                error = $"its DLL contains multiple '{nameof(Mod)}' subclasses.";
                return false;
            }

            // get implementation
            mod = (Mod)modAssembly.CreateInstance(modEntries[0].ToString());
            if (mod == null)
            {
                error = "its entry class couldn't be instantiated.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>Reload translations for all mods.</summary>
        private void ReloadTranslations()
        {
            this.ReloadTranslations(this.ModRegistry.GetAll());
        }

        /// <summary>Reload translations for the given mods.</summary>
        /// <param name="mods">The mods for which to reload translations.</param>
        private void ReloadTranslations(IEnumerable<IModMetadata> mods)
        {
            // core SMAPI translations
            {
                var translations = this.ReadTranslationFiles(Path.Combine(Constants.InternalFilesPath, "i18n"), out IList<string> errors);
                if (errors.Any() || !translations.Any())
                {
                    this.Monitor.Log("SMAPI couldn't load some core translations. You may need to reinstall SMAPI.", LogLevel.Warn);
                    foreach (string error in errors)
                        this.Monitor.Log($"  - {error}", LogLevel.Warn);
                }
                this.Translator.SetTranslations(translations);
            }

            // mod translations
            foreach (IModMetadata metadata in mods)
            {
                // top-level mod
                {
                    var translations = this.ReadTranslationFiles(Path.Combine(metadata.DirectoryPath, "i18n"), out IList<string> errors);
                    if (errors.Any())
                    {
                        metadata.LogAsMod("Mod couldn't load some translation files:", LogLevel.Warn);
                        foreach (string error in errors)
                            metadata.LogAsMod($"  - {error}", LogLevel.Warn);
                    }

                    metadata.Translations.SetTranslations(translations);
                }

                // fake content packs
                foreach (ContentPack pack in metadata.GetFakeContentPacks())
                    this.ReloadTranslationsForTemporaryContentPack(metadata, pack);
            }
        }

        /// <summary>Load or reload translations for a temporary content pack created by a mod.</summary>
        /// <param name="parentMod">The parent mod which created the content pack.</param>
        /// <param name="contentPack">The content pack instance.</param>
        private void ReloadTranslationsForTemporaryContentPack(IModMetadata parentMod, ContentPack contentPack)
        {
            var translations = this.ReadTranslationFiles(Path.Combine(contentPack.DirectoryPath, "i18n"), out IList<string> errors);
            if (errors.Any())
            {
                parentMod.LogAsMod($"Generated content pack at '{PathUtilities.GetRelativePath(Constants.ModsPath, contentPack.DirectoryPath)}' couldn't load some translation files:", LogLevel.Warn);
                foreach (string error in errors)
                    parentMod.LogAsMod($"  - {error}", LogLevel.Warn);
            }

            contentPack.TranslationImpl.SetTranslations(translations);
        }

        /// <summary>Read translations from a directory containing JSON translation files.</summary>
        /// <param name="folderPath">The folder path to search.</param>
        /// <param name="errors">The errors indicating why translation files couldn't be parsed, indexed by translation filename.</param>
        private IDictionary<string, IDictionary<string, string>> ReadTranslationFiles(string folderPath, out IList<string> errors)
        {
            JsonHelper jsonHelper = this.Toolkit.JsonHelper;

            // read translation files
            var translations = new Dictionary<string, IDictionary<string, string>>();
            errors = new List<string>();
            DirectoryInfo translationsDir = new DirectoryInfo(folderPath);
            if (translationsDir.Exists)
            {
                foreach (FileInfo file in translationsDir.EnumerateFiles("*.json"))
                {
                    string locale = Path.GetFileNameWithoutExtension(file.Name.ToLower().Trim());
                    try
                    {
                        if (!jsonHelper.ReadJsonFileIfExists(file.FullName, out IDictionary<string, string> data) || data == null)
                        {
                            errors.Add($"{file.Name} file couldn't be read"); // mainly happens when the file is corrupted or empty
                            continue;
                        }

                        translations[locale] = data;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{file.Name} file couldn't be parsed: {ex.GetLogSummary()}");
                    }
                }
            }

            // validate translations
            foreach (string locale in translations.Keys.ToArray())
            {
                // handle duplicates
                HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string key in translations[locale].Keys.ToArray())
                {
                    if (!keys.Add(key))
                    {
                        duplicateKeys.Add(key);
                        translations[locale].Remove(key);
                    }
                }
                if (duplicateKeys.Any())
                    errors.Add($"{locale}.json has duplicate translation keys: [{string.Join(", ", duplicateKeys)}]. Keys are case-insensitive.");
            }

            return translations;
        }

        /// <summary>Get the map display device which applies SMAPI features like tile rotation to loaded maps.</summary>
        /// <remarks>This is separate to let mods like PyTK wrap it with their own functionality.</remarks>
        private IDisplayDevice GetMapDisplayDevice()
        {
            return new SDisplayDevice(Game1.content, Game1.game1.GraphicsDevice);
        }

        /// <summary>Get the absolute path to the next available log file.</summary>
        private string GetLogPath()
        {
            // default path
            {
                FileInfo defaultFile = new FileInfo(Path.Combine(Constants.LogDir, $"{Constants.LogFilename}.{Constants.LogExtension}"));
                if (!defaultFile.Exists)
                    return defaultFile.FullName;
            }

            // get first disambiguated path
            for (int i = 2; i < int.MaxValue; i++)
            {
                FileInfo file = new FileInfo(Path.Combine(Constants.LogDir, $"{Constants.LogFilename}.player-{i}.{Constants.LogExtension}"));
                if (!file.Exists)
                    return file.FullName;
            }

            // should never happen
            throw new InvalidOperationException("Could not find an available log path.");
        }

        /// <summary>Delete normal (non-crash) log files created by SMAPI.</summary>
        private void PurgeNormalLogs()
        {
            DirectoryInfo logsDir = new DirectoryInfo(Constants.LogDir);
            if (!logsDir.Exists)
                return;

            foreach (FileInfo logFile in logsDir.EnumerateFiles())
            {
                // skip non-SMAPI file
                if (!logFile.Name.StartsWith(Constants.LogNamePrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                // skip crash log
                if (logFile.FullName == Constants.FatalCrashLog)
                    continue;

                // delete file
                try
                {
                    FileUtilities.ForceDelete(logFile);
                }
                catch (IOException)
                {
                    // ignore file if it's in use
                }
            }
        }

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        /// <param name="message">The fatal log message.</param>
        private void ExitGameImmediately(string message)
        {
            this.Monitor.LogFatal(message);
            this.CancellationToken.Cancel();
        }

        /// <summary>Get the screen ID that should be logged to distinguish between players in split-screen mode, if any.</summary>
        private int? GetScreenIdForLog()
        {
            if (Context.ScreenId != 0 || (Context.IsWorldReady && Context.IsSplitScreen))
                return Context.ScreenId;

            return null;
        }
    }
}
