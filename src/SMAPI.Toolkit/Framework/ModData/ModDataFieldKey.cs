namespace StardewModdingAPI.Toolkit.Framework.ModData;

/// <summary>The valid field keys.</summary>
public enum ModDataFieldKey
{
    /// <summary>A manifest update key.</summary>
    UpdateKey,

    /// <summary>The mod's predefined compatibility status.</summary>
    Status,

    /// <summary>A reason phrase for the <see cref="Status"/>, or <c>null</c> to use the default reason.</summary>
    StatusReasonPhrase,

    /// <summary>Technical details shown in TRACE logs for the <see cref="Status"/>, or <c>null</c> to omit it.</summary>
    StatusReasonDetails
}
