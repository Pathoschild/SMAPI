using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events;

/// <inheritdoc cref="IMultiplayerEvents" />
internal class ModMultiplayerEvents : ModEventsBase, IMultiplayerEvents
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public event EventHandler<PeerContextReceivedEventArgs> PeerContextReceived
    {
        add => this.EventManager.PeerContextReceived.Add(value, this.Mod);
        remove => this.EventManager.PeerContextReceived.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<PeerConnectedEventArgs> PeerConnected
    {
        add => this.EventManager.PeerConnected.Add(value, this.Mod);
        remove => this.EventManager.PeerConnected.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<ModMessageReceivedEventArgs> ModMessageReceived
    {
        add => this.EventManager.ModMessageReceived.Add(value, this.Mod);
        remove => this.EventManager.ModMessageReceived.Remove(value);
    }

    /// <inheritdoc />
    public event EventHandler<PeerDisconnectedEventArgs> PeerDisconnected
    {
        add => this.EventManager.PeerDisconnected.Add(value, this.Mod);
        remove => this.EventManager.PeerDisconnected.Remove(value);
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod which uses this instance.</param>
    /// <param name="eventManager">The underlying event manager.</param>
    internal ModMultiplayerEvents(IModMetadata mod, EventManager eventManager)
        : base(mod, eventManager) { }
}
