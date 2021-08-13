using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StardewModdingAPI.Framework.Reflection
{
    /// <summary>Generates proxy classes to access mod APIs through an arbitrary interface.</summary>
    internal class InterfaceProxyFactory
    {
        /*********
        ** Fields
        *********/
        /// <summary>The CLR module in which to create proxy classes.</summary>
        private readonly ModuleBuilder ModuleBuilder;

        /// <summary>The generated proxy types.</summary>
        private readonly IDictionary<string, InterfaceProxyBuilder> Builders = new Dictionary<string, InterfaceProxyBuilder>();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public InterfaceProxyFactory()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"StardewModdingAPI.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            this.ModuleBuilder = assemblyBuilder.DefineDynamicModule("StardewModdingAPI.Proxies");
        }

        /// <summary>Create an API proxy.</summary>
        /// <typeparam name="TInterface">The interface through which to access the API.</typeparam>
        /// <param name="instance">The API instance to access.</param>
        /// <param name="sourceModID">The unique ID of the mod consuming the API.</param>
        /// <param name="targetModID">The unique ID of the mod providing the API.</param>
        public TInterface CreateProxy<TInterface>(object instance, string sourceModID, string targetModID)
            where TInterface : class
        {
            lock (this.Builders)
            {
                // validate
                if (instance == null)
                    throw new InvalidOperationException("Can't proxy access to a null API.");
                if (!typeof(TInterface).IsInterface)
                    throw new InvalidOperationException("The proxy type must be an interface, not a class.");

                // get proxy type
                Type targetType = instance.GetType();
                string proxyTypeName = $"StardewModdingAPI.Proxies.From<{sourceModID}_{typeof(TInterface).FullName}>_To<{targetModID}_{targetType.FullName}>";
                if (!this.Builders.TryGetValue(proxyTypeName, out InterfaceProxyBuilder builder))
                {
                    builder = new InterfaceProxyBuilder(proxyTypeName, this.ModuleBuilder, typeof(TInterface), targetType);
                    this.Builders[proxyTypeName] = builder;
                }

                // create instance
                return (TInterface)builder.CreateInstance(instance);
            }
        }
    }
}
