using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Mono.Cecil;

namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>Metadata about a parsed assembly definition.</summary>
internal class AssemblyParseResult
{
    /*********
    ** Accessors
    *********/
    /// <summary>The original assembly file.</summary>
    public readonly FileInfo File;

    /// <summary>The assembly definition.</summary>
    public readonly AssemblyDefinition? Definition;

    /// <summary>The result of the assembly load.</summary>
    public AssemblyLoadStatus Status;

    /// <summary>Whether the <see cref="Definition"/> is loaded and ready (i.e. the <see cref="Status"/> is not <see cref="AssemblyLoadStatus.AlreadyLoaded"/> or <see cref="AssemblyLoadStatus.Failed"/>).</summary>
    [MemberNotNullWhen(true, nameof(AssemblyParseResult.Definition))]
    public bool HasDefinition => this.Status == AssemblyLoadStatus.Okay;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="file">The original assembly file.</param>
    /// <param name="assembly">The assembly definition.</param>
    /// <param name="status">The result of the assembly load.</param>
    public AssemblyParseResult(FileInfo file, AssemblyDefinition? assembly, AssemblyLoadStatus status)
    {
        this.File = file;
        this.Definition = assembly;
        this.Status = status;

        if (status == AssemblyLoadStatus.Okay && assembly == null)
            throw new InvalidOperationException($"Invalid assembly parse result: load status {status} with a null assembly.");
    }
}
