using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player;

/// <summary>A command which edits the color of a player feature.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class SetColorCommand : ConsoleCommand
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public SetColorCommand()
        : base("player_changecolor", "Sets the color of a player feature.\n\nUsage: player_changecolor <target> <color>\n- target: what to change (one of 'hair', 'eyes', or 'pants').\n- color: a color value in RGB format, like (255,255,255).") { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // parse arguments
        if (!args.TryGet(0, "target", out string? target, oneOf: new[] { "hair", "eyes", "pants" }))
            return;
        if (!args.TryGet(1, "color", out string? rawColor))
            return;

        // parse color
        if (!this.TryParseColor(rawColor, out Color color))
        {
            this.LogUsageError(monitor, "Argument 1 (color) must be an RBG value like '255,150,0'.");
            return;
        }

        // handle
        switch (target)
        {
            case "hair":
                Game1.player.hairstyleColor.Value = color;
                monitor.Log("OK, your hair color is updated.", LogLevel.Info);
                break;

            case "eyes":
                Game1.player.changeEyeColor(color);
                monitor.Log("OK, your eye color is updated.", LogLevel.Info);
                break;

            case "pants":
                Game1.player.pantsColor.Value = color;
                monitor.Log("OK, your pants color is updated.", LogLevel.Info);
                break;
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Try to parse a color from a string.</summary>
    /// <param name="input">The input string.</param>
    /// <param name="color">The color to set.</param>
    private bool TryParseColor(string input, out Color color)
    {
        string[] colorHexes = input.Split(new[] { ',' }, 3);
        if (int.TryParse(colorHexes[0], out int r) && int.TryParse(colorHexes[1], out int g) && int.TryParse(colorHexes[2], out int b))
        {
            color = new Color(r, g, b);
            return true;
        }

        color = Color.Transparent;
        return false;
    }
}