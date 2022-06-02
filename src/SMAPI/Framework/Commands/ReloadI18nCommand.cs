using System;

namespace StardewModdingAPI.Framework.Commands;

/// <summary>The 'reload_i18n' SMAPI console command.</summary>
internal class ReloadI18nCommand : IInternalCommand
{
    /*********
    ** Fields
    *********/
    /// <summary>Reload translations for all mods.</summary>
    private readonly Action ReloadTranslations;


    /*********
    ** Accessors
    *********/
    /// <summary>The command name, which the user must type to trigger it.</summary>
    public string Name { get; } = "reload_i18n";

    /// <summary>The human-readable documentation shown when the player runs the built-in 'help' command.</summary>
    public string Description { get; } = "Reloads translation files for all mods.\n\nUsage: reload_i18n";


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="reloadTranslations">Reload translations for all mods..</param>
    public ReloadI18nCommand(Action reloadTranslations)
    {
        this.ReloadTranslations = reloadTranslations;
    }

    /// <summary>Handle the console command when it's entered by the user.</summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="monitor">Writes messages to the console.</param>
    public void HandleCommand(string[] args, IMonitor monitor)
    {
        this.ReloadTranslations();
        monitor.Log("Reloaded translation files for all mods. This only affects new translations the mods fetch; if they cached some text, it may not be updated.", LogLevel.Info);
    }
}
