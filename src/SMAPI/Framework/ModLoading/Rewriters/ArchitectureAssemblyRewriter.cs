using System;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.RewriteFacades;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites Harmony 1.x assembly references to work with Harmony 2.x.</summary>
    internal class ArchitectureAssemblyRewriter : BaseInstructionHandler
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ArchitectureAssemblyRewriter()
            : base(defaultPhrase: "32-bit architecture") { }


        /// <inheritdoc />
        public override bool Handle( ModuleDefinition module )
        {
            if ( module.Attributes.HasFlag( ModuleAttributes.Required32Bit ) )
            {
                module.Attributes = module.Attributes & ~ModuleAttributes.Required32Bit;
                this.MarkRewritten();
                return true;
            }
            return false;
        }

    }
}
