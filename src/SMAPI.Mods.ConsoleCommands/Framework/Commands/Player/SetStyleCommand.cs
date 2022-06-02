using System.Diagnostics.CodeAnalysis;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which edits a player style.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetStyleCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetStyleCommand()
        : base("player_changestyle", "Sets the style of a player feature.\n\nUsage: player_changestyle <target> <value>.\n- target: what to change (one of 'hair', 'shirt', 'skin', 'acc', 'shoe', 'swim', or 'gender').\n- value: the integer style ID.") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // parse arguments
        if (!args.TryGet(0, "target", out string? target, oneOf: new[] { "hair", "shirt", "acc", "skin", "shoe", "swim", "gender" }))
            return;
        if (!args.TryGetInt(1, "style ID", out int styleID))
            return;

        // handle
        switch (target)
        {
            case "hair":
                Game1.player.changeHairStyle(styleID);
                monitor.Log("OK, your hair style is updated.", LogLevel.Info);
                break;

            case "shirt":
                Game1.player.changeShirt(styleID);
                monitor.Log("OK, your shirt style is updated.", LogLevel.Info);
                break;

            case "acc":
                Game1.player.changeAccessory(styleID);
                monitor.Log("OK, your accessory style is updated.", LogLevel.Info);
                break;

            case "skin":
                Game1.player.changeSkinColor(styleID);
                monitor.Log("OK, your skin color is updated.", LogLevel.Info);
                break;

            case "shoe":
                Game1.player.changeShoeColor(styleID);
                monitor.Log("OK, your shoe style is updated.", LogLevel.Info);
                break;

            case "swim":
                switch (styleID)
                {
                    case 0:
                        Game1.player.changeOutOfSwimSuit();
                        monitor.Log("OK, you're no longer in your swimming suit.", LogLevel.Info);
                        break;
                    case 1:
                        Game1.player.changeIntoSwimsuit();
                        monitor.Log("OK, you're now in your swimming suit.", LogLevel.Info);
                        break;
                    default:
                        this.LogUsageError(monitor, "The swim value should be 0 (no swimming suit) or 1 (swimming suit).");
                        break;
                }
                break;

            case "gender":
                switch (styleID)
                {
                    case 0:
                        Game1.player.changeGender(true);
                        monitor.Log("OK, you're now male.", LogLevel.Info);
                        break;
                    case 1:
                        Game1.player.changeGender(false);
                        monitor.Log("OK, you're now female.", LogLevel.Info);
                        break;
                    default:
                        this.LogUsageError(monitor, "The gender value should be 0 (male) or 1 (female).");
                        break;
                }
                break;
        }
    }
}