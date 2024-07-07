using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#if SMAPI_FOR_WINDOWS
using Microsoft.Win32;
#endif
using Newtonsoft.Json;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Deprecations;
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
using StardewModdingAPI.Framework.StateTracking.Snapshots;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;
using StardewModdingAPI.Toolkit.Utilities.PathLookups;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Mods;
using StardewValley.Objects;
using StardewValley.SDKs;
using xTile.Display;
using LanguageCode = StardewValley.LocalizedContentManager.LanguageCode;
using MiniMonoModHotfix = MonoMod.Utils.MiniMonoModHotfix;
using PathUtilities = StardewModdingAPI.Toolkit.Utilities.PathUtilities;

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
        /// <summary>A state which indicates whether SMAPI should exit immediately and any pending initialization should be cancelled.</summary>
        private ExitState ExitState;

        /// <summary>Whether the game should exit immediately and any pending initialization should be cancelled.</summary>
        private bool IsExiting => this.ExitState != ExitState.None;

        /// <summary>Manages the SMAPI console window and log file.</summary>
        private readonly LogManager LogManager;

        /// <summary>The core logger and monitor for SMAPI.</summary>
        private Monitor Monitor => this.LogManager.Monitor;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection = new();

        /// <summary>Encapsulates access to SMAPI core translations.</summary>
        private readonly Translator Translator = new();

        /// <summary>The SMAPI configuration settings.</summary>
        private readonly SConfig Settings;

        /// <summary>The mod toolkit used for generic mod interactions.</summary>
        private readonly ModToolkit Toolkit = new();

        /****
        ** Higher-level components
        ****/
        /// <summary>Manages console commands.</summary>
        private readonly CommandManager CommandManager;

        /// <summary>The underlying game instance.</summary>
        private SGameRunner Game = null!; // initialized very early

        /// <summary>SMAPI's content manager.</summary>
        private ContentCoordinator ContentCore = null!; // initialized very early

        /// <summary>The game's core multiplayer utility for the main player.</summary>
        private SMultiplayer Multiplayer = null!; // initialized very early

        /// <summary>Tracks the installed mods.</summary>
        /// <remarks>This is initialized after the game starts.</remarks>
        private readonly ModRegistry ModRegistry = new();

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

        /// <summary>Whether the game has initialized for any custom languages from <c>Data/AdditionalLanguages</c>.</summary>
        private bool AreCustomLanguagesInitialized;

        /// <summary>Whether the player just returned to the title screen.</summary>
        public bool JustReturnedToTitle { get; set; }

        /// <summary>The last language set by the game.</summary>
        private (string Locale, LanguageCode Code) LastLanguage { get; set; } = ("", LanguageCode.en);

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from an update error.</summary>
        private readonly Countdown UpdateCrashTimer = new(60); // 60 ticks = roughly one second

        /// <summary>A list of queued commands to parse and execute.</summary>
        private readonly CommandQueue RawCommandQueue = new();

        /// <summary>A list of commands to execute on each screen.</summary>
        private readonly PerScreen<List<QueuedCommand>> ScreenCommandQueue = new(() => new List<QueuedCommand>());

        /// <summary>The last <see cref="ProcessTicksElapsed"/> for which display events were raised.</summary>
        private readonly PerScreen<uint> LastRenderEventTick = new();


        /*********
        ** Accessors
        *********/
        /// <summary>Manages deprecation warnings.</summary>
        /// <remarks>This is initialized after the game starts. This is accessed directly because it's not part of the normal class model.</remarks>
        internal static DeprecationManager DeprecationManager { get; private set; } = null!; // initialized in constructor, which happens before other code can access it

        /// <summary>The singleton instance.</summary>
        /// <remarks>This is only intended for use by external code.</remarks>
        internal static SCore Instance { get; private set; } = null!; // initialized in constructor, which happens before other code can access it

        /// <summary>The number of game update ticks which have already executed. This is similar to <see cref="Game1.ticks"/>, but incremented more consistently for every tick.</summary>
        internal static uint TicksElapsed { get; private set; }

        /// <summary>A specialized form of <see cref="TicksElapsed"/> which is incremented each time SMAPI performs a processing tick (whether that's a game update, one wait cycle while synchronizing code, etc).</summary>
        internal static uint ProcessTicksElapsed { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modsPath">The path to search for mods.</param>
        /// <param name="writeToConsole">Whether to output log messages to the console.</param>
        /// <param name="developerMode">Whether to enable development features, or <c>null</c> to use the value from the settings file.</param>
        public SCore(string modsPath, bool writeToConsole, bool? developerMode)
        {
            SCore.Instance = this;

            // init paths
            this.VerifyPath(modsPath);
            this.VerifyPath(Constants.LogDir);
            Constants.ModsPath = modsPath;

            // init log file
            this.PurgeNormalLogs();
            string logPath = this.GetLogPath();

            // init settings
            {
                var deserializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

                this.Settings = JsonConvert.DeserializeObject<SConfig>(File.ReadAllText(Constants.ApiConfigPath)) ?? throw new InvalidOperationException("The 'smapi-internal/config.json' file is missing or invalid. You can reinstall SMAPI to fix this.");
                if (File.Exists(Constants.ApiUserConfigPath))
                    JsonConvert.PopulateObject(File.ReadAllText(Constants.ApiUserConfigPath), this.Settings, deserializerSettings);
                if (File.Exists(Constants.ApiModGroupConfigPath))
                    JsonConvert.PopulateObject(File.ReadAllText(Constants.ApiModGroupConfigPath), this.Settings, deserializerSettings);
                if (developerMode.HasValue)
                    this.Settings.OverrideDeveloperMode(developerMode.Value);
            }

            // init basics
            this.LogManager = new LogManager(logPath: logPath, colorConfig: this.Settings.ConsoleColors, writeToConsole: writeToConsole, verboseLogging: this.Settings.VerboseLogging, isDeveloperMode: this.Settings.DeveloperMode, getScreenIdForLog: this.GetScreenIdForLog);
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
        [SecurityCritical]
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
                AppDomain.CurrentDomain.UnhandledException += (_, e) => this.Monitor.Log($"Critical app domain exception: {e.ExceptionObject}", LogLevel.Error);

                // add more lenient assembly resolver
                AppDomain.CurrentDomain.AssemblyResolve += (_, e) => AssemblyLoader.ResolveAssembly(e.Name);

                // hook locale event
                LocalizedContentManager.OnLanguageChange += _ => this.OnLocaleChanged();

                // override game
                this.Multiplayer = new SMultiplayer(this.Monitor, this.EventManager, this.Toolkit.JsonHelper, this.ModRegistry, this.OnModMessageReceived, this.Settings.LogNetworkTraffic);
                SGame.CreateContentManagerImpl = this.CreateContentManager; // must be static since the game accesses it before the SGame constructor is called
                this.Game = new SGameRunner(
                    monitor: this.Monitor,
                    reflection: this.Reflection,
                    modHooks: new SModHooks(
                        parent: new ModHooks(),
                        beforeNewDayAfterFade: this.OnNewDayAfterFade,
                        onStageChanged: this.OnLoadStageChanged,
                        onRenderingStep: this.OnRenderingStep,
                        onRenderedStep: this.OnRenderedStep,
                        monitor: this.Monitor
                    ),
                    gameLogger: new SGameLogger(this.GetMonitorForGame()),
                    multiplayer: this.Multiplayer,
                    exitGameImmediately: this.ExitGameImmediately,

                    onGameContentLoaded: this.OnInstanceContentLoaded,
                    onLoadStageChanged: this.OnLoadStageChanged,
                    onGameUpdating: this.OnGameUpdating,
                    onPlayerInstanceUpdating: this.OnPlayerInstanceUpdating,
                    onPlayerInstanceRendered: this.OnRendered,
                    onGameExiting: this.OnGameExiting
                );
                GameRunner.instance = this.Game;

                // fix Harmony for mods
                if (this.Settings.FixHarmony)
                    MiniMonoModHotfix.Apply();

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
                this.Dispose(isError: false);
            }
            catch (Exception ex)
            {
                this.LogManager.LogFatalLaunchError(ex);
                this.LogManager.PressAnyKeyToExit();
                this.Dispose(isError: true);
            }
        }

        /// <summary>Get the core logger and monitor on behalf of the game.</summary>
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used via reflection")]
        public IMonitor GetMonitorForGame()
        {
            return this.LogManager.MonitorForGame;
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "May be disposed before SMAPI is fully initialized.")]
        public void Dispose()
        {
            this.Dispose(isError: true); // if we got here, SMAPI didn't detect the exit before it happened
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <param name="isError">Whether the process is exiting due to an error or crash.</param>
        [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "May be disposed before SMAPI is fully initialized.")]
        public void Dispose(bool isError)
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
            if (this.ExitState == ExitState.None || isError)
                this.ExitState = isError ? ExitState.Crash : ExitState.GameExit;
            this.ContentCore?.Dispose();
            this.Game?.Dispose();
            this.LogManager.Dispose(); // dispose last to allow for any last-second log messages

            // clean up SDK
            // This avoids Steam connection errors when it exits unexpectedly. The game avoids this
            // by killing the entire process, but we can't set the error code if we do that.
            try
            {
                FieldInfo? field = typeof(StardewValley.Program).GetField("_sdk", BindingFlags.NonPublic | BindingFlags.Static);
                SDKHelper? sdk = field?.GetValue(null) as SDKHelper;
                sdk?.Shutdown();
            }
            catch
            {
                // well, at least we tried
            }

            // end game with error code
            // This helps the OS decide whether to keep the window open (e.g. Windows may keep it open on error).
            Environment.Exit(this.ExitState == ExitState.Crash ? 1 : 0);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Initialize mods before the first game asset is loaded. At this point the core content managers are loaded (so mods can load their own assets), but the game is mostly uninitialized.</summary>
        private void InitializeBeforeFirstAssetLoaded()
        {
            if (this.IsExiting)
            {
                this.Monitor.Log("SMAPI shutting down: aborting initialization.", LogLevel.Warn);
                return;
            }

            // init TMX support
            xTile.Format.FormatManager.Instance.RegisterMapFormat(new TMXTile.TMXFormat(Game1.tileSize / Game1.pixelZoom, Game1.tileSize / Game1.pixelZoom, Game1.pixelZoom, Game1.pixelZoom));

            // load mod data
            ModToolkit toolkit = new();
            ModDatabase modDatabase = toolkit.GetModDatabase(Constants.ApiMetadataPath);

            // load mods
            {
                this.Monitor.Log("Loading mod metadata...", LogLevel.Debug);
                ModResolver resolver = new();

                // log loose files
                {
                    string[] looseFiles = new DirectoryInfo(this.ModsPath).GetFiles().Select(p => p.Name).ToArray();
                    if (looseFiles.Any())
                    {
                        if (looseFiles.Any(name => name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                        {
                            this.Monitor.Log($"Detected mod files directly inside the '{Path.GetFileName(this.ModsPath)}' folder. These will be ignored. Each mod must have its own subfolder instead.", LogLevel.Error);
                        }

                        this.Monitor.Log($"  Ignored loose files: {string.Join(", ", looseFiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))}");
                    }
                }

                // load manifests
                IModMetadata[] mods = resolver.ReadManifests(toolkit, this.ModsPath, modDatabase, useCaseInsensitiveFilePaths: this.Settings.UseCaseInsensitivePaths).ToArray();

                // filter out ignored mods
                foreach (IModMetadata mod in mods.Where(p => p.IsIgnored))
                    this.Monitor.Log($"  Skipped {mod.GetRelativePathWithRoot()} (folder name starts with a dot).");
                mods = mods.Where(p => !p.IsIgnored).ToArray();

                // validate manifests
                resolver.ValidateManifests(mods, Constants.ApiVersion, Constants.GameVersion, toolkit.GetUpdateUrl, getFileLookup: this.GetFileLookup);

                // apply load order customizations
                if (this.Settings.ModsToLoadEarly.Any() || this.Settings.ModsToLoadLate.Any())
                {
                    HashSet<string> installedIds = new HashSet<string>(mods.Select(p => p.Manifest?.UniqueID).Where(p => p is not null), StringComparer.OrdinalIgnoreCase);

                    string[] missingEarlyMods = this.Settings.ModsToLoadEarly.Where(id => !installedIds.Contains(id)).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
                    string[] missingLateMods = this.Settings.ModsToLoadLate.Where(id => !installedIds.Contains(id)).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
                    string[] duplicateMods = this.Settings.ModsToLoadLate.Where(id => this.Settings.ModsToLoadEarly.Contains(id)).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

                    if (missingEarlyMods.Any())
                        this.Monitor.Log($"  The 'smapi-internal/config.json' file lists mod IDs in {nameof(this.Settings.ModsToLoadEarly)} which aren't installed: '{string.Join("', '", missingEarlyMods)}'.", LogLevel.Warn);
                    if (missingLateMods.Any())
                        this.Monitor.Log($"  The 'smapi-internal/config.json' file lists mod IDs in {nameof(this.Settings.ModsToLoadLate)} which aren't installed: '{string.Join("', '", missingLateMods)}'.", LogLevel.Warn);
                    if (duplicateMods.Any())
                        this.Monitor.Log($"  The 'smapi-internal/config.json' file lists mod IDs which are in both {nameof(this.Settings.ModsToLoadEarly)} and {nameof(this.Settings.ModsToLoadLate)}: '{string.Join("', '", duplicateMods)}'. These will be loaded early.", LogLevel.Warn);

                    mods = resolver.ApplyLoadOrderOverrides(mods, this.Settings.ModsToLoadEarly, this.Settings.ModsToLoadLate);
                }

                // load mods
                mods = resolver.ProcessDependencies(mods, modDatabase).ToArray();
                this.LoadMods(mods, this.Toolkit.JsonHelper, this.ContentCore, modDatabase);

                // check for software likely to cause issues
                this.CheckForSoftwareConflicts();

                // check for updates
                _ = this.CheckForUpdatesAsync(mods); // ignore task since the main thread doesn't need to wait for it
            }

            // update window titles
            this.UpdateWindowTitles();
        }

        /// <summary>Raised after the game finishes initializing.</summary>
        private void OnGameInitialized()
        {
            // start SMAPI console
            if (this.Settings.ListenForConsoleInput)
            {
                new Thread(
                    () => this.LogManager.RunConsoleInputLoop(
                        commandManager: this.CommandManager,
                        reloadTranslations: this.ReloadTranslations,
                        handleInput: input => this.RawCommandQueue.Add(input),
                        continueWhile: () => this.IsGameRunning && !this.IsExiting
                    )
                ).Start();
            }
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
                if (this.IsExiting)
                {
                    this.Monitor.Log("SMAPI shutting down: aborting update.");
                    return;
                }

                /*********
                ** Prevent Harmony debug mode
                *********/
                if (HarmonyLib.Harmony.DEBUG && this.Settings.SuppressHarmonyDebugMode)
                {
                    HarmonyLib.Harmony.DEBUG = false;
                    this.Monitor.LogOnce("A mod enabled Harmony debug mode, which impacts performance and creates a file on your desktop. SMAPI will try to keep it disabled. (You can allow debug mode by editing the smapi-internal/config.json file.)", LogLevel.Warn);
                }

                /*********
                ** Parse commands
                *********/
                if (this.RawCommandQueue.TryDequeue(out string[]? rawCommands))
                {
                    foreach (string rawInput in rawCommands)
                    {
                        // parse command
                        string? name;
                        string[]? args;
                        Command? command;
                        int screenId;
                        try
                        {
                            if (!this.CommandManager.TryParse(rawInput, out name, out args, out command, out screenId))
                            {
                                this.Monitor.Log($"Unknown command '{(!string.IsNullOrWhiteSpace(name) ? name : rawInput)}'; type 'help' for a list of available commands.", LogLevel.Error);
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"Failed parsing that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                            continue;
                        }

                        // queue command for screen
                        this.ScreenCommandQueue.GetValueForScreen(screenId).Add(new(command, name, args));
                    }
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
                this.Monitor.Log($"An error occurred in the overridden update loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.UpdateCrashTimer.Decrement())
                    this.ExitGameImmediately("The game crashed when updating, and SMAPI was unable to recover the game.");
            }
            finally
            {
                SCore.TicksElapsed++;
                SCore.ProcessTicksElapsed++;
            }
        }

        /// <summary>Raised when the game instance for a local player is updating (once per <see cref="OnGameUpdating"/> per player).</summary>
        /// <param name="instance">The game instance being updated.</param>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <param name="runUpdate">Invoke the game's update logic.</param>
        private void OnPlayerInstanceUpdating(SGame instance, GameTime gameTime, Action runUpdate)
        {
            EventManager events = this.EventManager;
            bool verbose = this.Monitor.IsVerbose;

            try
            {
                /*********
                ** Reapply overrides
                *********/
                if (this.JustReturnedToTitle)
                {
                    if (Game1.mapDisplayDevice is not SDisplayDevice)
                        Game1.mapDisplayDevice = this.GetMapDisplayDevice();

                    this.JustReturnedToTitle = false;
                }

                /*********
                ** Execute commands
                *********/
                if (this.ScreenCommandQueue.Value.Any())
                {
                    var commandQueue = this.ScreenCommandQueue.Value;
                    foreach ((Command? command, string? name, string[]? args) in commandQueue)
                    {
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
                    this.Monitor.Log("Game loader synchronizing...", Monitor.ContextLogLevel);
                    Game1.game1.UpdateTitleScreen(Game1.currentGameTime); // run game logic to change music on load, etc
                    // ReSharper disable once ConstantConditionalAccessQualifier -- may become null within the loop
                    while (Game1.currentLoader?.MoveNext() == true)
                    {
                        SCore.ProcessTicksElapsed++;

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
                    this.Monitor.Log("Game loader done.", Monitor.ContextLogLevel);
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
                        this.Monitor.Log("Context: before save creation.", Monitor.ContextLogLevel);
                        events.SaveCreating.RaiseEmpty();
                    }

                    // raise before-save
                    if (Context.IsWorldReady && !instance.IsBetweenSaveEvents)
                    {
                        instance.IsBetweenSaveEvents = true;
                        this.Monitor.Log("Context: before save.", Monitor.ContextLogLevel);
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
                        this.Monitor.Log($"Context: after save creation, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.", Monitor.ContextLogLevel);
                        this.OnLoadStageChanged(LoadStage.CreatedSaveFile);
                        events.SaveCreated.RaiseEmpty();
                    }

                    if (instance.IsBetweenSaveEvents)
                    {
                        // raise after-save
                        instance.IsBetweenSaveEvents = false;
                        this.Monitor.Log($"Context: after save, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.", Monitor.ContextLogLevel);
                        events.Saved.RaiseEmpty();
                        events.DayStarted.RaiseEmpty();
                    }

                    /*********
                    ** Locale changed events
                    *********/
                    if (state.Locale.IsChanged)
                        this.Monitor.Log($"Context: locale set to {state.Locale.New} ({this.ContentCore.GetLocaleCode(state.Locale.New)}).", Monitor.ContextLogLevel);

                    /*********
                    ** Load / return-to-title events
                    *********/
                    if (wasWorldReady && !Context.IsWorldReady)
                        this.OnLoadStageChanged(LoadStage.None);
                    else if (Context.IsWorldReady && Context.LoadStage != LoadStage.Ready)
                    {
                        // print context
                        string context = $"Context: loaded save '{Constants.SaveFolderName}', starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}, locale set to {this.ContentCore.GetLocale()}.";
                        if (Context.IsMultiplayer)
                        {
                            int onlineCount = Game1.getOnlineFarmers().Count();
                            context += $" {(Context.IsMainPlayer ? "Main player" : "Farmhand")} with {onlineCount} {(onlineCount == 1 ? "player" : "players")} online.";
                        }
                        else
                            context += " Single-player.";

                        this.Monitor.Log(context, Monitor.ContextLogLevel);

                        // add context to window titles
                        this.UpdateWindowTitles();

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
                        if (verbose)
                            this.Monitor.Log($"Events: window size changed to {state.WindowSize.New}.", Monitor.ContextLogLevel);

                        if (events.WindowResized.HasListeners)
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
                            if (state.Cursor.IsChanged && events.CursorMoved.HasListeners)
                                events.CursorMoved.Raise(new CursorMovedEventArgs(state.Cursor.Old!, state.Cursor.New!));

                            // raise mouse wheel scrolled
                            if (state.MouseWheelScroll.IsChanged)
                            {
                                if (verbose)
                                    this.Monitor.Log($"Events: mouse wheel scrolled to {state.MouseWheelScroll.New}.");

                                if (events.MouseWheelScrolled.HasListeners)
                                    events.MouseWheelScrolled.Raise(new MouseWheelScrolledEventArgs(cursor, state.MouseWheelScroll.Old, state.MouseWheelScroll.New));
                            }

                            // raise input button events
                            if (inputState.ButtonStates.Count > 0)
                            {
                                if (events.ButtonsChanged.HasListeners)
                                    events.ButtonsChanged.Raise(new ButtonsChangedEventArgs(cursor, inputState));

                                bool raisePressed = events.ButtonPressed.HasListeners;
                                bool raiseReleased = events.ButtonReleased.HasListeners;
                                bool logInput = verbose || Monitor.ForceLogContext;

                                if (logInput || raisePressed || raiseReleased)
                                {
                                    foreach ((SButton button, SButtonState status) in inputState.ButtonStates)
                                    {
                                        switch (status)
                                        {
                                            case SButtonState.Pressed:
                                                if (logInput)
                                                    this.Monitor.Log($"Events: button {button} pressed.", Monitor.ContextLogLevel);

                                                if (raisePressed)
                                                    events.ButtonPressed.Raise(new ButtonPressedEventArgs(button, cursor, inputState));
                                                break;

                                            case SButtonState.Released:
                                                if (logInput)
                                                    this.Monitor.Log($"Events: button {button} released.", Monitor.ContextLogLevel);

                                                if (raiseReleased)
                                                    events.ButtonReleased.Raise(new ButtonReleasedEventArgs(button, cursor, inputState));
                                                break;
                                        }
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
                        IClickableMenu? was = state.ActiveMenu.Old;
                        IClickableMenu? now = state.ActiveMenu.New;

                        if (verbose || Monitor.ForceLogContext)
                            this.Monitor.Log($"Context: menu changed from {was?.GetType().FullName ?? "none"} to {now?.GetType().FullName ?? "none"}.", Monitor.ContextLogLevel);

                        // raise menu events
                        if (events.MenuChanged.HasListeners)
                            events.MenuChanged.Raise(new MenuChangedEventArgs(was, now));
                    }

                    /*********
                    ** World & player events
                    *********/
                    if (Context.IsWorldReady)
                    {
                        bool raiseWorldEvents = !state.SaveID.IsChanged; // don't report changes from unloaded => loaded

                        // location list changes
                        if (state.Locations.LocationList.IsChanged && (events.LocationListChanged.HasListeners || verbose))
                        {
                            var added = state.Locations.LocationList.Added.ToArray();
                            var removed = state.Locations.LocationList.Removed.ToArray();

                            if (verbose)
                            {
                                string addedText = added.Any() ? string.Join(", ", added.Select(p => p.Name)) : "none";
                                string removedText = removed.Any() ? string.Join(", ", removed.Select(p => p.Name)) : "none";
                                this.Monitor.Log($"Context: location list changed (added {addedText}; removed {removedText}).", Monitor.ContextLogLevel);
                            }

                            if (events.LocationListChanged.HasListeners)
                                events.LocationListChanged.Raise(new LocationListChangedEventArgs(added, removed));
                        }

                        // raise location contents changed
                        if (raiseWorldEvents)
                        {
                            foreach (LocationSnapshot locState in state.Locations.Locations)
                            {
                                GameLocation location = locState.Location;

                                // buildings changed
                                if (locState.Buildings.IsChanged && events.BuildingListChanged.HasListeners)
                                    events.BuildingListChanged.Raise(new BuildingListChangedEventArgs(location, locState.Buildings.Added, locState.Buildings.Removed));

                                // debris changed
                                if (locState.Debris.IsChanged && events.DebrisListChanged.HasListeners)
                                    events.DebrisListChanged.Raise(new DebrisListChangedEventArgs(location, locState.Debris.Added, locState.Debris.Removed));

                                // large terrain features changed
                                if (locState.LargeTerrainFeatures.IsChanged && events.LargeTerrainFeatureListChanged.HasListeners)
                                    events.LargeTerrainFeatureListChanged.Raise(new LargeTerrainFeatureListChangedEventArgs(location, locState.LargeTerrainFeatures.Added, locState.LargeTerrainFeatures.Removed));

                                // NPCs changed
                                if (locState.Npcs.IsChanged && events.NpcListChanged.HasListeners)
                                    events.NpcListChanged.Raise(new NpcListChangedEventArgs(location, locState.Npcs.Added, locState.Npcs.Removed));

                                // objects changed
                                if (locState.Objects.IsChanged && events.ObjectListChanged.HasListeners)
                                    events.ObjectListChanged.Raise(new ObjectListChangedEventArgs(location, locState.Objects.Added, locState.Objects.Removed));

                                // chest items changed
                                if (events.ChestInventoryChanged.HasListeners)
                                {
                                    foreach ((Chest chest, SnapshotItemListDiff diff) in locState.ChestItems)
                                        events.ChestInventoryChanged.Raise(new ChestInventoryChangedEventArgs(chest, location, added: diff.Added, removed: diff.Removed, quantityChanged: diff.QuantityChanged));
                                }

                                // terrain features changed
                                if (locState.TerrainFeatures.IsChanged && events.TerrainFeatureListChanged.HasListeners)
                                    events.TerrainFeatureListChanged.Raise(new TerrainFeatureListChangedEventArgs(location, locState.TerrainFeatures.Added, locState.TerrainFeatures.Removed));

                                // furniture changed
                                if (locState.Furniture.IsChanged && events.FurnitureListChanged.HasListeners)
                                    events.FurnitureListChanged.Raise(new FurnitureListChangedEventArgs(location, locState.Furniture.Added, locState.Furniture.Removed));
                            }
                        }

                        // raise time changed
                        if (raiseWorldEvents && state.Time.IsChanged)
                        {
                            if (verbose)
                                this.Monitor.Log($"Context: time changed to {state.Time.New}.", Monitor.ContextLogLevel);

                            if (events.TimeChanged.HasListeners)
                                events.TimeChanged.Raise(new TimeChangedEventArgs(state.Time.Old, state.Time.New));
                        }

                        // raise player events
                        if (raiseWorldEvents)
                        {
                            PlayerSnapshot playerState = state.CurrentPlayer!; // not null at this point
                            Farmer player = playerState.Player;

                            // raise current location changed
                            if (playerState.Location.IsChanged)
                            {
                                if (verbose)
                                    this.Monitor.Log($"Context: set location to {playerState.Location.New}.", Monitor.ContextLogLevel);

                                if (events.Warped.HasListeners)
                                    events.Warped.Raise(new WarpedEventArgs(player, playerState.Location.Old!, playerState.Location.New!));
                            }

                            // raise player leveled up a skill
                            bool raiseLevelChanged = events.LevelChanged.HasListeners;
                            if (verbose || raiseLevelChanged)
                            {
                                foreach ((SkillType skill, var value) in playerState.Skills)
                                {
                                    if (!value.IsChanged)
                                        continue;

                                    if (verbose)
                                        this.Monitor.Log($"Events: player skill '{skill}' changed from {value.Old} to {value.New}.", Monitor.ContextLogLevel);

                                    if (raiseLevelChanged)
                                        events.LevelChanged.Raise(new LevelChangedEventArgs(player, skill, value.Old, value.New));
                                }
                            }

                            // raise player inventory changed
                            if (playerState.Inventory.IsChanged)
                            {
                                if (verbose)
                                    this.Monitor.Log("Events: player inventory changed.", Monitor.ContextLogLevel);

                                if (events.InventoryChanged.HasListeners)
                                {
                                    SnapshotItemListDiff inventory = playerState.Inventory;
                                    events.InventoryChanged.Raise(new InventoryChangedEventArgs(player, added: inventory.Added, removed: inventory.Removed, quantityChanged: inventory.QuantityChanged));
                                }
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

                        if (events.GameLaunched.HasListeners)
                            events.GameLaunched.Raise(new GameLaunchedEventArgs());
                    }

                    // preloaded
                    if (Context.IsSaveLoaded && Context.LoadStage != LoadStage.Loaded && Context.LoadStage != LoadStage.Ready && Game1.dayOfMonth != 0)
                        this.OnLoadStageChanged(LoadStage.Loaded);

                    // additional languages initialized
                    if (!this.AreCustomLanguagesInitialized && TitleMenu.ticksUntilLanguageLoad < 0)
                    {
                        this.AreCustomLanguagesInitialized = true;
                        this.ContentCore.OnAdditionalLanguagesInitialized();
                    }
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
            LanguageCode languageCode = this.ContentCore.Language;

            // update core translations
            this.Translator.SetLocale(locale, languageCode);

            // update mod translation helpers
            foreach (IModMetadata mod in this.ModRegistry.GetAll())
            {
                TranslationHelper translations = mod.Translations!; // not null at this point
                translations.SetLocale(locale, languageCode);

                foreach (ContentPack contentPack in mod.GetFakeContentPacks())
                    contentPack.TranslationImpl.SetLocale(locale, languageCode);
            }

            // raise event
            if (this.EventManager.LocaleChanged.HasListeners)
            {
                this.EventManager.LocaleChanged.Raise(
                    new LocaleChangedEventArgs(
                        oldLanguage: this.LastLanguage.Code,
                        oldLocale: this.LastLanguage.Locale,
                        newLanguage: languageCode,
                        newLocale: locale
                    )
                );
            }
            this.LastLanguage = (locale, languageCode);
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
            if (this.Monitor.IsVerbose || Monitor.ForceLogContext)
                this.Monitor.Log($"Context: load stage changed to {newStage}", Monitor.ContextLogLevel);

            // handle stages
            switch (newStage)
            {
                case LoadStage.ReturningToTitle:
                    this.Monitor.Log("Context: returning to title", Monitor.ContextLogLevel);
                    this.OnReturningToTitle();
                    break;

                case LoadStage.None:
                    this.JustReturnedToTitle = true;
                    this.UpdateWindowTitles();
                    break;

                case LoadStage.Loaded:
                    // override chat box
                    Game1.onScreenMenus.Remove(Game1.chatBox);
                    Game1.onScreenMenus.Add(Game1.chatBox = new SChatBox(this.LogManager.MonitorForGame));
                    break;
            }

            // raise events
            EventManager events = this.EventManager;
            if (events.LoadStageChanged.HasListeners)
                events.LoadStageChanged.Raise(new LoadStageChangedEventArgs(oldStage, newStage));
            if (newStage == LoadStage.None)
                events.ReturnedToTitle.RaiseEmpty();
        }

        /// <summary>Raised when the game starts a render step in the draw loop.</summary>
        /// <param name="step">The render step being started.</param>
        /// <param name="spriteBatch">The sprite batch being drawn (which might not always be open yet).</param>
        /// <param name="renderTarget">The render target being drawn.</param>
        private void OnRenderingStep(RenderSteps step, SpriteBatch spriteBatch, RenderTarget2D renderTarget)
        {
            EventManager events = this.EventManager;

            // raise 'Rendering' before first event
            if (this.LastRenderEventTick.Value != SCore.TicksElapsed)
            {
                this.RaiseRenderEvent(events.Rendering, spriteBatch, renderTarget);
                this.LastRenderEventTick.Value = SCore.TicksElapsed;
            }

            // raise other events
            switch (step)
            {
                case RenderSteps.World:
                    this.RaiseRenderEvent(events.RenderingWorld, spriteBatch, renderTarget);
                    break;

                case RenderSteps.Menu:
                    this.RaiseRenderEvent(events.RenderingActiveMenu, spriteBatch, renderTarget);
                    break;

                case RenderSteps.HUD:
                    this.RaiseRenderEvent(events.RenderingHud, spriteBatch, renderTarget);
                    break;
            }

            // raise generic rendering stage event
            if (events.RenderingStep.HasListeners)
                this.RaiseRenderEvent(events.RenderingStep, spriteBatch, renderTarget, RenderingStepEventArgs.Instance(step));
        }

        /// <summary>Raised when the game finishes a render step in the draw loop.</summary>
        /// <param name="step">The render step being started.</param>
        /// <param name="spriteBatch">The sprite batch being drawn (which might not always be open yet).</param>
        /// <param name="renderTarget">The render target being drawn.</param>
        private void OnRenderedStep(RenderSteps step, SpriteBatch spriteBatch, RenderTarget2D renderTarget)
        {
            var events = this.EventManager;

            switch (step)
            {
                case RenderSteps.World:
                    this.RaiseRenderEvent(events.RenderedWorld, spriteBatch, renderTarget);
                    break;

                case RenderSteps.Menu:
                    this.RaiseRenderEvent(events.RenderedActiveMenu, spriteBatch, renderTarget);
                    break;

                case RenderSteps.HUD:
                    this.RaiseRenderEvent(events.RenderedHud, spriteBatch, renderTarget);
                    break;
            }

            // raise generic rendering stage event
            if (events.RenderedStep.HasListeners)
                this.RaiseRenderEvent(events.RenderedStep, spriteBatch, renderTarget, RenderedStepEventArgs.Instance(step));
        }

        /// <summary>Raised after an instance finishes a draw loop.</summary>
        /// <param name="renderTarget">The render target being drawn to the screen.</param>
        private void OnRendered(RenderTarget2D renderTarget)
        {
            this.RaiseRenderEvent(this.EventManager.Rendered, Game1.spriteBatch, renderTarget);
        }

        /// <summary>Raise a rendering/rendered event, temporarily opening the given sprite batch if needed to let mods draw to it.</summary>
        /// <typeparam name="TEventArgs">The event args type to construct.</typeparam>
        /// <param name="event">The event to raise.</param>
        /// <param name="spriteBatch">The sprite batch being drawn to the screen.</param>
        /// <param name="renderTarget">The render target being drawn to the screen.</param>
        private void RaiseRenderEvent<TEventArgs>(ManagedEvent<TEventArgs> @event, SpriteBatch spriteBatch, RenderTarget2D renderTarget)
            where TEventArgs : EventArgs, new()
        {
            this.RaiseRenderEvent(@event, spriteBatch, renderTarget, Singleton<TEventArgs>.Instance);
        }

        /// <summary>Raise a rendering/rendered event, temporarily opening the given sprite batch if needed to let mods draw to it.</summary>
        /// <typeparam name="TEventArgs">The event args type to construct.</typeparam>
        /// <param name="event">The event to raise.</param>
        /// <param name="spriteBatch">The sprite batch being drawn to the screen.</param>
        /// <param name="renderTarget">The render target being drawn to the screen.</param>
        /// <param name="eventArgs">The event arguments to pass to the event.</param>
        private void RaiseRenderEvent<TEventArgs>(ManagedEvent<TEventArgs> @event, SpriteBatch spriteBatch, RenderTarget2D renderTarget, TEventArgs eventArgs)
            where TEventArgs : EventArgs
        {
            if (!@event.HasListeners)
                return;

            bool wasOpen = spriteBatch.IsOpen(this.Reflection);
            bool hadRenderTarget = Game1.graphics.GraphicsDevice.RenderTargetCount > 0;

            if (!hadRenderTarget && !Game1.IsOnMainThread())
                return; // can't set render target on background thread

            try
            {
                if (!wasOpen)
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

                if (!hadRenderTarget)
                {
                    renderTarget ??= Game1.game1.uiScreen?.IsDisposed != true
                        ? Game1.game1.uiScreen
                        : Game1.nonUIRenderTarget;

                    if (renderTarget != null)
                        Game1.SetRenderTarget(renderTarget);
                }

                @event.Raise(eventArgs);
            }
            finally
            {
                if (!wasOpen)
                    spriteBatch.End();

                if (!hadRenderTarget && renderTarget != null)
                    Game1.SetRenderTarget(null);
            }
        }

        /// <summary>A callback invoked before <see cref="Game1.newDayAfterFade"/> runs.</summary>
        protected void OnNewDayAfterFade()
        {
            this.EventManager.DayEnding.RaiseEmpty();

            this.Reflection.NewCacheInterval();
        }

        /// <summary>A callback invoked after an asset is fully loaded through a content manager.</summary>
        /// <param name="contentManager">The content manager through which the asset was loaded.</param>
        /// <param name="assetName">The asset name that was loaded.</param>
        private void OnAssetLoaded(IContentManager contentManager, IAssetName assetName)
        {
            if (this.EventManager.AssetReady.HasListeners)
                this.EventManager.AssetReady.Raise(new AssetReadyEventArgs(assetName, assetName.GetBaseAssetName()));
        }

        /// <summary>A callback invoked after assets have been invalidated from the content cache.</summary>
        /// <param name="assetNames">The invalidated asset names.</param>
        private void OnAssetsInvalidated(IList<IAssetName> assetNames)
        {
            if (this.EventManager.AssetsInvalidated.HasListeners)
                this.EventManager.AssetsInvalidated.Raise(new AssetsInvalidatedEventArgs(assetNames, assetNames.Select(p => p.GetBaseAssetName())));
        }

        /// <summary>Get the load/edit operations to apply to an asset by querying registered <see cref="IContentEvents.AssetRequested"/> event handlers.</summary>
        /// <param name="asset">The asset info being requested.</param>
        private AssetOperationGroup? RequestAssetOperations(IAssetInfo asset)
        {
            // get event
            var requestedEvent = this.EventManager.AssetRequested;
            if (!requestedEvent.HasListeners)
                return null;

            // raise event
            AssetRequestedEventArgs args = new(asset, this.GetOnBehalfOfContentPack);
            requestedEvent.Raise(
                invoke: (mod, invoke) =>
                {
                    args.SetMod(mod);
                    invoke(args);
                }
            );

            // collect operations
            return args.LoadOperations.Count != 0 || args.EditOperations.Count != 0
                ? new AssetOperationGroup(args.LoadOperations, args.EditOperations)
                : null;
        }

        /// <summary>Get the mod metadata for a content pack whose ID matches <paramref name="id"/>, if it's a valid content pack for the given <paramref name="mod"/>.</summary>
        /// <param name="mod">The mod requesting to act on the content pack's behalf.</param>
        /// <param name="id">The content pack ID.</param>
        /// <param name="verb">The verb phrase indicating what action will be performed, like 'load assets' or 'edit assets'.</param>
        /// <returns>Returns the content pack metadata if valid, else <c>null</c>.</returns>
        private IModMetadata? GetOnBehalfOfContentPack(IModMetadata mod, string? id, string verb)
        {
            if (id == null)
                return null;

            string errorPrefix = $"Can't {verb} on behalf of content pack ID '{id}'";

            // get target mod
            IModMetadata? onBehalfOf = this.ModRegistry.Get(id);
            if (onBehalfOf == null)
            {
                mod.LogAsModOnce($"{errorPrefix}: there's no content pack installed with that ID.", LogLevel.Warn);
                return null;
            }

            // make sure it's a content pack for the requesting mod
            if (!onBehalfOf.IsContentPack || !string.Equals(onBehalfOf.Manifest.ContentPackFor?.UniqueID, mod.Manifest.UniqueID, StringComparison.OrdinalIgnoreCase))
            {
                mod.LogAsModOnce($"{errorPrefix}: that isn't a content pack for this mod.", LogLevel.Warn);
                return null;
            }

            return onBehalfOf;
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
            this.Dispose(isError: false);
        }

        /// <summary>Raised when a mod network message is received.</summary>
        /// <param name="message">The message to deliver to applicable mods.</param>
        private void OnModMessageReceived(ModMessageModel message)
        {
            if (this.EventManager.ModMessageReceived.HasListeners)
            {
                // get mod IDs to notify
                HashSet<string> modIDs = new(message.ToModIDs ?? this.ModRegistry.GetAll().Select(p => p.Manifest.UniqueID), StringComparer.OrdinalIgnoreCase);
                if (message.FromPlayerID == Game1.player?.UniqueMultiplayerID)
                    modIDs.Remove(message.FromModID); // don't send a broadcast back to the sender

                // raise events
                ModMessageReceivedEventArgs? args = null;
                this.EventManager.ModMessageReceived.Raise(
                    invoke: (mod, invoke) =>
                    {
                        if (modIDs.Contains(mod.Manifest.UniqueID))
                        {
                            args ??= new(message, this.Toolkit.JsonHelper);
                            invoke(args);
                        }
                    }
                );
            }
        }

        /// <summary>Constructor a content manager to read game content files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        private LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            // Game1._temporaryContent initializing from SGame constructor
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract -- this is the method that initializes it
            if (this.ContentCore == null)
            {
                this.ContentCore = new ContentCoordinator(
                    serviceProvider: serviceProvider,
                    rootDirectory: rootDirectory,
                    currentCulture: Thread.CurrentThread.CurrentUICulture,
                    monitor: this.Monitor,
                    multiplayer: this.Multiplayer,
                    reflection: this.Reflection,
                    jsonHelper: this.Toolkit.JsonHelper,
                    onLoadingFirstAsset: this.InitializeBeforeFirstAssetLoaded,
                    onAssetLoaded: this.OnAssetLoaded,
                    onAssetsInvalidated: this.OnAssetsInvalidated,
                    getFileLookup: this.GetFileLookup,
                    requestAssetOperations: this.RequestAssetOperations
                );
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

        /// <summary>Set the titles for the game and console windows.</summary>
        private void UpdateWindowTitles()
        {
            string consoleTitle = $"SMAPI {Constants.ApiVersion} - running Stardew Valley {Constants.GameVersion}";
            string gameTitle = $"Stardew Valley {Constants.GameVersion} - running SMAPI {Constants.ApiVersion}";

            string suffix = "";
            if (this.ModRegistry.AreAllModsLoaded)
                suffix += $" with {this.ModRegistry.GetAll().Count()} mods";
            if (Context.IsMultiplayer)
                suffix += $" [{(Context.IsMainPlayer ? "main player" : "farmhand")}]";

            this.Game.Window.Title = gameTitle + suffix;
            this.LogManager.SetConsoleTitle(consoleTitle + suffix);
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
                        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey);
                        if (key == null)
                            return Array.Empty<string>();

                        return key
                            .GetSubKeyNames()
                            .Select(subkeyName =>
                            {
                                using RegistryKey? subkey = key.OpenSubKey(subkeyName);
                                string? displayName = (string?)subkey?.GetValue("DisplayName");
                                string? displayVersion = (string?)subkey?.GetValue("DisplayVersion");

                                if (displayName != null && displayVersion != null && displayName.EndsWith($" {displayVersion}"))
                                    displayName = displayName.Substring(0, displayName.Length - displayVersion.Length - 1);

                                return displayName;
                            })
                            .ToArray();
                    })
                    .Where(name => name != null && (name.Contains("MSI Afterburner") || name.Contains("RivaTuner")))
                    .Select(name => name!)
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
        private async Task CheckForUpdatesAsync(IModMetadata[] mods)
        {
            try
            {
                if (!this.Settings.CheckForUpdates)
                    return;

                // create client
                using WebApiClient client = new(this.Settings.WebApiBaseUrl, Constants.ApiVersion);
                this.Monitor.Log("Checking for updates...");

                // check SMAPI version
                {
                    ISemanticVersion? updateFound = null;
                    string? updateUrl = null;
                    try
                    {
                        // fetch update check
                        IDictionary<string, ModEntryModel> response = await client.GetModInfoAsync(
                            mods: new[] { new ModSearchEntryModel("Pathoschild.SMAPI", Constants.ApiVersion, new[] { $"GitHub:{this.Settings.GitHubProjectName}" }) },
                            apiVersion: Constants.ApiVersion,
                            gameVersion: Constants.GameVersion,
                            platform: Constants.Platform
                        );
                        ModEntryModel updateInfo = response.Single().Value;
                        updateFound = updateInfo.SuggestedUpdate?.Version;
                        updateUrl = updateInfo.SuggestedUpdate?.Url;

                        // log message
                        if (updateFound != null)
                            this.Monitor.Log($"You can update SMAPI to {updateFound}: {updateUrl}", LogLevel.Alert);
                        else
                            this.Monitor.Log("   SMAPI okay.");

                        // show errors
                        if (updateInfo.Errors.Any())
                        {
                            this.Monitor.Log("Couldn't check for a new version of SMAPI. This won't affect your game, but you may not be notified of new versions if this keeps happening.", LogLevel.Warn);
                            this.Monitor.Log($"Error: {string.Join("\n", updateInfo.Errors)}");
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
                        this.LogManager.WriteUpdateMarker(updateFound.ToString(), updateUrl ?? Constants.HomePageUrl);
                }

                // check mod versions
                if (mods.Any())
                {
                    try
                    {
                        HashSet<string> suppressUpdateChecks = this.Settings.SuppressUpdateChecks;

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
                        IDictionary<string, ModEntryModel> results = await client.GetModInfoAsync(searchMods.ToArray(), apiVersion: Constants.ApiVersion, gameVersion: Constants.GameVersion, platform: Constants.Platform);

                        // extract update alerts & errors
                        var updates = new List<Tuple<IModMetadata, ISemanticVersion, string>>();
                        var errors = new StringBuilder();
                        foreach (IModMetadata mod in mods.OrderBy(p => p.DisplayName))
                        {
                            // link to update-check data
                            if (!mod.HasID() || !results.TryGetValue(mod.Manifest.UniqueID, out ModEntryModel? result))
                                continue;
                            mod.SetUpdateData(result);

                            // handle errors
                            if (result.Errors.Any())
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
                            foreach ((IModMetadata mod, ISemanticVersion newVersion, string newUrl) in updates)
                                this.Monitor.Log($"   {mod.DisplayName} {newVersion}: {newUrl} (you have {mod.Manifest.Version})", LogLevel.Alert);
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
            }
            catch (Exception ex)
            {
                this.Monitor.Log("Couldn't check for updates. This won't affect your game, but you won't be notified of SMAPI or mod updates if this keeps happening.", LogLevel.Warn);
                this.Monitor.Log(ex is WebException && ex.InnerException == null
                    ? ex.Message
                    : ex.ToString()
                );
            }
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
            using (AssemblyLoader modAssemblyLoader = new(Constants.Platform, this.Monitor, this.Settings.ParanoidWarnings, this.Settings.RewriteMods, this.Settings.LogTechnicalDetailsForBrokenMods))
            {
                // init
                HashSet<string> suppressUpdateChecks = this.Settings.SuppressUpdateChecks;
                IInterfaceProxyFactory proxyFactory = new InterfaceProxyFactory();

                // load mods
                foreach (IModMetadata mod in mods)
                {
                    if (!this.TryLoadMod(mod, mods, modAssemblyLoader, proxyFactory, jsonHelper, contentCore, modDatabase, suppressUpdateChecks, out ModFailReason? failReason, out string? errorPhrase, out string? errorDetails))
                    {
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
            this.LogManager.LogModInfo(loaded, loadedContentPacks, loadedMods, skippedMods.ToArray(), this.Settings.ParanoidWarnings, this.Settings.LogTechnicalDetailsForBrokenMods, this.Settings.FixHarmony);

            // initialize translations
            this.ReloadTranslations(loaded);

            // initialize loaded non-content-pack mods
            this.Monitor.Log("Launching mods...", LogLevel.Debug);
            foreach (IModMetadata metadata in loadedMods)
            {
                IMod mod =
                    metadata.Mod
                    ?? throw new InvalidOperationException($"The '{metadata.DisplayName}' mod is not initialized correctly."); // should never happen, but avoids nullability warnings

                // initialize mod
                Context.HeuristicModsRunningCode.Push(metadata);
                {
                    // call entry method
                    try
                    {
                        mod.Entry(mod.Helper);
                    }
                    catch (Exception ex)
                    {
                        metadata.LogAsMod($"Mod crashed on entry and might not work correctly. Technical details:\n{ex.GetLogSummary()}", LogLevel.Error);
                    }

                    // get mod API
                    try
                    {
                        object? api = mod.GetApi();
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

                    // validate mod doesn't implement both GetApi() and GetApi(mod)
                    if (metadata.Api != null && mod.GetType().GetMethod(nameof(Mod.GetApi), new[] { typeof(IModInfo) })!.DeclaringType != typeof(Mod))
                        metadata.LogAsMod($"Mod implements both {nameof(Mod.GetApi)}() and {nameof(Mod.GetApi)}({nameof(IModInfo)}), which isn't allowed. The latter will be ignored.", LogLevel.Error);
                }
                Context.HeuristicModsRunningCode.TryPop(out _);
            }

            // unlock mod integrations
            this.ModRegistry.AreAllModsInitialized = true;

            this.Monitor.Log("Mods loaded and ready!", LogLevel.Debug);
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
        /// <param name="errorDetails">More detailed info about the error intended for developers (if any).</param>
        /// <returns>Returns whether the mod was successfully loaded.</returns>
        private bool TryLoadMod(IModMetadata mod, IModMetadata[] mods, AssemblyLoader assemblyLoader, IInterfaceProxyFactory proxyFactory, JsonHelper jsonHelper, ContentCoordinator contentCore, ModDatabase modDatabase, HashSet<string> suppressUpdateChecks, [NotNullWhen(false)] out ModFailReason? failReason, out string? errorReasonPhrase, out string? errorDetails)
        {
            errorDetails = null;

            // log entry
            {
                string relativePath = mod.GetRelativePathWithRoot();
                if (mod.IsContentPack)
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}, ID: {mod.Manifest.UniqueID}) [content pack]...");
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract -- mod may be invalid at this point
                else if (mod.Manifest?.EntryDll != null)
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}{Path.DirectorySeparatorChar}{mod.Manifest.EntryDll}, ID: {mod.Manifest.UniqueID})..."); // don't use Path.Combine here, since EntryDLL might not be valid
                else
                    this.Monitor.Log($"   {mod.DisplayName} (from {relativePath}, ID: {mod.Manifest?.UniqueID ?? "<unknown>"})...");
            }

            // add warning for missing update key
            if (mod.HasID() && !suppressUpdateChecks.Contains(mod.Manifest!.UniqueID) && !mod.HasValidUpdateKeys())
                mod.SetWarning(ModWarning.NoUpdateKeys);

            // validate status
            if (mod.Status == ModMetadataStatus.Failed)
            {
                this.Monitor.Log($"      Failed: {mod.ErrorDetails ?? mod.Error}");
                failReason = mod.FailReason ?? ModFailReason.LoadFailed;
                errorReasonPhrase = mod.Error;
                return false;
            }
            IManifest manifest = mod.Manifest!;

            // validate dependencies
            // Although dependencies are validated before mods are loaded, a dependency may have failed to load.
            foreach (IManifestDependency dependency in manifest.Dependencies.Where(p => p.IsRequired))
            {
                // not missing
                if (this.ModRegistry.Get(dependency.UniqueID) != null)
                    continue;

                // ignored in compatibility list (e.g. fully replaced by the game code)
                if (modDatabase.Get(dependency.UniqueID)?.IgnoreDependencies is true)
                    continue;

                // mark failed
                string dependencyName = mods
                    .FirstOrDefault(otherMod => otherMod.HasID(dependency.UniqueID))
                    ?.DisplayName ?? dependency.UniqueID;
                errorReasonPhrase = $"it needs the '{dependencyName}' mod, which couldn't be loaded.";
                failReason = ModFailReason.MissingDependencies;
                return false;
            }

            // load as content pack
            if (mod.IsContentPack)
            {
                IMonitor monitor = this.LogManager.GetMonitor(manifest.UniqueID, mod.DisplayName);
                IFileLookup fileLookup = this.GetFileLookup(mod.DirectoryPath);
                GameContentHelper gameContentHelper = new(this.ContentCore, mod, mod.DisplayName, monitor, this.Reflection);
                IModContentHelper modContentHelper = new ModContentHelper(this.ContentCore, mod.DirectoryPath, mod, mod.DisplayName, gameContentHelper.GetUnderlyingContentManager(), this.Reflection);
                TranslationHelper translationHelper = new(mod, contentCore.GetLocale(), contentCore.Language);
                IContentPack contentPack = new ContentPack(mod.DirectoryPath, manifest, modContentHelper, translationHelper, jsonHelper, fileLookup);
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
                FileInfo assemblyFile = this.GetFileLookup(mod.DirectoryPath).GetFile(manifest.EntryDll!);

                // load mod
                Assembly modAssembly;
                try
                {
                    modAssembly = assemblyLoader.Load(mod, assemblyFile, assumeCompatible: mod.DataRecord?.Status == ModStatus.AssumeCompatible);
                    this.ModRegistry.TrackAssemblies(mod, modAssembly);
                }
                catch (IncompatibleInstructionException) // details already in trace logs
                {
                    string[] updateUrls = new[] { modDatabase.GetModPageUrlFor(manifest.UniqueID), "https://smapi.io/mods" }.Where(p => p != null).ToArray()!;
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
                    if (ex is BadImageFormatException && !EnvironmentUtility.Is64BitAssembly(assemblyFile.FullName))
                        errorReasonPhrase = "it needs to be updated for 64-bit mode.";

                    errorDetails = $"Error: {ex.GetLogSummary()}";
                    failReason = ModFailReason.LoadFailed;
                    return false;
                }

                // initialize mod
                try
                {
                    // get mod instance
                    if (!this.TryLoadModEntry(mod, modAssembly, out Mod? modEntry, out errorReasonPhrase))
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
                            .Where(p => p.IsContentPack && mod.HasID(p.Manifest.ContentPackFor!.UniqueID))
                            .Select(p => p.ContentPack!)
                            .ToArray();
                    }

                    // init mod helpers
                    IMonitor monitor = this.LogManager.GetMonitor(manifest.UniqueID, mod.DisplayName);
                    TranslationHelper translationHelper = new(mod, contentCore.GetLocale(), contentCore.Language);
                    IModHelper modHelper;
                    {
                        IModEvents events = new ModEvents(mod, this.EventManager);
                        ICommandHelper commandHelper = new CommandHelper(mod, this.CommandManager);
                        GameContentHelper gameContentHelper = new(contentCore, mod, mod.DisplayName, monitor, this.Reflection);
                        IModContentHelper modContentHelper = new ModContentHelper(contentCore, mod.DirectoryPath, mod, mod.DisplayName, gameContentHelper.GetUnderlyingContentManager(), this.Reflection);
                        IContentPackHelper contentPackHelper = new ContentPackHelper(
                            mod: mod,
                            contentPacks: new Lazy<IContentPack[]>(GetContentPacks),
                            createContentPack: (dirPath, fakeManifest) => this.CreateFakeContentPack(dirPath, fakeManifest, contentCore, mod)
                        );
                        IDataHelper dataHelper = new DataHelper(mod, mod.DirectoryPath, jsonHelper);
                        IReflectionHelper reflectionHelper = new ReflectionHelper(mod, mod.DisplayName, this.Reflection);
                        IModRegistry modRegistryHelper = new ModRegistryHelper(mod, this.ModRegistry, proxyFactory, monitor);
                        IMultiplayerHelper multiplayerHelper = new MultiplayerHelper(mod, this.Multiplayer);

                        modHelper = new ModHelper(mod, mod.DirectoryPath, () => this.GetCurrentGameInstance().Input, events, gameContentHelper, modContentHelper, contentPackHelper, commandHelper, dataHelper, modRegistryHelper, reflectionHelper, multiplayerHelper, translationHelper);
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

        /// <summary>Create a fake content pack instance for a parent mod.</summary>
        /// <param name="packDirPath">The absolute path to the fake content pack's directory.</param>
        /// <param name="packManifest">The fake content pack's manifest.</param>
        /// <param name="contentCore">The content manager to use for mod content.</param>
        /// <param name="parentMod">The mod for which the content pack is being created.</param>
        private IContentPack CreateFakeContentPack(string packDirPath, IManifest packManifest, ContentCoordinator contentCore, IModMetadata parentMod)
        {
            // create fake mod info
            string relativePath = Path.GetRelativePath(Constants.ModsPath, packDirPath);
            IModMetadata fakeMod = new ModMetadata(
                displayName: packManifest.Name,
                directoryPath: packDirPath,
                rootPath: Constants.ModsPath,
                manifest: packManifest,
                dataRecord: null,
                isIgnored: false
            );

            // create mod helpers
            IMonitor packMonitor = this.LogManager.GetMonitor(packManifest.UniqueID, packManifest.Name);
            GameContentHelper gameContentHelper = new(contentCore, fakeMod, packManifest.Name, packMonitor, this.Reflection);
            IModContentHelper packContentHelper = new ModContentHelper(contentCore, packDirPath, fakeMod, packManifest.Name, gameContentHelper.GetUnderlyingContentManager(), this.Reflection);
            TranslationHelper packTranslationHelper = new(fakeMod, contentCore.GetLocale(), contentCore.Language);

            // add content pack
            IFileLookup fileLookup = this.GetFileLookup(packDirPath);
            ContentPack contentPack = new(packDirPath, packManifest, packContentHelper, packTranslationHelper, this.Toolkit.JsonHelper, fileLookup);
            this.ReloadTranslationsForTemporaryContentPack(parentMod, contentPack);
            parentMod.FakeContentPacks.Add(new WeakReference<ContentPack>(contentPack));

            // log change
            string pathLabel = packDirPath.Contains("..") ? packDirPath : relativePath;
            this.Monitor.Log($"{parentMod.DisplayName} created dynamic content pack '{packManifest.Name}' (unique ID: {packManifest.UniqueID}{(packManifest.Name.Contains(pathLabel) ? "" : $", path: {pathLabel}")}).");

            return contentPack;
        }

        /// <summary>Load a mod's entry class.</summary>
        /// <param name="metadata">The mod metadata whose entry class is being loaded.</param>
        /// <param name="modAssembly">The mod assembly.</param>
        /// <param name="mod">The loaded instance.</param>
        /// <param name="error">The error indicating why loading failed (if applicable).</param>
        /// <returns>Returns whether the mod entry class was successfully loaded.</returns>
        private bool TryLoadModEntry(IModMetadata metadata, Assembly modAssembly, [NotNullWhen(true)] out Mod? mod, [NotNullWhen(false)] out string? error)
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
            Context.HeuristicModsRunningCode.Push(metadata);
            try
            {
                mod = (Mod?)modAssembly.CreateInstance(modEntries[0].ToString());
            }
            finally
            {
                Context.HeuristicModsRunningCode.TryPop(out _);
            }

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

                    metadata.Translations!.SetTranslations(translations);
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
            DirectoryInfo translationsDir = new(folderPath);
            if (translationsDir.Exists)
            {
                foreach (FileInfo file in translationsDir.EnumerateFiles("*.json"))
                {
                    string locale = Path.GetFileNameWithoutExtension(file.Name.ToLower().Trim());
                    try
                    {
                        if (!jsonHelper.ReadJsonFileIfExists(file.FullName, out IDictionary<string, string>? data))
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
                HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
                HashSet<string> duplicateKeys = new(StringComparer.OrdinalIgnoreCase);
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

        /// <summary>Get a file lookup for the given directory.</summary>
        /// <param name="rootDirectory">The root path to scan.</param>
        private IFileLookup GetFileLookup(string rootDirectory)
        {
            return this.Settings.UseCaseInsensitivePaths
                ? CaseInsensitiveFileLookup.GetCachedFor(rootDirectory)
                : MinimalFileLookup.GetCachedFor(rootDirectory);
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
                FileInfo defaultFile = new(Path.Combine(Constants.LogDir, $"{Constants.LogFilename}.{Constants.LogExtension}"));
                if (!defaultFile.Exists)
                    return defaultFile.FullName;
            }

            // get first disambiguated path
            for (int i = 2; i < int.MaxValue; i++)
            {
                FileInfo file = new(Path.Combine(Constants.LogDir, $"{Constants.LogFilename}.player-{i}.{Constants.LogExtension}"));
                if (!file.Exists)
                    return file.FullName;
            }

            // should never happen
            throw new InvalidOperationException("Could not find an available log path.");
        }

        /// <summary>Delete normal (non-crash) log files created by SMAPI.</summary>
        private void PurgeNormalLogs()
        {
            DirectoryInfo logsDir = new(Constants.LogDir);
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
            this.LogManager.WriteCrashLog();

            this.ExitState = ExitState.Crash;
            this.Game.Exit();
        }

        /// <summary>Get the screen ID that should be logged to distinguish between players in split-screen mode, if any.</summary>
        private int? GetScreenIdForLog()
        {
            if (Context.ScreenId != 0 || (Context.IsWorldReady && Context.IsSplitScreen))
                return Context.ScreenId;

            return null;
        }


        /*********
        ** Private types
        *********/
        /// <summary>A queued console command to run during the update loop.</summary>
        /// <param name="Command">The command which can handle the input.</param>
        /// <param name="Name">The parsed command name.</param>
        /// <param name="Args">The parsed command arguments.</param>
        private readonly record struct QueuedCommand(Command Command, string Name, string[] Args);
    }
}
