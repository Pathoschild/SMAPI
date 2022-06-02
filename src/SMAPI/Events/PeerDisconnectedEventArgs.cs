using System;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IMultiplayerEvents.PeerDisconnected"/> event.</summary>
public class PeerDisconnectedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The peer who disconnected.</summary>
    public IMultiplayerPeer Peer { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="peer">The peer who disconnected.</param>
    internal PeerDisconnectedEventArgs(IMultiplayerPeer peer)
    {
        this.Peer = peer;
    }
}
