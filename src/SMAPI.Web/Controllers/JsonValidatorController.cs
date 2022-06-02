using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using StardewModdingAPI.Web.Framework;
using StardewModdingAPI.Web.Framework.Storage;
using StardewModdingAPI.Web.ViewModels.JsonValidator;

namespace StardewModdingAPI.Web.Controllers;

/// <summary>Provides a web UI for validating JSON schemas.</summary>
internal class JsonValidatorController : Controller
{
    /*********
    ** Fields
    *********/
    /// <summary>Provides access to raw data storage.</summary>
    private readonly IStorageProvider Storage;

    /// <summary>The supported JSON schemas (names indexed by ID).</summary>
    private readonly IDictionary<string, string> SchemaFormats = new Dictionary<string, string>
    {
        ["none"] = "None",
        ["manifest"] = "SMAPI: manifest",
        ["i18n"] = "SMAPI: translations (i18n)",
        ["content-patcher"] = "Content Patcher"
    };

    /// <summary>The schema ID to use if none was specified.</summary>
    private string DefaultSchemaID = "none";

    /// <summary>A token in an error message which indicates that the child errors should be displayed instead.</summary>
    private readonly string TransparentToken = "$transparent";


    /*********
    ** Public methods
    *********/
    /***
    ** Constructor
    ***/
    /// <summary>Construct an instance.</summary>
    /// <param name="storage">Provides access to raw data storage.</param>
    public JsonValidatorController(IStorageProvider storage)
    {
        this.Storage = storage;
    }

    /***
    ** Web UI
    ***/
    /// <summary>Render the schema validator UI.</summary>
    /// <param name="schemaName">The schema name with which to validate the JSON, or 'edit' to return to the edit screen.</param>
    /// <param name="id">The stored file ID.</param>
    /// <param name="operation">The operation to perform for the selected log ID. This can be 'edit', 'renew', or any other value to view.</param>
    [HttpGet]
    [Route("json")]
    [Route("json/{schemaName}")]
    [Route("json/{schemaName}/{id}")]
    [Route("json/{schemaName}/{id}/{operation}")]
    public async Task<ViewResult> Index(string? schemaName = null, string? id = null, string? operation = null)
    {
        // parse arguments
        schemaName = this.NormalizeSchemaName(schemaName);
        operation = operation?.Trim().ToLower();
        bool hasId = !string.IsNullOrWhiteSpace(id);
        bool isEditView = !hasId || operation == "edit";
        bool renew = operation == "renew";

        // build result model
        var result = this.GetModel(id, schemaName, isEditView);
        if (!hasId)
            return this.View("Index", result);

        // fetch raw JSON
        StoredFileInfo file = await this.Storage.GetAsync(id!, renew);
        if (string.IsNullOrWhiteSpace(file.Content))
            return this.View("Index", result.SetUploadError("The JSON file seems to be empty."));
        result.SetContent(file.Content, expiry: file.Expiry, uploadWarning: file.Warning);

        // skip parsing if we're going to the edit screen
        if (isEditView)
            return this.View("Index", result);

        // parse JSON
        JToken parsed;
        {
            // load raw JSON
            var settings = new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                CommentHandling = CommentHandling.Load
            };
            try
            {
                parsed = JToken.Parse(file.Content, settings);
            }
            catch (JsonReaderException ex)
            {
                return this.View("Index", result.AddErrors(new JsonValidatorErrorModel(ex.LineNumber, ex.Path!, ex.Message, ErrorType.None)));
            }

            // format JSON
            string formatted = parsed.ToString(Formatting.Indented);
            result.SetContent(formatted, expiry: file.Expiry, uploadWarning: file.Warning);
            parsed = JToken.Parse(formatted); // update line number references
        }

        // skip if no schema selected
        if (schemaName == "none")
            return this.View("Index", result);

        // load schema
        JSchema schema;
        {
            FileInfo? schemaFile = this.FindSchemaFile(schemaName);
            if (schemaFile == null)
                return this.View("Index", result.SetParseError($"Invalid schema '{schemaName}'."));
            schema = JSchema.Parse(await System.IO.File.ReadAllTextAsync(schemaFile.FullName));
        }

        // get format doc URL
        result.FormatUrl = this.GetExtensionField<string>(schema, "@documentationUrl");

