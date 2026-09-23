using System;
using System.IO;
using HarmonyLib;
using StardewValley;
using AndroidUtils = Android.Util;

namespace StardewModdingAPI.Mobile;

public static class AndroidLogger
{
    const string Tag = "SMAPI-Tag";
    static bool isAdbEnabled = Android.Provider.Settings.Global.GetInt(
            Android.App.Application.Context.ContentResolver,
            Android.Provider.Settings.Global.AdbEnabled, 0
        ) == 1;

    public static void Log(object msg)
    {
        if (msg == null)
            msg = "";

        if (isAdbEnabled)
            AndroidUtils.Log.Debug(Tag, msg.ToString());
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveGame), "LogVerbose")]
    private static void Prefix_SaveGame_LogVerbose(string message)
    {
        Console.WriteLine("Prefix_SaveGame_LogVerbose: " + message);
    }
}

