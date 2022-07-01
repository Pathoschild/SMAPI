using Newtonsoft.Json.Schema;

namespace StardewModdingAPI.Web.ViewModels.JsonValidator;

/// <summary>The view model for a JSON validator error.</summary>
public class JsonValidatorErrorModel
{
    /*********
    ** Accessors
    *********/
    /// <summary>The line number on which the error occurred.</summary>
    public int Line { get; }

    /// <summary>The field path in the JSON file where the error occurred.</summary>
    public string? Path { get; }

    /// <summary>A human-readable description of the error.</summary>
    public string Message { get; }

    /// <summary>The schema error type.</summary>
    public ErrorType SchemaErrorType { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="line">The line number on which the error occurred.</param>
    /// <param name="path">The field path in the JSON file where the error occurred.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="schemaErrorType">The schema error type.</param>
    public JsonValidatorErrorModel(int line, string? path, string message, ErrorType schemaErrorType)
    {
        this.Line = line;
        this.Path = path;
        this.Message = message;
        this.SchemaErrorType = schemaErrorType;
    }
}
