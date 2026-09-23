using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

public class OptionsDropDownFacade : OptionsDropDown, IRewriteFacade
{
    public OptionsDropDownFacade(string label, int whichOption, int x = -1, int y = -1) : base(label, whichOption, x, y)
    {
    }
    // add params: context
    public void draw(SpriteBatch b, int slotX, int slotY, IClickableMenu context = null)
    {
        base.draw(b, slotX, slotY);
    }
}
