using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Galaxy.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Networking;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Network;
using StardewValley.SDKs;

namespace StardewModdingAPI.Framework;

/// <summary>SMAPI's implementation of the game's core multiplayer logic.</summary>
/// <remarks>
/// SMAPI syncs mod context to all players through the host as such:
///   1. Farmhand sends ModContext + PlayerIntro.
///   2. If host receives ModContext: it stores the context, replies with known contexts, and forwards it to other farmhands.
///   3. If host receives PlayerIntro before ModContext: it stores a 'vanilla player' context, and forwards it to other farmhands.
///   4. If farmhand receives ModContext: it stores it.
///   5. If farmhand receives ServerIntro without a preceding ModContext: it stores a 'vanilla host' context.
///   6. If farmhand receives PlayerIntro without a preceding ModContext AND it's not the host peer: it stores a 'vanilla player' context.
///
/// Once a farmhand/server stored a context, messages can be sent to that player through the SMAPI APIs.
/// </remarks>
internal class SMultiplayer : Multiplayer
{
    /*********
    ** Fields
    *********/
    /// <summary>Encapsulates monitoring and logging.</summary>
    private readonly IMonitor Monitor;

    /// <summary>Tracks the installed mods.</summary>
    private readonly ModRegistry ModRegistry;

    /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
    private readonly JsonHelper JsonHelper;

    /// <summary>Simplifies access to private code.</summary>
    private readonly Reflector Reflection;

    /// <summary>Manages SMAPI events.</summary>
    private readonly EventManager EventManager;

    /// <summary>A callback to invoke when a mod message is received.</summary>
    private readonly Action<ModMessageModel> OnModMessageReceived;

    /// <summary>Whether to log network traffic.</summary>
    private readonly bool LogNetworkTraffic;

    /// <summary>The backing field for <see cref="Peers"/>.</summary>
    private readonly PerScreen<IDictionary<long, MultiplayerPeer>> PeersImpl = new(() => new Dictionary<long, MultiplayerPeer>());

    /// <summary>The backing field for <see cref="HostPeer"/>.</summary>
    private readonly PerScreen<MultiplayerPeer?> HostPeerImpl = new();


    /*********
    ** Accessors
    *********/
    /// <summary>The metadata for each connected peer.</summary>
    public IDictionary<long, MultiplayerPeer> Peers => this.PeersImpl.Value;

    /// <summary>The metadata for the host player, if the current player is a farmhand.</summary>
    public MultiplayerPeer? HostPeer
    {
        get => this.HostPeerImpl.Value;
        private set => this.HostPeerImpl.Value = value;
    }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="monitor">Encapsulates monitoring and logging.</param>
    /// <param name="eventManager">Manages SMAPI events.</param>
    /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
    /// <param name="modRegistry">Tracks the installed mods.</param>
    /// <param name="reflection">Simplifies access to private code.</param>
    /// <param name="onModMessageReceived">A callback to invoke when a mod message is received.</param>
    /// <param name="logNetworkTraffic">Whether to log network traffic.</param>
    public SMultiplayer(IMonitor monitor, EventManager eventManager, JsonHelper jsonHelper, ModRegistry modRegistry, Reflector reflection, Action<ModMessageModel> onModMessageReceived, bool logNetworkTraffic)
    {
        this.Monitor = monitor;
        this.EventManager = eventManager;
        this.JsonHelper = jsonHelper;
        this.ModRegistry = modRegistry;
        this.Reflection = reflection;
        this.OnModMessageReceived = onModMessageReceived;
        this.LogNetworkTraffic = logNetworkTraffic;
    }

    /// <summary>Perform cleanup needed when a multiplayer session ends.</summary>
    public void CleanupOnMultiplayerExit()
    {
        this.Peers.Clear();
        this.HostPeer = null;
    }

