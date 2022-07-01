using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Web.Framework.LogParsing.Models;

/// <summary>Metadata about a mod or content pack in the log.</summary>
public class LogModInfo
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod name.</summary>
    public string Name { get; }

    /// <summary>The mod author.</summary>
    public string Author { get; }

    /// <summary>The mod version.</summary>
    public string Version { get; private set; }

    /// <summary>The mod description.</summary>
    public string Description { get; }

    /// <summary>The update version.</summary>
    public string? UpdateVersion { get; private set; }

    /// <summary>The update link.</summary>
    public string? UpdateLink { get; private set; }

    /// <summary>The name of the mod for which this is a content pack (if applicable).</summary>
    public string? ContentPackFor { get; }

    /// <summary>The number of errors logged by this mod.</summary>
    public int Errors { get; set; }

    /// <summary>Whether the mod was loaded into the game.</summary>
    public bool Loaded { get; }

    /// <summary>Whether the mod has an update available.</summary>
    [MemberNotNullWhen(true, nameof(LogModInfo.UpdateVersion), nameof(LogModInfo.UpdateLink))]
    public bool HasUpdate => this.UpdateVersion != null && this.Version != this.UpdateVersion;

    /// <summary>Whether this is an actual mod (rather than a special entry for SMAPI or the game itself).</summary>
    public bool IsMod { get; }

    /// <summary>Whether this is a C# code mod.</summary>
    public bool IsCodeMod { get; }

    /// <summary>Whether this is a content pack for another mod.</summary>
    [MemberNotNullWhen(true, nameof(LogModInfo.ContentPackFor))]
    public bool IsContentPack { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="name">The mod name.</param>
    /// <param name="author">The mod author.</param>
    /// <param name="version">The mod version.</param>
    /// <param name="description">The mod description.</param>
    /// <param name="updateVersion">The update version.</param>
    /// <param name="updateLink">The update link.</param>
    /// <param name="contentPackFor">The name of the mod for which this is a content pack (if applicable).</param>
    /// <param name="errors">The number of errors logged by this mod.</param>
    /// <param name="loaded">Whether the mod was loaded into the game.</param>
    /// <param name="isMod">Whether this is an actual mod (instead of a special entry for SMAPI or the game).</param>
    public LogModInfo(string name, string author, string version, string description, string? updateVersion = null, string? updateLink = null, string? contentPackFor = null, int errors = 0, bool loaded = true, bool isMod = true)
    {
        this.Name = name;
        this.Author = author;
        this.Version = version;
        this.Description = description;
        this.UpdateVersion = updateVersion;
        this.UpdateLink = updateLink;
        this.ContentPackFor = contentPackFor;
        this.Errors = errors;
        this.Loaded = loaded;

        if (isMod)
        {
            this.IsMod = true;
            this.IsContentPack = !string.IsNullOrWhiteSpace(this.ContentPackFor);
            this.IsCodeMod = !this.IsContentPack;
        }
    }

    /// <summary>Add an update alert for this mod.</summary>
    /// <param name="updateVersion">The update version.</param>
    /// <param name="updateLink">The update link.</param>
    public void SetUpdate(string updateVersion, string updateLink)
    {
        this.UpdateVersion = updateVersion;
        this.UpdateLink = updateLink;
    }

    /// <summary>Override the version number, for cases like SMAPI itself where the version is only known later during parsing.</summary>
    /// <param name="version">The new mod version.</param>
    public void OverrideVersion(string version)
    {
        this.Version = version;
    }
}
