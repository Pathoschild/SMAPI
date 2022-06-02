using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters;

/// <summary>Automatically fix references to fields that have been replaced by a property or const field.</summary>
internal class HeuristicFieldRewriter : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The assembly names to which to rewrite broken references.</summary>
    private readonly ISet<string> RewriteReferencesToAssemblies;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="rewriteReferencesToAssemblies">The assembly names to which to rewrite broken references.</param>
    public HeuristicFieldRewriter(ISet<string> rewriteReferencesToAssemblies)
        : base(defaultPhrase: "field changed to property") // ignored since we specify phrases
    {
        this.RewriteReferencesToAssemblies = rewriteReferencesToAssemblies;
    }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        // get field ref
        FieldReference? fieldRef = RewriteHelper.AsFieldReference(instruction);
        if (fieldRef == null || !this.ShouldValidate(fieldRef.DeclaringType))
            return false;

        // skip if not broken
        FieldDefinition? fieldDefinition = fieldRef.Resolve();
        if (fieldDefinition?.HasConstant == false)
            return false;

        // rewrite if possible
        TypeDefinition? declaringType = fieldRef.DeclaringType.Resolve();
        bool isRead = instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld;
        return
            this.TryRewriteToProperty(module, instruction, fieldRef, declaringType, isRead)
            || this.TryRewriteToConstField(instruction, fieldDefinition);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Whether references to the given type should be validated.</summary>
    /// <param name="type">The type reference.</param>
    private bool ShouldValidate([NotNullWhen(true)] TypeReference? type)
    {
        return type != null && this.RewriteReferencesToAssemblies.Contains(type.Scope.Name);
    }

    /// <summary>Try rewriting the field into a matching property.</summary>
    /// <param name="module">The assembly module containing the instruction.</param>
    /// <param name="instruction">The CIL instruction to rewrite.</param>
    /// <param name="fieldRef">The field reference.</param>
    /// <param name="declaringType">The type on which the field was defined.</param>
    /// <param name="isRead">Whether the field is being read; else it's being written to.</param>
    private bool TryRewriteToProperty(ModuleDefinition module, Instruction instruction, FieldReference fieldRef, TypeDefinition declaringType, bool isRead)
    {
        // get equivalent property
        PropertyDefinition? property = declaringType?.Properties.FirstOrDefault(p => p.Name == fieldRef.Name);
        MethodDefinition? method = isRead ? property?.GetMethod : property?.SetMethod;
        if (method == null)
            return false;

        // rewrite field to property
        instruction.OpCode = OpCodes.Call;
        instruction.Operand = module.ImportReference(method);

        this.Phrases.Add($"{fieldRef.DeclaringType.Name}.{fieldRef.Name} (field => property)");
        return this.MarkRewritten();
    }

    /// <summary>Try rewriting the field into a matching const field.</summary>
    /// <param name="instruction">The CIL instruction to rewrite.</param>
    /// <param name="field">The field definition.</param>
    private bool TryRewriteToConstField(Instruction instruction, FieldDefinition? field)
    {
        // must have been a static field read, and the new field must be const
        if (instruction.OpCode != OpCodes.Ldsfld || field?.HasConstant != true)
            return false;

        // get opcode for value type
        Instruction? loadInstruction = RewriteHelper.GetLoadValueInstruction(field.Constant);
        if (loadInstruction == null)
            return false;

        // rewrite to constant
        instruction.OpCode = loadInstruction.OpCode;
        instruction.Operand = loadInstruction.Operand;

        this.Phrases.Add($"{field.DeclaringType.Name}.{field.Name} (field => const)");
        return this.MarkRewritten();
    }
}