using System;
using System.Reflection;
using StardewModdingAPI.Events;
using StardewModdingAPI.Internal.Patching;
using StardewModdingAPI.Mods.ErrorHandler.ModPatches;
using StardewModdingAPI.Mods.ErrorHandler.Patches;
using StardewValley;

namespace StardewModdingAPI.Mods.ErrorHandler;

/// <summary>The main entry point for the mod.</summary>
public class ModEntry : Mod
{
    /*********
    ** Private methods
    *********/
    /// <summary>Whether custom content was removed from the save data to avoid a crash.</summary>
    private bool IsSaveContentRemoved;


    /*********
    ** Public methods
    *********/
    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides simplified APIs for writing mods.</param>
    public override void Entry(IModHelper helper)
    {
        // get SMAPI core types
        IMonitor monitorForGame = this.GetMonitorForGame();

        // apply patches
        HarmonyPatcher.Apply(this.ModManifest.UniqueID, this.Monitor,
            // game patches
            new DialoguePatcher(monitorForGame, this.Helper.Reflection),
            new EventPatcher(monitorForGame),
            new GameLocationPatcher(monitorForGame),
            new IClickableMenuPatcher(),
            new NpcPatcher(monitorForGame),
            new ObjectPatcher(),
            new SaveGamePatcher(this.Monitor, this.OnSaveContentRemoved),
            new SpriteBatchPatcher(),
            new UtilityPatcher(),

            // mod patches
            new PyTkPatcher(helper.ModRegistry)
        );

        // hook events
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Raised after custom content is removed from the save data to avoid a crash.</summary>
    internal void OnSaveContentRemoved()
    {
        this.IsSaveContentRemoved = true;
    }

    /// <summary>The method invoked when a save is loaded.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // show in-game warning for removed save content
        if (this.IsSaveContentRemoved)
        {
            this.IsSaveContentRemoved = false;
            Game1.addHUDMessage(new HUDMessage(this.Helper.Translation.Get("warn.invalid-content-removed"), HUDMessage.error_type));
        }
    }

    /// <summary>Get the monitor with which to log game errors.</summary>
    private IMonitor GetMonitorForGame()
    {
        // get SMAPI core
        Type coreType = Type.GetType("StardewModdingAPI.Framework.SCore, StardewModdingAPI", throwOnError: false)
                        ?? throw new InvalidOperationException("Can't access SMAPI's core type. This mod may not work correctly.");
        object core = coreType.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null)
                      ?? throw new InvalidOperationException("Can't access SMAPI's core instance. This mod may not work correctly.");

        // get monitor
        MethodInfo getMonitorForGame = coreType.GetMethod("GetMonitorForGame")
                                       ?? throw new InvalidOperationException("Can't access the SMAPI's 'GetMonitorForGame' method. This mod may not work correctly.");

        return (IMonitor?)getMonitorForGame.Invoke(core, Array.Empty<object>()) ?? this.Monitor;
    }
}
