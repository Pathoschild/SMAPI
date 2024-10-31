using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using StardewModdingAPI.Framework.Logging;
using StardewModdingAPI.Internal.ConsoleWriting;

namespace StardewModdingAPI.Framework
{
    /// <summary>Encapsulates monitoring and logic for a given module.</summary>
    internal class Monitor : IMonitor
    {
        /*********
        ** Fields
        *********/
        /// <summary>The name of the module which logs messages using this instance.</summary>
        private readonly string Source;

        /// <summary>Handles writing text to the console.</summary>
        private readonly IConsoleWriter ConsoleWriter;

        /// <summary>The log file to which to write messages.</summary>
        private readonly LogFileManager LogFile;

        /// <summary>The maximum length of the <see cref="LogLevel"/> values.</summary>
        private static readonly int MaxLevelLength = Enum.GetValues<LogLevel>().Max(level => level.ToString().Length);

        /// <summary>The cached representation for each level when added to a log header.</summary>
        private static readonly Dictionary<ConsoleLogLevel, string> LogStrings = Enum.GetValues<ConsoleLogLevel>().ToDictionary(level => level, level => level.ToString().ToUpperInvariant().PadRight(Monitor.MaxLevelLength));

        /// <summary>A cache of messages that should only be logged once.</summary>
        private readonly HashSet<LogOnceCacheKey> LogOnceCache = new();

        /// <summary>Get the screen ID that should be logged to distinguish between players in split-screen mode, if any.</summary>
        private readonly Func<int?> GetScreenIdForLog;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether to log basic contextual info (like buttons pressed and menus opened) even if <see cref="IsVerbose"/> is disabled.</summary>
        public static bool ForceLogContext { get; set; }

        /// <summary>The current log level for contextual info that's relevant to the <see cref="ForceLogContext"/> flag.</summary>
        public static LogLevel ContextLogLevel => Monitor.ForceLogContext ? LogLevel.Info : LogLevel.Trace;

        /// <inheritdoc />
        public bool IsVerbose { get; }

        /// <summary>Whether to show the full log stamps (with time/level/logger) in the console. If false, shows a simplified stamp with only the logger.</summary>
        internal bool ShowFullStampInConsole { get; set; }

        /// <summary>Whether to show trace messages in the console.</summary>
        internal bool ShowTraceInConsole { get; set; }

        /// <summary>Whether to write anything to the console. This should be disabled if no console is available.</summary>
        internal bool WriteToConsole { get; set; } = true;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="source">The name of the module which logs messages using this instance.</param>
        /// <param name="logFile">The log file to which to write messages.</param>
        /// <param name="consoleWriter">The console writer to use for console output.</param>
        /// <param name="isVerbose">Whether verbose logging is enabled. This enables more detailed diagnostic messages than are normally needed.</param>
        /// <param name="getScreenIdForLog">Get the screen ID that should be logged to distinguish between players in split-screen mode, if any.</param>
        public Monitor(string source, LogFileManager logFile, IConsoleWriter consoleWriter, bool isVerbose, Func<int?> getScreenIdForLog)
        {
            // validate
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("The log source cannot be empty.");

            // initialize
            this.Source = source;
            this.LogFile = logFile ?? throw new ArgumentNullException(nameof(logFile), "The log file manager cannot be null.");
            this.ConsoleWriter = consoleWriter;
            this.IsVerbose = isVerbose;
            this.GetScreenIdForLog = getScreenIdForLog;
        }

        /// <inheritdoc />
        public void Log(string message, LogLevel level = LogLevel.Trace)
        {
            this.LogImpl(this.Source, message, (ConsoleLogLevel)level);
        }

        /// <inheritdoc />
        public void LogOnce(string message, LogLevel level = LogLevel.Trace)
        {
            if (this.LogOnceCache.Add(new LogOnceCacheKey(message, level)))
                this.LogImpl(this.Source, message, (ConsoleLogLevel)level);
        }

        /// <inheritdoc />
        public void VerboseLog(string message)
        {
            if (this.IsVerbose)
                this.Log(message);
        }

        /// <inheritdoc />
        public void VerboseLog([InterpolatedStringHandlerArgument("")] ref VerboseLogStringHandler message)
        {
            if (this.IsVerbose)
                this.Log(message.ToString());
        }

        /// <summary>Write a newline to the console and log file.</summary>
        internal void Newline()
        {
            if (this.WriteToConsole)
                Console.WriteLine();
            this.LogFile.WriteLine("");
        }

        /// <summary>Log a fatal error message.</summary>
        /// <param name="message">The message to log.</param>
        internal void LogFatal(string message)
        {
            this.LogImpl(this.Source, message, ConsoleLogLevel.Critical);
        }

        /// <summary>Log console input from the user.</summary>
        /// <param name="input">The user input to log.</param>
        internal void LogUserInput(string input)
        {
            string prefix = this.GenerateMessagePrefix(this.Source, (ConsoleLogLevel)LogLevel.Info);
            string output = $"{prefix} $>{input}";
            Console.WriteLine(output);
            this.LogFile.WriteLine(output);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Write a message line to the log.</summary>
        /// <param name="source">The name of the mod logging the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log level.</param>
        private void LogImpl(string source, string message, ConsoleLogLevel level)
        {
            // generate message
            string prefix = this.GenerateMessagePrefix(source, level);
            string fullMessage = $"{prefix} {message}";
            string consoleMessage = this.ShowFullStampInConsole ? fullMessage : $"[{source}] {message}";

            // write to console
            if (this.WriteToConsole && (this.ShowTraceInConsole || level != ConsoleLogLevel.Trace))
                this.ConsoleWriter.WriteLine(consoleMessage, level);

            // write to log file
            this.LogFile.WriteLine(fullMessage);
        }

        /// <summary>Generate a message prefix for the current time.</summary>
        /// <param name="source">The name of the mod logging the message.</param>
        /// <param name="level">The log level.</param>
        private string GenerateMessagePrefix(string source, ConsoleLogLevel level)
        {
            string levelStr = Monitor.LogStrings[level];
            int? playerIndex = this.GetScreenIdForLog();

            return $"[{DateTime.Now:HH:mm:ss} {levelStr}{(playerIndex != null ? $" screen_{playerIndex}" : "")} {source}]";
        }
    }
}
