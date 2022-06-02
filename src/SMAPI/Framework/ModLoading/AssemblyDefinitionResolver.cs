using System.Collections.Generic;
using Mono.Cecil;

namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>A minimal assembly definition resolver which resolves references to known assemblies.</summary>
internal class AssemblyDefinitionResolver : DefaultAssemblyResolver
{
    /*********
    ** Fields
    *********/
    /// <summary>The known assemblies.</summary>
    private readonly IDictionary<string, AssemblyDefinition> Lookup = new Dictionary<string, AssemblyDefinition>();


    /*********
    ** Public methods
    *********/
    /// <summary>Add known assemblies to the resolver.</summary>
    /// <param name="assemblies">The known assemblies.</param>
    public void Add(params AssemblyDefinition[] assemblies)
    {
        foreach (AssemblyDefinition assembly in assemblies)
            this.AddWithExplicitNames(assembly, assembly.Name.Name, assembly.Name.FullName);
    }

    /// <summary>Add a known assembly to the resolver with the given names. This overrides the assembly names that would normally be assigned.</summary>
    /// <param name="assembly">The assembly to add.</param>
    /// <param name="names">The assembly names for which it should be returned.</param>
    public void AddWithExplicitNames(AssemblyDefinition assembly, params string[] names)
    {
        this.RegisterAssembly(assembly);
        foreach (string name in names)
            this.Lookup[name] = assembly;
    }

    /// <summary>Resolve an assembly reference.</summary>
    /// <param name="name">The assembly name.</param>
    /// <exception cref="AssemblyResolutionException">The assembly can't be resolved.</exception>
    public override AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        return this.ResolveName(name.Name) ?? base.Resolve(name);
    }

    /// <summary>Resolve an assembly reference.</summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="parameters">The assembly reader parameters.</param>
    /// <exception cref="AssemblyResolutionException">The assembly can't be resolved.</exception>
    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
    {
        return this.ResolveName(name.Name) ?? base.Resolve(name, parameters);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Resolve a known assembly definition based on its short or full name.</summary>
    /// <param name="name">The assembly's short or full name.</param>
    private AssemblyDefinition? ResolveName(string name)
    {
        return this.Lookup.TryGetValue(name, out AssemblyDefinition? match)
            ? match
            : null;
    }
}