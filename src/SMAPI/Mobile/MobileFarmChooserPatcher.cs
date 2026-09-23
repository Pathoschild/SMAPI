using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Widget;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile;

[HarmonyPatch]
internal static class MobileFarmChooserPatcher
{
    public static void Patch(Harmony h)
    {
        {
            var ctor = AccessTools.Constructor(typeof(MobileFarmChooser),
                [typeof(int), typeof(int), typeof(int), typeof(int),
            typeof(CharacterCustomization.Source), typeof(bool), typeof(bool)]);
            h.Patch(ctor,
                prefix: AccessTools.Method(typeof(MobileFarmChooserPatcher), nameof(Prefix_Ctor)),
                postfix: AccessTools.Method(typeof(MobileFarmChooserPatcher), nameof(Postfix_Ctor)));
        }

        {
            var method = AccessTools.Method(typeof(MobileFarmChooser), "optionButtonClick");
            h.Patch(method,
                prefix: AccessTools.Method(
                    typeof(MobileFarmChooserPatcher),
                    nameof(Prefix_optionButtonClick)));
        }


        var monitor = SCore.Instance.SMAPIMonitor;
        monitor.Log("MobileFarmChooserPatcher patched");
    }

    static Dictionary<string, ModFarmType> modFarmsLookup = new();
    const string MeadowlandsFarm_ID = "MeadowlandsFarm";
    static int selectFarmIndexCounter = 0;

    static void Prefix_Ctor(MobileFarmChooser __instance)
    {
        // force reset to Standard Farm
        Game1.whichFarm = 0;
        selectFarmIndexCounter = 0;

        // setup first time
        if (modFarmsLookup.Count == 0)
        {
            var additionalFarms = DataLoader.AdditionalFarms(Game1.content);
            foreach (var farm in additionalFarms)
            {
                modFarmsLookup.TryAdd(farm.Id, farm);
            }
        }
    }

    static void Postfix_Ctor(MobileFarmChooser __instance,
        int ___startX,
        int ___buttonY,
        int ___farmButtonWidth,
        int ___farmButtonSpacing,
        bool ___isStandaloneScreen,
        Dictionary<int, ClickableComponent> ___farmTypeButtonLookup)
    {
        var menu = __instance;
        // check if is already added
        if (menu.farmTypeButtons.Count != 8)
            return;

        int startX = ___startX;
        int buttonY = ___buttonY;
        int buttonOffset = ___farmButtonWidth + ___farmButtonSpacing;

        int modFarmIndex = 7;
        foreach (var farm in modFarmsLookup.Values)
        {
            if (farm.Id == MeadowlandsFarm_ID)
                continue;

            modFarmIndex++;

            var texture = Game1.content.Load<Texture2D>(farm.IconTexture);
            var farmDetail = GetFarmTypeTooltip(farm.TooltipStringPath);

            int currentButtonIndex = menu.farmTypeButtons.Count;
            var farmButton = new ClickableTextureComponent(
                "ModFarm_" + farm.Id,
                new Rectangle(startX + currentButtonIndex * buttonOffset, buttonY, 76, 76),
                null,
                farmDetail,
                texture,
                new Rectangle(0, 0, 22, 20),
                4f);

            menu.farmTypeButtons.Add(farmButton);
            ___farmTypeButtonLookup.TryAdd(modFarmIndex, farmButton);
        }

        // debug only
#if false
        Game1.player.name.Value = "Guy";
        Game1.player.farmName.Value = "Hello Guy";
        Game1.player.favoriteThing.Value = "I dont know";
#endif
    }

    static string GetFarmTypeTooltip(string translationKey)
    {
        string text = Game1.content.LoadString(translationKey);
        string[] parts = text.Split('_', 2);
        if (parts.Length == 1 || parts[1].Length == 0)
        {
            text = parts[0] + "_ ";
        }
        return text;
    }

    static bool Prefix_optionButtonClick(
        MobileFarmChooser __instance,
        CharacterCustomization.Source ___source,
        ref string ___nameString,
        ref string ___descString,
        ref Vector2 ___nameSize,
        ref Vector2 ___descSize,
        Dictionary<int, ClickableComponent> ___farmTypeButtonLookup,
        bool ___isStandaloneScreen,

        string name
    )
    {
        // not initialize
        if (modFarmsLookup.Count == 0)
            return true;

        var menu = __instance;
        var farmTypeButtons = menu.farmTypeButtons;
        // check if you select any farm button
        var farmTypeButton = farmTypeButtons.SingleOrDefault(f => f.name == name);
        Console.WriteLine("selected farm btn: " + farmTypeButton?.name);
        if (farmTypeButton == null)
            return true;

        // ready
        // refresh first!!
        Game1.whichModFarm = null;
        // skip if not select any ModFarm type
        if (farmTypeButton.name.StartsWith("ModFarm_") == false)
            return true;

        var modFarmID = name.Replace("ModFarm_", "");
        if (modFarmsLookup.TryGetValue(modFarmID, out var pickModFarm) == false)
            return true;

        // assign current farm type mod

        var source = ___source;
        if (source == CharacterCustomization.Source.NewGame
            || source == CharacterCustomization.Source.HostNewFarm)
        {
            Game1.whichFarm = 7;
            Game1.whichModFarm = pickModFarm;
            Game1.spawnMonstersAtNight = pickModFarm.SpawnMonstersByDefault;
            Game1.playSound("coin");

            ___nameString = farmTypeButton.hoverText.Split('_')[0];
            ___descString = farmTypeButton.hoverText.Split('_')[1];
            ___nameSize = Game1.dialogueFont.MeasureString(___nameString);
            ___descSize = Game1.dialogueFont.MeasureString(___descString);

            Console.WriteLine("apply which mod farm: " + pickModFarm.Id);

            return false;
        }

        return true;
    }

