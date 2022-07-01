using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Toolkit.Framework.ModData;

/// <summary>The parsed mod metadata from SMAPI's internal mod list.</summary>
public class ModDataRecord
{
    /*********
    ** Accessors
    *********/
    /// <summary>The mod's default display name.</summary>
    public string DisplayName { get; }

    /// <summary>The mod's current unique ID.</summary>
    public string ID { get; }

    /// <summary>The former mod IDs (if any).</summary>
    public string[] FormerIDs { get; }

    /// <summary>The mod warnings to suppress, even if they'd normally be shown.</summary>
    public ModWarning SuppressWarnings { get; }

    /// <summary>The versioned field data.</summary>
    public ModDataField[] Fields { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="displayName">The mod's default display name.</param>
    /// <param name="model">The raw data model.</param>
    internal ModDataRecord(string displayName, ModDataModel model)
    {
        this.DisplayName = displayName;
        this.ID = model.ID;
        this.FormerIDs = model.GetFormerIDs().ToArray();
        this.SuppressWarnings = model.SuppressWarnings;
        this.Fields = model.GetFields().ToArray();
    }

    /// <summary>Get whether the mod has (or previously had) the given ID.</summary>
    /// <param name="id">The mod ID.</param>
    public bool HasID(string id)
    {
        // try main ID
        if (this.ID.Equals(id, StringComparison.OrdinalIgnoreCase))
            return true;

        // try former IDs
        foreach (string formerID in this.FormerIDs)
        {
            if (formerID.Equals(id, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Get the possible mod IDs.</summary>
    public IEnumerable<string> GetIDs()
    {
        return this.FormerIDs
            .Concat(new[] { this.ID })
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct();
    }

    /// <summary>Get the default update key for this mod, if any.</summary>
    public string? GetDefaultUpdateKey()
    {
        string? updateKey = this.Fields.FirstOrDefault(p => p.Key == ModDataFieldKey.UpdateKey && p.IsDefault)?.Value;
        return !string.IsNullOrWhiteSpace(updateKey)
            ? updateKey
            : null;
    }

    /// <summary>Get a parsed representation of the <see cref="ModDataRecord.Fields"/> which match a given manifest.</summary>
    /// <param name="manifest">The manifest to match.</param>
    public ModDataRecordVersionedFields GetVersionedFields(IManifest? manifest)
    {
        ModDataRecordVersionedFields parsed = new(this);
        foreach (ModDataField field in this.Fields.Where(field => field.IsMatch(manifest)))
        {
            switch (field.Key)
            {
                // update key
                case ModDataFieldKey.UpdateKey:
                    parsed.UpdateKey = field.Value;
                    break;

                // status
                case ModDataFieldKey.Status:
                    parsed.Status = (ModStatus)Enum.Parse(typeof(ModStatus), field.Value, ignoreCase: true);
                    parsed.StatusUpperVersion = field.UpperVersion;
                    break;

                // status reason phrase
                case ModDataFieldKey.StatusReasonPhrase:
                    parsed.StatusReasonPhrase = field.Value;
                    break;

                // status technical reason
                case ModDataFieldKey.StatusReasonDetails:
                    parsed.StatusReasonDetails = field.Value;
                    break;
            }
        }

        return parsed;
    }
}
