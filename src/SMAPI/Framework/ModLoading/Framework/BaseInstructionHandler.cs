using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Framework
{
    /// <summary>The base implementation for a CIL instruction handler or rewriter.</summary>
    internal abstract class BaseInstructionHandler : IInstructionHandler
    {
        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string DefaultPhrase { get; }

        /// <inheritdoc />
        public ISet<InstructionHandleResult> Flags { get; } = new HashSet<InstructionHandleResult>();

        /// <inheritdoc />
        public ISet<string> Phrases { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public virtual bool Handle(ModuleDefinition module)
        {
            return false;
        }

        /// <inheritdoc />
        public virtual bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
        {
            return false;
        }

        /// <inheritdoc />
        public virtual bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            return false;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="defaultPhrase">A brief noun phrase indicating what the handler matches, used if <see cref="Phrases"/> is empty.</param>
        protected BaseInstructionHandler(string defaultPhrase)
        {
            this.DefaultPhrase = defaultPhrase;
        }

        /// <summary>Raise a result flag.</summary>
        /// <param name="flag">The result flag to set.</param>
        /// <param name="resultMessage">The result message to add.</param>
        /// <returns>Returns true for convenience.</returns>
        protected bool MarkFlag(InstructionHandleResult flag, string resultMessage = null)
        {
            this.Flags.Add(flag);
            if (resultMessage != null)
                this.Phrases.Add(resultMessage);
            return true;
        }

        /// <summary>Raise a generic flag indicating that the code was rewritten.</summary>
        public bool MarkRewritten()
        {
            return this.MarkFlag(InstructionHandleResult.Rewritten);
        }
    }
}
