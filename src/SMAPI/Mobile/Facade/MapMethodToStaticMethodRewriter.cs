using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Mobile.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StardewModdingAPI.Mobile;

public sealed class MapMethodWithCallback
{
    public readonly string typeFullName;

    public delegate bool CallbackDelegate(
        MapMethodWithCallback mapMethod,
        MethodReference methodReference,
        ModuleDefinition module,
        ILProcessor cil,
        Instruction instruction);

    public readonly CallbackDelegate callback;

    public MapMethodWithCallback(string typeFullName, CallbackDelegate callback)
    {
        this.typeFullName = typeFullName;
        this.callback = callback;
    }
}


internal class MapMethodToStaticMethodRewriter : BaseInstructionHandler
{
    static string FormatCecilType(Type type)
    {
        return RewriteHelper.GetFullCecilName(type);
    }
    static string FormatCecilParameterList(ParameterInfo[] parameters)
    {
        var paramTypes = parameters.Select(p => RewriteHelper.GetFullCecilName(p.ParameterType));
        return string.Join(",", paramTypes);
    }

    public sealed class MapMethodToStaticKeyValue(MethodInfo srcMethod, MethodInfo newMethod)
    {
        public readonly MethodInfo srcMethod = srcMethod;
        public readonly MethodInfo newMethod = newMethod;
        public string srcMethodFullName = GetMethodFullName(srcMethod);
        public string newMethodFullName = GetMethodFullName(newMethod);
        public void AddPramToSrc(Type newType)
        {
            bool isEmptyParam = this.srcMethodFullName[this.srcMethodFullName.Length - 2] == '(';
            if (isEmptyParam)
            {
                this.srcMethodFullName = this.srcMethodFullName.Replace(")", $"{newType.FullName})");
            }
            else
            {
                this.srcMethodFullName = this.srcMethodFullName.Replace(")", $",{newType.FullName})");
            }
        }
    }
    //key == Src Method FullName
    public readonly Dictionary<string, MapMethodToStaticKeyValue> MapMethods = new();

    //Key: {return Type} {method.Name};
    public readonly Dictionary<string, MapMethodWithCallback> MapMethodWithCallbacks = new();

    public MapMethodToStaticMethodRewriter() : base("Rewrite map method to static method")
    {

    }

    public static string GetMethodFullName(MethodInfo method)
    {
        var type = method.DeclaringType;
        string returnFullName = FormatCecilType(method.ReturnType);
        string paramsFullName = FormatCecilParameterList(method.GetParameters());
        return $"{returnFullName} {type}::{method.Name}({paramsFullName})";
    }
    public delegate bool SelectMethodCallbackDelegate(MethodInfo method);
    public delegate void PostEditAddMethod(MapMethodToStaticKeyValue mapMethod);

    public MethodInfo HandleSelectorMethod(Type type, SelectMethodCallbackDelegate selectorCallback)
    {
        var methods = type.GetMethods();
        foreach (var m in methods)
        {
            if (selectorCallback.Invoke(m))
                return m;
        }
        return null;
    }

    public MapMethodToStaticMethodRewriter Add(Type srcType,
        SelectMethodCallbackDelegate srcMethodSelector,
        Type newType,
        SelectMethodCallbackDelegate newMethodSelector,
        PostEditAddMethod option = null)
    {

        var srcMethod = this.HandleSelectorMethod(srcType, srcMethodSelector);
        if (srcMethod == null)
        {
            Console.WriteLine("Error not found src method in type: " + srcType);
            return this;
        }

        var newMethod = this.HandleSelectorMethod(newType, newMethodSelector);
        if (newMethod == null)
        {
            Console.WriteLine("Error not found new method in type: " + newType);
            return this;
        }

        var mapMethod = new MapMethodToStaticKeyValue(srcMethod, newMethod);

        if (option != null)
            option.Invoke(mapMethod);

        //added make sure you finsih edit src & new method full name
        this.MapMethods.TryAdd(mapMethod.srcMethodFullName, mapMethod);
        return this;
    }
    public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {
        var thisMethod = RewriteHelper.AsMethodReference(instruction);
        if (thisMethod == null)
            return false;
        string thisMethodFullName = thisMethod.FullName;

#if false
        //debug only
        if (thisMethodFullName.Contains("BundleType"))
        {
            Console.WriteLine("il method ref found: " + thisMethodFullName);
        }
#endif
        try
        {
            if (this.MapMethodWithCallbacks.TryGetValue(thisMethod.DeclaringType.FullName, out var mapMethodStartWith))
            {
                //Console.WriteLine("try replace method case 1: " + thisMethodFullName);
                bool isMark = mapMethodStartWith.callback(mapMethodStartWith, thisMethod, module, cil, instruction);
                if (isMark)
                {
                    this.Phrases.Add($"Replace Method: {thisMethodFullName}");
                    return this.MarkRewritten();
                }
            }

            if (this.MapMethods.ContainsKey(thisMethodFullName))
            {
                //Console.WriteLine("try replace method case 2: " + thisMethodFullName);
                instruction.Operand = module.ImportReference(this.MapMethods[thisMethodFullName].newMethod);
                this.Phrases.Add($"Replace Method: {thisMethodFullName}");

                return this.MarkRewritten();
            }

            if (this.MapMethodWithFullName.ContainsKey(thisMethodFullName))
            {
                //Console.WriteLine("try replace method case 3: " + thisMethodFullName);
                instruction.Operand = module.ImportReference(this.MapMethodWithFullName[thisMethodFullName]);
                this.Phrases.Add($"Replace Method: {thisMethodFullName}");

                return this.MarkRewritten();
            }
        }
        catch (Exception ex)
        {
            var monitor = SCore.Instance.SMAPIMonitor;
            monitor.Log(ex.GetLogSummary(), LogLevel.Error);
        }
        return false;
    }


    public MapMethodToStaticMethodRewriter AddWithTypeFullName(
        string typeFullName,
        MapMethodWithCallback.CallbackDelegate callback)
    {

        var mapMethod = new MapMethodWithCallback(typeFullName, callback);
        this.MapMethodWithCallbacks.TryAdd(typeFullName, mapMethod);

        return this;
    }

    Dictionary<string, MethodInfo> MapMethodWithFullName = new();
    public MapMethodToStaticMethodRewriter AddWithMethodFullName(
        string originalMethodFullName, MethodInfo newMethodToReplace)
    {

        this.MapMethodWithFullName[originalMethodFullName] = newMethodToReplace;
        return this;
    }
}
