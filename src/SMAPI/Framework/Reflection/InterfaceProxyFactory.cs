using System.Reflection;
using System.Reflection.Emit;
using Nanoray.Pintail;

namespace StardewModdingAPI.Framework.Reflection;

/// <inheritdoc />
internal class InterfaceProxyFactory : IInterfaceProxyFactory
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying proxy type builder.</summary>
    private readonly IProxyManager<string> ProxyManager;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public InterfaceProxyFactory()
    {
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"StardewModdingAPI.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("StardewModdingAPI.Proxies");
        this.ProxyManager = new ProxyManager<string>(moduleBuilder, new ProxyManagerConfiguration<string>(
            proxyPrepareBehavior: ProxyManagerProxyPrepareBehavior.Eager,
            proxyObjectInterfaceMarking: ProxyObjectInterfaceMarking.Disabled
        ));
    }

    /// <inheritdoc />
    public TInterface CreateProxy<TInterface>(object instance, string sourceModID, string targetModID)
        where TInterface : class
    {
        return this.ProxyManager.ObtainProxy<string, TInterface>(instance, targetContext: targetModID, proxyContext: sourceModID);
    }
}
