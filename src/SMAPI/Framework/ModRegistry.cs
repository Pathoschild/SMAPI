using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace StardewModdingAPI.Framework;

/// <summary>Tracks the installed mods.</summary>
internal class ModRegistry
{
    /*********
    ** Fields
    *********/
    /// <summary>The registered mod data.</summary>
    private readonly List<IModMetadata> Mods = new();

    /// <summary>An assembly full name => mod lookup.</summary>
    private readonly IDictionary<string, IModMetadata> ModNamesByAssembly = new Dictionary<string, IModMetadata>();

    /// <summary>Whether all mod assemblies have been loaded.</summary>
    public bool AreAllModsLoaded { get; set; }

    /// <summary>Whether all mods have been initialized and their <see cref="IMod.Entry"/> method called.</summary>
    public bool AreAllModsInitialized { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Register a mod.</summary>
    /// <param name="metadata">The mod metadata.</param>
    public void Add(IModMetadata metadata)
    {
        this.Mods.Add(metadata);
    }

    /// <summary>Track a mod's assembly for use via <see cref="GetFrom(Type?)"/>.</summary>
    /// <param name="metadata">The mod metadata.</param>
    /// <param name="modAssembly">The mod assembly.</param>
    public void TrackAssemblies(IModMetadata metadata, Assembly modAssembly)
    {
        this.ModNamesByAssembly[modAssembly.FullName!] = metadata;
    }

    /// <summary>Get metadata for all loaded mods.</summary>
    /// <param name="assemblyMods">Whether to include SMAPI mods.</param>
    /// <param name="contentPacks">Whether to include content pack mods.</param>
    public IEnumerable<IModMetadata> GetAll(bool assemblyMods = true, bool contentPacks = true)
    {
        IEnumerable<IModMetadata> query = this.Mods;
        if (!assemblyMods)
            query = query.Where(p => p.IsContentPack);
        if (!contentPacks)
            query = query.Where(p => !p.IsContentPack);

        return query;
    }

    /// <summary>Get metadata for a loaded mod.</summary>
    /// <param name="uniqueID">The mod's unique ID.</param>
    /// <returns>Returns the mod's metadata, or <c>null</c> if not found.</returns>
    public IModMetadata? Get(string uniqueID)
    {
        // normalize search ID
        if (string.IsNullOrWhiteSpace(uniqueID))
            return null;
        uniqueID = uniqueID.Trim();

        // find match
        return this.GetAll().FirstOrDefault(p => p.HasID(uniqueID));
    }

    /// <summary>Get the mod metadata from one of its assemblies.</summary>
    /// <param name="type">The type to check.</param>
    /// <returns>Returns the mod's metadata, or <c>null</c> if the type isn't part of a known mod.</returns>
    public IModMetadata? GetFrom(Type? type)
    {
        // null
        if (type == null)
            return null;

        // known type
        string assemblyName = type.Assembly.FullName!;
        if (this.ModNamesByAssembly.ContainsKey(assemblyName))
            return this.ModNamesByAssembly[assemblyName];

        // not found
        return null;
    }

    /// <summary>Get the mod metadata from a stack frame, if any.</summary>
    /// <param name="frame">The stack frame to check.</param>
    /// <returns>Returns the mod's metadata, or <c>null</c> if the frame isn't part of a known mod.</returns>
    public IModMetadata? GetFrom(StackFrame frame)
    {
        MethodBase? method = frame.GetMethod();
        return this.GetFrom(method?.ReflectedType);
    }

    /// <summary>Get the mod metadata from the closest assembly registered as a source of deprecation warnings.</summary>
    /// <param name="frames">The call stack to analyze.</param>
    /// <returns>Returns the mod's metadata, or <c>null</c> if no registered assemblies were found.</returns>
    public IModMetadata? GetFromStack(StackFrame[] frames)
    {
        foreach (StackFrame frame in frames)
        {
            IModMetadata? mod = this.GetFrom(frame);
            if (mod != null)
                return mod;
        }

        return null;
    }
}
