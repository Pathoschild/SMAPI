using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Mods;

internal static class UnlockableBundlesModFix
{
    static IMonitor monitor;
    static Assembly asm;
    const string asmFileName = "Unlockable Bundles.dll";
    public static void Init(AndroidModFixManager modFix)
    {
        monitor = modFix.monitor;
        modFix.RegisterOnModLoaded(asmFileName, OnModLoaded);
        modFix.RegisterOnPostModEntry(asmFileName, OnPostModEntry);
    }
    static void OnPostModEntry(IMod mod)
    {
        monitor.Log("OnPostModEntry on UnlockableBundlesModFix");
        var harmony = new Harmony("DLX.Bundles");
        var InvPage_Ctor = AccessTools.Constructor(typeof(InventoryPage), [typeof(int), typeof(int), typeof(int), typeof(int)]);
        harmony.Unpatch(InvPage_Ctor, Mod_Constructor_Postfix_MethodInfo);
        monitor.Log("Unpatched: " + Mod_Constructor_Postfix_MethodInfo);
    }

    static Type Mod_InventoryPage_Type;
    static MethodInfo Mod_Constructor_Postfix_MethodInfo;
    private static void OnModLoaded(Assembly assembly)
    {
        monitor.Log("Start UnlockableBundlesModFix");
        asm = assembly;

        var hp = AndroidPatcher.harmony;
        try
        {
            Mod_InventoryPage_Type = asm.GetType("Unlockable_Bundles.Lib._InventoryPage");
            Mod_Constructor_Postfix_MethodInfo = AccessTools.Method(Mod_InventoryPage_Type, "Constructor_Postfix");

            //hp.Patch(
            //    original: AccessTools.Constructor(typeof(InventoryPage), [typeof(int), typeof(int), typeof(int), typeof(int)]),
            //    postfix: new(typeof(UnlockableBundlesModFix), nameof(Postfix_InvPage_Ctor))
            //);

            hp.Patch(
                original: AccessTools.Method(typeof(GameMenu), "setupMenus"),
                postfix: new(typeof(UnlockableBundlesModFix), nameof(Postfix_GameMenu_setupMenus))
            );
        }
        catch (Exception ex)
        {
            monitor.Log(ex.GetLogSummary(), LogLevel.Error);
        }

        monitor.Log("Done UnlockableBundlesModFix");
    }

    private static void Postfix_GameMenu_setupMenus(
        GameMenu __instance,
        bool standardTabs, bool optionsOnly)
    {
        Console.WriteLine("on post fix GameMenu.setupMenus");
        if (__instance.pages.Count == 0)
            return;

        var inventoryPage = __instance.pages[0] as InventoryPage;
        var menu = inventoryPage.inventory;

        //prepare state
        inventoryPage.organizeButton = menu.organizeButton;
        inventoryPage.junimoNoteIcon = __instance.junimoNoteIcon;

        //ready
        Mod_Constructor_Postfix_MethodInfo.Invoke(null, [inventoryPage]);

        //restore
        inventoryPage.organizeButton = inventoryPage.junimoNoteIcon = null;
    }
}
