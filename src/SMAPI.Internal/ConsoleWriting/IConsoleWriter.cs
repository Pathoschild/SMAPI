namespace StardewModdingAPI.Internal.ConsoleWriting;

/// <summary>Writes text to the console.</summary>
internal interface IConsoleWriter
{
    /// <summary>Write a message line to the log.</summary>
    /// <param name="message">The message to log.</param>
    /// <param name="level">The log level.</param>
    void WriteLine(string message, ConsoleLogLevel level);
}
