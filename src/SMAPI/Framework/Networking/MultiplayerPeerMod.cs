using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Framework.Networking;

internal class MultiplayerPeerMod : IMultiplayerPeerMod
{
    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string ID { get; }

    /// <inheritdoc />
    public ISemanticVersion Version { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="mod">The mod metadata.</param>
    [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier", Justification = "The ID shouldn't be null, but we should handle it to avoid an error just in case.")]
    public MultiplayerPeerMod(RemoteContextModModel mod)
    {
        this.Name = mod.Name;
        this.ID = mod.ID?.Trim() ?? string.Empty;
        this.Version = mod.Version;
    }
}
