using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Symbols;

/// <summary>Provides assembly symbol writers for Mono.Cecil.</summary>
internal class SymbolWriterProvider : ISymbolWriterProvider
{
    /*********
    ** Fields
    *********/
    /// <summary>The default symbol writer provider.</summary>
    private readonly ISymbolWriterProvider DefaultProvider = new DefaultSymbolWriterProvider();

    /// <summary>The symbol writer provider for the portable PDB format.</summary>
    private readonly ISymbolWriterProvider PortablePdbProvider = new PortablePdbWriterProvider();


    /*********
    ** Public methods
    *********/
    /// <summary>Get a symbol writer for a given module and assembly path.</summary>
    /// <param name="module">The loaded assembly module.</param>
    /// <param name="fileName">The assembly name.</param>
    public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
    {
        return this.DefaultProvider.GetSymbolWriter(module, fileName);
    }

    /// <summary>Get a symbol writer for a given module and symbol stream.</summary>
    /// <param name="module">The loaded assembly module.</param>
    /// <param name="symbolStream">The loaded symbol file stream.</param>
    public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
    {
        // Not implemented in default native pdb writer, so fallback to portable
        return this.PortablePdbProvider.GetSymbolWriter(module, symbolStream);
    }
}