        // validate JSON
        parsed.IsValid(schema, out IList<ValidationError> rawErrors);
        var errors = rawErrors
            .SelectMany(this.GetErrorModels)
            .ToArray();
        return this.View("Index", result.AddErrors(errors));
    }

    /***
    ** JSON
    ***/
    /// <summary>Save raw JSON data.</summary>
    [HttpPost, AllowLargePosts]
    [Route("json")]
    public async Task<ActionResult> PostAsync(JsonValidatorRequestModel? request)
    {
        if (request == null)
            return this.View("Index", this.GetModel(null, null, isEditView: true).SetUploadError("The request seems to be invalid."));

        // normalize schema name
        string schemaName = this.NormalizeSchemaName(request.SchemaName);

        // get raw text
        string? input = request.Content;
        if (string.IsNullOrWhiteSpace(input))
            return this.View("Index", this.GetModel(null, schemaName, isEditView: true).SetUploadError("The JSON file seems to be empty."));

        // upload file
        UploadResult result = await this.Storage.SaveAsync(input);
        if (!result.Succeeded)
            return this.View("Index", this.GetModel(result.ID, schemaName, isEditView: true).SetContent(input, null).SetUploadError(result.UploadError));

        // redirect to view
        return this.Redirect(this.Url.PlainAction("Index", "JsonValidator", new { schemaName, id = result.ID })!);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Build a JSON validator model.</summary>
    /// <param name="pasteID">The stored file ID.</param>
    /// <param name="schemaName">The schema name with which the JSON was validated.</param>
    /// <param name="isEditView">Whether to show the edit view.</param>
    private JsonValidatorModel GetModel(string? pasteID, string? schemaName, bool isEditView)
    {
        return new JsonValidatorModel(pasteID, schemaName, this.SchemaFormats, isEditView);
    }

    /// <summary>Get a normalized schema name, or the <see cref="DefaultSchemaID"/> if blank.</summary>
    /// <param name="schemaName">The raw schema name to normalize.</param>
    private string NormalizeSchemaName(string? schemaName)
    {
        schemaName = schemaName?.Trim().ToLower();
        return !string.IsNullOrWhiteSpace(schemaName)
            ? schemaName
            : this.DefaultSchemaID;
    }

    /// <summary>Get the schema file given its unique ID.</summary>
    /// <param name="id">The schema ID.</param>
    private FileInfo? FindSchemaFile(string? id)
    {
        // normalize ID
        id = id?.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(id))
            return null;

        // get matching file
        DirectoryInfo schemaDir = new(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "schemas"));
        foreach (FileInfo file in schemaDir.EnumerateFiles("*.json"))
        {
            if (file.Name.Equals($"{id}.json"))
                return file;
        }

        return null;
    }

    /// <summary>Get view models representing a schema validation error and any child errors.</summary>
    /// <param name="error">The error to represent.</param>
    private IEnumerable<JsonValidatorErrorModel> GetErrorModels(ValidationError error)
    {
        // skip through transparent errors
        if (this.IsTransparentError(error))
        {
            foreach (JsonValidatorErrorModel model in error.ChildErrors.SelectMany(this.GetErrorModels))
                yield return model;
            yield break;
        }

        // get message
        string? message = this.GetOverrideError(error);
        if (message == null || message == this.TransparentToken)
            message = this.FlattenErrorMessage(error);

        // build model
        yield return new JsonValidatorErrorModel(error.LineNumber, error.Path, message, error.ErrorType);
    }

    /// <summary>Get a flattened, human-readable message for a schema validation error and any child errors.</summary>
    /// <param name="error">The error to represent.</param>
    /// <param name="indent">The indentation level to apply for inner errors.</param>
    private string FlattenErrorMessage(ValidationError error, int indent = 0)
    {
        // get override
        string? message = this.GetOverrideError(error);
        if (message != null && message != this.TransparentToken)
            return message;

        // skip through transparent errors
        if (this.IsTransparentError(error))
            error = error.ChildErrors[0];

        // get friendly representation of main error
        message = error.Message;
        switch (error.ErrorType)
        {
            case ErrorType.Const:
                message = $"Invalid value. Found '{error.Value}', but expected '{error.Schema.Const}'.";
                break;

            case ErrorType.Enum:
                message = $"Invalid value. Found '{error.Value}', but expected one of '{string.Join("', '", error.Schema.Enum)}'.";
                break;

            case ErrorType.Required:
                message = $"Missing required fields: {string.Join(", ", (List<string>)error.Value!)}.";
                break;
        }

        // add inner errors
        foreach (ValidationError childError in error.ChildErrors)
            message += "\n" + "".PadLeft(indent * 2, ' ') + $"==> {childError.Path}: " + this.FlattenErrorMessage(childError, indent + 1);
        return message;
    }

    /// <summary>Get whether a validation error should be omitted in favor of its child errors in user-facing error messages.</summary>
    /// <param name="error">The error to check.</param>
    private bool IsTransparentError(ValidationError error)
    {
        if (!error.ChildErrors.Any())
            return false;

        string? @override = this.GetOverrideError(error);
        return
            @override == this.TransparentToken
            || (error.ErrorType == ErrorType.Then && @override == null);
    }

    /// <summary>Get an override error from the JSON schema, if any.</summary>
    /// <param name="error">The schema validation error.</param>
    private string? GetOverrideError(ValidationError error)
    {
        string? GetRawOverrideError()
        {
            // get override errors
            IDictionary<string, string?>? errors = this.GetExtensionField<Dictionary<string, string?>>(error.Schema, "@errorMessages");
            if (errors == null)
                return null;
            errors = new Dictionary<string, string?>(errors, StringComparer.OrdinalIgnoreCase);

            // match error by type and message
            foreach ((string target, string? errorMessage) in errors)
            {
                if (!target.Contains(":"))
                    continue;

                string[] parts = target.Split(':', 2);
                if (parts[0].Equals(error.ErrorType.ToString(), StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(error.Message, parts[1]))
                    return errorMessage?.Trim();
            }

            // match by type
            return errors.TryGetValue(error.ErrorType.ToString(), out string? message)
                ? message?.Trim()
                : null;
        }

        return GetRawOverrideError()
            ?.Replace("@value", this.FormatValue(error.Value));
    }

    /// <summary>Get an extension field from a JSON schema.</summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="schema">The schema whose extension fields to search.</param>
    /// <param name="key">The case-insensitive field key.</param>
    private T? GetExtensionField<T>(JSchema schema, string key)
    {
        foreach ((string curKey, JToken value) in schema.ExtensionData)
        {
            if (curKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                return value.ToObject<T>();
        }

        return default;
    }

    /// <summary>Format a schema value for display.</summary>
    /// <param name="value">The value to format.</param>
    private string FormatValue(object? value)
    {
        return value switch
        {
            List<string> list => string.Join(", ", list),
            _ => value?.ToString() ?? "null"
        };
    }
}