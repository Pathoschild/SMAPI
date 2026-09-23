using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Android.Hardware.Lights;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModLoading.Rewriters;

namespace StardewModdingAPI.Mobile.Facade;
[HarmonyPatch]
public static class Texture2DRewriter
{
    static void CropPixels(
        Color[] srcPixels, Color[] dstPixels,
        int srcWidth, int srcHeight,
        int dstWidth, int dstHeight)
    {
        //Console.WriteLine($"Crop Pixel: src size: {srcWidth} x {srcHeight}");
        //Console.WriteLine($"Crop Pixel: dst size: {dstWidth} x {dstHeight}");

        //use simple loop
        if (dstWidth <= 32 && dstHeight <= 32)
        {
            for (int y = 0; y < dstHeight; y++)
            {
                for (int x = 0; x < dstWidth; x++)
                {
                    int sourceIndex = y * srcWidth + x;
                    int targetIndex = y * dstWidth + x;
                    dstPixels[targetIndex] = srcPixels[sourceIndex];
                }
            }

            return;
        }

        //check if size height more than width
        //we should ForEach in y
        //!! if it's same size we don't crop,
        //we just clone it

        if (dstHeight > dstWidth)
        {
            Parallel.For(0, dstHeight, y =>
            {
                int sourceIndex = y * srcWidth;
                int targetIndex = y * dstWidth;
                Array.Copy(srcPixels, sourceIndex, dstPixels, targetIndex, dstWidth);
            });
        }
        else if (dstHeight < dstWidth)
        {
            Parallel.For(0, dstWidth, x =>
            {
                int sourceIndex = x * srcHeight;
                int targetIndex = x * dstHeight;
                Array.Copy(srcPixels, sourceIndex, dstPixels, targetIndex, dstHeight);
            });
        }
        else //it same size
        {
            Array.Copy(srcPixels, dstPixels, dstPixels.Length);
        }
    }
    public static void Fix_GetDataForPC(this Texture2D texture, Color[] data)
    {
        try
        {
            if (texture.ActualWidth == texture.Width
                && texture.ActualHeight == texture.Height)
            {
                //correct size
                texture.GetData(data);
                return;
            }

            var monitor = SCore.Instance.SMAPIMonitor;
            //monitor.Log($"try fix GetDataForPC(); texture: {texture.Name}, crop resize to: {texture.Width}x{texture.Height}");

            //correct to data pixels
            Color[] srcPixels = new Color[texture.ActualWidth * texture.ActualHeight];
            texture.GetData(srcPixels);

            //crop image
            CropPixels(srcPixels, data,
                texture.ActualWidth, texture.ActualHeight,
                texture.Width, texture.Height);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static MethodInfo Fix_GetData_MethodInfo = AccessTools.Method(typeof(Texture2DRewriter), nameof(Fix_GetDataForPC));
    public static bool RewriterCallback(
        MapMethodWithCallback mapMethod,
        MethodReference method,
        ModuleDefinition module, ILProcessor cil, Instruction instruction)
    {

        switch (method.FullName)
        {
            case "System.Void Microsoft.Xna.Framework.Graphics.Texture2D::GetData<Microsoft.Xna.Framework.Color>(!!0[])":
                instruction.Operand = module.ImportReference(Fix_GetData_MethodInfo);
                return true;//mark rewrite
            default:
                return false;//no rewrite
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Texture2D), nameof(Texture2D.CopyFromTexture))]
    static bool FixCopyFromTexture(Texture2D __instance, Texture2D other)
    {
        var srcTexture = other;
        var dstTexture = __instance;

        //get data, android use size power two
        Color[] dataInActualSize = new Color[other.ActualWidth * other.ActualHeight];
        other.GetData(dataInActualSize);

        //set width, height
        width_Field.SetValue(dstTexture, srcTexture.Width);
        height_Field.SetValue(dstTexture, srcTexture.Height);

        //set actual width, height
        ActualWidth_Prop.SetValue(dstTexture, srcTexture.ActualWidth);
        ActualHeight_Prop.SetValue(dstTexture, srcTexture.ActualHeight);

        //set texel width, height
        float texelWidth = (float)TexelWidth_Prop.GetValue(other);
        TexelWidth_Prop.SetValue(dstTexture, texelWidth);

        float texelHeight = (float)TexelHeight_Prop.GetValue(other);
        TexelHeight_Prop.SetValue(dstTexture, texelHeight);

        dstTexture.SetData(dataInActualSize);
        //done Fix CopyFromTexture

        return false;
    }
    static PropertyInfo TexelWidth_Prop = AccessTools.Property(typeof(Texture2D), "TexelWidth");
    static PropertyInfo TexelHeight_Prop = AccessTools.Property(typeof(Texture2D), "TexelHeight");
    static PropertyInfo ActualWidth_Prop = AccessTools.Property(typeof(Texture2D), "ActualWidth");
    static PropertyInfo ActualHeight_Prop = AccessTools.Property(typeof(Texture2D), "ActualHeight");
    static FieldInfo width_Field = AccessTools.Field(typeof(Texture2D), "width");
    static FieldInfo height_Field = AccessTools.Field(typeof(Texture2D), "height");
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Texture2D), "GetColorData")]
    static bool Fix_GetColorData(ref Color[] __result, Texture2D __instance)
    {
        var texture = __instance;
        switch (texture.Format)
        {
            case SurfaceFormat.Color:
                Color[] pixels = new Color[texture.ActualWidth * texture.ActualHeight];
                texture.GetData(pixels);
                //return value
                __result = pixels;
                //skip original method
                return false;
        }

        return true;
    }



}
