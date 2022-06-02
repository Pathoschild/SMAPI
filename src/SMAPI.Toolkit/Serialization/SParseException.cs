using System;

namespace StardewModdingAPI.Toolkit.Serialization;

/// <summary>A format exception which provides a user-facing error message.</summary>
internal class SParseException : FormatException
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="ex">The underlying exception, if any.</param>
    public SParseException(string message, Exception? ex = null)
        : base(message, ex) { }
}
