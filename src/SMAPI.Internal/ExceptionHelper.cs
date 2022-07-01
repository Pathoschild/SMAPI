using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace StardewModdingAPI.Internal;

/// <summary>Provides extension methods for handling exceptions.</summary>
internal static class ExceptionHelper
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get a string representation of an exception suitable for writing to the error log.</summary>
    /// <param name="exception">The error to summarize.</param>
    public static string GetLogSummary(this Exception? exception)
    {
        try
        {
            string message;
            switch (exception)
            {
                case TypeLoadException ex:
                    message = $"Failed loading type '{ex.TypeName}': {exception}";
                    break;

                case ReflectionTypeLoadException ex:
                    string summary = ex.ToString();
                    foreach (Exception? childEx in ex.LoaderExceptions)
                        summary += $"\n\n{childEx?.GetLogSummary()}";
                    message = summary;
                    break;

                default:
                    message = exception?.ToString() ?? $"<null exception>\n{Environment.StackTrace}";
                    break;
            }

            return ExceptionHelper.SimplifyExtensionMessage(message);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed handling {exception?.GetType().FullName} (original message: {exception?.Message})", ex);
        }
    }

    /// <summary>Simplify common patterns in exception log messages that don't convey useful info.</summary>
    /// <param name="message">The log message to simplify.</param>
    public static string SimplifyExtensionMessage(string message)
    {
        // remove namespace for core exception types
        message = Regex.Replace(
            message,
            @"(?:StardewModdingAPI\.Framework\.Exceptions|Microsoft\.Xna\.Framework|System|System\.IO)\.([a-zA-Z]+Exception):",
            "$1:"
        );

        // remove unneeded root build paths for SMAPI and Stardew Valley
        message = message
            .Replace(@"E:\source\_Stardew\SMAPI\src\", "")
            .Replace(@"C:\GitlabRunner\builds\Gq5qA5P4\0\ConcernedApe\", "");

        // remove placeholder info in Linux/macOS stack traces
        return message
            .Replace(@"<filename unknown>:0", "");
    }
}
