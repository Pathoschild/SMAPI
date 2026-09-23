using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

/// <summary>A clickable component representing a shop tab, which applies a filter to the list of displayed shop items when clicked.</summary>
public class ShopTabClickableTextureComponentFacade : ClickableTextureComponent
{
    /// <summary>Matches items to show when this tab is selected.</summary>
    public Func<ISalable, bool> Filter;

    public ShopTabClickableTextureComponentFacade(string name, Rectangle bounds, string label, string hoverText, Texture2D texture, Rectangle sourceRect, float scale, bool drawShadow = false)
        : base(name, bounds, label, hoverText, texture, sourceRect, scale, drawShadow)
    {
    }

    public ShopTabClickableTextureComponentFacade(Rectangle bounds, Texture2D texture, Rectangle sourceRect, float scale, bool drawShadow = false)
        : base(bounds, texture, sourceRect, scale, drawShadow)
    {
    }
}

