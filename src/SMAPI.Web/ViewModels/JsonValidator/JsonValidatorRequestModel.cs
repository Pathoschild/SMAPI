namespace StardewModdingAPI.Web.ViewModels.JsonValidator;

/// <summary>The view model for a JSON validation request.</summary>
public class JsonValidatorRequestModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The schema name with which to validate the JSON.</summary>
    public string? SchemaName { get; set; }

    /// <summary>The raw content to validate.</summary>
    public string? Content { get; set; }
}
