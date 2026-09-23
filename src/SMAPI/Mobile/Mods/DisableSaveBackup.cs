using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI.Framework;
using StardewValley.Menus;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Internal;
using System.Runtime.CompilerServices;

namespace StardewModdingAPI.Mobile.Mods;

static class DisableSaveBackup
{
    static bool isApplyPatched = false;
    public static void Init(AndroidModFixManager modFix)
    {
        modFix.RegisterOnModLoaded(SpaceCoreFix.SpaceCoreDllFileName, OnModLoaded);
        modFix.RegisterOnModLoaded(FarmTypeManagerFix.DllFileName, OnModLoaded);
    }

    static void OnModLoaded(Assembly assembly)
    {
        ApplyPatch();
    }

    static void ApplyPatch()
    {
        if (isApplyPatched)
            return;

        isApplyPatched = true;
        var monitor = SCore.Instance.SMAPIMonitor;
        try
        {
            monitor.Log("Start DisableQuickSave Fix..");

            var harmony = AndroidPatcher.harmony;

            var OptionPageCtor = AccessTools.Constructor(OptionsPageType, [
                typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(float),
                    typeof(float),
                ]);
            harmony.Patch(
                original: OptionPageCtor,
                postfix: new(typeof(DisableSaveBackup), nameof(Postfix_OptionsPage_Ctor)));

            var saveWholeBackup = typeof(Game1).GetMethod(nameof(Game1.saveWholeBackup));
            harmony.Patch(
                original: saveWholeBackup,
                prefix: new(typeof(DisableSaveBackup), nameof(Prefix_saveWholeBackup))
            );
            monitor.Log("Disable Game1.saveWholeBackup");

            //Disable this, caller from SMAPIActivity.OnPause();
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.emergencyBackup)),
                prefix: new(typeof(DisableSaveBackup), nameof(Prefix_emergencyBackup))
            );
            monitor.Log("Disable Game1.emergencyBackup()");
        }
        catch (Exception ex)
        {
            monitor.Log(ex.ToString(), LogLevel.Error);
        }
    }
    static Type OptionsPageType = typeof(OptionsPage);
    static FieldInfo optionsFieldInfo = OptionsPageType.GetField("options",
        BindingFlags.Instance | BindingFlags.NonPublic);

    static Type OptionsButtonType = typeof(OptionsButton);
    static FieldInfo btnLabelFieldInfo = OptionsButtonType.GetField("_label",
        BindingFlags.Instance | BindingFlags.NonPublic);
    static FieldInfo btnPaddingYField = OptionsButtonType.GetField("paddingY",
        BindingFlags.Instance | BindingFlags.NonPublic);

    const string DisableQuickSaveButtonText = "Disable SaveBackup By SMAPI Android! \n Please Use Mod QuickSave!";
    static void Postfix_OptionsPage_Ctor(OptionsPage __instance,
        ref List<OptionsElement> ___options,
        int x, int y, int width, int height, float widthMod = 1f, float heightMod = 1f)
    {
        try
        {
            var saveBackupString = Game1.content.LoadString("Strings\\UI:save_backup");
            var btn = ___options.Find(e =>
            {
                if (e is OptionsButton btn)
                    return btn.label.Equals(saveBackupString, StringComparison.OrdinalIgnoreCase);
                return false;
            }) as OptionsButton;
            if (btn is null)
                return;

            btnLabelFieldInfo.SetValue(btn, DisableQuickSaveButtonText);
            int paddingY = (int)btnPaddingYField.GetValue(btn);
            btn.enabled = false;

            int num = (int)Game1.dialogueFont.MeasureString(DisableQuickSaveButtonText).X + 64;
            int num2 = (int)Game1.dialogueFont.MeasureString(DisableQuickSaveButtonText).Y + paddingY * 2;
            btn.bounds = new Rectangle(btn.bounds.X, btn.bounds.Y, num, num2);
            btn.button = new ClickableComponent(btn.bounds, "OptionsButton_" + DisableQuickSaveButtonText);
        }
        catch (Exception ex)
        {
            var monitor = SCore.Instance.SMAPIMonitor;
            monitor.Log(ex.GetLogSummary(), LogLevel.Warn);
        }
    }

    static bool Prefix_saveWholeBackup()
    {
        var monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("bypass saveWholeBackup()");
        return false;
    }

    static bool Prefix_emergencyBackup()
    {
        var monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("bypass Game1.emergencyBackup()");
        return false;
    }
}
