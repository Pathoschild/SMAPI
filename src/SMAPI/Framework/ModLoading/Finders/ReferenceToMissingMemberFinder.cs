using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders;

/// <summary>Finds references to a field, property, or method which no longer exists.</summary>
/// <remarks>This implementation is purely heuristic. It should never return a false positive, but won't detect all cases.</remarks>
internal class ReferenceToMissingMemberFinder : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The assembly names to which to heuristically detect broken references.</summary>
    private readonly ISet<string> ValidateReferencesToAssemblies;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="validateReferencesToAssemblies">The assembly names to which to heuristically detect broken references.</param>
    public ReferenceToMissingMemberFinder(ISet<string> validateReferencesToAssemblies)
        : base(defaultPhrase: "")
    {
        this.ValidateReferencesToAssemblies = validateReferencesToAssemblies;
    }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        // field reference
        FieldReference? fieldRef = RewriteHelper.AsFieldReference(instruction);
        if (fieldRef != null && this.ShouldValidate(fieldRef.DeclaringType))
        {
            FieldDefinition? target = fieldRef.Resolve();
            if (target == null || target.HasConstant)
            {
                this.MarkFlag(InstructionHandleResult.NotCompatible, $"reference to {fieldRef.DeclaringType.FullName}.{fieldRef.Name} (no such field)");
                return false;
            }
        }

        // method reference
        MethodReference? methodRef = RewriteHelper.AsMethodReference(instruction);
        if (methodRef != null && this.ShouldValidate(methodRef.DeclaringType) && !this.IsUnsupported(methodRef))
        {
            MethodDefinition? target = methodRef.Resolve();
            if (target == null)
            {
                string phrase;
                if (this.IsProperty(methodRef))
                    phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name.Substring(4)} (no such property)";
                else if (methodRef.Name == ".ctor")
                    phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name} (no matching constructor)";
                else
                    phrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name} (no such method)";

                this.MarkFlag(InstructionHandleResult.NotCompatible, phrase);
                return false;
            }
        }

        return false;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Whether references to the given type should be validated.</summary>
    /// <param name="type">The type reference.</param>
    private bool ShouldValidate([NotNullWhen(true)] TypeReference? type)
    {
        return type != null && this.ValidateReferencesToAssemblies.Contains(type.Scope.Name);
    }

    /// <summary>Get whether a method reference is a special case that's not currently supported (e.g. array methods).</summary>
    /// <param name="method">The method reference.</param>
    private bool IsUnsupported(MethodReference method)
    {
        return
            method.DeclaringType.Name.Contains("["); // array methods
    }

    /// <summary>Get whether a method reference is a property getter or setter.</summary>
    /// <param name="method">The method reference.</param>
    private bool IsProperty(MethodReference method)
    {
        return method.Name.StartsWith("get_") || method.Name.StartsWith("set_");
    }
}