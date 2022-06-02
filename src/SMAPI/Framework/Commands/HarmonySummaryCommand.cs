using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace StardewModdingAPI.Framework.Commands;

/// <summary>The 'harmony_summary' SMAPI console command.</summary>
internal class HarmonySummaryCommand : IInternalCommand
{
    /*********
    ** Accessors
    *********/
    /// <summary>The command name, which the user must type to trigger it.</summary>
    public string Name { get; } = "harmony_summary";

    /// <summary>The human-readable documentation shown when the player runs the built-in 'help' command.</summary>
    public string Description { get; } = "Harmony is a library which rewrites game code, used by SMAPI and some mods. This command lists current Harmony patches.\n\nUsage: harmony_summary\nList all Harmony patches.\n\nUsage: harmony_summary <search>\n- search: one more more words to search. If any word matches a method name, the method and all its patchers will be listed; otherwise only matching patchers will be listed for the method.";


    /*********
    ** Public methods
    *********/
    /// <summary>Handle the console command when it's entered by the user.</summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="monitor">Writes messages to the console.</param>
    public void HandleCommand(string[] args, IMonitor monitor)
    {
        SearchResult[] matches = this.FilterPatches(args).OrderBy(p => p.MethodName).ToArray();

        StringBuilder result = new();

        if (!matches.Any())
            result.AppendLine("No current patches match your search.");
        else
        {
            result.AppendLine(args.Any() ? "Harmony patches which match your search terms:" : "Current Harmony patches:");
            result.AppendLine();
            foreach (var match in matches)
            {
                result.AppendLine($"   {match.MethodName}");
                foreach (var ownerGroup in match.PatchTypesByOwner.OrderBy(p => p.Key))
                {
                    var sortedTypes = ownerGroup.Value
                        .OrderBy(p => p switch
                        {
                            PatchType.Prefix => 0,
                            PatchType.Postfix => 1,
                            PatchType.Finalizer => 2,
                            PatchType.Transpiler => 3,
                            _ => 4
                        });

                    result.AppendLine($"      - {ownerGroup.Key} ({string.Join(", ", sortedTypes).ToLower()})");
                }
                result.AppendLine();
            }
        }

        monitor.Log(result.ToString().TrimEnd(), LogLevel.Info);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get all current Harmony patches matching any of the given search terms.</summary>
    /// <param name="searchTerms">The search terms to match.</param>
    private IEnumerable<SearchResult> FilterPatches(string[] searchTerms)
    {
        bool hasSearch = searchTerms.Any();
        bool IsMatch(string? target) => !hasSearch || searchTerms.Any(search => target != null && target.IndexOf(search, StringComparison.OrdinalIgnoreCase) > -1);

        foreach (SearchResult patch in this.GetAllPatches())
        {
            // matches entire patch
            if (IsMatch(patch.MethodDescription))
            {
                yield return patch;
                continue;
            }

            // matches individual patchers
            foreach ((string patcherId, ISet<PatchType> patchTypes) in patch.PatchTypesByOwner.ToArray())
            {
                if (!IsMatch(patcherId) && !patchTypes.Any(type => IsMatch(type.ToString())))
                    patch.PatchTypesByOwner.Remove(patcherId);
            }

            if (patch.PatchTypesByOwner.Any())
                yield return patch;
        }
    }

    /// <summary>Get all current Harmony patches.</summary>
    private IEnumerable<SearchResult> GetAllPatches()
    {
        foreach (MethodBase method in Harmony.GetAllPatchedMethods().ToArray())
        {
            // get metadata for method
            HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(method);

            IDictionary<PatchType, IReadOnlyCollection<Patch>> patchGroups = new Dictionary<PatchType, IReadOnlyCollection<Patch>>
            {
                [PatchType.Prefix] = patchInfo.Prefixes,
                [PatchType.Postfix] = patchInfo.Postfixes,
                [PatchType.Finalizer] = patchInfo.Finalizers,
                [PatchType.Transpiler] = patchInfo.Transpilers
            };

            // get patch types by owner
            var typesByOwner = new Dictionary<string, ISet<PatchType>>();
            foreach ((PatchType type, IReadOnlyCollection<Patch> patches) in patchGroups)
            {
                foreach (Patch patch in patches)
                {
                    if (!typesByOwner.TryGetValue(patch.owner, out ISet<PatchType>? patchTypes))
                        typesByOwner[patch.owner] = patchTypes = new HashSet<PatchType>();
                    patchTypes.Add(type);
                }
            }

            // create search result
            yield return new SearchResult(method, typesByOwner);
        }
    }

    /// <summary>A Harmony patch type.</summary>
    private enum PatchType
    {
        /// <summary>A prefix patch.</summary>
        Prefix,

        /// <summary>A postfix patch.</summary>
        Postfix,

        /// <summary>A finalizer patch.</summary>
        Finalizer,

        /// <summary>A transpiler patch.</summary>
        Transpiler
    }

    /// <summary>A patch search result for a method.</summary>
    private class SearchResult
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A simple human-readable name for the patched method.</summary>
        public string MethodName { get; }

        /// <summary>A detailed description for the patched method.</summary>
        public string MethodDescription { get; }

        /// <summary>The patch types by the Harmony instance ID that added them.</summary>
        public IDictionary<string, ISet<PatchType>> PatchTypesByOwner { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="method">The patched method.</param>
        /// <param name="patchTypesByOwner">The patch types by the Harmony instance ID that added them.</param>
        public SearchResult(MethodBase method, IDictionary<string, ISet<PatchType>> patchTypesByOwner)
        {
            this.MethodName = $"{method.DeclaringType?.FullName}.{method.Name}";
            this.MethodDescription = method.FullDescription();
            this.PatchTypesByOwner = patchTypesByOwner;
        }
    }
}
