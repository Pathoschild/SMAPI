using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Internal.ConsoleWriting;

/// <summary>Writes color-coded text to the console.</summary>
internal class ColorfulConsoleWriter : IConsoleWriter
{
    /*********
    ** Fields
    *********/
    /// <summary>The target platform.</summary>
    private readonly Platform Platform;

    /// <summary>The console text color for each log level.</summary>
    private IDictionary<ConsoleLogLevel, ConsoleColor>? Colors;

    /// <summary>Whether the current console supports color formatting.</summary>
    [MemberNotNullWhen(true, nameof(ColorfulConsoleWriter.Colors))]
    private bool SupportsColor { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="platform">The target platform.</param>
    public ColorfulConsoleWriter(Platform platform)
        : this(platform, MonitorColorScheme.AutoDetect, ColorfulConsoleWriter.GetDefaultColorSchemeConfig(MonitorColorScheme.AutoDetect)) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="platform">The target platform.</param>
    /// <param name="colorSchemeId">The color scheme ID in <paramref name="colorConfig"/> to use, or <see cref="MonitorColorScheme.AutoDetect"/> to select one automatically.</param>
    /// <param name="colorConfig">The colors to use for text written to the SMAPI console.</param>
    public ColorfulConsoleWriter(Platform platform, MonitorColorScheme colorSchemeId, Dictionary<MonitorColorScheme, Dictionary<ConsoleLogLevel, ConsoleColor>> colorConfig)
    {
        this.Platform = platform;

        this.SetColors(colorSchemeId, colorConfig);
    }

    /// <summary>Set the color scheme to apply.</summary>
    /// <param name="colorSchemeId">The color scheme ID in <paramref name="colorSchemes"/> to use, or <see cref="MonitorColorScheme.AutoDetect"/> to select one automatically.</param>
    /// <param name="colorSchemes">The colors to use for text written to the SMAPI console.</param>
    public void SetColors(MonitorColorScheme colorSchemeId, Dictionary<MonitorColorScheme, Dictionary<ConsoleLogLevel, ConsoleColor>> colorSchemes)
    {
        if (colorSchemeId == MonitorColorScheme.None)
        {
            this.SupportsColor = false;
            this.Colors = null;
        }
        else
        {
            this.SupportsColor = this.TestColorSupport();
            this.Colors = ColorfulConsoleWriter.GetConsoleColorScheme(this.Platform, colorSchemeId, colorSchemes);
        }
    }

    /// <summary>Write a message line to the log.</summary>
    /// <param name="message">The message to log.</param>
    /// <param name="level">The log level.</param>
    public void WriteLine(string message, ConsoleLogLevel level)
    {
        if (this.SupportsColor)
        {
            if (level == ConsoleLogLevel.Critical)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(message);
                Console.ResetColor(); // reset color before line break, so we don't apply background color to the next line
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = this.Colors[level];
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }
        else
            Console.WriteLine(message);
    }

    /// <summary>Get the default color scheme config for cases where it's not configurable (e.g. the installer).</summary>
    /// <param name="useScheme">The default color scheme ID to use, or <see cref="MonitorColorScheme.AutoDetect"/> to select one automatically.</param>
    /// <remarks>The colors here should be kept in sync with the SMAPI config file.</remarks>
    public static Dictionary<MonitorColorScheme, Dictionary<ConsoleLogLevel, ConsoleColor>> GetDefaultColorSchemeConfig(MonitorColorScheme useScheme)
    {
        return new Dictionary<MonitorColorScheme, Dictionary<ConsoleLogLevel, ConsoleColor>>
        {
            [MonitorColorScheme.DarkBackground] = new()
            {
                [ConsoleLogLevel.Trace] = ConsoleColor.DarkGray,
                [ConsoleLogLevel.Debug] = ConsoleColor.DarkGray,
                [ConsoleLogLevel.Info] = ConsoleColor.White,
                [ConsoleLogLevel.Warn] = ConsoleColor.Yellow,
                [ConsoleLogLevel.Error] = ConsoleColor.Red,
                [ConsoleLogLevel.Alert] = ConsoleColor.Magenta,
                [ConsoleLogLevel.Success] = ConsoleColor.DarkGreen
            },
            [MonitorColorScheme.LightBackground] = new()
            {
                [ConsoleLogLevel.Trace] = ConsoleColor.DarkGray,
                [ConsoleLogLevel.Debug] = ConsoleColor.DarkGray,
                [ConsoleLogLevel.Info] = ConsoleColor.Black,
                [ConsoleLogLevel.Warn] = ConsoleColor.DarkYellow,
                [ConsoleLogLevel.Error] = ConsoleColor.Red,
                [ConsoleLogLevel.Alert] = ConsoleColor.DarkMagenta,
                [ConsoleLogLevel.Success] = ConsoleColor.DarkGreen
            }
        };
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Test whether the current console supports color formatting.</summary>
    private bool TestColorSupport()
    {
        try
        {
            Console.ForegroundColor = Console.ForegroundColor;
            return true;
        }
        catch (Exception)
        {
            return false; // Mono bug
        }
    }

    /// <summary>Get the color scheme to use for the current console.</summary>
    /// <param name="platform">The target platform.</param>
    /// <param name="colorSchemeId">The color scheme ID in <paramref name="colorConfig"/> to use, or <see cref="MonitorColorScheme.AutoDetect"/> to select one automatically.</param>
    /// <param name="colorConfig">The colors to use for text written to the SMAPI console.</param>
    private static IDictionary<ConsoleLogLevel, ConsoleColor> GetConsoleColorScheme(Platform platform, MonitorColorScheme colorSchemeId, Dictionary<MonitorColorScheme, Dictionary<ConsoleLogLevel, ConsoleColor>> colorConfig)
    {
        // get color scheme ID
        if (colorSchemeId == MonitorColorScheme.AutoDetect)
        {
            // macOS doesn't provide console background color info, but it's usually white.
            // Android has no TTY; Console.BackgroundColor throws PlatformNotSupportedException.
            if (platform == Platform.Mac)
                colorSchemeId = MonitorColorScheme.LightBackground;
            else if (platform == Platform.Android)
                colorSchemeId = MonitorColorScheme.DarkBackground;
            else
            {
                try
                {
                    colorSchemeId = ColorfulConsoleWriter.IsDark(Console.BackgroundColor)
                        ? MonitorColorScheme.DarkBackground
                        : MonitorColorScheme.LightBackground;
                }
                catch (PlatformNotSupportedException)
                {
                    colorSchemeId = MonitorColorScheme.DarkBackground;
                }
            }
        }

        // get colors for scheme
        return colorConfig.TryGetValue(colorSchemeId, out Dictionary<ConsoleLogLevel, ConsoleColor>? scheme)
            ? scheme
            : throw new NotSupportedException($"Unknown color scheme '{colorSchemeId}'.");
    }

    /// <summary>Get whether a console color should be considered dark, which is subjectively defined as 'white looks better than black on this text'.</summary>
    /// <param name="color">The color to check.</param>
    private static bool IsDark(ConsoleColor color)
    {
        switch (color)
        {
            case ConsoleColor.Black:
            case ConsoleColor.Blue:
            case ConsoleColor.DarkBlue:
            case ConsoleColor.DarkMagenta: // PowerShell
            case ConsoleColor.DarkRed:
            case ConsoleColor.Red:
                return true;

            default:
                return false;
        }
    }
}
