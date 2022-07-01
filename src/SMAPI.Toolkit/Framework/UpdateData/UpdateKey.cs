using System;
using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Toolkit.Framework.UpdateData;

/// <summary>A namespaced mod ID which uniquely identifies a mod within a mod repository.</summary>
public class UpdateKey : IEquatable<UpdateKey>
{
    /*********
    ** Accessors
    *********/
    /// <summary>The raw update key text.</summary>
    public string RawText { get; }

    /// <summary>The mod site containing the mod.</summary>
    public ModSiteKey Site { get; }

    /// <summary>The mod ID within the repository.</summary>
    public string? ID { get; }

    /// <summary>If specified, a substring in download names/descriptions to match.</summary>
    public string? Subkey { get; }

    /// <summary>Whether the update key seems to be valid.</summary>
#if NET5_0_OR_GREATER
        [MemberNotNullWhen(true, nameof(UpdateKey.ID))]
#endif
    public bool LooksValid { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="rawText">The raw update key text.</param>
    /// <param name="site">The mod site containing the mod.</param>
    /// <param name="id">The mod ID within the site.</param>
    /// <param name="subkey">If specified, a substring in download names/descriptions to match.</param>
    public UpdateKey(string? rawText, ModSiteKey site, string? id, string? subkey)
    {
        this.RawText = rawText?.Trim() ?? string.Empty;
        this.Site = site;
        this.ID = id?.Trim();
        this.Subkey = subkey?.Trim();
        this.LooksValid =
            site != ModSiteKey.Unknown
            && !string.IsNullOrWhiteSpace(id);
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="site">The mod site containing the mod.</param>
    /// <param name="id">The mod ID within the site.</param>
    /// <param name="subkey">If specified, a substring in download names/descriptions to match.</param>
    public UpdateKey(ModSiteKey site, string? id, string? subkey)
        : this(UpdateKey.GetString(site, id, subkey), site, id, subkey) { }

    /// <summary>Parse a raw update key.</summary>
    /// <param name="raw">The raw update key to parse.</param>
    public static UpdateKey Parse(string? raw)
    {
        // extract site + ID
        string? rawSite;
        string? id;
        {
            string[]? parts = raw?.Trim().Split(':');
            if (parts?.Length != 2)
                return new UpdateKey(raw, ModSiteKey.Unknown, null, null);

            rawSite = parts[0].Trim();
            id = parts[1].Trim();
        }
        if (string.IsNullOrWhiteSpace(id))
            id = null;

        // extract subkey
        string? subkey = null;
        if (id != null)
        {
            string[] parts = id.Split('@');
            if (parts.Length == 2)
            {
                id = parts[0].Trim();
                subkey = $"@{parts[1]}".Trim();
            }
        }

        // parse
        if (!Enum.TryParse(rawSite, true, out ModSiteKey site))
            return new UpdateKey(raw, ModSiteKey.Unknown, id, subkey);
        if (id == null)
            return new UpdateKey(raw, site, null, subkey);

        return new UpdateKey(raw, site, id, subkey);
    }

    /// <summary>Parse a raw update key if it's valid.</summary>
    /// <param name="raw">The raw update key to parse.</param>
    /// <param name="parsed">The parsed update key, if valid.</param>
    /// <returns>Returns whether the update key was successfully parsed.</returns>
    public static bool TryParse(string raw, out UpdateKey parsed)
    {
        parsed = UpdateKey.Parse(raw);
        return parsed.LooksValid;
    }

    /// <summary>Get a string that represents the current object.</summary>
    public override string ToString()
    {
        return this.LooksValid
            ? UpdateKey.GetString(this.Site, this.ID, this.Subkey)
            : this.RawText;
    }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    public bool Equals(UpdateKey? other)
    {
        if (!this.LooksValid)
        {
            return
                other?.LooksValid == false
                && this.RawText.Equals(other.RawText, StringComparison.OrdinalIgnoreCase);
        }

        return
            other != null
            && this.Site == other.Site
            && string.Equals(this.ID, other.ID, StringComparison.OrdinalIgnoreCase)
            && string.Equals(this.Subkey, other.Subkey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="obj">The object to compare with the current object.</param>
    public override bool Equals(object? obj)
    {
        return obj is UpdateKey other && this.Equals(other);
    }

    /// <summary>Serves as the default hash function. </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return this.ToString().ToLower().GetHashCode();
    }

    /// <summary>Get the string representation of an update key.</summary>
    /// <param name="site">The mod site containing the mod.</param>
    /// <param name="id">The mod ID within the repository.</param>
    /// <param name="subkey">If specified, a substring in download names/descriptions to match.</param>
    public static string GetString(ModSiteKey site, string? id, string? subkey = null)
    {
        return $"{site}:{id}{subkey}".Trim();
    }
}
