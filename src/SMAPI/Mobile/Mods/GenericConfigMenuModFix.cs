using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework;

namespace StardewModdingAPI.Mobile.Mods;

internal static class GenericConfigMenuModFix
{
    static IMonitor monitor;
    internal static void Init(AndroidModFixManager modFix)
    {
        monitor = SCore.Instance.SMAPIMonitor;
        modFix.RegisterOnModLoaded("GenericModConfigMenu.dll", OnModLoaded);
    }
    static MethodInfo ModEntry_OnRendered_MethodInfo;
    static void OnModLoaded(Assembly asm)
    {
        monitor.Log("Start GenericModConfigFix");

        var hp = AndroidPatcher.harmony;
        var modEntry = asm.GetType("GenericModConfigMenu.Mod");
        ModEntry_OnRendered_MethodInfo = AccessTools.Method(modEntry, "OnRendered");
        hp.Patch(
            original: ModEntry_OnRendered_MethodInfo,
            prefix: new(typeof(GenericConfigMenuModFix), nameof(ModEntry_OnRendered))
        );
        //new rendered on Overlays
        SCore.OnRenderedStepEvent += SCore_OnRenderedStepEvent;

        monitor.Log("Patch ModEntry.OnRendered()");

        monitor.Log("Done GenericModConfigFix");
    }

    private static void SCore_OnRenderedStepEvent(StardewValley.Mods.RenderSteps step, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Graphics.RenderTarget2D? renderTarget)
    {
        if (SModHooks.CurrentRenderedStep == StardewValley.Mods.RenderSteps.Overlays)
        {
            IModMetadata modAPI = SCore.Instance.GetModRegistry().Get("spacechase0.GenericModConfigMenu");
            ModEntry_OnRendered_MethodInfo.Invoke(modAPI.Mod, [null, new RenderedEventArgs()]);
        }
    }

    static bool ModEntry_OnRendered(object sender, RenderedEventArgs e)
    {
        if (SModHooks.CurrentRenderedStep != StardewValley.Mods.RenderSteps.Overlays)
            return false;

        return true;
    }
}
