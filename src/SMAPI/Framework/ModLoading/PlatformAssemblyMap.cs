using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>Metadata for mapping assemblies to the current <see cref="Platform"/>.</summary>
internal class PlatformAssemblyMap : IDisposable
{
    /*********
    ** Accessors
    *********/
    /****
    ** Data
    ****/
    /// <summary>The target game platform.</summary>
    public readonly Platform TargetPlatform;

    /// <summary>The short assembly names to remove as assembly reference, and replace with the <see cref="Targets"/>. These should be short names (like "Stardew Valley").</summary>
    public readonly string[] RemoveNames;

    /****
    ** Metadata
    ****/
    /// <summary>The assemblies to target. Equivalent types should be rewritten to use these assemblies.</summary>
    public readonly Assembly[] Targets;

    /// <summary>An assembly => reference cache.</summary>
    public readonly IDictionary<Assembly, AssemblyNameReference> TargetReferences;

    /// <summary>An assembly => module cache.</summary>
    public readonly IDictionary<Assembly, ModuleDefinition> TargetModules;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="targetPlatform">The target game platform.</param>
    /// <param name="removeAssemblyNames">The assembly short names to remove (like <c>Stardew Valley</c>).</param>
    /// <param name="targetAssemblies">The assemblies to target.</param>
    public PlatformAssemblyMap(Platform targetPlatform, string[] removeAssemblyNames, Assembly[] targetAssemblies)
    {
        // save data
        this.TargetPlatform = targetPlatform;
        this.RemoveNames = removeAssemblyNames;

        // cache assembly metadata
        this.Targets = targetAssemblies;
        this.TargetReferences = this.Targets.ToDictionary(assembly => assembly, assembly => AssemblyNameReference.Parse(assembly.FullName));
        this.TargetModules = this.Targets.ToDictionary(assembly => assembly, assembly => ModuleDefinition.ReadModule(assembly.Modules.Single().FullyQualifiedName, new ReaderParameters { InMemory = true }));
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        foreach (ModuleDefinition module in this.TargetModules.Values)
            module.Dispose();
    }
}
