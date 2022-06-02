namespace StardewModdingAPI.Toolkit.Serialization.Converters;

/// <summary>Handles deserialization of <see cref="ISemanticVersion"/>, allowing for non-standard extensions.</summary>
internal class NonStandardSemanticVersionConverter : SemanticVersionConverter
{
    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public NonStandardSemanticVersionConverter()
    {
        this.AllowNonStandard = true;
    }
}
