using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StardewModdingAPI.Toolkit.Utilities;
using StardewModdingAPI.Web.Framework;
using StardewModdingAPI.Web.Framework.LogParsing;
using StardewModdingAPI.Web.Framework.LogParsing.Models;
using StardewModdingAPI.Web.Framework.Storage;
using StardewModdingAPI.Web.ViewModels;

namespace StardewModdingAPI.Web.Controllers;

/// <summary>Provides a web UI and API for parsing SMAPI log files.</summary>
internal class LogParserController : Controller
{
    /*********
    ** Fields
    *********/
    /// <summary>Provides access to raw data storage.</summary>
    private readonly IStorageProvider Storage;


    /*********
    ** Public methods
    *********/
    /***
    ** Constructor
    ***/
    /// <summary>Construct an instance.</summary>
    /// <param name="storage">Provides access to raw data storage.</param>
    public LogParserController(IStorageProvider storage)
    {
        this.Storage = storage;
    }

    /***
    ** Web UI
    ***/
    /// <summary>Render the log parser UI.</summary>
    /// <param name="id">The stored file ID.</param>
    /// <param name="format">How to render the log view.</param>
    /// <param name="renew">Whether to reset the log expiry.</param>
    [HttpGet]
    [Route("log")]
    [Route("log/{id}")]
    public async Task<ActionResult> Index(string? id = null, LogViewFormat format = LogViewFormat.Default, bool renew = false)
    {
        // fresh page
        if (string.IsNullOrWhiteSpace(id))
            return this.View("Index", this.GetModel(id));

        // fetch log
        StoredFileInfo file = await this.Storage.GetAsync(id, renew);

        // render view
        switch (format)
        {
            case LogViewFormat.Default:
            case LogViewFormat.RawView:
            {
                ParsedLog log = file.Success
                    ? new LogParser().Parse(file.Content)
                    : new ParsedLog { IsValid = false, Error = file.Error };

                return this.View("Index", this.GetModel(id, uploadWarning: file.Warning, expiry: file.Expiry).SetResult(log, showRaw: format == LogViewFormat.RawView));
            }

            case LogViewFormat.RawDownload:
            {
                string content = file.Error ?? file.Content ?? string.Empty;
                return this.File(Encoding.UTF8.GetBytes(content), "plain/text", $"SMAPI log ({id}).txt");
            }

            default:
                throw new InvalidOperationException($"Unknown log view format '{format}'.");
        }
    }

    /***
    ** JSON
    ***/
    /// <summary>Save raw log data.</summary>
    [HttpPost, AllowLargePosts]
    [Route("log")]
    public async Task<ActionResult> PostAsync()
    {
        // get raw log text
        string? input = this.Request.Form["input"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(input))
            return this.View("Index", this.GetModel(null, uploadError: "The log file seems to be empty."));

        // upload log
        UploadResult uploadResult = await this.Storage.SaveAsync(input);
        if (!uploadResult.Succeeded)
            return this.View("Index", this.GetModel(null, uploadError: uploadResult.UploadError));

        // redirect to view
        return this.Redirect(this.Url.PlainAction("Index", "LogParser", new { id = uploadResult.ID })!);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Build a log parser model.</summary>
    /// <param name="pasteID">The stored file ID.</param>
    /// <param name="expiry">When the uploaded file will no longer be available.</param>
    /// <param name="uploadWarning">A non-blocking warning while uploading the log.</param>
    /// <param name="uploadError">An error which occurred while uploading the log.</param>
    private LogParserModel GetModel(string? pasteID, DateTimeOffset? expiry = null, string? uploadWarning = null, string? uploadError = null)
    {
        Platform? platform = this.DetectClientPlatform();

        return new LogParserModel(pasteID, platform)
        {
            UploadWarning = uploadWarning,
            UploadError = uploadError,
            Expiry = expiry
        };
    }

    /// <summary>Detect the viewer's OS.</summary>
    /// <returns>Returns the viewer OS if known, else null.</returns>
    private Platform? DetectClientPlatform()
    {
        string userAgent = this.Request.Headers["User-Agent"];
        switch (userAgent)
        {
            case string ua when ua.Contains("Windows"):
                return Platform.Windows;

            case string ua when ua.Contains("Android"): // check for Android before Linux because Android user agents also contain Linux
                return Platform.Android;

            case string ua when ua.Contains("Linux"):
                return Platform.Linux;

            case string ua when ua.Contains("Mac"):
                return Platform.Mac;

            default:
                return null;
        }
    }
}