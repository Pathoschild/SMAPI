using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders;

/// <summary>Finds incompatible CIL instructions that reference a given method.</summary>
internal class MethodFinder : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The full type name for which to find references.</summary>
    private readonly string FullTypeName;

    /// <summary>The method name for which to find references.</summary>
    private readonly string MethodName;

    /// <summary>The result to return for matching instructions.</summary>
    private readonly InstructionHandleResult Result;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fullTypeName">The full type name for which to find references.</param>
    /// <param name="methodName">The method name for which to find references.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    public MethodFinder(string fullTypeName, string methodName, InstructionHandleResult result)
        : base(defaultPhrase: $"{fullTypeName}.{methodName} method")
    {
        this.FullTypeName = fullTypeName;
        this.MethodName = methodName;
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
            && methodRef.Name == this.MethodName;
    }
}
