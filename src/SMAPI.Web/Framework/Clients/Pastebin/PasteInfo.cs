using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Web.Framework.Clients.Pastebin;

/// <summary>The response for a get-paste request.</summary>
internal class PasteInfo
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether the log was successfully fetched.</summary>
    [MemberNotNullWhen(true, nameof(PasteInfo.Content))]
    [MemberNotNullWhen(false, nameof(PasteInfo.Error))]
    public bool Success => this.Error == null || this.Content != null;

    /// <summary>The fetched paste content (if <see cref="Success"/> is <c>true</c>).</summary>
    public string? Content { get; internal set; }

    /// <summary>The error message (if <see cref="Success"/> is <c>false</c>).</summary>
    public string? Error { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="content">The fetched paste content.</param>
    /// <param name="error">The error message, if it failed.</param>
    public PasteInfo(string? content, string? error)
    {
        this.Content = content;
        this.Error = error;
    }
}
