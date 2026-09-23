using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Widget;
using StardewModdingAPI.Framework.Input;
using StardewValley;

namespace StardewModdingAPI.Mobile;

//using on Virtual Keyboard
public static class MobileInputTool
{
    public static void OverrideButton(SButton sButton, bool setDown)
    {
        var input = Game1.input as SInputState;
        input.OverrideButton(sButton, true);
    }
}
