using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewModdingAPI.Framework;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using Iced.Intel;
using MonoMod.Utils;
using StardewValley.Extensions;
using Android.Telecom;
using static HarmonyLib.Code;

namespace StardewModdingAPI.Mobile.Mods;

internal static partial class SpaceCoreFix
{
    static Assembly modAssembly;
    public const string SpaceCoreDllFileName = "SpaceCore.dll";
    public static void Init(AndroidModFixManager androidModFix)
    {
        androidModFix.RegisterOnModLoaded(SpaceCoreDllFileName, OnModLoaded);
    }
    static void OnRewriterAssembly(Mono.Cecil.AssemblyDefinition assemblyDef)
    {
        Console.WriteLine("try rewrite SpaceCore.dll");
        var mainModule = assemblyDef.MainModule;

        //Fix FarmerAlwaysAcceptVirtualCurrencyPatch1
        {
            var FarmerAlwaysAcceptVirtualCurrencyPatch1
                = mainModule.GetType("SpaceCore.VanillaAssetExpansion.FarmerAlwaysAcceptVirtualCurrencyPatch1");

            var harmonyPatchAttribute = FarmerAlwaysAcceptVirtualCurrencyPatch1.CustomAttributes[0];
            var harmonyPatch_Types = harmonyPatchAttribute.ConstructorArguments[2];
            var argTypeArray = harmonyPatch_Types.Value as CustomAttributeArgument[];

            var boolType = mainModule.ImportReference(typeof(bool));
            var newArgBoolType = new CustomAttributeArgument(boolType, boolType);
            var argTypeList = new List<CustomAttributeArgument>();
            argTypeList.AddRange(argTypeArray);
            argTypeList.Add(newArgBoolType);
            harmonyPatchAttribute.ConstructorArguments[2] = new CustomAttributeArgument(
                harmonyPatch_Types.Type,
                argTypeList.ToArray()
            );
        }
    }

    static void OnModLoaded(Assembly asm)
    {
        modAssembly = asm;
        var monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("Start SpaceCoreFix");
        try
        {
            var harmony = AndroidPatcher.harmony;
            var SpaceCoreModEntry = modAssembly.GetType("SpaceCore.SpaceCore");
            harmony.Patch(
                original: AccessTools.Method(SpaceCoreModEntry, "GatherLocals"),
                prefix: AccessTools.Method(typeof(SpaceCoreFix), nameof(Prefix_GatherLocals))
            );
            monitor.Log("Disable GatherLocals()");
        }
        catch (Exception ex)
        {
            monitor.Log(ex.ToString());
        }

        monitor.Log("Done SpaceCoreFix");
    }

    static bool Prefix_GatherLocals()
    {
        var monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("call GatherLocals() for android");
        try
        {

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return false;
    }
}
