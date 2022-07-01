using System;
using System.Collections.Generic;
using StardewValley;

namespace StardewModdingAPI;

/// <summary>Provides multiplayer utilities.</summary>
public interface IMultiplayerHelper : IModLinked
{
    /// <summary>Get a new multiplayer ID.</summary>
    long GetNewID();

    /// <summary>Get the locations which are being actively synced from the host.</summary>
    IEnumerable<GameLocation> GetActiveLocations();

    /// <summary>Get a connected player.</summary>
    /// <param name="id">The player's unique ID.</param>
    /// <returns>Returns the connected player, or <c>null</c> if no such player is connected.</returns>
    IMultiplayerPeer? GetConnectedPlayer(long id);

    /// <summary>Get all connected players.</summary>
    IEnumerable<IMultiplayerPeer> GetConnectedPlayers();

    /// <summary>Send a message to mods installed by connected players.</summary>
    /// <typeparam name="TMessage">The data type. This can be a class with a default constructor, or a value type.</typeparam>
    /// <param name="message">The data to send over the network.</param>
    /// <param name="messageType">A message type which receiving mods can use to decide whether it's the one they want to handle, like <c>SetPlayerLocation</c>. This doesn't need to be globally unique, since mods should check the originating mod ID.</param>
    /// <param name="modIDs">The mod IDs which should receive the message on the destination computers, or <c>null</c> for all mods. Specifying mod IDs is recommended to improve performance, unless it's a general-purpose broadcast.</param>
    /// <param name="playerIDs">The <see cref="Farmer.UniqueMultiplayerID" /> values for the players who should receive the message, or <c>null</c> for all players. If you don't need to broadcast to all players, specifying player IDs is recommended to reduce latency.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="message"/> or <paramref name="messageType" /> is null.</exception>
    void SendMessage<TMessage>(TMessage message, string messageType, string[]? modIDs = null, long[]? playerIDs = null);
}
