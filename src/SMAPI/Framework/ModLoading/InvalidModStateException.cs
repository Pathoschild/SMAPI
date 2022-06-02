using System;

namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>An exception which indicates that something went seriously wrong while loading mods, and SMAPI should abort outright.</summary>
internal class InvalidModStateException : Exception
{
    /// <summary>Construct an instance.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="ex">The underlying exception, if any.</param>
    public InvalidModStateException(string message, Exception? ex = null)
        : base(message, ex) { }
}
