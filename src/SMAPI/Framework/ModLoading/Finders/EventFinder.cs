using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders;

/// <summary>Finds incompatible CIL instructions that reference a given event.</summary>
internal class EventFinder : BaseInstructionHandler
{
    /*********
    ** Fields
    *********/
    /// <summary>The full type name for which to find references.</summary>
    private readonly string FullTypeName;

    /// <summary>The method names for which to find references.</summary>
    private readonly ISet<string> MethodNames;

    /// <summary>The result to return for matching instructions.</summary>
    private readonly InstructionHandleResult Result;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="fullTypeName">The full type name for which to find references.</param>
    /// <param name="eventNames">The event names for which to find references.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    public EventFinder(string fullTypeName, string[] eventNames, InstructionHandleResult result)
        : base(defaultPhrase: $"{string.Join(", ", eventNames.Select(p => $"{fullTypeName}.{p}"))} event{(eventNames.Length != 1 ? "s" : "")}") // default phrase should never be used
    {
        this.FullTypeName = fullTypeName;
        this.Result = result;

        this.MethodNames = new HashSet<string>();
        foreach (string name in eventNames)
        {
            this.MethodNames.Add($"add_{name}");
            this.MethodNames.Add($"remove_{name}");
        }
    }

    /// <summary>Construct an instance.</summary>
    /// <param name="fullTypeName">The full type name for which to find references.</param>
    /// <param name="eventName">The event name for which to find references.</param>
    /// <param name="result">The result to return for matching instructions.</param>
    public EventFinder(string fullTypeName, string eventName, InstructionHandleResult result)
        : this(fullTypeName, new[] { eventName }, result) { }

    /// <inheritdoc />
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        if (this.MethodNames.Any())
        {
            MethodReference? methodRef = RewriteHelper.AsMethodReference(instruction);
            if (methodRef != null && methodRef.DeclaringType.FullName == this.FullTypeName && this.MethodNames.Contains(methodRef.Name))
            {
                string eventName = methodRef.Name.Split(new[] { '_' }, 2)[1];
                this.MethodNames.Remove($"add_{eventName}");
                this.MethodNames.Remove($"remove_{eventName}");

                this.MarkFlag(this.Result);
                this.Phrases.Add($"{this.FullTypeName}.{eventName} event");
            }
        }

        return false;
    }
}
