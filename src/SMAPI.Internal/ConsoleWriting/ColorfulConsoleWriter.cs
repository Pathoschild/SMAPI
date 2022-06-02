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
    /// <summary>The console text color for each log level.</summary>
    private readonly IDictionary<ConsoleLogLevel, ConsoleColor>? Colors;

    /// <summary>Whether the current console supports color formatting.</summary>
    [MemberNotNullWhen(true, nameof(ColorfulConsoleWriter.Colors))]
    private bool SupportsColor { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="platform">The target platform.</param>
    public ColorfulConsoleWriter(Platform platform)
        : this(platform, ColorfulConsoleWriter.GetDefaultColorSchemeConfig(MonitorColorScheme.AutoDetect)) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="platform">The target platform.</param>
    /// <param name="colorConfig">The colors to use for text written to the SMAPI console.</param>
    public ColorfulConsoleWriter(Platform platform, ColorSchemeConfig colorConfig)
    {
        if (colorConfig.UseScheme == MonitorColorScheme.None)
        {
            this.SupportsColor = false;
            this.Colors = null;
        }
        else
        {
            this.SupportsColor = this.TestColorSupport();
            this.Colors = this.GetConsoleColorScheme(platform, colorConfig);
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
                Console.WriteLine(message);
                Console.ResetColor();
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
    public static ColorSchemeConfig GetDefaultColorSchemeConfig(MonitorColorScheme useScheme)
    {
        return new ColorSchemeConfig(
            useScheme: useScheme,
            schemes: new Dictionary<MonitorColorScheme, IDictionary<ConsoleLogLevel, ConsoleColor>>
            {
                [MonitorColorScheme.DarkBackground] = new Dictionary<ConsoleLogLevel, ConsoleColor>
                {
                    [ConsoleLogLevel.Trace] = ConsoleColor.DarkGray,
                    [ConsoleLogLevel.Debug] = ConsoleColor.DarkGray,
                    [ConsoleLogLevel.Info] = ConsoleColor.White,
                    [ConsoleLogLevel.Warn] = ConsoleColor.Yellow,
                    [ConsoleLogLevel.Error] = ConsoleColor.Red,
                    [ConsoleLogLevel.Alert] = ConsoleColor.Magenta,
                    [ConsoleLogLevel.Success] = ConsoleColor.DarkGreen
                },
                [MonitorColorScheme.LightBackground] = new Dictionary<ConsoleLogLevel, ConsoleColor>
                {
                    [ConsoleLogLevel.Trace] = ConsoleColor.DarkGray,
                    [ConsoleLogLevel.Debug] = ConsoleColor.DarkGray,
                    [ConsoleLogLevel.Info] = ConsoleColor.Black,
                    [ConsoleLogLevel.Warn] = ConsoleColor.DarkYellow,
                    [ConsoleLogLevel.Error] = ConsoleColor.Red,
                    [ConsoleLogLevel.Alert] = ConsoleColor.DarkMagenta,
                    [ConsoleLogLevel.Success] = ConsoleColor.DarkGreen
                }
            }
        );
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
    /// <param name="colorConfig">The colors to use for text written to the SMAPI console.</param>
    private IDictionary<ConsoleLogLevel, ConsoleColor> GetConsoleColorScheme(Platform platform, ColorSchemeConfig colorConfig)
    {
        // get color scheme ID
        MonitorColorScheme schemeID = colorConfig.UseScheme;
        if (schemeID == MonitorColorScheme.AutoDetect)
        {
            schemeID = platform == Platform.Mac
                ? MonitorColorScheme.LightBackground // macOS doesn't provide console background color info, but it's usually white.
                : ColorfulConsoleWriter.IsDark(Console.BackgroundColor) ? MonitorColorScheme.DarkBackground : MonitorColorScheme.LightBackground;
        }

        // get colors for scheme
        return colorConfig.Schemes.TryGetValue(schemeID, out IDictionary<ConsoleLogLevel, ConsoleColor>? scheme)
            ? scheme
            : throw new NotSupportedException($"Unknown color scheme '{schemeID}'.");
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
