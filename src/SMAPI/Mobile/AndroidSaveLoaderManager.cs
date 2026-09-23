using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Framework;
using StardewValley;

namespace StardewModdingAPI.Mobile;

internal static class AndroidSaveLoaderManager
{
    public static bool IsSaveParsed => Context.LoadStage is LoadStage.SaveParsed;
    static Monitor monitor;
    internal static void StartLoader()
    {
        monitor = SCore.Instance.SMAPIMonitor as Monitor;
        monitor.Log("Game loader with AndroidSaveLoader currentLoader.MoveNext()", Monitor.ContextLogLevel);
        AndroidGameLoopManager.RegisterOnGameUpdating(OnGameUpdating_AndroidSaveLoader);
    }

    //run Save.currentLoader.NextMove() within main game updating
    static bool OnGameUpdating_AndroidSaveLoader(GameTime gameTime)
    {
        Game1.game1.UpdateTitleScreenDuringLoadingMode();
        var score = SCore.Instance;

        // raise load stage changed
        int? step = Game1.currentLoader?.Current;
        switch (step)
        {
            case 20 when (!IsSaveParsed && SaveGame.loaded != null):
                score.OnLoadStageChanged(LoadStage.SaveParsed);
                break;

            case 36:
                score.OnLoadStageChanged(LoadStage.SaveLoadedBasicInfo);
                break;

            case 50:
                score.OnLoadStageChanged(LoadStage.SaveLoadedLocations);
                break;

            default:
                if (Game1.gameMode == Game1.playingGameMode)
                    score.OnLoadStageChanged(LoadStage.Preloaded);
                break;
        }

        if (step is null)
        {
            //save game loaded
            //break; // done
            monitor.Log("Game loader done.", Monitor.ContextLogLevel);
            AndroidGameLoopManager.UnregisterOnGameUpdating(OnGameUpdating_AndroidSaveLoader);
            return false;
        }
        else
        {
            return true;
        }
    }
}
