using System.Diagnostics.CodeAnalysis;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub;

/// <summary>A file fetched from a GitHub repository.</summary>
internal class GitFile
{
    /*********
    ** Accessors
    *********/
    /// <summary>Whether the file changed since it was last fetched (i.e. <see cref="ETag"/> does not match the one provided). If this is false, the <see cref="Content"/> is <c>null</c>.</summary>
    [MemberNotNullWhen(true, nameof(GitFile.Content))]
    public bool IsModified { get; }

    /// <summary>The raw file content, or <c>null</c> if <see cref="IsModified"/> is false.</summary>
    public string? Content { get; }

    /// <summary>The HTTP ETag header value which identifies this version of the file, if provided by GitHub. This can be passed to a later request to only fetch the file if it changed.</summary>
    public string? ETag { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="content"><inheritdoc cref="Content" path="/summary"/></param>
    /// <param name="eTag"><inheritdoc cref="ETag" path="/summary"/></param>
    public GitFile(string content, string? eTag)
    {
        this.IsModified = true;
        this.Content = content;
        this.ETag = eTag;
    }

    /// <summary>Construct an instance for a file which is unchanged since the given entity tag.</summary>
    /// <param name="eTag"><inheritdoc cref="ETag" path="/summary"/></param>
    public GitFile(string? eTag)
    {
        this.IsModified = false;
        this.ETag = eTag;
    }
}
