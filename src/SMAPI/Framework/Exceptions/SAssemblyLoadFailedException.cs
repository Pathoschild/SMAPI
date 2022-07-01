using System;

namespace StardewModdingAPI.Framework.Exceptions;

/// <summary>An exception thrown when an assembly can't be loaded by SMAPI, with all the relevant details in the message.</summary>
internal class SAssemblyLoadFailedException : Exception
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="message">The error message.</param>
    public SAssemblyLoadFailedException(string message)
        : base(message) { }
}
