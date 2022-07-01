using System;

namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>An exception raised when an incompatible instruction is found while loading a mod assembly.</summary>
internal class IncompatibleInstructionException : Exception
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public IncompatibleInstructionException()
        : base("Found incompatible CIL instructions.") { }

    /// <summary>Construct an instance.</summary>
    /// <param name="message">A message which describes the error.</param>
    public IncompatibleInstructionException(string message)
        : base(message) { }
}
