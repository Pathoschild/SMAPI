using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Galaxy.Api;
using StardewValley.Network;
using StardewValley.SDKs;

namespace StardewModdingAPI.Framework.Networking;

/// <summary>A multiplayer server used to connect to an incoming player. This is an implementation of <see cref="LidgrenServer"/> that adds support for SMAPI's metadata context exchange.</summary>
internal class SGalaxyNetServer : GalaxyNetServer
{
    /*********
    ** Fields
    *********/
    /// <summary>A callback to raise when receiving a message. This receives the incoming message, a method to send a message, and a callback to run the default logic.</summary>
    private readonly Action<IncomingMessage, Action<OutgoingMessage>, Action> OnProcessingMessage;

    /// <summary>SMAPI's implementation of the game's core multiplayer logic.</summary>
    private readonly SMultiplayer Multiplayer;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="gameServer">The underlying game server.</param>
    /// <param name="multiplayer">SMAPI's implementation of the game's core multiplayer logic.</param>
    /// <param name="onProcessingMessage">A callback to raise when receiving a message. This receives the incoming message, a method to send a message, and a callback to run the default logic.</param>
    public SGalaxyNetServer(IGameServer gameServer, SMultiplayer multiplayer, Action<IncomingMessage, Action<OutgoingMessage>, Action> onProcessingMessage)
        : base(gameServer)
    {
        this.Multiplayer = multiplayer;
        this.OnProcessingMessage = onProcessingMessage;
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Read and process a message from the client.</summary>
    /// <param name="peer">The Galaxy peer ID.</param>
    /// <param name="messageStream">The data to process.</param>
    /// <remarks>This reimplements <see cref="GalaxyNetServer.onReceiveMessage"/>, but adds a callback to <see cref="OnProcessingMessage"/>.</remarks>
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The callback is invoked synchronously.")]
    protected override void onReceiveMessage(GalaxyID peer, Stream messageStream)
    {
        using IncomingMessage message = new();
        using BinaryReader reader = new(messageStream);

        message.Read(reader);
        ulong peerID = peer.ToUint64(); // note: GalaxyID instances get reused, so need to store the underlying ID instead
        this.OnProcessingMessage(message, outgoing => this.SendMessageToPeerID(peerID, outgoing), () =>
        {
            if (this.peers.ContainsLeft(message.FarmerID) && (long)this.peers[message.FarmerID] == (long)peerID)
                this.gameServer.processIncomingMessage(message);
            else if (message.MessageType == StardewValley.Multiplayer.playerIntroduction)
            {
                NetFarmerRoot farmer = this.Multiplayer.readFarmer(message.Reader);
                GalaxyID capturedPeer = new(peerID);
                this.gameServer.checkFarmhandRequest(Convert.ToString(peerID), this.getConnectionId(peer), farmer, msg => this.sendMessage(capturedPeer, msg), () => this.peers[farmer.Value.UniqueMultiplayerID] = capturedPeer.ToUint64());
            }
        });
    }

    /// <summary>Send a message to a remote peer.</summary>
    /// <param name="peerID">The unique Galaxy ID, derived from <see cref="GalaxyID.ToUint64"/>.</param>
    /// <param name="message">The message to send.</param>
    private void SendMessageToPeerID(ulong peerID, OutgoingMessage message)
    {
        this.sendMessage(new GalaxyID(peerID), message);
    }
}