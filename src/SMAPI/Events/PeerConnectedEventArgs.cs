using System;

namespace StardewModdingAPI.Events;

/// <summary>Event arguments for an <see cref="IMultiplayerEvents.PeerConnected"/> event.</summary>
public class PeerConnectedEventArgs : EventArgs
{
    /*********
    ** Accessors
    *********/
    /// <summary>The peer whose metadata was received.</summary>
    public IMultiplayerPeer Peer { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="peer">The peer whose metadata was received.</param>
    internal PeerConnectedEventArgs(IMultiplayerPeer peer)
    {
        this.Peer = peer;
    }
}
