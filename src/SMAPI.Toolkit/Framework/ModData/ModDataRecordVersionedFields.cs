namespace StardewModdingAPI.Toolkit.Framework.ModData;

/// <summary>The versioned fields from a <see cref="ModDataRecord"/> for a specific manifest.</summary>
public class ModDataRecordVersionedFields
{
    /*********
    ** Accessors
    *********/
    /// <summary>The underlying data record.</summary>
    public ModDataRecord DataRecord { get; }

    /// <summary>The update key to apply (if any).</summary>
    public string? UpdateKey { get; set; }

    /// <summary>The predefined compatibility status.</summary>
    public ModStatus Status { get; set; } = ModStatus.None;

    /// <summary>A reason phrase for the <see cref="Status"/>, or <c>null</c> to use the default reason.</summary>
    public string? StatusReasonPhrase { get; set; }

    /// <summary>Technical details shown in TRACE logs for the <see cref="Status"/>, or <c>null</c> to omit it.</summary>
    public string? StatusReasonDetails { get; set; }

    /// <summary>The upper version for which the <see cref="Status"/> applies (if any).</summary>
    public ISemanticVersion? StatusUpperVersion { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="dataRecord">The underlying data record.</param>
    public ModDataRecordVersionedFields(ModDataRecord dataRecord)
    {
        this.DataRecord = dataRecord;
    }
}
