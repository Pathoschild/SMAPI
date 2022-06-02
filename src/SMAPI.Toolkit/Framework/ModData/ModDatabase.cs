using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Toolkit.Framework.ModData;

/// <summary>Handles access to SMAPI's internal mod metadata list.</summary>
public class ModDatabase
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying mod data records indexed by default display name.</summary>
    private readonly ModDataRecord[] Records;

    /// <summary>Get an update URL for an update key (if valid).</summary>
    private readonly Func<string, string?> GetUpdateUrl;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an empty instance.</summary>
    public ModDatabase()
        : this(Array.Empty<ModDataRecord>(), _ => null) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="records">The underlying mod data records indexed by default display name.</param>
    /// <param name="getUpdateUrl">Get an update URL for an update key (if valid).</param>
    public ModDatabase(IEnumerable<ModDataRecord> records, Func<string, string?> getUpdateUrl)
    {
        this.Records = records.ToArray();
        this.GetUpdateUrl = getUpdateUrl;
    }

    /// <summary>Get all mod data records.</summary>
    public IEnumerable<ModDataRecord> GetAll()
    {
        return this.Records;
    }

    /// <summary>Get a mod data record.</summary>
    /// <param name="modID">The unique mod ID.</param>
    public ModDataRecord? Get(string? modID)
    {
        return !string.IsNullOrWhiteSpace(modID)
            ? this.Records.FirstOrDefault(p => p.HasID(modID))
            : null;
    }

    /// <summary>Get the mod page URL for a mod (if available).</summary>
    /// <param name="id">The unique mod ID.</param>
    public string? GetModPageUrlFor(string? id)
    {
        // get update key
        ModDataRecord? record = this.Get(id);
        ModDataField? updateKeyField = record?.Fields.FirstOrDefault(p => p.Key == ModDataFieldKey.UpdateKey);
        if (updateKeyField == null)
            return null;

        // get update URL
        return this.GetUpdateUrl(updateKeyField.Value);
    }
}
