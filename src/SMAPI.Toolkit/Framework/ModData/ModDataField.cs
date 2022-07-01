using System.Linq;

namespace StardewModdingAPI.Toolkit.Framework.ModData;

/// <summary>A versioned mod metadata field.</summary>
public class ModDataField
{
    /*********
    ** Accessors
    *********/
    /// <summary>The field key.</summary>
    public ModDataFieldKey Key { get; }

    /// <summary>The field value.</summary>
    public string Value { get; }

    /// <summary>Whether this field should only be applied if it's not already set.</summary>
    public bool IsDefault { get; }

    /// <summary>The lowest version in the range, or <c>null</c> for all past versions.</summary>
    public ISemanticVersion? LowerVersion { get; }

    /// <summary>The highest version in the range, or <c>null</c> for all future versions.</summary>
    public ISemanticVersion? UpperVersion { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="key">The field key.</param>
    /// <param name="value">The field value.</param>
    /// <param name="isDefault">Whether this field should only be applied if it's not already set.</param>
    /// <param name="lowerVersion">The lowest version in the range, or <c>null</c> for all past versions.</param>
    /// <param name="upperVersion">The highest version in the range, or <c>null</c> for all future versions.</param>
    public ModDataField(ModDataFieldKey key, string value, bool isDefault, ISemanticVersion? lowerVersion, ISemanticVersion? upperVersion)
    {
        this.Key = key;
        this.Value = value;
        this.IsDefault = isDefault;
        this.LowerVersion = lowerVersion;
        this.UpperVersion = upperVersion;
    }

    /// <summary>Get whether this data field applies for the given manifest.</summary>
    /// <param name="manifest">The mod manifest.</param>
    public bool IsMatch(IManifest? manifest)
    {
        return
            manifest?.Version != null // ignore invalid manifest
            && (!this.IsDefault || !this.HasFieldValue(manifest, this.Key))
            && (this.LowerVersion == null || !manifest.Version.IsOlderThan(this.LowerVersion))
            && (this.UpperVersion == null || !manifest.Version.IsNewerThan(this.UpperVersion));
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get whether a manifest field has a meaningful value for the purposes of enforcing <see cref="IsDefault"/>.</summary>
    /// <param name="manifest">The mod manifest.</param>
    /// <param name="key">The field key matching <see cref="ModDataFieldKey"/>.</param>
    private bool HasFieldValue(IManifest manifest, ModDataFieldKey key)
    {
        switch (key)
        {
            // update key
            case ModDataFieldKey.UpdateKey:
                return manifest.UpdateKeys.Any(p => !string.IsNullOrWhiteSpace(p));

            // non-manifest fields
            case ModDataFieldKey.StatusReasonPhrase:
            case ModDataFieldKey.StatusReasonDetails:
            case ModDataFieldKey.Status:
                return false;

            default:
                return false;
        }
    }
}