    /// <summary>Initialize a client before the game connects to a remote server.</summary>
    /// <param name="client">The client to initialize.</param>
    public override Client InitClient(Client client)
    {
        switch (client)
        {
            case LidgrenClient:
            {
                string address = this.Reflection.GetField<string?>(client, "address").GetValue() ?? throw new InvalidOperationException("Can't initialize base networking client: no valid address found.");
                return new SLidgrenClient(address, this.OnClientProcessingMessage, this.OnClientSendingMessage);
            }

            case GalaxyNetClient:
            {
                GalaxyID address = this.Reflection.GetField<GalaxyID?>(client, "lobbyId").GetValue() ?? throw new InvalidOperationException("Can't initialize GOG networking client: no valid address found.");
                return new SGalaxyNetClient(address, this.OnClientProcessingMessage, this.OnClientSendingMessage);
            }

            default:
                this.Monitor.Log($"Unknown multiplayer client type: {client.GetType().AssemblyQualifiedName}");
                return client;
        }
    }

    /// <summary>Initialize a server before the game connects to an incoming player.</summary>
    /// <param name="server">The server to initialize.</param>
    public override Server InitServer(Server server)
    {
        switch (server)
        {
            case LidgrenServer:
            {
                IGameServer gameServer = this.Reflection.GetField<IGameServer?>(server, "gameServer").GetValue() ?? throw new InvalidOperationException("Can't initialize base networking client: the required 'gameServer' field wasn't found.");
                return new SLidgrenServer(gameServer, this, this.OnServerProcessingMessage);
            }

            case GalaxyNetServer:
            {
                IGameServer gameServer = this.Reflection.GetField<IGameServer?>(server, "gameServer").GetValue() ?? throw new InvalidOperationException("Can't initialize GOG networking client: the required 'gameServer' field wasn't found.");
                return new SGalaxyNetServer(gameServer, this, this.OnServerProcessingMessage);
            }

            default:
                this.Monitor.Log($"Unknown multiplayer server type: {server.GetType().AssemblyQualifiedName}");
                return server;
        }
    }

    /// <summary>A callback raised when sending a message as a farmhand.</summary>
    /// <param name="message">The message being sent.</param>
    /// <param name="sendMessage">Send an arbitrary message through the client.</param>
    /// <param name="resume">Resume sending the underlying message.</param>
    protected void OnClientSendingMessage(OutgoingMessage message, Action<OutgoingMessage> sendMessage, Action resume)
    {
        if (this.LogNetworkTraffic)
            this.Monitor.Log($"CLIENT SEND {(MessageType)message.MessageType} {message.FarmerID}");

        switch (message.MessageType)
        {
            // sync mod context (step 1)
            case (byte)MessageType.PlayerIntroduction:
                sendMessage(new OutgoingMessage((byte)MessageType.ModContext, Game1.player.UniqueMultiplayerID, this.GetContextSyncMessageFields()));
                resume();
                break;

            // run default logic
            default:
                resume();
                break;
        }
    }

