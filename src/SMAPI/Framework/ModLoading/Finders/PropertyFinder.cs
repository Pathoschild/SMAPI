using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders;

/// <summary>Finds incompatible CIL instructions that reference a given property.</summary>
internal class PropertyFinder : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The full type name for which to find references.</summary>
    private readonly string FullTypeName;

    /// <summary>The property name for which to find references.</summary>
    private readonly string PropertyName;

    /// <summary>The result to return for matching instructions.</summary>
    private readonly InstructionHandleResult Result;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fullTypeName">The full type name for which to find references.</param>
    /// <param name="propertyName">The property name for which to find references.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    public PropertyFinder(string fullTypeName, string propertyName, InstructionHandleResult result)
        : base(defaultPhrase: $"{fullTypeName}.{propertyName} property")
    {
        this.FullTypeName = fullTypeName;
        this.PropertyName = propertyName;
        this.Result = result;
    }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        if (!this.Flags.Contains(this.Result) && this.IsMatch(instruction))
            this.MarkFlag(this.Result);

        return false;
    }


    /*********
    ** Protected methods
    *********/
    /// <summary>Get whether a CIL instruction matches.</summary>
    /// <param name="instruction">The IL instruction.</param>
    protected bool IsMatch(Instruction instruction)
    {
        MethodReference? methodRef = RewriteHelper.AsMethodReference(instruction);
        return
            methodRef != null
            && methodRef.DeclaringType.FullName == this.FullTypeName
            && (methodRef.Name == "get_" + this.PropertyName || methodRef.Name == "set_" + this.PropertyName);
    }
}
