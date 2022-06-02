using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Utilities;
using StardewModdingAPI.Web.Framework.LogParsing.Models;

namespace StardewModdingAPI.Web.ViewModels;

/// <summary>The view model for the log parser page.</summary>
public class LogParserModel
{
    /*********
    ** Fields
    *********/
    /// <summary>A regex pattern matching characters to remove from a mod name to create the slug ID.</summary>
    private readonly Regex SlugInvalidCharPattern = new("[^a-z0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);


    /*********
    ** Accessors
    *********/
    /// <summary>The paste ID.</summary>
    public string? PasteID { get; }

    /// <summary>The viewer's detected OS, if known.</summary>
    public Platform? DetectedPlatform { get; }

    /// <summary>The parsed log info.</summary>
    public ParsedLog? ParsedLog { get; private set; }

    /// <summary>Whether to show the raw unparsed log.</summary>
    public bool ShowRaw { get; private set; }

    /// <summary>A non-blocking warning while uploading the log.</summary>
    public string? UploadWarning { get; set; }

    /// <summary>An error which occurred while uploading the log.</summary>
    public string? UploadError { get; set; }

    /// <summary>An error which occurred while parsing the log file.</summary>
    public string? ParseError => this.ParsedLog?.Error;

    /// <summary>When the uploaded file will no longer be available.</summary>
    public DateTimeOffset? Expiry { get; set; }

    /// <summary>Whether parsed log data is available.</summary>
    [MemberNotNullWhen(true, nameof(LogParserModel.PasteID), nameof(LogParserModel.ParsedLog))]
    public bool HasLog => this.ParsedLog != null;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="pasteID">The paste ID.</param>
    /// <param name="platform">The viewer's detected OS, if known.</param>
    public LogParserModel(string? pasteID, Platform? platform)
    {
        this.PasteID = pasteID;
        this.DetectedPlatform = platform;
        this.ParsedLog = null;
        this.ShowRaw = false;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="pasteId">The paste ID.</param>
    /// <param name="detectedPlatform">The viewer's detected OS, if known.</param>
    /// <param name="parsedLog">The parsed log info.</param>
    /// <param name="showRaw">Whether to show the raw unparsed log.</param>
    /// <param name="uploadWarning">A non-blocking warning while uploading the log.</param>
    /// <param name="uploadError">An error which occurred while uploading the log.</param>
    /// <param name="expiry">When the uploaded file will no longer be available.</param>
    [JsonConstructor]
    public LogParserModel(string pasteId, Platform? detectedPlatform, ParsedLog? parsedLog, bool showRaw, string? uploadWarning, string? uploadError, DateTime? expiry)
    {
        this.PasteID = pasteId;
        this.DetectedPlatform = detectedPlatform;
        this.ParsedLog = parsedLog;
        this.ShowRaw = showRaw;
        this.UploadWarning = uploadWarning;
        this.UploadError = uploadError;
        this.Expiry = expiry;
    }

    /// <summary>Set the log parser result.</summary>
    /// <param name="parsedLog">The parsed log info.</param>
    /// <param name="showRaw">Whether to show the raw unparsed log.</param>
    public LogParserModel SetResult(ParsedLog parsedLog, bool showRaw)
    {
        this.ParsedLog = parsedLog;
        this.ShowRaw = showRaw;

        return this;
    }

    /// <summary>Get all content packs in the log grouped by the mod they're for.</summary>
    public IDictionary<string, LogModInfo[]> GetContentPacksByMod()
    {
        // get all mods & content packs
        LogModInfo[]? mods = this.ParsedLog?.Mods;
        if (mods == null || !mods.Any())
            return new Dictionary<string, LogModInfo[]>();

        // group by mod
        return mods
            .Where(mod => mod.IsContentPack)
            .GroupBy(mod => mod.ContentPackFor!)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    /// <summary>Get a sanitized mod name that's safe to use in anchors, attributes, and URLs.</summary>
    /// <param name="modName">The mod name.</param>
    public string GetSlug(string modName)
    {
        return this.SlugInvalidCharPattern.Replace(modName, "");
    }
}