    /// <summary>Process an incoming network message as the host player.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sendMessage">A method which sends the given message to the client.</param>
    /// <param name="resume">Process the message using the game's default logic.</param>
    public void OnServerProcessingMessage(IncomingMessage message, Action<OutgoingMessage> sendMessage, Action resume)
    {
        if (this.LogNetworkTraffic)
            this.Monitor.Log($"SERVER RECV {(MessageType)message.MessageType} {message.FarmerID}");

        switch (message.MessageType)
        {
            // sync mod context (step 2)
            case (byte)MessageType.ModContext:
            {
                // parse message
                RemoteContextModel? model = this.ReadContext(message.Reader);
                this.Monitor.Log($"Received context for farmhand {message.FarmerID} running {(model != null ? $"SMAPI {model.ApiVersion} with {model.Mods.Length} mods" : "vanilla")}.");

                // store peer
                MultiplayerPeer newPeer = new(
                    playerID: message.FarmerID,
                    screenID: this.GetScreenId(message.FarmerID),
                    model: model,
                    sendMessage: sendMessage,
                    isHost: false
                );
                if (this.Peers.ContainsKey(message.FarmerID))
                {
                    this.Monitor.Log($"Received mod context from farmhand {message.FarmerID}, but the game didn't see them disconnect. This may indicate issues with the network connection.", LogLevel.Info);
                    this.Peers.Remove(message.FarmerID);
                    return;
                }
                this.AddPeer(newPeer, canBeHost: false, raiseEvent: false);

                // reply with own context
                this.Monitor.VerboseLog("   Replying with host context...");
                newPeer.SendMessage(new OutgoingMessage((byte)MessageType.ModContext, Game1.player.UniqueMultiplayerID, this.GetContextSyncMessageFields()));

                // reply with other players' context
                foreach (MultiplayerPeer otherPeer in this.Peers.Values.Where(p => p.PlayerID != newPeer.PlayerID))
                {
                    this.Monitor.VerboseLog($"   Replying with context for player {otherPeer.PlayerID}...");
                    newPeer.SendMessage(new OutgoingMessage((byte)MessageType.ModContext, otherPeer.PlayerID, this.GetContextSyncMessageFields(otherPeer)));
                }

                // forward to other peers
                if (this.Peers.Count > 1)
                {
                    object[] fields = this.GetContextSyncMessageFields(newPeer);
                    foreach (MultiplayerPeer otherPeer in this.Peers.Values.Where(p => p.PlayerID != newPeer.PlayerID))
                    {
                        this.Monitor.VerboseLog($"   Forwarding context to player {otherPeer.PlayerID}...");
                        otherPeer.SendMessage(new OutgoingMessage((byte)MessageType.ModContext, newPeer.PlayerID, fields));
                    }
                }

                // raise event
                if (this.EventManager.PeerContextReceived.HasListeners)
                    this.EventManager.PeerContextReceived.Raise(new PeerContextReceivedEventArgs(newPeer));
            }
                break;

            // handle player intro
            case (byte)MessageType.PlayerIntroduction:
                // store peer if new
                if (!this.Peers.ContainsKey(message.FarmerID))
                {
                    this.Monitor.Log($"Received connection for vanilla player {message.FarmerID}.");
                    MultiplayerPeer peer = new(
                        playerID: message.FarmerID,
                        screenID: this.GetScreenId(message.FarmerID),
                        model: null,
                        sendMessage: sendMessage,
                        isHost: false
                    );
                    this.AddPeer(peer, canBeHost: false);
                }

                // let game handle connection
                resume();

                // raise event
                if (this.EventManager.PeerConnected.HasListeners)
                    this.EventManager.PeerConnected.Raise(new PeerConnectedEventArgs(this.Peers[message.FarmerID]));
                break;

            // handle mod message
            case (byte)MessageType.ModMessage:
                this.ReceiveModMessage(message);
                break;

            default:
                resume();
                break;
        }
    }

