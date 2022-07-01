using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters;

/// <summary>Automatically fix references to methods that had extra optional parameters added.</summary>
internal class HeuristicMethodRewriter : BaseInstructionHandler
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
    public HeuristicMethodRewriter(ISet<string> rewriteReferencesToAssemblies)
        : base(defaultPhrase: "methods with missing parameters") // ignored since we specify phrases
    {
        this.RewriteReferencesToAssemblies = rewriteReferencesToAssemblies;
    }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        // get method ref
        MethodReference? methodRef = RewriteHelper.AsMethodReference(instruction);
        if (methodRef == null || !this.ShouldValidate(methodRef.DeclaringType))
            return false;

        // skip if not broken
        if (methodRef.Resolve() != null)
            return false;

        // get type
        TypeDefinition? type = methodRef.DeclaringType.Resolve();
        if (type == null)
            return false;

        // get method definition
        MethodDefinition? method = null;
        foreach (MethodDefinition match in type.Methods.Where(p => p.Name == methodRef.Name))
        {
            // reference matches initial parameters of definition
            if (methodRef.Parameters.Count >= match.Parameters.Count || !this.InitialParametersMatch(methodRef, match))
                continue;

            // all remaining parameters in definition are optional
            if (!match.Parameters.Skip(methodRef.Parameters.Count).All(p => p.IsOptional))
                continue;

            method = match;
            break;
        }
        if (method == null)
            return false;

        // get instructions to inject parameter values
        var loadInstructions = method.Parameters.Skip(methodRef.Parameters.Count)
            .Select(p => RewriteHelper.GetLoadValueInstruction(p.Constant))
            .ToArray();
        if (loadInstructions.Any(p => p == null))
            return false; // SMAPI needs to load the value onto the stack before the method call, but the optional parameter type wasn't recognized

        // rewrite method reference
        foreach (Instruction? loadInstruction in loadInstructions)
            cil.InsertBefore(instruction, loadInstruction);
        instruction.Operand = module.ImportReference(method);

        this.Phrases.Add($"{methodRef.DeclaringType.Name}.{methodRef.Name} (added missing optional parameters)");
        return this.MarkRewritten();
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

    /// <summary>Get whether every parameter in the method reference matches the exact order and type of the parameters in the method definition. This ignores extra parameters in the definition.</summary>
    /// <param name="methodRef">The method reference whose parameters to check.</param>
    /// <param name="method">The method definition whose parameters to check against.</param>
    private bool InitialParametersMatch(MethodReference methodRef, MethodDefinition method)
    {
        if (methodRef.Parameters.Count > method.Parameters.Count)
            return false;

        for (int i = 0; i < methodRef.Parameters.Count; i++)
        {
            if (!RewriteHelper.IsSameType(methodRef.Parameters[i].ParameterType, method.Parameters[i].ParameterType))
                return false;
        }

        return true;
    }
}
