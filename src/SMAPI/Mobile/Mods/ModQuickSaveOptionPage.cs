using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI.Framework;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile;

static class ModQuickSaveOptionPage
{
    static string QuickSaveButtonText = "Quick Save";
    static string LoadQuickSaveButtonText = "Load Last Quick Save";
    static bool m_isQuickSaveModAvailable = false;
    static Assembly modAssembly;
    static Type MainType;
    static MethodInfo TrySaveMethod;
    static MethodInfo TryLoadMethod;
    static IMonitor Monitor;

    internal static void Init(AndroidModFixManager modFix)
    {
        Monitor = SCore.Instance.GetMonitorForGame();
        modFix.RegisterOnModLoaded("QuickSave.dll", (asm) =>
        {
            m_isQuickSaveModAvailable = true;
            modAssembly = asm;
            MainType = modAssembly.GetType("QuickSave.Lib.Main");
            TrySaveMethod = AccessTools.Method(MainType, "TrySave");
            TryLoadMethod = AccessTools.Method(MainType, "TryLoad");
        });
    }

    static OptionsPage m_optionPage;
    internal static void SetupOptionPage(OptionsPage page, ref List<OptionsElement> options)
    {
        if (!m_isQuickSaveModAvailable)
            return;

        m_optionPage = page;
        var btnQuickSave = new OptionsButton(QuickSaveButtonText, OnClickQuickSave);
        var btnLoadQuickSave = new OptionsButton(LoadQuickSaveButtonText, OnClickLoadQuickSave);
        options.Insert(2, btnQuickSave);
        options.Insert(3, btnLoadQuickSave);
    }

    static void TryCloseMenu()
    {
        if (m_optionPage == null)
            return;

        var OnTapCloseButtonMethod = AccessTools.Method(m_optionPage.GetType(), "OnTapCloseButton");
        OnTapCloseButtonMethod.Invoke(m_optionPage, []);
    }

    static void OnClickQuickSave()
    {
        try
        {
            TrySaveMethod.Invoke(null, null);
            TryCloseMenu();
        }
        catch (Exception ex)
        {
            Monitor.Log($"Error invoking QuickSave TrySave(): {ex}", LogLevel.Error);
        }
    }

    static void OnClickLoadQuickSave()
    {
        try
        {
            TryLoadMethod.Invoke(null, null);
            TryCloseMenu();
        }
        catch (Exception ex)
        {
            Monitor.Log($"Error invoking QuickSave TryLoad(): {ex}", LogLevel.Error);
        }
    }

}
