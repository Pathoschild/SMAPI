using System;

namespace StardewModdingAPI.ModBuildConfig.Framework;

/// <summary>A user error whose message can be displayed to the user.</summary>
internal class UserErrorException : Exception
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="message">The error message.</param>
    public UserErrorException(string message)
        : base(message) { }
}
