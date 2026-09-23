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
        // SCore.Instance is assigned before LogManager exists. Reading SMAPIMonitor in that window
        // throws NullReferenceException and replaces the original startup error.
        IMonitor? monitor = TryGetMonitor();
        LogExit("Try Exit Game At SMAPIActivityTool", monitor);
        try
        {
            MainActivity.Finish();
            LogExit("Done Exit Game.", monitor);
        }
        catch (Exception ex)
        {
            LogExit(ex.GetLogSummary(), monitor);
            AndroidLogger.Log(ex);
            Console.WriteLine(ex);
            throw;
        }
    }

    /// <summary>Read SMAPI's monitor when it has been constructed.</summary>
    private static IMonitor? TryGetMonitor()
    {
        try
        {
            return SCore.Instance?.TryGetSMAPIMonitor();
        }
        catch (Exception ex)
        {
            AndroidLogger.Log("SMAPI monitor is not ready during exit: " + ex);
            return null;
        }
    }

    /// <summary>Log an exit message through SMAPI when possible, otherwise logcat.</summary>
    private static void LogExit(string message, IMonitor? monitor)
    {
        try
        {
            if (monitor != null)
                monitor.Log(message);
            else
                AndroidLogger.Log(message);
        }
        catch (Exception ex)
        {
            AndroidLogger.Log(ex);
        }
    }
}
