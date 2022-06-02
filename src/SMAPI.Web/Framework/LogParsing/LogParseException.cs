using System;

namespace StardewModdingAPI.Web.Framework.LogParsing;

/// <summary>An error while parsing the log file which doesn't require a stack trace to troubleshoot.</summary>
internal class LogParseException : Exception
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="message">The user-friendly error message.</param>
    public LogParseException(string message)
        : base(message) { }
}
