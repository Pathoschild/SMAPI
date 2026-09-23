using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework;
using StardewValley.Menus;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Mods;
using StardewModdingAPI.Framework.Events;
using System.Diagnostics;

namespace StardewModdingAPI.Mobile;

internal static class VersionInfoMenu
{
    internal static void Init()
    {
        SCore.OnRenderedStepEvent += SCore_OnRenderedStepEvent;
    }

    static void SCore_OnRenderedStepEvent(RenderSteps step, SpriteBatch spriteBatch, RenderTarget2D? renderTarget)
    {
        if (step == RenderSteps.Menu)
        {
            RenderVerionInfo(spriteBatch);
        }

        else if (step == RenderSteps.Overlays)
        {
            var titleMenu = Game1.activeClickableMenu as TitleMenu;

            // WIP: fix bug stuck on white screen.
            if (titleMenu != null && TitleMenu.subMenu == null)
            {
                if (titleMenu.birds.Count > 0)
                    titleMenu.skipToTitleButtons();
            }
        }
    }


    static SpriteFont font;
    static List<string> texts;
    static float textLineHeight;
    private static void RenderVerionInfo(SpriteBatch spriteBatch)
    {
        //check if it's on customize character or other sub menu, so we don't should render
        var titleMenu = Game1.activeClickableMenu as TitleMenu;
        if (titleMenu == null || TitleMenu.subMenu != null)
            return;

        if (titleMenu?.isTransitioningButtons is true)
            return;

        if (titleMenu.logoFadeTimer > 0)
            return;

        if (font == null && Game1.smallFont != null)
            font = Game1.smallFont;

        if (font == null)
            return;

        if (texts == null)
        {
            texts = new();
            textLineHeight = font.MeasureString("AAA").Y;

            texts.Add($"SMAPI: v{Constants.ApiVersionForAndroid}");
            texts.Add($"Build: {SMAPIAndroidBuild.BuildCode}");
            texts.Add($"Game: v{Constants.GameVersion}");
            texts.Add("Discord: Stardew SMAPI Thailand");
            texts.Add("Github: NRTnarathip");
        }


        var viewport = Game1.viewport;
        for (int i = 0; i < texts.Count; i++)
        {
            string text = texts[texts.Count - i - 1];
            var pos = Vector2.Zero;
            pos.X = 50;
            pos.Y = viewport.Height - (20 + textLineHeight + (textLineHeight * i));

            //shadow
            spriteBatch.DrawString(font, text, pos + new Vector2(-2, 2), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(2, 2), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(-2, -2), shadowColor);
            spriteBatch.DrawString(font, text, pos + new Vector2(2, -2), shadowColor);
            //text
            spriteBatch.DrawString(font, text, pos, Color.White);
        }
    }
    static Color shadowColor = Color.Black;
}
