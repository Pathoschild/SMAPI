using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Lidgren.Network;
using StardewValley.Network;

namespace StardewModdingAPI.Framework.Networking;

/// <summary>A multiplayer server used to connect to an incoming player. This is an implementation of <see cref="LidgrenServer"/> that adds support for SMAPI's metadata context exchange.</summary>
internal class SLidgrenServer : LidgrenServer
{
    /*********
    ** Fields
    *********/
    /// <summary>SMAPI's implementation of the game's core multiplayer logic.</summary>
    private readonly SMultiplayer Multiplayer;

    /// <summary>A callback to raise when receiving a message. This receives the incoming message, a method to send a message, and a callback to run the default logic.</summary>
    private readonly Action<IncomingMessage, Action<OutgoingMessage>, Action> OnProcessingMessage;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="multiplayer">SMAPI's implementation of the game's core multiplayer logic.</param>
    /// <param name="gameServer">The underlying game server.</param>
    /// <param name="onProcessingMessage">A callback to raise when receiving a message. This receives the incoming message, a method to send a message, and a callback to run the default logic.</param>
    public SLidgrenServer(IGameServer gameServer, SMultiplayer multiplayer, Action<IncomingMessage, Action<OutgoingMessage>, Action> onProcessingMessage)
        : base(gameServer)
    {
        this.Multiplayer = multiplayer;
        this.OnProcessingMessage = onProcessingMessage;
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Parse a data message from a client.</summary>
    /// <param name="rawMessage">The raw network message to parse.</param>
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The callback is invoked synchronously.")]
    protected override void parseDataMessageFromClient(NetIncomingMessage rawMessage)
    {
        // add hook to call multiplayer core
        NetConnection peer = rawMessage.SenderConnection;
        using IncomingMessage message = new();
        using Stream readStream = new NetBufferReadStream(rawMessage);
        using BinaryReader reader = new(readStream);

        while (rawMessage.LengthBits - rawMessage.Position >= 8)
        {
            message.Read(reader);
            NetConnection connection = rawMessage.SenderConnection; // don't pass rawMessage into context because it gets reused
            this.OnProcessingMessage(message, outgoing => this.sendMessage(connection, outgoing), () =>
            {
                if (this.peers.ContainsLeft(message.FarmerID) && this.peers[message.FarmerID] == peer)
                    this.gameServer.processIncomingMessage(message);
                else if (message.MessageType == StardewValley.Multiplayer.playerIntroduction)
                {
                    NetFarmerRoot farmer = this.Multiplayer.readFarmer(message.Reader);
                    this.gameServer.checkFarmhandRequest("", this.getConnectionId(rawMessage.SenderConnection), farmer, msg => this.sendMessage(peer, msg), () => this.peers[farmer.Value.UniqueMultiplayerID] = peer);
                }
            });
        }
    }
}
