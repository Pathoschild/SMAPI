using System;
using Mono.Cecil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters;

/// <summary>Rewrites all references to a type.</summary>
internal class TypeReferenceRewriter : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The full type name to which to find references.</summary>
    private readonly string FromTypeName;

    /// <summary>The new type to reference.</summary>
    private readonly Type ToType;

    /// <summary>Get whether a matched type should be ignored.</summary>
    private readonly Func<TypeReference, bool>? ShouldIgnore;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fromTypeFullName">The full type name to which to find references.</param>
    /// <param name="toType">The new type to reference.</param>
    /// <param name="shouldIgnore">Get whether a matched type should be ignored.</param>
    public TypeReferenceRewriter(string fromTypeFullName, Type toType, Func<TypeReference, bool>? shouldIgnore = null)
        : base($"{fromTypeFullName} type")
    {
        this.FromTypeName = fromTypeFullName;
        this.ToType = toType;
        this.ShouldIgnore = shouldIgnore;
    }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
    {
        // check type reference
        if (type.FullName != this.FromTypeName || this.ShouldIgnore?.Invoke(type) == true)
            return false;

        // rewrite to new type
        replaceWith(module.ImportReference(this.ToType));
        return this.MarkRewritten();
    }
}
