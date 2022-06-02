using System;

namespace StardewModdingAPI;

/// <summary>The base class for a mod.</summary>
public abstract class Mod : IMod, IDisposable
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public IModHelper Helper { get; internal set; } = null!;

    /// <inheritdoc />
    public IMonitor Monitor { get; internal set; } = null!;

    /// <inheritdoc />
    public IManifest ModManifest { get; internal set; } = null!;


    /*********
    ** Public methods
    *********/
    /// <inheritdoc />
    public abstract void Entry(IModHelper helper);

    /// <inheritdoc />
    public virtual object? GetApi()
    {
        return null;
    }

    /// <summary>Release or reset unmanaged resources.</summary>
    public void Dispose()
    {
        (this.Helper as IDisposable)?.Dispose(); // deliberately do this outside overridable dispose method so mods don't accidentally suppress it
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Release or reset unmanaged resources when the game exits. There's no guarantee this will be called on every exit.</summary>
    /// <param name="disposing">Whether the instance is being disposed explicitly rather than finalized. If this is false, the instance shouldn't dispose other objects since they may already be finalized.</param>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>Destruct the instance.</summary>
    ~Mod()
    {
        (this.Helper as IDisposable)?.Dispose(); // deliberately do this outside overridable dispose method so mods don't accidentally suppress it
        this.Dispose(false);
    }
}