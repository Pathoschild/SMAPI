namespace StardewModdingAPI.Web.ViewModels;

/// <summary>How a log file should be displayed.</summary>
public enum LogViewFormat
{
    /// <summary>Render a parsed log and metadata.</summary>
    Default,

    /// <summary>Render a raw log with parsed metadata.</summary>
    RawView,

    /// <summary>Render directly as a text file.</summary>
    RawDownload
}
