using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Network;

namespace StardewModdingAPI.Framework.Networking;

/// <summary>Metadata about a connected player.</summary>
internal class MultiplayerPeer : IMultiplayerPeer
{
    /*********
    ** Fields
    *********/
    /// <summary>A method which sends a message to the peer.</summary>
    private readonly Action<OutgoingMessage> SendMessageImpl;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public long PlayerID { get; }

    /// <inheritdoc />
    public bool IsHost { get; }

    /// <inheritdoc />
    public bool IsSplitScreen => this.ScreenID != null;

    /// <inheritdoc />
    public bool HasSmapi => this.ApiVersion != null;

    /// <inheritdoc />
    public int? ScreenID { get; }

    /// <inheritdoc />
    public GamePlatform? Platform { get; }

    /// <inheritdoc />
    public ISemanticVersion? GameVersion { get; }

    /// <inheritdoc />
    public ISemanticVersion? ApiVersion { get; }

    /// <inheritdoc />
    public IEnumerable<IMultiplayerPeerMod> Mods { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="playerID">The player's unique ID.</param>
    /// <param name="screenID">The player's screen ID, if applicable.</param>
    /// <param name="model">The metadata to copy.</param>
    /// <param name="sendMessage">A method which sends a message to the peer.</param>
    /// <param name="isHost">Whether this is a connection to the host player.</param>
    public MultiplayerPeer(long playerID, int? screenID, RemoteContextModel? model, Action<OutgoingMessage> sendMessage, bool isHost)
    {
        this.PlayerID = playerID;
        this.ScreenID = screenID;
        this.IsHost = isHost;

        if (model != null)
        {
            this.Platform = model.Platform;
            this.GameVersion = model.GameVersion;
            this.ApiVersion = model.ApiVersion;
            this.Mods = model.Mods.Select(mod => new MultiplayerPeerMod(mod)).ToArray();
        }
        else
            this.Mods = Array.Empty<IMultiplayerPeerMod>();

        this.SendMessageImpl = sendMessage;
    }

    /// <inheritdoc />
    public IMultiplayerPeerMod? GetMod(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || !this.Mods.Any())
            return null;

        id = id.Trim();
        return this.Mods.FirstOrDefault(mod => mod.ID != null && mod.ID.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Send a message to the given peer, bypassing the game's normal validation to allow messages before the connection is approved.</summary>
    /// <param name="message">The message to send.</param>
    public void SendMessage(OutgoingMessage message)
    {
        this.SendMessageImpl(message);
    }
}
