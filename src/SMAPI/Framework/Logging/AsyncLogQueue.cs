// Originally implemented by Ekyso in SMAPI-For-Cinderbox
// https://github.com/Ekyso/SMAPI-For-Cinderbox/blob/2d499a9cf2521d2b97552988f30f7989e7bb4247/src/SMAPI/Framework/Logging/AsyncLogQueue.cs
using System;
using System.Collections.Concurrent;
using System.Threading;
using StardewModdingAPI.Internal.ConsoleWriting;

namespace StardewModdingAPI.Framework.Logging;

/// <summary>Provides asynchronous logging by queuing messages and processing them on a background thread.</summary>
internal sealed class AsyncLogQueue : IDisposable
{
    /*********
    ** Fields
    *********/
    /// <summary>The queue of pending log messages.</summary>
    private readonly BlockingCollection<LogEntry> Queue = new(new ConcurrentQueue<LogEntry>());

    /// <summary>The background thread that processes log messages.</summary>
    private readonly Thread WorkerThread;

    /// <summary>Whether the queue has been disposed.</summary>
    private bool IsDisposed;

    /// <summary>The log file manager for file output.</summary>
    private readonly LogFileManager LogFile;

    /// <summary>The console writer for logcat output.</summary>
    private readonly IConsoleWriter ConsoleWriter;

    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public AsyncLogQueue(LogFileManager logFile, IConsoleWriter consoleWriter)
    {
        this.LogFile = logFile;
        this.ConsoleWriter = consoleWriter;
        this.WorkerThread = new Thread(this.ProcessQueue)
        {
            Name = "SMAPI.AsyncLogger",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };
        this.WorkerThread.Start();
    }

    /// <summary>Queue a message for logging.</summary>
    /// <param name="consoleMessage">The message to write to console/logcat.</param>
    /// <param name="fileMessage">The message to write to the log file.</param>
    /// <param name="level">The log level.</param>
    /// <param name="writeToConsole">Whether to write to console/logcat.</param>
    public void Enqueue(string consoleMessage, string fileMessage, ConsoleLogLevel level, bool writeToConsole)
    {
        if (this.IsDisposed)
            return;

        try
        {
            this.Queue.Add(new LogEntry(consoleMessage, fileMessage, level, writeToConsole));
        }
        catch (InvalidOperationException)
        {
            // queue was marked as complete
        }
    }

    /// <summary>Queue a newline for logging.</summary>
    /// <param name="writeToConsole">Whether to write to console.</param>
    public void EnqueueNewline(bool writeToConsole)
    {
        if (this.IsDisposed)
            return;

        try
        {
            this.Queue.Add(new LogEntry(null, "", ConsoleLogLevel.Info, writeToConsole, isNewline: true));
        }
        catch (InvalidOperationException)
        {
            // queue was marked as complete
        }
    }

    /// <summary>Flush all pending messages synchronously.</summary>
    public void Flush()
    {
        // wait for queue to drain
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (this.Queue.Count > 0 && DateTime.UtcNow < timeout)
        {
            Thread.Sleep(10);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.IsDisposed)
            return;

        this.IsDisposed = true;
        this.Queue.CompleteAdding();

        // wait for worker thread to finish
        this.WorkerThread.Join(TimeSpan.FromSeconds(5));

        this.Queue.Dispose();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Process log messages from the queue.</summary>
    private void ProcessQueue()
    {
        try
        {
            foreach (var entry in this.Queue.GetConsumingEnumerable())
            {
                try
                {
                    this.WriteEntry(entry);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AsyncLogQueue] Error writing log entry: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected when disposing
        }
    }

    /// <summary>Write a log entry to the configured outputs.</summary>
    /// <param name="entry">The entry to write.</param>
    private void WriteEntry(LogEntry entry)
    {
        if (entry.IsNewline)
        {
            if (entry.WriteToConsole)
                Console.WriteLine();
            this.LogFile?.WriteLine("");
            return;
        }

        // write to console/logcat
        if (entry.WriteToConsole && entry.ConsoleMessage != null)
        {
            this.ConsoleWriter?.WriteLine(entry.ConsoleMessage, entry.Level);
        }

        // write to log file
        if (entry.FileMessage != null)
        {
            this.LogFile?.WriteLine(entry.FileMessage);
        }
    }


    /*********
    ** Private types
    *********/
    /// <summary>A log entry waiting to be written.</summary>
    private readonly struct LogEntry
    {
        /// <summary>The message to write to console/logcat.</summary>
        public readonly string? ConsoleMessage;

        /// <summary>The message to write to the log file.</summary>
        public readonly string? FileMessage;

        /// <summary>The log level.</summary>
        public readonly ConsoleLogLevel Level;

        /// <summary>Whether to write to console/logcat.</summary>
        public readonly bool WriteToConsole;

        /// <summary>Whether this is a newline entry.</summary>
        public readonly bool IsNewline;

        /// <summary>Construct an instance.</summary>
        public LogEntry(string? consoleMessage, string? fileMessage, ConsoleLogLevel level, bool writeToConsole, bool isNewline = false)
        {
            this.ConsoleMessage = consoleMessage;
            this.FileMessage = fileMessage;
            this.Level = level;
            this.WriteToConsole = writeToConsole;
            this.IsNewline = isNewline;
        }
    }
}
