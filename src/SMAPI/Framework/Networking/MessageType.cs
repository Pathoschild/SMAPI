using StardewValley;

namespace StardewModdingAPI.Framework.Networking;

/// <summary>Network message types recognized by SMAPI and Stardew Valley.</summary>
internal enum MessageType : byte
{
    /*********
    ** SMAPI
    *********/
    /// <summary>A data message intended for mods to consume.</summary>
    ModMessage = 254,

    /// <summary>Metadata context about a player synced by SMAPI.</summary>
    ModContext = 255,

    /*********
    ** Vanilla
    *********/
    /// <summary>Metadata about the host server sent to a farmhand.</summary>
    ServerIntroduction = Multiplayer.serverIntroduction,

    /// <summary>Metadata about a player sent to a farmhand or server.</summary>
    PlayerIntroduction = Multiplayer.playerIntroduction
}
