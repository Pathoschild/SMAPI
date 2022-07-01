using System;

namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

/// <summary>The data overrides to apply to matching mods.</summary>
public class WikiDataOverrideEntry
{
    /*********
    ** Accessors
    *********/
    /// <summary>The unique mod IDs for the mods to override.</summary>
    public string[] Ids { get; set; } = Array.Empty<string>();

    /// <summary>Maps local versions to a semantic version for update checks.</summary>
    public ChangeDescriptor? ChangeLocalVersions { get; set; }

    /// <summary>Maps remote versions to a semantic version for update checks.</summary>
    public ChangeDescriptor? ChangeRemoteVersions { get; set; }

    /// <summary>Update keys to add (optionally prefixed by '+'), remove (prefixed by '-'), or replace.</summary>
    public ChangeDescriptor? ChangeUpdateKeys { get; set; }

    /// <summary>Whether the entry has any changes.</summary>
    public bool HasChanges =>
        this.ChangeLocalVersions?.HasChanges == true
        || this.ChangeRemoteVersions?.HasChanges == true
        || this.ChangeUpdateKeys?.HasChanges == true;
}
