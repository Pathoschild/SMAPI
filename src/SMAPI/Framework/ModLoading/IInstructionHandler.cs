using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>Performs predefined logic for detected CIL instructions.</summary>
    internal interface IInstructionHandler
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A brief noun phrase indicating what the handler matches, used if <see cref="Phrases"/> is empty.</summary>
        string DefaultPhrase { get; }

        /// <summary>The rewrite flags raised for the current module.</summary>
        ISet<InstructionHandleResult> Flags { get; }

        /// <summary>The brief noun phrases indicating what the handler matched for the current module.</summary>
        ISet<string> Phrases { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Rewrite a module definition if needed.</summary>
        /// <param name="module">The assembly module.</param>
        /// <returns>Returns whether the module was changed.</returns>
        bool Handle(ModuleDefinition module);

        /// <summary>Rewrite a type reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="type">The type definition to handle.</param>
        /// <param name="replaceWith">Replaces the type reference with a new one.</param>
        /// <returns>Returns whether the type was changed.</returns>
        bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith);

        /// <summary>Rewrite a CIL instruction reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The CIL instruction to handle.</param>
        /// <returns>Returns whether the instruction was changed.</returns>
        bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction);
    }
}
