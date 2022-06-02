using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Web.Framework.LogParsing.Models;

/// <summary>A parsed log message.</summary>
public class LogMessage
{
    /*********
    ** Accessors
    *********/
    /// <summary>The local time when the log was posted.</summary>
    public string Time { get; }

    /// <summary>The log level.</summary>
    public LogLevel Level { get; }

    /// <summary>The screen ID in split-screen mode.</summary>
    public int ScreenId { get; }

    /// <summary>The mod name.</summary>
    public string Mod { get; }

    /// <summary>The log text.</summary>
    public string Text { get; }

    /// <summary>The number of times this message was repeated consecutively.</summary>
    public int Repeated { get; set; }

    /// <summary>The section that this log message belongs to.</summary>
    public LogSection? Section { get; set; }

    /// <summary>Whether this message is the first one of its section.</summary>
    [MemberNotNullWhen(true, nameof(LogMessage.Section))]
    public bool IsStartOfSection { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance/</summary>
    /// <param name="time">The local time when the log was posted.</param>
    /// <param name="level">The log level.</param>
    /// <param name="screenId">The screen ID in split-screen mode.</param>
    /// <param name="mod">The mod name.</param>
    /// <param name="text">The log text.</param>
    /// <param name="repeated">The number of times this message was repeated consecutively.</param>
    /// <param name="section">The section that this log message belongs to.</param>
    /// <param name="isStartOfSection">Whether this message is the first one of its section.</param>
    public LogMessage(string time, LogLevel level, int screenId, string mod, string text, int repeated = 0, LogSection? section = null, bool isStartOfSection = false)
    {
        this.Time = time;
        this.Level = level;
        this.ScreenId = screenId;
        this.Mod = mod;
        this.Text = text;
        this.Repeated = repeated;
        this.Section = section;
        this.IsStartOfSection = isStartOfSection;
    }
}
