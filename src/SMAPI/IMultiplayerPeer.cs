using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI;

/// <summary>Metadata about a connected player.</summary>
public interface IMultiplayerPeer
{
    /*********
    ** Accessors
    *********/
    /// <summary>The player's unique ID.</summary>
    long PlayerID { get; }

    /// <summary>Whether this is a connection to the host player.</summary>
    bool IsHost { get; }

    /// <summary>Whether this a local player on the same computer in split-screen mote.</summary>
    bool IsSplitScreen { get; }

    /// <summary>Whether the player has SMAPI installed.</summary>
    [MemberNotNullWhen(true, nameof(IMultiplayerPeer.Platform), nameof(IMultiplayerPeer.GameVersion), nameof(IMultiplayerPeer.ApiVersion), nameof(IMultiplayerPeer.Mods))]
    bool HasSmapi { get; }

    /// <summary>The player's screen ID, if applicable.</summary>
    /// <remarks>See <see cref="Context.ScreenId"/> for details. This is only visible to players in split-screen mode. A remote player won't see this value, even if the other players are in split-screen mode.</remarks>
    int? ScreenID { get; }

    /// <summary>The player's OS platform, if <see cref="HasSmapi"/> is true.</summary>
    GamePlatform? Platform { get; }

    /// <summary>The installed version of Stardew Valley, if <see cref="HasSmapi"/> is true.</summary>
    ISemanticVersion? GameVersion { get; }

    /// <summary>The installed version of SMAPI, if <see cref="HasSmapi"/> is true.</summary>
    ISemanticVersion? ApiVersion { get; }

    /// <summary>The installed mods, if <see cref="HasSmapi"/> is true.</summary>
    IEnumerable<IMultiplayerPeerMod> Mods { get; }


    /*********
    ** Methods
    *********/
    /// <summary>Get metadata for a mod installed by the player.</summary>
    /// <param name="id">The unique mod ID.</param>
    /// <returns>Returns the mod info, or <c>null</c> if the player doesn't have that mod.</returns>
    IMultiplayerPeerMod? GetMod(string? id);
}
