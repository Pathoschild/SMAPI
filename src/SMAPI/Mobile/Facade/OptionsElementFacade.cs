using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

public static class OptionsElementRewriter
{
    public static Type OptinosElementType = typeof(OptionsElement);

    //Map OptionsElement Type & Method draw(spriteBatchslotX,slotY,context)
    public static Dictionary<string, MethodInfo> OptionsElementType = new();
    public static Type[] drawParams = [typeof(SpriteBatch), typeof(int), typeof(int), typeof(IClickableMenu)];
    private static readonly ThreadLocal<int> drawAtInstanceCounter = new ThreadLocal<int>(() => 0);

    public static void draw(this OptionsElement element, SpriteBatch b, int slotX, int slotY, IClickableMenu? context = null)
    {
        var thisType = element.GetType();
        if (thisType.FullName == OptinosElementType.FullName)
        {
            //Log($"on virtual draw base: {element.label} loop: {drawAtInstanceCounter.Value}");
            element.draw(b, slotX, slotY);
            return;
        }

        if (!OptionsElementType.TryGetValue(thisType.FullName, out var drawMethod))
        {
            drawMethod = thisType.GetMethod("draw",
                BindingFlags.Public | BindingFlags.Instance, null, drawParams, null);
            OptionsElementType.Add(thisType.FullName, drawMethod);
        }
        if (drawMethod != null)
        {
            //protect loop infinity
            //we should draw at instance when call at first time
            if (drawAtInstanceCounter.Value == 0)
            {
                drawAtInstanceCounter.Value++;
                try
                {
                    //Log($"on void override draw: {element.label} loop: " + drawAtInstanceCounter.Value);
                    drawMethod.Invoke(element, [b, slotX, slotY, context]);
                    return;
                }
                finally
                {
                    //draw finish
                    drawAtInstanceCounter.Value--;
                }
            }

            //Log($"on virtual draw after call override: {element.label} loop: {drawAtInstanceCounter.Value}");
            element.draw(b, slotX, slotY);
        }
    }

}
