using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal;

namespace StardewModdingAPI.Mobile;
internal class AndroidModFixManager
{
    class OnModLoadedCallbackList
    {
        List<Action<Assembly>> callbackList = new();
        public void InvokeAll(Assembly asm)
        {
            foreach (var callback in this.callbackList)
            {
                callback.Invoke(asm);
            }
        }

        internal void AddCallback(Action<Assembly> callback)
        {
            this.callbackList.Add(callback);
        }
    }
    Dictionary<string, OnModLoadedCallbackList> OnModLoadedRegistry = new();
    public static AndroidModFixManager Instance { get; private set; }
    public IMonitor monitor;
    AndroidModFixManager()
    {
        Instance = this;
        this.monitor = SCore.Instance.SMAPIMonitor;
    }

    public static AndroidModFixManager Init()
    {
        Instance = new();
        AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

        return Instance;
    }

    static void CurrentDomain_AssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        Instance.OnAsmLoad(args.LoadedAssembly);
    }
    public static Action<Assembly> OnModLoaded;
    private void OnAsmLoad(Assembly asm)
    {
        OnModLoaded?.Invoke(asm);

        string name = asm.GetName().Name;
        string dllFileName = name + ".dll";
        if (this.OnModLoadedRegistry.TryGetValue(dllFileName, out var cbList))
        {
            try
            {
                cbList.InvokeAll(asm);
            }
            catch (Exception ex)
            {
                var monitor = SCore.Instance.SMAPIMonitor;
                monitor.Log(ex.ToString(), LogLevel.Error);
            }
        }
    }

    public void RegisterOnModLoaded(string asmDllFileName, Action<Assembly> callback)
    {
        //create new item
        if (this.OnModLoadedRegistry.TryGetValue(asmDllFileName, out var cbList) is false)
        {
            cbList = new();
            this.OnModLoadedRegistry.Add(asmDllFileName, cbList);
        }
        //added
        cbList.AddCallback(callback);

    }

    //key: AssemblyName, value: callback
    Dictionary<string, Action<Mono.Cecil.AssemblyDefinition>> OnRewriteModDictionary = new();
    internal void RegisterRewriteModAssemblyDef(string v, Action<Mono.Cecil.AssemblyDefinition> callback)
    {
        this.OnRewriteModDictionary.TryAdd(v, callback);
    }

    internal void TryRewriteMod(Framework.ModLoading.AssemblyParseResult assembly, out bool hasRewrite, out Exception exception)
    {
        string assemblyName = assembly.Definition.Name.Name;
        hasRewrite = false;
        exception = null;

        if (this.OnRewriteModDictionary.TryGetValue(assemblyName, out var callback))
        {
            var monitor = SCore.Instance.SMAPIMonitor;
            this.OnRewriteModDictionary.Remove(assemblyName);

            monitor.Log("Try ModFixManager rewrite mod: " + assembly.Definition.Name);
            try
            {
                callback.Invoke(assembly.Definition);
                hasRewrite = true;
                monitor.Log("Done rewrite mod: " + assembly.Definition.Name);
            }
            catch (Exception ex)
            {
                monitor.Log(ex.ToString(), LogLevel.Error);
                exception = ex;
            }
        }
    }

    internal delegate void OnPostfixModEntryDelegate(IMod mod);
    static Dictionary<string, OnPostfixModEntryDelegate> EventOnPostfixModEntry = new();
    internal void RegisterOnPostModEntry(string asmFileName, OnPostfixModEntryDelegate onPostModEntry)
    {
        EventOnPostfixModEntry[asmFileName] = onPostModEntry;
    }

    internal void OnPostfixModEntry(IMod mod)
    {
        try
        {
            string asmFileName = new AssemblyName(mod.GetType().Assembly.FullName).Name + ".dll";
            if (EventOnPostfixModEntry.TryGetValue(asmFileName, out var callback))
            {
                callback(mod);
            }
        }
        catch (Exception ex)
        {
            var monitor = SCore.Instance.SMAPIMonitor;
            monitor.Log(ex.GetLogSummary(), LogLevel.Error);
        }
    }
}
