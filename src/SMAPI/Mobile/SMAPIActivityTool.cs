using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using HarmonyLib;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Internal;
using StardewValley;

namespace StardewModdingAPI.Mobile;

internal static class SMAPIActivityTool
{
    static Activity _activity;
    public static Activity MainActivity
    {
        get
        {
            if (_activity == null)
            {
                var activityField = AccessTools.Field(typeof(MainActivity), "instance");
                _activity = activityField.GetValue(null) as Android.App.Activity;
            }
            return _activity;
        }
    }

    public static void ExitGame()
    {
        IMonitor? monitor = SCore.Instance?.SMAPIMonitor;
        monitor?.Log("Try Exit Game At SMAPIActivityTool");
        try
        {
            MainActivity.Finish();
            monitor?.Log("Done Exit Game.");
        }
        catch (Exception ex)
        {
            monitor?.Log(ex.GetLogSummary());
            Console.WriteLine(ex);
            throw;
        }

    }
}
