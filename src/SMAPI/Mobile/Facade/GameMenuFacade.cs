using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

public class GameMenuFacade : GameMenu, IRewriteFacade
{
    public GameMenuFacade(int startingTab,
        int extra = -1,
        bool playOpeningSound = true)
            : base()
    {

    }
}
