using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Internal.ConsoleWriting;

/// <summary>The console color scheme options.</summary>
internal class ColorSchemeConfig
{
    /*********
    ** Accessors
    *********/
    /// <summary>The default color scheme ID to use, or <see cref="MonitorColorScheme.AutoDetect"/> to select one automatically.</summary>
    public MonitorColorScheme UseScheme { get; }

    /// <summary>The available console color schemes.</summary>
    public IDictionary<MonitorColorScheme, IDictionary<ConsoleLogLevel, ConsoleColor>> Schemes { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="useScheme">The default color scheme ID to use, or <see cref="MonitorColorScheme.AutoDetect"/> to select one automatically.</param>
    /// <param name="schemes">The available console color schemes.</param>
    public ColorSchemeConfig(MonitorColorScheme useScheme, IDictionary<MonitorColorScheme, IDictionary<ConsoleLogLevel, ConsoleColor>> schemes)
    {
        this.UseScheme = useScheme;
        this.Schemes = schemes;
    }
}
