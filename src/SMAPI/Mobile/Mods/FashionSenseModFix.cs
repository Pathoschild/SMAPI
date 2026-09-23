using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Mods;

internal static class FashionSenseModFix
{
    public const string DllFileName = "FashionSense.dll";
    internal static void Init(AndroidModFixManager modFix)
    {
        modFix.RegisterOnModLoaded(DllFileName, OnAsmLoaded);
    }
    // cache type
    static Type CharacterCustomizationPatch_Type;
    static Type SimpleSlider_Type;

    static void OnAsmLoaded(Assembly assembly)
    {
        IMonitor monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("start FashionSense ModFix");
        try
        {
            var harmony = new Harmony(nameof(FashionSenseModFix));

            CharacterCustomizationPatch_Type = assembly.GetType("FashionSense.Framework.Patches.Menus.CharacterCustomizationPatch");
            SimpleSlider_Type = assembly.GetType("FashionSense.Framework.UI.Components.SimpleSlider");

            {
                var Apply_Method = AccessTools.Method(CharacterCustomizationPatch_Type, "Apply");
                harmony.Patch(Apply_Method, prefix: AccessTools.Method(typeof(FashionSenseModFix), nameof(Prefix_Apply)));
            }

            {
                var SimpleSlider_Ctor = SimpleSlider_Type.FirstConstructor((m) => true);
                harmony.Patch(SimpleSlider_Ctor,
                    postfix: AccessTools.Method(typeof(FashionSenseModFix), nameof(SimpleSlider_Ctor_Postfix)));
            }

            monitor.Log("successfully FashionSense ModFix");
        }
        catch (Exception ex)
        {
            monitor.Log(ex.ToString(), LogLevel.Error);
        }
    }

    static bool Prefix_Apply(Harmony harmony)
    {
        IMonitor monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("start prefix Apply(harmony)");

        var charCustomize = typeof(CharacterCustomization);

        // PerformHoverActionPostfix
        {
            //var PerformHoverActionPostfix_Method = AccessTools.Method(
            //    CharacterCustomizationPatch_Type,
            //    "PerformHoverActionPostfix");

            //harmony.Patch(AccessTools.Method(charCustomize, nameof(CharacterCustomization.performHoverAction),
            //    [typeof(int), typeof(int)]),
            //    postfix: PerformHoverActionPostfix_Method);
        }

        // DrawPostfix
        {
            //var DrawPostfix_Method = AccessTools.Method(
            //    CharacterCustomizationPatch_Type,
            //    "DrawPostfix");

            //harmony.Patch(AccessTools.Method(charCustomize, nameof(CharacterCustomization.draw),
            //    [typeof(SpriteBatch)]), postfix: DrawPostfix_Method);
        }
        monitor.Log("done prefix Apply(harmony)");

        return false;
    }

    static FieldInfo input_fieldInfo;
    static void SimpleSlider_Ctor_Postfix(object __instance, Rectangle bounds, int min, int max)
    {
        if (input_fieldInfo == null)
            input_fieldInfo = AccessTools.Field(SimpleSlider_Type, "input");

        var inputTextbox = input_fieldInfo.GetValue(__instance) as TextBox;
        inputTextbox.drawBackground = false;
        inputTextbox.Y -= 12; // fix text number align to center
    }
}
