using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;

namespace StardewModdingAPI.Framework.ModLoading.Symbols;

/// <summary>Reads symbol data for an assembly.</summary>
internal class SymbolReader : ISymbolReader
{
    /*********
    ** Fields
    *********/
    /// <summary>The module for which to read symbols.</summary>
    private readonly ModuleDefinition Module;

    /// <summary>The symbol file stream.</summary>
    private readonly Stream Stream;

    /// <summary>The underlying symbol reader.</summary>
    private ISymbolReader Reader;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="module">The module for which to read symbols.</param>
    /// <param name="stream">The symbol file stream.</param>
    public SymbolReader(ModuleDefinition module, Stream stream)
    {
        this.Module = module;
        this.Stream = stream;
        this.Reader = new NativePdbReaderProvider().GetSymbolReader(module, stream);
    }

    /// <summary>Get the symbol writer provider for the assembly.</summary>
    public ISymbolWriterProvider GetWriterProvider()
    {
        return new PortablePdbWriterProvider();
    }

    /// <summary>Process a debug header in the symbol file.</summary>
    /// <param name="header">The debug header.</param>
    public bool ProcessDebugHeader(ImageDebugHeader header)
    {
        try
        {
            return this.Reader.ProcessDebugHeader(header);
        }
        catch
        {
            this.Reader.Dispose();
            this.Reader = new PortablePdbReaderProvider().GetSymbolReader(this.Module, this.Stream);
            return this.Reader.ProcessDebugHeader(header);
        }
    }

    /// <summary>Read the method debug information for a method in the assembly.</summary>
    /// <param name="method">The method definition.</param>
    public MethodDebugInformation Read(MethodDefinition method)
    {
        return this.Reader.Read(method);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Reader.Dispose();
    }
}
