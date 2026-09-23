using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StardewModdingAPI.Mobile.Vectors;

internal static class VectorTypeConverterFix
{
    public static void ApplyPatch(Harmony hp)
    {
        TypeDescriptor.AddAttributes(
            typeof(Vector2),
            [new TypeConverterAttribute(typeof(Vector2TypeConverter))]);

        TypeDescriptor.AddAttributes(
            typeof(Vector3),
            [new TypeConverterAttribute(typeof(Vector3TypeConverter))]);

        TypeDescriptor.AddAttributes(
            typeof(Vector4),
            [new TypeConverterAttribute(typeof(Vector4TypeConverter))]);
    }
}