    /// <summary>Process an incoming network message as a farmhand.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sendMessage">Send an arbitrary message through the client.</param>
    /// <param name="resume">Resume processing the message using the game's default logic.</param>
    /// <returns>Returns whether the message was handled.</returns>
    public void OnClientProcessingMessage(IncomingMessage message, Action<OutgoingMessage> sendMessage, Action resume)
    {
        if (this.LogNetworkTraffic)
            this.Monitor.Log($"CLIENT RECV {(MessageType)message.MessageType} {message.FarmerID}");

        switch (message.MessageType)
        {
            // mod context sync (step 4)
            case (byte)MessageType.ModContext:
            {
                // parse message
                RemoteContextModel? model = this.ReadContext(message.Reader);
                this.Monitor.Log($"Received context for {(model?.IsHost == true ? "host" : "farmhand")} {message.FarmerID} running {(model != null ? $"SMAPI {model.ApiVersion} with {model.Mods.Length} mods" : "vanilla")}.");

                // store peer
                MultiplayerPeer peer = new(
                    playerID: message.FarmerID,
                    screenID: this.GetScreenId(message.FarmerID),
                    model: model,
                    sendMessage: sendMessage,
                    isHost: model?.IsHost ?? this.HostPeer == null
                );
                if (peer.IsHost && this.HostPeer != null)
                {
                    this.Monitor.Log($"Rejected mod context from host player {peer.PlayerID}: already received host data from {(peer.PlayerID == this.HostPeer.PlayerID ? "that player" : $"player {peer.PlayerID}")}.", LogLevel.Error);
                    return;
                }
                this.AddPeer(peer, canBeHost: true);
            }
                break;

            // handle server intro
            case (byte)MessageType.ServerIntroduction:
            {
                // store peer
                if (!this.Peers.ContainsKey(message.FarmerID) && this.HostPeer == null)
                {
                    this.Monitor.Log($"Received connection for vanilla host {message.FarmerID}.");
                    var peer = new MultiplayerPeer(
                        playerID: message.FarmerID,
                        screenID: this.GetScreenId(message.FarmerID),
                        model: null,
                        sendMessage: sendMessage,
                        isHost: true
                    );
                    this.AddPeer(peer, canBeHost: false);
                }
                resume();
                break;
            }

            // handle player intro
            case (byte)MessageType.PlayerIntroduction:
            {
                // store peer
                if (!this.Peers.TryGetValue(message.FarmerID, out MultiplayerPeer? peer))
                {
                    peer = new MultiplayerPeer(
                        playerID: message.FarmerID,
                        screenID: this.GetScreenId(message.FarmerID),
                        model: null,
                        sendMessage: sendMessage,
                        isHost: this.HostPeer == null
                    );
                    this.Monitor.Log($"Received connection for vanilla {(peer.IsHost ? "host" : "farmhand")} {message.FarmerID}.");
                    this.AddPeer(peer, canBeHost: true);
                }

                resume();
                break;
            }

            // handle mod message
            case (byte)MessageType.ModMessage:
                this.ReceiveModMessage(message);
                break;

            default:
                resume();
                break;
        }
    }

    /// <summary>Remove players who are disconnecting.</summary>
    protected override void removeDisconnectedFarmers()
    {
        foreach (long playerID in this.disconnectingFarmers)
        {
            if (this.Peers.TryGetValue(playerID, out MultiplayerPeer? peer))
            {
                this.Monitor.Log($"Player quit: {playerID}");
                this.Peers.Remove(playerID);

                if (this.EventManager.PeerDisconnected.HasListeners)
                    this.EventManager.PeerDisconnected.Raise(new PeerDisconnectedEventArgs(peer));
            }
        }

        base.removeDisconnectedFarmers();
    }

    /// <summary>Broadcast a mod message to matching players.</summary>
    /// <param name="message">The data to send over the network.</param>
    /// <param name="messageType">A message type which receiving mods can use to decide whether it's the one they want to handle, like <c>SetPlayerLocation</c>. This doesn't need to be globally unique, since mods should check the originating mod ID.</param>
    /// <param name="fromModID">The unique ID of the mod sending the message.</param>
    /// <param name="toModIDs">The mod IDs which should receive the message on the destination computers, or <c>null</c> for all mods. Specifying mod IDs is recommended to improve performance, unless it's a general-purpose broadcast.</param>
    /// <param name="toPlayerIDs">The <see cref="Farmer.UniqueMultiplayerID" /> values for the players who should receive the message, or <c>null</c> for all players. If you don't need to broadcast to all players, specifying player IDs is recommended to reduce latency.</param>
    public void BroadcastModMessage<TMessage>(TMessage message, string messageType, string fromModID, string[]? toModIDs, long[]? toPlayerIDs)
    {
        // validate input
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrWhiteSpace(messageType))
            throw new ArgumentNullException(nameof(messageType));
        if (string.IsNullOrWhiteSpace(fromModID))
            throw new ArgumentNullException(nameof(fromModID));

