using System;
using System.Collections.Generic;
using Mono.Cecil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders;

/// <summary>Finds incompatible CIL instructions that reference a given type.</summary>
internal class TypeFinder : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The full type names remaining to match.</summary>
    private readonly ISet<string> FullTypeNames;

    /// <summary>The result to return for matching instructions.</summary>
    private readonly InstructionHandleResult Result;

    /// <summary>Get whether a matched type should be ignored.</summary>
    private readonly Func<TypeReference, bool>? ShouldIgnore;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fullTypeNames">The full type names to match.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    /// <param name="shouldIgnore">Get whether a matched type should be ignored.</param>
    public TypeFinder(string[] fullTypeNames, InstructionHandleResult result, Func<TypeReference, bool>? shouldIgnore = null)
        : base(defaultPhrase: $"{string.Join(", ", fullTypeNames)} type{(fullTypeNames.Length != 1 ? "s" : "")}") // default phrase should never be used
    {
        this.FullTypeNames = new HashSet<string>(fullTypeNames);
        this.Result = result;
        this.ShouldIgnore = shouldIgnore;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="fullTypeName">The full type name to match.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    /// <param name="shouldIgnore">Get whether a matched type should be ignored.</param>
    public TypeFinder(string fullTypeName, InstructionHandleResult result, Func<TypeReference, bool>? shouldIgnore = null)
        : this(new[] { fullTypeName }, result, shouldIgnore) { }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
    {
        if (this.FullTypeNames.Contains(type.FullName) && this.ShouldIgnore?.Invoke(type) != true)
        {
            this.FullTypeNames.Remove(type.FullName);

            this.MarkFlag(this.Result);
            this.Phrases.Add($"{type.FullName} type");
        }

        return false;
    }
}
