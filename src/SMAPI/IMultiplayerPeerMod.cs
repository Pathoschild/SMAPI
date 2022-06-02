namespace StardewModdingAPI;

/// <summary>Metadata about a mod installed by a connected player.</summary>
public interface IMultiplayerPeerMod
{
    /// <summary>The mod's display name.</summary>
    string Name { get; }

    /// <summary>The unique mod ID.</summary>
    string ID { get; }

    /// <summary>The mod version.</summary>
    ISemanticVersion Version { get; }
}
