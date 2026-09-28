namespace StardewModdingAPI.Toolkit.Framework.ModBlacklistData;

/// <summary>A loose file entry in the <see cref="ModBlacklistModel"/>.</summary>
public class LooseFileBlacklistEntryModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The file name to block (if any).</summary>
    public string? Name { get; }

    /// <summary>The file extension to block (if any), including the dot.</summary>
    public string? Extension { get; }

    /// <summary>The MD5 hash to block (if any).</summary>
    public string? Hash { get; }

    /// <summary>A player-friendly explanation of why the mod is blocked and what they should do next.</summary>
    public string? Message { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
    /// <param name="extension"><inheritdoc cref="Extension" path="/summary"/></param>
    /// <param name="hash"><inheritdoc cref="Hash" path="/summary"/></param>
    /// <param name="message"><inheritdoc cref="Message" path="/summary"/></param>
    public LooseFileBlacklistEntryModel(string? name, string? extension, string? hash, string? message)
    {
        this.Name = name;
        this.Extension = extension;
        this.Hash = hash;
        this.Message = message;
    }
}
