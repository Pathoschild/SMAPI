using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StardewModdingAPI.Framework.Networking;

/// <summary>The metadata for a mod message.</summary>
internal class ModMessageModel
{
    /*********
    ** Accessors
    *********/
    /****
    ** Origin
    ****/
    /// <summary>The unique ID of the player who broadcast the message.</summary>
    public long FromPlayerID { get; }

    /// <summary>The unique ID of the mod which broadcast the message.</summary>
    public string FromModID { get; }

    /****
    ** Destination
    ****/
    /// <summary>The players who should receive the message.</summary>
    public long[]? ToPlayerIDs { get; init; }

    /// <summary>The mods which should receive the message, or <c>null</c> for all mods.</summary>
    public string[]? ToModIDs { get; }

    /// <summary>A message type which receiving mods can use to decide whether it's the one they want to handle, like <c>SetPlayerLocation</c>. This doesn't need to be globally unique, since mods should check the originating mod ID.</summary>
    public string Type { get; }

    /// <summary>The custom mod data being broadcast.</summary>
    public JToken Data { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fromPlayerID">The unique ID of the player who broadcast the message.</param>
    /// <param name="fromModID">The unique ID of the mod which broadcast the message.</param>
    /// <param name="toPlayerIDs">The players who should receive the message, or <c>null</c> for all players.</param>
    /// <param name="toModIDs">The mods which should receive the message, or <c>null</c> for all mods.</param>
    /// <param name="type">A message type which receiving mods can use to decide whether it's the one they want to handle, like <c>SetPlayerLocation</c>. This doesn't need to be globally unique, since mods should check the originating mod ID.</param>
    /// <param name="data">The custom mod data being broadcast.</param>
    [JsonConstructor]
    public ModMessageModel(long fromPlayerID, string fromModID, long[]? toPlayerIDs, string[]? toModIDs, string type, JToken data)
    {
        this.FromPlayerID = fromPlayerID;
        this.FromModID = fromModID;
        this.ToPlayerIDs = toPlayerIDs;
        this.ToModIDs = toModIDs;
        this.Type = type;
        this.Data = data;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="message">The message to clone.</param>
    public ModMessageModel(ModMessageModel message)
    {
        this.FromPlayerID = message.FromPlayerID;
        this.FromModID = message.FromModID;
        this.ToPlayerIDs = message.ToPlayerIDs?.ToArray();
        this.ToModIDs = message.ToModIDs?.ToArray();
        this.Type = message.Type;
        this.Data = message.Data;
    }
}