        // get valid peers
        var sendToPeers = this.Peers.Values.Where(p => p.HasSmapi).ToList();
        bool sendToSelf = true;

        // filter by player ID
        if (toPlayerIDs != null)
        {
            var ids = new HashSet<long>(toPlayerIDs);
            sendToPeers.RemoveAll(peer => !ids.Contains(peer.PlayerID));
            sendToSelf = ids.Contains(Game1.player.UniqueMultiplayerID);
        }

        // filter by mod ID
        if (toModIDs != null)
        {
            var ids = new HashSet<string>(toModIDs, StringComparer.OrdinalIgnoreCase);
            sendToPeers.RemoveAll(peer => peer.Mods.All(mod => !ids.Contains(mod.ID)));
            sendToSelf = sendToSelf && toModIDs.Any(id => this.ModRegistry.Get(id) != null);
        }

        // validate recipients
        if (!sendToSelf && !sendToPeers.Any())
        {
            this.Monitor.VerboseLog($"Ignored '{messageType}' broadcast from mod {fromModID}: none of the specified player IDs can receive this message.");
            return;
        }

        // get data to send
        ModMessageModel model = new(
            fromPlayerID: Game1.player.UniqueMultiplayerID,
            fromModID: fromModID,
            toModIDs: toModIDs,
            toPlayerIDs: sendToPeers.Select(p => p.PlayerID).ToArray(),
            type: messageType,
            data: JToken.FromObject(message)
        );
        string data = JsonConvert.SerializeObject(model, Formatting.None);

        // send self-message
        if (sendToSelf)
        {
            if (this.LogNetworkTraffic)
                this.Monitor.Log($"Broadcasting '{messageType}' message to self: {data}.");

            this.OnModMessageReceived(model);
        }

