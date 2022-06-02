using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters;

/// <summary>Rewrites method references from one parent type to another if the signatures match.</summary>
internal class MethodParentRewriter : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The full name of the type whose methods to remap.</summary>
    private readonly string FromType;

    /// <summary>The type with methods to map to.</summary>
    private readonly Type ToType;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fromType">The type whose methods to remap.</param>
    /// <param name="toType">The type with methods to map to.</param>
    /// <param name="nounPhrase">A brief noun phrase indicating what the instruction finder matches (or <c>null</c> to generate one).</param>
    public MethodParentRewriter(string fromType, Type toType, string? nounPhrase = null)
        : base(nounPhrase ?? $"{fromType.Split('.').Last()} methods")
    {
        this.FromType = fromType;
        this.ToType = toType;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="fromType">The type whose methods to remap.</param>
    /// <param name="toType">The type with methods to map to.</param>
    /// <param name="nounPhrase">A brief noun phrase indicating what the instruction finder matches (or <c>null</c> to generate one).</param>
    public MethodParentRewriter(Type fromType, Type toType, string? nounPhrase = null)
        : this(fromType.FullName!, toType, nounPhrase) { }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        // get method ref
        MethodReference? methodRef = RewriteHelper.AsMethodReference(instruction);
        if (!this.IsMatch(methodRef))
            return false;

        // rewrite
        methodRef.DeclaringType = module.ImportReference(this.ToType);
        return this.MarkRewritten();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get whether a CIL instruction matches.</summary>
    /// <param name="methodRef">The method reference.</param>
    private bool IsMatch([NotNullWhen(true)] MethodReference? methodRef)
    {
        return
            methodRef != null
            && methodRef.DeclaringType.FullName == this.FromType
            && RewriteHelper.HasMatchingSignature(this.ToType, methodRef);
    }
}
