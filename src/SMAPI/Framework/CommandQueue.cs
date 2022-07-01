using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Framework;

/// <summary>A thread-safe command queue optimized for infrequent changes.</summary>
internal class CommandQueue
{
    /********
    ** Fields
    ********/
    /// <summary>The underlying list of queued commands to parse and execute.</summary>
    private readonly List<string> RawCommandQueue = new();


    /********
    ** Public methods
    ********/
    /// <summary>Add a command to the queue.</summary>
    /// <param name="command">The command to add.</param>
    public void Add(string command)
    {
        lock (this.RawCommandQueue)
            this.RawCommandQueue.Add(command);
    }

    /// <summary>Remove and return all queued commands, if any.</summary>
    /// <param name="queued">The commands that were dequeued, in the order they were originally queued.</param>
    /// <returns>Returns whether any values were dequeued.</returns>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Deliberately check if it's empty before locking unnecessarily.")]
    public bool TryDequeue([NotNullWhen(true)] out string[]? queued)
    {
        if (this.RawCommandQueue.Count is 0)
        {
            queued = null;
            return false;
        }

        lock (this.RawCommandQueue)
        {
            queued = this.RawCommandQueue.ToArray();
            this.RawCommandQueue.Clear();
            return queued.Length > 0;
        }
    }
}
