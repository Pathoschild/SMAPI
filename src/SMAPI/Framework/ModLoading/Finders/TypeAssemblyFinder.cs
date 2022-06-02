using System;
using Mono.Cecil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders;

/// <summary>Finds incompatible CIL instructions that reference types in a given assembly.</summary>
internal class TypeAssemblyFinder : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The full assembly name to which to find references.</summary>
    private readonly string AssemblyName;

    /// <summary>The result to return for matching instructions.</summary>
    private readonly InstructionHandleResult Result;

    /// <summary>Get whether a matched type should be ignored.</summary>
    private readonly Func<TypeReference, bool>? ShouldIgnore;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="assemblyName">The full assembly name to which to find references.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    /// <param name="shouldIgnore">Get whether a matched type should be ignored.</param>
    public TypeAssemblyFinder(string assemblyName, InstructionHandleResult result, Func<TypeReference, bool>? shouldIgnore = null)
        : base(defaultPhrase: $"{assemblyName} assembly")
    {
        this.AssemblyName = assemblyName;
        this.Result = result;
        this.ShouldIgnore = shouldIgnore;
    }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
    {
        if (type.Scope.Name == this.AssemblyName && this.ShouldIgnore?.Invoke(type) != true)
            this.MarkFlag(this.Result);

        return false;
    }
}
