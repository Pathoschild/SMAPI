using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

[HarmonyPatch]
public class OptionsPageFacade : OptionsPage, IRewriteFacade
{
    public OptionsPageFacade(int x, int y, int width, int height, float widthMod = 1, float heightMod = 1)
        : base(x, y, width, height, widthMod, heightMod)
    {
    }

    static FieldInfo _optionsField = typeof(OptionsPage).GetField("options", BindingFlags.Instance | BindingFlags.NonPublic);
    public List<OptionsElement> options
    {
        get => _optionsField.GetValue(this) as List<OptionsElement>;
        set => _optionsField.SetValue(this, value);
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(OptionsPage), methodType: MethodType.Constructor,
        [
        typeof(int), typeof(int), typeof(int), typeof(int),
        typeof(float), typeof(float),
        ])]
    static void PostfixOptionsPage(OptionsPage __instance,
        ref List<OptionsElement> ___options,
        int x, int y, int width, int height, float widthMod = 1f, float heightMod = 1f)
    {
        // added Toggle sound typing dialog
        int toggleAnimalSoundsIndex = ___options.FindIndex(e =>
        {
            OptionsCheckbox? checkbox = e as OptionsCheckbox;
            if (checkbox?.whichOption == 43)
                return true;
            return false;
        });

        ___options.Insert(toggleAnimalSoundsIndex,
            new OptionsCheckbox(
                Game1.content.LoadString("Strings\\StringsFromCSFiles:OptionsPage.cs.11246"),
                3
            ));


        // check QuickSave mod available
        ModQuickSaveOptionPage.SetupOptionPage(__instance, ref ___options);
    }
}
