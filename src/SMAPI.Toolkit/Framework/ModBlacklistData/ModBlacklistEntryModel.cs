namespace StardewModdingAPI.Toolkit.Framework.ModBlacklistData;

/// <summary>A mod entry in the <see cref="ModBlacklistModel"/>.</summary>
public class ModBlacklistEntryModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The manifest <c>UniqueID</c> values to block (if any).</summary>
    public string? Id { get; }

    /// <summary>The MD5 hash of the entry DLL to block (if any).</summary>
    /// <remarks>This should be omitted if the <see cref="Id"/> is sufficiently unique, to block all versions of a mod.</remarks>
    public string? EntryDllHash { get; }

    /// <summary>A player-friendly explanation of why the mod is blocked and what they should do next.</summary>
    /// <remarks>This should usually reuse one of the existing messages for consistency.</remarks>
    public string? Message { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="id"><inheritdoc cref="Id" path="/summary"/></param>
    /// <param name="entryDllHash"><inheritdoc cref="EntryDllHash" path="/summary"/></param>
    /// <param name="message"><inheritdoc cref="Message" path="/summary"/></param>
    public ModBlacklistEntryModel(string? id, string? entryDllHash, string message)
    {
        this.Id = id;
        this.EntryDllHash = entryDllHash;
        this.Message = message;
    }
}
