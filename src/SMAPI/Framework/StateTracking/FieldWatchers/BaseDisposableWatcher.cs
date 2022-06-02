using System;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers;

/// <summary>The base implementation for a disposable watcher.</summary>
internal abstract class BaseDisposableWatcher : IDisposable
{
    /*********
    ** Fields
    *********/
    /// <summary>Whether the watcher has been disposed.</summary>
    protected bool IsDisposed { get; private set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Stop watching the field and release all references.</summary>
    public virtual void Dispose()
    {
        this.IsDisposed = true;
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Throw an exception if the watcher is disposed.</summary>
    /// <exception cref="ObjectDisposedException">The watcher is disposed.</exception>
    protected void AssertNotDisposed()
    {
        if (this.IsDisposed)
            throw new ObjectDisposedException(this.GetType().Name);
    }
}
