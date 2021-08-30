using Mono.Cecil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Removes the 32-bit-only from loaded assemblies.</summary>
    internal class ArchitectureAssemblyRewriter : BaseInstructionHandler
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ArchitectureAssemblyRewriter()
            : base(defaultPhrase: "32-bit architecture") { }


        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module)
        {
            if (module.Attributes.HasFlag(ModuleAttributes.Required32Bit))
            {
                module.Attributes &= ~ModuleAttributes.Required32Bit;
                this.MarkRewritten();
                return true;
            }

            return false;
        }

    }
}