        // send message to peers
        if (sendToPeers.Any())
        {
            if (Context.IsMainPlayer)
            {
                foreach (MultiplayerPeer peer in sendToPeers)
                {
                    if (this.LogNetworkTraffic)
                        this.Monitor.Log($"Broadcasting '{messageType}' message to farmhand {peer.PlayerID}: {data}.");

                    peer.SendMessage(new OutgoingMessage((byte)MessageType.ModMessage, peer.PlayerID, data));
                }
            }
            else if (this.HostPeer?.HasSmapi == true)
            {
                if (this.LogNetworkTraffic)
                    this.Monitor.Log($"Broadcasting '{messageType}' message to host {this.HostPeer.PlayerID}: {data}.");

                this.HostPeer.SendMessage(new OutgoingMessage((byte)MessageType.ModMessage, this.HostPeer.PlayerID, data));
            }
            else
                this.Monitor.VerboseLog("  Can't send message because no valid connections were found.");
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Save a received peer.</summary>
    /// <param name="peer">The peer to add.</param>
    /// <param name="canBeHost">Whether to track the peer as the host if applicable.</param>
    /// <param name="raiseEvent">Whether to raise the <see cref="Events.EventManager.PeerContextReceived"/> event.</param>
    private void AddPeer(MultiplayerPeer peer, bool canBeHost, bool raiseEvent = true)
    {
        // store
        this.Peers[peer.PlayerID] = peer;
        if (canBeHost && peer.IsHost)
            this.HostPeer = peer;

        // raise event
        if (raiseEvent && this.EventManager.PeerContextReceived.HasListeners)
            this.EventManager.PeerContextReceived.Raise(new PeerContextReceivedEventArgs(peer));
    }

    /// <summary>Read the metadata context for a player.</summary>
    /// <param name="reader">The stream reader.</param>
    private RemoteContextModel? ReadContext(BinaryReader reader)
    {
        string data = reader.ReadString();
        RemoteContextModel? model = this.JsonHelper.Deserialize<RemoteContextModel>(data);
        return model?.ApiVersion != null
            ? model
            : null; // no data available for vanilla players
    }

    /// <summary>Receive a mod message sent from another player's mods.</summary>
    /// <param name="message">The raw message to parse.</param>
    private void ReceiveModMessage(IncomingMessage message)
    {
        // read message JSON
        string json = message.Reader.ReadString();
        if (this.LogNetworkTraffic)
            this.Monitor.Log($"Received message: {json}.");

        // deserialize model
        ModMessageModel? model;
        try
        {
            model = this.JsonHelper.Deserialize<ModMessageModel>(json);
            if (model is null)
            {
                this.Monitor.Log($"Received invalid mod message from {message.FarmerID}.\nRaw message data: {json}");
                return;
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Received invalid mod message from {message.FarmerID}.\nRaw message data: {json}\nError details: {ex.GetLogSummary()}");
            return;
        }

        // get player IDs
        HashSet<long> playerIDs = new HashSet<long>(model.ToPlayerIDs ?? this.GetKnownPlayerIDs());

        // notify local mods
        if (playerIDs.Contains(Game1.player.UniqueMultiplayerID))
            this.OnModMessageReceived(model);

        // forward to other players
        if (Context.IsMainPlayer && playerIDs.Any(p => p != Game1.player.UniqueMultiplayerID))
        {
            foreach (long playerID in playerIDs)
            {
                if (playerID != Game1.player.UniqueMultiplayerID && playerID != model.FromPlayerID && this.Peers.TryGetValue(playerID, out MultiplayerPeer? peer))
                {
                    ModMessageModel newModel = new(model)
                    {
                        ToPlayerIDs = new[] { peer.PlayerID }
                    };

                    this.Monitor.VerboseLog($"  Forwarding message to player {peer.PlayerID}.");
                    peer.SendMessage(new OutgoingMessage((byte)MessageType.ModMessage, peer.PlayerID, this.JsonHelper.Serialize(newModel, Formatting.None)));
                }
            }
        }
    }

    /// <summary>Get the screen ID for a given player ID, if the player is local.</summary>
    /// <param name="playerId">The player ID to check.</param>
    private int? GetScreenId(long playerId)
    {
        return SGameRunner.Instance.GetScreenId(playerId);
    }

    /// <summary>Get all connected player IDs, including the current player.</summary>
    private IEnumerable<long> GetKnownPlayerIDs()
    {
        yield return Game1.player.UniqueMultiplayerID;
        foreach (long peerID in this.Peers.Keys)
            yield return peerID;
    }

    /// <summary>Get the fields to include in a context sync message sent to other players.</summary>
    private object[] GetContextSyncMessageFields()
    {
        RemoteContextModel model = new(
            isHost: Context.IsWorldReady && Context.IsMainPlayer,
            platform: Constants.TargetPlatform,
            apiVersion: Constants.ApiVersion,
            gameVersion: Constants.GameVersion,
            mods: this.ModRegistry
                .GetAll()
                .Select(mod => new RemoteContextModModel(
                    id: mod.Manifest.UniqueID,
                    name: mod.Manifest.Name,
                    version: mod.Manifest.Version
                ))
                .ToArray()
        );

        return new object[] { this.JsonHelper.Serialize(model, Formatting.None) };
    }

    /// <summary>Get the fields to include in a context sync message sent to other players.</summary>
    /// <param name="peer">The peer whose data to represent.</param>
    private object[] GetContextSyncMessageFields(IMultiplayerPeer peer)
    {
        if (!peer.HasSmapi)
            return new object[] { "{}" };

        RemoteContextModel model = new(
            isHost: peer.IsHost,
            platform: peer.Platform.Value,
            apiVersion: peer.ApiVersion,
            gameVersion: peer.GameVersion,
            mods: peer.Mods
                .Select(mod => new RemoteContextModModel(
                    id: mod.ID,
                    name: mod.Name,
                    version: mod.Version
                ))
                .ToArray()
        );

        return new object[] { this.JsonHelper.Serialize(model, Formatting.None) };
    }
}