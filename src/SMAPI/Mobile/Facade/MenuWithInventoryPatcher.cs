using System;
using System.Diagnostics;
using Android.AdServices.Topics;
using HarmonyLib;
using Iced.Intel;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Mobile;

namespace StardewModdingAPI.Mobile.Facade;

[HarmonyPatch]
static class MenuWithInventoryPatcher
{
    [HarmonyPatch(methodType: MethodType.Constructor)]
    [HarmonyPatch(typeof(MenuWithInventory))]
    [HarmonyPatch(new Type[] {
        typeof(InventoryMenu.highlightThisItem), // Parameter 1: highlighterMethod
        typeof(bool), // Parameter 2: okButton
        typeof(bool), // Parameter 3: trashCan
        typeof(int),  // Parameter 4: xPositionOnScreen
        typeof(int),  // Parameter 5: yPositionOnScreen
        typeof(int),  // Parameter 6: width
        typeof(int)   // Parameter 7: height
    })]
    [HarmonyPostfix]
    static void FixCtor(MenuWithInventory __instance,
        InventoryMenu.highlightThisItem highlighterMethod = null,
        bool okButton = false, bool trashCan = false,
        int xPositionOnScreen = 0, int yPositionOnScreen = 0,
        int width = 1280, int height = 720)
    {

        var menu = __instance;
        int xPosOnScreen = menu.xPositionOnScreen;
        int yPosOnScreen = menu.yPositionOnScreen;

        if (trashCan)
        {
            __instance.trashCan = new ClickableTextureComponent(
                new Rectangle(xPosOnScreen + width + 4, yPosOnScreen + height - 192 - 32 - IClickableMenu.borderWidth - 104, 64, 104), Game1.mouseCursors, new Rectangle(564 + Game1.player.trashCanLevel * 18, 102, 18, 26), 4f)
            {
                myID = 5948,
                downNeighborID = 4857,
                leftNeighborID = 12,
                upNeighborID = 106
            };
            Console.WriteLine("Done patch create trashCan Obj in side: " + __instance);
        }
    }
}
