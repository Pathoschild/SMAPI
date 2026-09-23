using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Mobile.Audio;
using StardewValley;
using StardewValley.Audio;

namespace StardewModdingAPI.Mobile;

internal static class AndroidContentLoaderManager
{
    public static bool IsLoaded => LoadState == LoadStateEnum.Loaded;

    public static bool FinishedFirstInitSerializers
    {
        get => (bool)AccessTools.Field(typeof(Game1), "FinishedFirstInitSerializers").GetValue(null);
        set => AccessTools.Field(typeof(Game1), "FinishedFirstInitSerializers").SetValue(null, value);
    }
    public static bool FinishedFirstLoadContent
    {
        get => (bool)AccessTools.Field(typeof(Game1), "FinishedFirstLoadContent").GetValue(null);
        set => AccessTools.Field(typeof(Game1), "FinishedFirstLoadContent").SetValue(null, value);
    }

    public static bool FinishedFirstInitSounds
    {
        get => (bool)AccessTools.Field(typeof(Game1), "FinishedFirstInitSounds").GetValue(null);
        set => AccessTools.Field(typeof(Game1), "FinishedFirstInitSounds").SetValue(null, value);
    }
    public static bool FinishedCustomLoadContent = false;
    static int CallingTick = 0;
    public static void UpdateMoveNextLoadContent()
    {
        CallingTick++;

        if (CallingTick == 1)
            OnSetupFirstTick();

        var currentLoaderEnumerator = SGame.LoadContentEnumerator;
        bool isLoadContentFinish = currentLoaderEnumerator.MoveNext() is false;
        if (isLoadContentFinish)
        {
            FinishedFirstLoadContent = true;
            //update additional content
            //debug
            FinishedCustomLoadContent = true;
        }


        if (FinishedFirstLoadContent && FinishedFirstInitSounds
            && FinishedFirstInitSerializers && FinishedCustomLoadContent)
        {
            AccessTools.Field(typeof(Game1), "FinishedIncrementalLoad").SetValue(null, true);
            LoadState = LoadStateEnum.Loaded;
            SGame.LoadContentEnumerator = null;
            OnPrefix_AfterLoadContent();
            AccessTools.Method(typeof(Game1), "AfterLoadContent").Invoke(Game1.game1, null);
            OnPostfix_AfterLoadContent();
        }
    }
    static void OnSetupFirstTick()
    {
        LoadState = LoadStateEnum.Loading;

        //change AudioCueModificationManager
        //debug
        Game1.CueModification = new CustomAudioCueModificationManager();
    }

    static IMonitor Monitor => SCore.Instance.SMAPIMonitor;
    static void Log(string msg) => Monitor.Log(msg);

    static void OnPrefix_AfterLoadContent()
    {
    }

    static void OnPostfix_AfterLoadContent()
    {
        (SGame.game1 as SGame).OnAndroidContentLoaded();
    }


    public enum LoadStateEnum
    {
        None,
        Loading,
        Loaded,
    }
    public static LoadStateEnum LoadState = LoadStateEnum.None;
}