    // helper method
    static void optionButtonClick(this MobileFarmChooser menu, string name)
    {
        var method = AccessTools.Method(typeof(MobileFarmChooser), "optionButtonClick");
        method.Invoke(menu, [name]);
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(MobileFarmChooser), nameof(MobileFarmChooser.receiveLeftClick))]
    static bool Prefix_receiveLeftClick(
        // my params
        MobileFarmChooser __instance,
        bool ___isStandaloneScreen,
       ClickableTextureComponent ___leftSelectButton,
    ClickableTextureComponent ___rightSelectButton,
    TextBox ___farmnameBox,
    ClickableTextureComponent ___backButton,
    ClickableTextureComponent ___okButton,


        // original params
        int x, int y, bool playSound = true
    )
    {
        var menu = __instance;
        var farmTypeButtons = menu.farmTypeButtons;
        var leftSelectButton = ___leftSelectButton;
        var rightSelectButton = ___rightSelectButton;
        bool isStandaloneScreen = ___isStandaloneScreen;
        var farmnameBox = ___farmnameBox;


        if (isStandaloneScreen)
        {
            foreach (ClickableTextureComponent farmTypeButton in farmTypeButtons)
            {
                if (farmTypeButton.containsPoint(x, y) && !farmTypeButton.name.Contains("Gray"))
                {
                    menu.optionButtonClick(farmTypeButton.name);
                }
            }
        }
        else
        {
            var oldSelectFarmIndex = selectFarmIndexCounter;
            if (leftSelectButton.containsPoint(x, y))
                selectFarmIndexCounter--;
            else if (rightSelectButton.containsPoint(x, y))
                selectFarmIndexCounter++;

            if (oldSelectFarmIndex != selectFarmIndexCounter)
            {
                if (selectFarmIndexCounter >= farmTypeButtons.Count)
                    selectFarmIndexCounter = 0;
                else if (selectFarmIndexCounter < 0)
                    selectFarmIndexCounter = farmTypeButtons.Count - 1;

                var currentSelectFarmType = farmTypeButtons[selectFarmIndexCounter];
                Game1.whichFarm = Math.Clamp(selectFarmIndexCounter, 0, 7);
                menu.optionButtonClick(currentSelectFarmType.name);
            }
        }

        if (isStandaloneScreen)
        {
            farmnameBox?.Update();
            if (___okButton.containsPoint(x, y) && menu.canLeaveMenu())
            {
                Game1.playSound("smallSelect");
            }
            if (___backButton.containsPoint(x, y))
            {
                Game1.playSound("smallSelect");
            }
        }

        return false;
    }

    // fix FarmType button not render correct
    // On Single Player
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MobileFarmChooser), nameof(MobileFarmChooser.draw))]
    static void Prefix_draw(
        MobileFarmChooser __instance,
        Dictionary<int, ClickableComponent> ___farmTypeButtonLookup,
        bool ___isStandaloneScreen,
        CharacterCustomization.Source ___source,

        // original params
        SpriteBatch b
    )
    {
        // fake with current farm type index 
        // Single Player
        if (___source.HasFlag(CharacterCustomization.Source.HostNewFarm) is false)
        {
            if (Game1.whichModFarm?.Id != MeadowlandsFarm_ID)
            {
                Game1.whichFarm = selectFarmIndexCounter;
                ___farmTypeButtonLookup[7] = __instance.farmTypeButtons[selectFarmIndexCounter];
            }
        }
    }

    // Fix farm type button render incorrect
    // on Single Player
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MobileFarmChooser), nameof(MobileFarmChooser.draw))]
    static void Postfix_draw(
        MobileFarmChooser __instance,
        Dictionary<int, ClickableComponent> ___farmTypeButtonLookup,
        CharacterCustomization.Source ___source,

        SpriteBatch b)
    {
        // restore to back correct type
        if (___source.HasFlag(CharacterCustomization.Source.HostNewFarm) is false)
        {
            Game1.whichFarm = Math.Clamp(selectFarmIndexCounter, 0, 7);
            ___farmTypeButtonLookup[7] = __instance.farmTypeButtons[7];
        }
    }
}

