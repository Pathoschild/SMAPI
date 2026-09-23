using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

[HarmonyPatch]
internal static class LetterViewerMenuRewriter
{
    public const string OnPageChange_FullName
        = "System.Void StardewValley.Menus.LetterViewerMenu::OnPageChange()";
    public static MethodInfo OnPageChangeProxy_MethodInfo
        = AccessTools.Method(typeof(LetterViewerMenuRewriter), nameof(OnPageChange_Proxy));
    static void OnPageChange_Proxy(this LetterViewerMenu menu)
    {
    }

    static int m_pageCache = 0;

    static void HandleOnPageChange(LetterViewerMenu menu)
    {
        var OnPageChange_MethodInfo = AccessTools.Method(menu.GetType(), "OnPageChange");
        if (OnPageChange_MethodInfo == null)
            return;

        OnPageChange_MethodInfo.Invoke(menu, null);
    }

    public static void ApplyPatch(Harmony h)
    {
        var LetterMenuType = typeof(LetterViewerMenu);

        h.Patch(
            original: AccessTools.Constructor(LetterMenuType, [typeof(string)]),
            postfix: new(typeof(LetterViewerMenuRewriter),
                nameof(Postfix_LetterMenuCtor1))
        );

        h.Patch(
            original: AccessTools.Constructor(LetterMenuType, [typeof(int)]),
            postfix: new(typeof(LetterViewerMenuRewriter),
                nameof(Postfix_LetterMenuCtor2))
        );

        h.Patch(
            original: AccessTools.Constructor(LetterMenuType,
                [typeof(string), typeof(string), typeof(bool)]),
            postfix: new(typeof(LetterViewerMenuRewriter),
                nameof(Postfix_LetterMenuCtor3))
        );
    }


    static void Postfix_LetterMenuCtor1(LetterViewerMenu __instance, string text)
    {
        m_pageCache = __instance.page;
        HandleOnPageChange(__instance);
    }

    static void Postfix_LetterMenuCtor2(LetterViewerMenu __instance, int secretNoteIndex)
    {
        m_pageCache = __instance.page;
        HandleOnPageChange(__instance);
    }

    static void Postfix_LetterMenuCtor3(LetterViewerMenu __instance,
        string mail, string mailTitle, bool fromCollection = false)
    {
        m_pageCache = __instance.page;
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(LetterViewerMenu), nameof(LetterViewerMenu.receiveGamePadButton))]
    static void Prefix_receiveGamePadButton(LetterViewerMenu __instance, Buttons b)
    {
        m_pageCache = __instance.page;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LetterViewerMenu), nameof(LetterViewerMenu.receiveGamePadButton))]
    static void Postfix_receiveGamePadButton(LetterViewerMenu __instance, Buttons b)
    {
        var prevPage = m_pageCache;
        m_pageCache = __instance.page;

        if (m_pageCache != prevPage)
        {
            HandleOnPageChange(__instance);
        }
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(LetterViewerMenu), nameof(LetterViewerMenu.receiveLeftClick))]
    static void Prefix_receiveLeftClick(LetterViewerMenu __instance, int x, int y, bool playSound = true)
    {
        m_pageCache = __instance.page;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LetterViewerMenu), nameof(LetterViewerMenu.receiveLeftClick))]
    static void Postfix_receiveLeftClick(LetterViewerMenu __instance, int x, int y, bool playSound = true)
    {
        var prevPage = m_pageCache;
        m_pageCache = __instance.page;

        if (m_pageCache != prevPage)
        {
            HandleOnPageChange(__instance);
        }
    }
}
