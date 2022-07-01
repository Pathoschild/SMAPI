namespace StardewModdingAPI.Internal.ConsoleWriting;

/// <summary>The log severity levels.</summary>
internal enum ConsoleLogLevel
{
    /// <summary>Tracing info intended for developers.</summary>
    Trace,

    /// <summary>Troubleshooting info that may be relevant to the player.</summary>
    Debug,

    /// <summary>Info relevant to the player. This should be used judiciously.</summary>
    Info,

    /// <summary>An issue the player should be aware of. This should be used rarely.</summary>
    Warn,

    /// <summary>A message indicating something went wrong.</summary>
    Error,

    /// <summary>Important information to highlight for the player when player action is needed (e.g. new version available). This should be used rarely to avoid alert fatigue.</summary>
    Alert,

    /// <summary>A critical issue that generally signals an immediate end to the application.</summary>
    Critical,

    /// <summary>A success message that generally signals a successful end to a task.</summary>
    Success
}
