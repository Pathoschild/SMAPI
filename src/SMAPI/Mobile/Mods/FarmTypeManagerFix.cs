using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewValley.Monsters;
using StardewValley;
using StardewModdingAPI.Framework;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Mobile.Mods;

internal static class FarmTypeManagerFix
{
    public const string DllFileName = "FarmTypeManager.dll";
    internal static void Init(AndroidModFixManager modFix)
    {
        modFix.RegisterOnModLoaded(DllFileName, OnAsmLoaded);
    }
    static void OnAsmLoaded(Assembly asm)
    {
        IMonitor monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("start FarmTypeManagerFix");
        try
        {
            ApplyFix(asm);
        }
        catch (Exception ex)
        {
            monitor.Log(ex.ToString(), LogLevel.Error);
        }
        monitor.Log("done FarmTypeManagerFix");
    }

    static void ApplyFix(Assembly ftm)
    {
        IMonitor monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("Start patching fix");
        Harmony harmony = new Harmony(nameof(FarmTypeManagerFix));
        Type? HarmonyPatch_OptimizeMonsterCode = ftm.GetType("FarmTypeManager.ModEntry+HarmonyPatch_OptimizeMonsterCode");
        MethodInfo ApplyPatch_Original = AccessTools.Method(HarmonyPatch_OptimizeMonsterCode, "ApplyPatch");
        harmony.Patch(
            original: ApplyPatch_Original,
            prefix: AccessTools.Method(typeof(FarmTypeManagerFix), nameof(ApplyPatch_EmptyImpl))
        );

        monitor.Log($"Applying Harmony patch \"{nameof(FarmTypeManagerFix)}\": prefixing SDV method \"Monster.findPlayer\".", LogLevel.Trace);
        harmony.Patch(
            original: AccessTools.Method(typeof(Monster), "findPlayer", new Type[] { }),
            prefix: new HarmonyMethod(typeof(FarmTypeManagerFix), nameof(Monster_findPlayer_Prefix))
        );

        monitor.Log($"Applying Harmony patch \"{nameof(FarmTypeManagerFix)}\": postfixing SDV method \"Monster.findPlayer\".", LogLevel.Trace);
        harmony.Patch(
            original: AccessTools.Method(typeof(Monster), "findPlayer", new Type[] { }),
            postfix: new HarmonyMethod(typeof(FarmTypeManagerFix), nameof(Monster_findPlayer_Postfix))
        );
    }
    public static bool ApplyPatch_EmptyImpl(Harmony harmony)
    {
        return false;
    }

    public static void OptimizeMonsterCode_ApplyPatch(Harmony harmony)
    {
        IMonitor monitor = SCore.Instance.SMAPIMonitor;
        try
        {
            //apply Harmony patches
            //bug in here
            monitor.Log($"Applying Harmony patch \"{nameof(OptimizeMonsterCode_ApplyPatch)}\": prefixing SDV method \"GameLocation.isCollidingPosition\".", LogLevel.Trace);

            monitor.Log($"Applying Harmony patch \"{nameof(OptimizeMonsterCode_ApplyPatch)}\": prefixing SDV method \"Monster.findPlayer\".", LogLevel.Trace);
            harmony.Patch(
                original: AccessTools.Method(typeof(Monster), "findPlayer", new Type[] { }),
                prefix: new HarmonyMethod(typeof(FarmTypeManagerFix), nameof(Monster_findPlayer_Prefix))
            );

            monitor.Log($"Applying Harmony patch \"{nameof(FarmTypeManagerFix)}\": postfixing SDV method \"Monster.findPlayer\".", LogLevel.Trace);
            harmony.Patch(
                original: AccessTools.Method(typeof(Monster), "findPlayer", new Type[] { }),
                postfix: new HarmonyMethod(typeof(FarmTypeManagerFix), nameof(Monster_findPlayer_Postfix))
            );
        }
        catch (Exception ex)
        {
            monitor.LogOnce($"Harmony patch \"{nameof(FarmTypeManagerFix)}\" failed to apply. Monsters might slow the game down or cause errors. Full error message: \n{ex.ToString()}", LogLevel.Error);
        }
    }

    private static bool Monster_findPlayer_Prefix(ref Farmer __result)
    {
        IMonitor monitor = SCore.Instance.SMAPIMonitor;

        try
        {
            if (!Context.IsMultiplayer) //if this is NOT a multiplayer session
            {
                __result = Game1.player; //return the current player
                return false; //skip the original method
            }
            else
                return true; //call the original method
        }
        catch (Exception ex)
        {
            monitor.LogOnce($"Harmony patch \"{nameof(Monster_findPlayer_Prefix)}\" has encountered an error. Monsters might cause the game to run slower in single-player mode. Full error message: \n{ex.ToString()}", LogLevel.Error);
            return true; //call the original method
        }
    }

    private static void Monster_findPlayer_Postfix(ref Farmer __result)
    {
        IMonitor monitor = SCore.Instance.SMAPIMonitor;
        try
        {
            if (__result == null) //if this method failed to return a farmer (possible due to other mods' patches, multiplayer/threading issues, etc)
            {
                __result = Game1.player; //assign the local player (should never be null because it causes immediate crashes in most contexts, but may still be possible)

                if (__result == null) //if the result is somehow still null
                {
                    monitor.LogOnce($"Monster.findPlayer and Game1.player both returned null. If errors occur, please share your full log file with this mod's developer.", LogLevel.Debug);
                    return;
                }
                else
                {
                    monitor.LogOnce($"Monster.findPlayer returned null. Using Game1.player instead.", LogLevel.Trace);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            monitor.LogOnce($"Harmony patch \"{nameof(Monster_findPlayer_Postfix)}\" has encountered an error. Full error message: \n{ex.ToString()}", LogLevel.Error);
            return; //call the original method
        }
    }
}
