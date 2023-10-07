using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>An <see cref="AssemblyLoadContext"/> which redirects <see cref="Assembly"/> load requests if another mod already loaded a given assembly and marked it public.</summary>
    internal class ModAssemblyLoadContext : AssemblyLoadContext
    {
        /// <summary>A mutable list of all of the mods' <see cref="AssemblyLoadContext"/>s, which <i>will include</i> this one.</summary>
        private readonly IReadOnlyList<ModAssemblyLoadContext> AllModAssemblyLoadContexts;

        /// <summary>A preprocessed list of all private assembly names handled by this <see cref="ModAssemblyLoadContext"/>.</summary>
        internal readonly IReadOnlyList<string> PrivateAssemblyNames;

        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod the <see cref="ModAssemblyLoadContext"/> is for.</param>
        /// <param name="allModAssemblyLoadContexts">A mutable list of all of the mods' <see cref="AssemblyLoadContext"/>s, which <i>will include</i> this one.</param>
        internal ModAssemblyLoadContext(IModMetadata mod, IReadOnlyList<ModAssemblyLoadContext> allModAssemblyLoadContexts) : base(mod.Manifest.UniqueID)
        {
            this.AllModAssemblyLoadContexts = allModAssemblyLoadContexts;

            // a mod author can mark a private assembly with a trailing "!" - this will treat the assembly as used no matter what. useful for types loaded via reflection
            this.PrivateAssemblyNames = mod.Manifest.PrivateAssemblies
                .Select(name => name.EndsWith("!") ? name[..^1] : name)
                .ToList();
        }

        /// <inheritdoc/>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // ignore unnamed assemblies
            if (assemblyName.Name is null)
                return base.Load(assemblyName);

            // normally here we'd try to load the assembly from a file in the mod's folder, but we load all assemblies in AssemblyLoader
            // maybe assembly rewriting and loading could be moved here at some point - it would help with assemblies only used via reflection, not that it happens too often, if ever

            // use any assembly loaded by another mod, as long it's not private
            foreach (ModAssemblyLoadContext context in this.AllModAssemblyLoadContexts)
            {
                if (context == this)
                    continue;

                Assembly? alreadyLoadedAssembly = context.Assemblies
                    .Where(assembly => assembly.GetName().Name is string name && !context.PrivateAssemblyNames.Contains(name))
                    .FirstOrDefault(assembly => assembly.GetName() == assemblyName);

                if (alreadyLoadedAssembly is not null)
                    return alreadyLoadedAssembly;
            }

            // fallback
            return base.Load(assemblyName);
        }
    }
}
