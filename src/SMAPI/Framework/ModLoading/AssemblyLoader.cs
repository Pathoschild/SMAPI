using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.Symbols;
using StardewModdingAPI.Metadata;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>Preprocesses and loads mod assemblies.</summary>
    internal class AssemblyLoader : IDisposable
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>Whether to detect paranoid mode issues.</summary>
        private readonly bool ParanoidMode;

        /// <summary>Metadata for mapping assemblies to the current platform.</summary>
        private readonly PlatformAssemblyMap AssemblyMap;

        /// <summary>A type => assembly lookup for types which should be rewritten.</summary>
        private readonly IDictionary<string, Assembly> TypeAssemblies;

        /// <summary>A minimal assembly definition resolver which resolves references to known loaded assemblies.</summary>
        private readonly AssemblyDefinitionResolver AssemblyDefinitionResolver;

        /// <summary>Provides assembly symbol readers for Mono.Cecil.</summary>
        private readonly SymbolReaderProvider SymbolReaderProvider = new();

        /// <summary>Provides assembly symbol writers for Mono.Cecil.</summary>
        private readonly SymbolWriterProvider SymbolWriterProvider = new();

        /// <summary>The objects to dispose as part of this instance.</summary>
        private readonly HashSet<IDisposable> Disposables = new();

        /// <summary>The instruction finders and rewriters to apply.</summary>
        private readonly IInstructionHandler[] InstructionHandlers;

        /// <summary>Whether to rewrite mods for compatibility.</summary>
        private readonly bool RewriteMods;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="targetPlatform">The current game platform.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="paranoidMode">Whether to detect paranoid mode issues.</param>
        /// <param name="rewriteMods">Whether to rewrite mods for compatibility.</param>
        public AssemblyLoader(Platform targetPlatform, IMonitor monitor, bool paranoidMode, bool rewriteMods)
        {
            this.Monitor = monitor;
            this.ParanoidMode = paranoidMode;
            this.RewriteMods = rewriteMods;
            this.AssemblyMap = this.TrackForDisposal(Constants.GetAssemblyMap(targetPlatform));

            // init resolver
            this.AssemblyDefinitionResolver = this.TrackForDisposal(new AssemblyDefinitionResolver());
            Constants.ConfigureAssemblyResolver(this.AssemblyDefinitionResolver);

            // generate type => assembly lookup for types which should be rewritten
            this.TypeAssemblies = new Dictionary<string, Assembly>();
            foreach (Assembly assembly in this.AssemblyMap.Targets)
            {
                ModuleDefinition module = this.AssemblyMap.TargetModules[assembly];
                foreach (TypeDefinition type in module.GetTypes())
                {
                    if (!type.IsPublic)
                        continue; // no need to rewrite
                    if (type.Namespace.Contains("<"))
                        continue; // ignore assembly metadata
                    this.TypeAssemblies[type.FullName] = assembly;
                }
            }

            // init rewriters
            this.InstructionHandlers = new InstructionMetadata().GetHandlers(this.ParanoidMode, this.RewriteMods).ToArray();

        }

        /// <summary>Preprocess and load an assembly.</summary>
        /// <param name="mod">The mod for which the assembly is being loaded.</param>
        /// <param name="assemblyFile">The assembly file.</param>
        /// <param name="assumeCompatible">Assume the mod is compatible, even if incompatible code is detected.</param>
        /// <param name="assemblyLoadContext">The <see cref="AssemblyLoadContext"/> for the currently being loaded mod.</param>
        /// <returns>Returns the rewrite metadata for the preprocessed assembly.</returns>
        /// <exception cref="IncompatibleInstructionException">An incompatible CIL instruction was found while rewriting the assembly.</exception>
        public Assembly Load(IModMetadata mod, FileInfo assemblyFile, bool assumeCompatible, ModAssemblyLoadContext assemblyLoadContext)
        {
            // get referenced local assemblies
            AssemblyParseResult[] assemblies;
            {
                HashSet<string> visitedAssemblyNames = new HashSet<string>( // don't try loading assemblies that are already loaded
                    from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    let name = assembly.GetName().Name
                    where name != null
                    where !assemblyLoadContext.PrivateAssemblyNames.Contains(name)
                    let visitedAssemblyLoadContext = AssemblyLoadContext.GetLoadContext(assembly) as ModAssemblyLoadContext
                    where visitedAssemblyLoadContext != null
                    where !visitedAssemblyLoadContext.PrivateAssemblyNames.Contains(name)
                    select name
                );
                assemblies = this.GetReferencedLocalAssemblies(assemblyFile, visitedAssemblyNames, this.AssemblyDefinitionResolver).ToArray();
            }

            // validate private assembly names
            for (int i = 0; i < mod.Manifest.PrivateAssemblies.Length; i++)
            {
                bool isPrivateAssemblyUsed = false;
                string privateAssemblyName = mod.Manifest.PrivateAssemblies[i];

                // a mod author can mark a private assembly with a trailing "!" - this will treat the assembly as used no matter what. useful for types loaded via reflection
                if (privateAssemblyName.EndsWith("!"))
                {
                    isPrivateAssemblyUsed = true;
                    privateAssemblyName = privateAssemblyName[..^1];
                }

                if (!isPrivateAssemblyUsed && assemblies.Any(a => a.Definition?.Name.Name == privateAssemblyName))
                    isPrivateAssemblyUsed = true;
                if (!isPrivateAssemblyUsed)
                    this.Monitor.Log($"      Mod {mod.DisplayName} specifies a private assembly {privateAssemblyName}, but it does not use it.", LogLevel.Warn);
            }

            // validate load
            if (!assemblies.Any() || assemblies[0].Status == AssemblyLoadStatus.Failed)
            {
                throw new SAssemblyLoadFailedException(!assemblyFile.Exists
                    ? $"Could not load '{assemblyFile.FullName}' because it doesn't exist."
                    : $"Could not load '{assemblyFile.FullName}'."
                );
            }
            if (assemblies.Last().Status == AssemblyLoadStatus.AlreadyLoaded) // mod assembly is last in dependency order
                throw new SAssemblyLoadFailedException($"Could not load '{assemblyFile.FullName}' because it was already loaded. Do you have two copies of this mod?");

            // rewrite & load assemblies in leaf-to-root order
            bool oneAssembly = assemblies.Length == 1;
            Assembly? lastAssembly = null;
            HashSet<string> loggedMessages = new HashSet<string>();
            foreach (AssemblyParseResult assembly in assemblies)
            {
                if (!assembly.HasDefinition)
                    continue;

                // rewrite assembly
                bool changed = this.RewriteAssembly(mod, assembly.Definition, loggedMessages, logPrefix: "      ");

                // detect broken assembly reference
                foreach (AssemblyNameReference reference in assembly.Definition.MainModule.AssemblyReferences)
                {
                    if (!reference.Name.StartsWith("System.") && !this.IsAssemblyLoaded(reference))
                    {
                        this.Monitor.LogOnce(loggedMessages, $"      Broken code in {assembly.File.Name}: reference to missing assembly '{reference.FullName}'.");
                        if (!assumeCompatible)
                            throw new IncompatibleInstructionException($"Found a reference to missing assembly '{reference.FullName}' while loading assembly {assembly.File.Name}.");
                        mod.SetWarning(ModWarning.BrokenCodeLoaded);
                        break;
                    }
                }

                // load assembly
                bool loadAsPrivate = assemblyLoadContext.PrivateAssemblyNames.Contains(assembly.Definition.Name.Name);
                if (changed)
                {
                    if (!oneAssembly)
                        this.Monitor.Log($"      Loading{(loadAsPrivate ? " (private)" : "")} {assembly.File.Name} (rewritten)...");

                    // load assembly
                    using MemoryStream outAssemblyStream = new();
                    using MemoryStream outSymbolStream = new();
                    assembly.Definition.Write(outAssemblyStream, new WriterParameters { WriteSymbols = true, SymbolStream = outSymbolStream, SymbolWriterProvider = this.SymbolWriterProvider });
                    outAssemblyStream.Position = 0;
                    outSymbolStream.Position = 0;
                    lastAssembly = assemblyLoadContext.LoadFromStream(outAssemblyStream, outSymbolStream);
                }
                else
                {
                    if (!oneAssembly)
                        this.Monitor.Log($"      Loading{(loadAsPrivate ? " (private)" : "")} {assembly.File.Name}...");
                    lastAssembly = assemblyLoadContext.LoadFromAssemblyPath(assembly.File.FullName);
                }

                // track loaded assembly for definition resolution
                this.AssemblyDefinitionResolver.Add(assembly.Definition);
            }

            // throw if incompatibilities detected
            if (!assumeCompatible && mod.Warnings.HasFlag(ModWarning.BrokenCodeLoaded))
                throw new IncompatibleInstructionException();

            // last assembly loaded is the root
            return lastAssembly!;
        }

        /// <summary>Get whether an assembly is loaded.</summary>
        /// <param name="reference">The assembly name reference.</param>
        public bool IsAssemblyLoaded(AssemblyNameReference reference)
        {
            try
            {
                _ = this.AssemblyDefinitionResolver.Resolve(reference);
                return true;
            }
            catch (AssemblyResolutionException)
            {
                return false;
            }
        }

        /// <summary>Resolve an assembly by its name.</summary>
        /// <param name="name">The assembly name.</param>
        /// <remarks>
        /// This implementation returns the first loaded assembly which matches the short form of
        /// the assembly name, to resolve assembly resolution issues when rewriting
        /// assemblies (especially with Mono). Since this is meant to be called on <see cref="AppDomain.AssemblyResolve"/>,
        /// the implicit assumption is that loading the exact assembly failed.
        /// </remarks>
        public static Assembly? ResolveAssembly(string name)
        {
            string shortName = name.Split(',', 2).First(); // get simple name (without version and culture)
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(p => p.GetName().Name == shortName);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            foreach (IDisposable instance in this.Disposables)
                instance.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Track an object for disposal as part of the assembly loader.</summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="instance">The disposable instance.</param>
        private T TrackForDisposal<T>(T instance)
            where T : IDisposable
        {
            this.Disposables.Add(instance);
            return instance;
        }

        /****
        ** Assembly parsing
        ****/
        /// <summary>Get a list of referenced local assemblies starting from the mod assembly, ordered from leaf to root.</summary>
        /// <param name="file">The assembly file to load.</param>
        /// <param name="visitedAssemblyNames">The assembly names that should be skipped.</param>
        /// <param name="assemblyResolver">A resolver which resolves references to known assemblies.</param>
        /// <returns>Returns the rewrite metadata for the preprocessed assembly.</returns>
        private IEnumerable<AssemblyParseResult> GetReferencedLocalAssemblies(FileInfo file, HashSet<string> visitedAssemblyNames, IAssemblyResolver assemblyResolver)
        {
            // validate
            if (file.Directory == null)
                throw new InvalidOperationException($"Could not get directory from file path '{file.FullName}'.");
            if (!file.Exists)
                yield break; // not a local assembly

            // add the assembly's directory temporarily if needed
            // this is needed by F# mods which bundle FSharp.Core.dll, for example
            string? temporarySearchDir = null;
            if (this.AssemblyDefinitionResolver.TryAddSearchDirectory(file.DirectoryName))
                temporarySearchDir = file.DirectoryName;

            // read assembly
            AssemblyDefinition assembly;
            try
            {
                byte[] assemblyBytes = File.ReadAllBytes(file.FullName);
                Stream readStream = this.TrackForDisposal(new MemoryStream(assemblyBytes));

                try
                {
                    // read assembly with symbols
                    FileInfo symbolsFile = new(Path.Combine(Path.GetDirectoryName(file.FullName)!, Path.GetFileNameWithoutExtension(file.FullName)) + ".pdb");
                    if (symbolsFile.Exists)
                        this.SymbolReaderProvider.TryAddSymbolData(file.Name, () => this.TrackForDisposal(symbolsFile.OpenRead()));
                    assembly = this.TrackForDisposal(AssemblyDefinition.ReadAssembly(readStream, new ReaderParameters(ReadingMode.Immediate) { AssemblyResolver = assemblyResolver, InMemory = true, ReadSymbols = true, SymbolReaderProvider = this.SymbolReaderProvider }));
                }
                catch (SymbolsNotMatchingException ex)
                {
                    // read assembly without symbols
                    this.Monitor.Log($"      Failed loading PDB for '{file.Name}'. Technical details:\n{ex}");
                    readStream.Position = 0;
                    assembly = this.TrackForDisposal(AssemblyDefinition.ReadAssembly(readStream, new ReaderParameters(ReadingMode.Immediate) { AssemblyResolver = assemblyResolver, InMemory = true }));
                }
            }
            finally
            {
                // clean up temporary search directory
                if (temporarySearchDir is not null)
                    this.AssemblyDefinitionResolver.RemoveSearchDirectory(temporarySearchDir);
            }

            // skip if already visited
            if (visitedAssemblyNames.Contains(assembly.Name.Name))
            {
                yield return new AssemblyParseResult(file, null, AssemblyLoadStatus.AlreadyLoaded);
                yield break;
            }
            visitedAssemblyNames.Add(assembly.Name.Name);

            // yield referenced assemblies
            foreach (AssemblyNameReference dependency in assembly.MainModule.AssemblyReferences)
            {
                FileInfo dependencyFile = new(Path.Combine(file.Directory.FullName, $"{dependency.Name}.dll"));
                foreach (AssemblyParseResult result in this.GetReferencedLocalAssemblies(dependencyFile, visitedAssemblyNames, assemblyResolver))
                    yield return result;
            }

            // yield assembly
            yield return new AssemblyParseResult(file, assembly, AssemblyLoadStatus.Okay);
        }

        /****
        ** Assembly rewriting
        ****/
        /// <summary>Rewrite the types referenced by an assembly.</summary>
        /// <param name="mod">The mod for which the assembly is being loaded.</param>
        /// <param name="assembly">The assembly to rewrite.</param>
        /// <param name="loggedMessages">The messages that have already been logged for this mod.</param>
        /// <param name="logPrefix">A string to prefix to log messages.</param>
        /// <returns>Returns whether the assembly was modified.</returns>
        /// <exception cref="IncompatibleInstructionException">An incompatible CIL instruction was found while rewriting the assembly.</exception>
        private bool RewriteAssembly(IModMetadata mod, AssemblyDefinition assembly, HashSet<string> loggedMessages, string logPrefix)
        {
            ModuleDefinition module = assembly.MainModule;
            string filename = $"{assembly.Name.Name}.dll";

            // swap assembly references if needed (e.g. XNA => MonoGame)
            bool platformChanged = false;
            if (this.RewriteMods)
            {
                for (int i = 0; i < module.AssemblyReferences.Count; i++)
                {
                    // remove old assembly reference
                    if (this.AssemblyMap.RemoveNames.Any(name => module.AssemblyReferences[i].Name == name))
                    {
                        platformChanged = true;
                        module.AssemblyReferences.RemoveAt(i);
                        i--;
                        this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Rewrote {filename} for OS...");
                    }
                }
                if (platformChanged)
                {
                    // add target assembly references
                    foreach (AssemblyNameReference target in this.AssemblyMap.TargetReferences.Values)
                        module.AssemblyReferences.Add(target);

                    // rewrite type scopes to use target assemblies
                    IEnumerable<TypeReference> typeReferences = module.GetTypeReferences().OrderBy(p => p.FullName);
                    foreach (TypeReference type in typeReferences)
                        this.ChangeTypeScope(type);

                    // rewrite types using custom attributes
                    foreach (TypeDefinition type in module.GetTypes())
                    {
                        foreach (CustomAttribute attr in type.CustomAttributes)
                        {
                            foreach (CustomAttributeArgument conField in attr.ConstructorArguments)
                            {
                                if (conField.Value is TypeReference typeRef)
                                    this.ChangeTypeScope(typeRef);
                            }
                        }
                    }
                }
            }

            // reset instruction handlers
            IInstructionHandler[] handlers = this.InstructionHandlers;
            foreach (IInstructionHandler handler in handlers)
                handler.Reset();

            // find or rewrite code
            RecursiveRewriter rewriter = new(
                module: module,
                rewriteModule: curModule =>
                {
                    bool rewritten = false;
                    foreach (IInstructionHandler handler in handlers)
                        rewritten |= handler.Handle(curModule);
                    return rewritten;
                },
                rewriteType: (type, replaceWith) =>
                {
                    bool rewritten = false;
                    foreach (IInstructionHandler handler in handlers)
                        rewritten |= handler.Handle(module, type, replaceWith);
                    return rewritten;
                },
                rewriteInstruction: (ref Instruction instruction, ILProcessor cil) =>
                {
                    bool rewritten = false;
                    foreach (IInstructionHandler handler in handlers)
                        rewritten |= handler.Handle(module, cil, instruction);
                    return rewritten;
                }
            );
            bool anyRewritten = rewriter.RewriteModule();

            // handle rewrite flags
            foreach (IInstructionHandler handler in handlers)
            {
                foreach (var flag in handler.Flags)
                    this.ProcessInstructionHandleResult(mod, handler, flag, loggedMessages, logPrefix, filename);
            }

            return platformChanged || anyRewritten;
        }

        /// <summary>Process the result from an instruction handler.</summary>
        /// <param name="mod">The mod being analyzed.</param>
        /// <param name="handler">The instruction handler.</param>
        /// <param name="result">The result returned by the handler.</param>
        /// <param name="loggedMessages">The messages already logged for the current mod.</param>
        /// <param name="logPrefix">A string to prefix to log messages.</param>
        /// <param name="filename">The assembly filename for log messages.</param>
        private void ProcessInstructionHandleResult(IModMetadata mod, IInstructionHandler handler, InstructionHandleResult result, HashSet<string> loggedMessages, string logPrefix, string filename)
        {
            // get message template
            // ($phrase is replaced with the noun phrase or messages)
            string? template = null;
            switch (result)
            {
                case InstructionHandleResult.Rewritten:
                    template = $"{logPrefix}Rewrote {filename} to fix $phrase...";
                    break;

                case InstructionHandleResult.NotCompatible:
                    template = $"{logPrefix}Broken code in {filename}: $phrase.";
                    mod.SetWarning(ModWarning.BrokenCodeLoaded);
                    break;

                case InstructionHandleResult.DetectedGamePatch:
                    template = $"{logPrefix}Detected game patcher in assembly {filename}."; // no need for phrase, which would confusingly be 'Harmony 1.x' here
                    mod.SetWarning(ModWarning.PatchesGame);
                    break;

                case InstructionHandleResult.DetectedSaveSerializer:
                    template = $"{logPrefix}Detected possible save serializer change ($phrase) in assembly {filename}.";
                    mod.SetWarning(ModWarning.ChangesSaveSerializer);
                    break;

                case InstructionHandleResult.DetectedUnvalidatedUpdateTick:
                    template = $"{logPrefix}Detected reference to $phrase in assembly {filename}.";
                    mod.SetWarning(ModWarning.UsesUnvalidatedUpdateTick);
                    break;

                case InstructionHandleResult.DetectedConsoleAccess:
                    template = $"{logPrefix}Detected direct console access ($phrase) in assembly {filename}.";
                    mod.SetWarning(ModWarning.AccessesConsole);
                    break;

                case InstructionHandleResult.DetectedFilesystemAccess:
                    template = $"{logPrefix}Detected filesystem access ($phrase) in assembly {filename}.";
                    mod.SetWarning(ModWarning.AccessesFilesystem);
                    break;

                case InstructionHandleResult.DetectedShellAccess:
                    template = $"{logPrefix}Detected shell or process access ($phrase) in assembly {filename}.";
                    mod.SetWarning(ModWarning.AccessesShell);
                    break;

                case InstructionHandleResult.None:
                    break;

                default:
                    throw new NotSupportedException($"Unrecognized instruction handler result '{result}'.");
            }
            if (template == null)
                return;

            // format messages
            string phrase = handler.Phrases.Any()
                ? string.Join(", ", handler.Phrases)
                : handler.DefaultPhrase;
            this.Monitor.LogOnce(loggedMessages, template.Replace("$phrase", phrase));
        }

        /// <summary>Get the correct reference to use for compatibility with the current platform.</summary>
        /// <param name="type">The type reference to rewrite.</param>
        private void ChangeTypeScope(TypeReference? type)
        {
            // check skip conditions
            if (type == null || type.FullName.StartsWith("System."))
                return;

            // get assembly
            if (!this.TypeAssemblies.TryGetValue(type.FullName, out Assembly? assembly))
                return;

            // replace scope
            AssemblyNameReference assemblyRef = this.AssemblyMap.TargetReferences[assembly];
            type.Scope = assemblyRef;
        }
    }
}
