using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders;

/// <summary>Finds incompatible CIL instructions that reference a given field.</summary>
internal class FieldFinder : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The full type name for which to find references.</summary>
    private readonly string FullTypeName;

    /// <summary>The field names for which to find references.</summary>
    private readonly ISet<string> FieldNames;

    /// <summary>The result to return for matching instructions.</summary>
    private readonly InstructionHandleResult Result;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fullTypeName">The full type name for which to find references.</param>
    /// <param name="fieldNames">The field names for which to find references.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    public FieldFinder(string fullTypeName, string[] fieldNames, InstructionHandleResult result)
        : base(defaultPhrase: $"{string.Join(", ", fieldNames.Select(p => $"{fullTypeName}.{p}"))} field{(fieldNames.Length != 1 ? "s" : "")}") // default phrase should never be used
    {
        this.FullTypeName = fullTypeName;
        this.FieldNames = new HashSet<string>(fieldNames);
        this.Result = result;
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="fullTypeName">The full type name for which to find references.</param>
    /// <param name="fieldName">The field name for which to find references.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    public FieldFinder(string fullTypeName, string fieldName, InstructionHandleResult result)
        : this(fullTypeName, new[] { fieldName }, result) { }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        if (this.FieldNames.Any())
        {
            FieldReference? fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null && fieldRef.DeclaringType.FullName == this.FullTypeName && this.FieldNames.Contains(fieldRef.Name))
            {
                this.FieldNames.Remove(fieldRef.Name);

                this.MarkFlag(this.Result);
                this.Phrases.Add($"{this.FullTypeName}.{fieldRef.Name} field");
            }
        }

        return false;
    }
}
