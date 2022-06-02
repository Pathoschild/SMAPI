using StardewModdingAPI.Internal.ConsoleWriting;

namespace StardewModdingAPI;

/// <summary>The log severity levels.</summary>
public enum LogLevel
{
    /// <summary>Tracing info intended for developers.</summary>
    Trace = ConsoleLogLevel.Trace,

    /// <summary>Troubleshooting info that may be relevant to the player.</summary>
    Debug = ConsoleLogLevel.Debug,

    /// <summary>Info relevant to the player. This should be used judiciously.</summary>
    Info = ConsoleLogLevel.Info,

    /// <summary>An issue the player should be aware of. This should be used rarely.</summary>
    Warn = ConsoleLogLevel.Warn,

    /// <summary>A message indicating something went wrong.</summary>
    Error = ConsoleLogLevel.Error,

    /// <summary>Important information to highlight for the player when player action is needed (e.g. new version available). This should be used rarely to avoid alert fatigue.</summary>
    Alert = ConsoleLogLevel.Alert
}
