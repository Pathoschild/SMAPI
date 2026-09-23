#if SMAPI_FOR_ANDROID
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Mobile.Facade;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using ShopTabClickableTextureComponent = StardewModdingAPI.Mobile.Facade.ShopTabClickableTextureComponentFacade;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

public partial class ShopMenuFacade
{
    public ShopMenuFacade(
        string shopId,
        List<ISalable> itemsForSale,
        int currency = 0,
        string who = null,
        ShopMenu.OnPurchaseDelegate on_purchase = null,
        Func<ISalable, bool> on_sell = null,
        bool playOpenSound = true)
            : base(shopId,
                itemsForSale: itemsForSale,
                currency: currency,
                who: who,
                on_purchase: on_purchase,
                on_sell: on_sell,
                context: null)
    {
    }


    List<ShopTabClickableTextureComponentFacade> _tabButtons = new();
    public List<ShopTabClickableTextureComponentFacade> tabButtons
    {
        get => this._tabButtons;
        set => this._tabButtons = value;
    }

    /// <summary>Add the filter tabs for a catalogue (e.g. flooring and wallpaper).</summary>
    public void UseCatalogueTabs()
    {
        this.tabButtons = new List<ShopTabClickableTextureComponentFacade>
        {
            new ShopTabClickableTextureComponentFacade(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(96, 48, 16, 16), 4f)
            {
                myID = 99999,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable item) => true
            },
            new ShopTabClickableTextureComponentFacade(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(48, 64, 16, 16), 4f)
            {
                myID = 100000,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable item) => item is Wallpaper wallpaper2 && wallpaper2.isFloor.Value
            },
            new ShopTabClickableTextureComponentFacade(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(32, 64, 16, 16), 4f)
            {
                myID = 100001,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable item) => item is Wallpaper wallpaper && !wallpaper.isFloor.Value
            }
        };
        this.repositionTabs();
    }

    /// <summary>Add the filter tabs for a furniture catalogue (e.g. tables, seats, paintings, etc).</summary>
	public void UseFurnitureCatalogueTabs()
    {
        this.tabButtons = new List<ShopTabClickableTextureComponent>
        {
            new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(96, 48, 16, 16), 4f)
            {
                myID = 99999,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable _) => true
            },
            new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(80, 48, 16, 16), 4f)
            {
                myID = 100000,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable item) => item is Furniture furniture5 && (furniture5.IsTable() || furniture5.furniture_type.Value == 4)
            },
            new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(64, 48, 16, 16), 4f)
            {
                myID = 100001,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable item) => item is Furniture furniture4 && (furniture4.furniture_type.Value == 0 || furniture4.furniture_type.Value == 1 || furniture4.furniture_type.Value == 2 || furniture4.furniture_type.Value == 3)
            },
            new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(64, 64, 16, 16), 4f)
            {
                myID = 100002,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable item) => item is Furniture furniture3 && (furniture3.furniture_type.Value == 6 || furniture3.furniture_type.Value == 13)
            },
            new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(96, 64, 16, 16), 4f)
            {
                myID = 100003,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable item) => item is Furniture furniture2 && furniture2.furniture_type.Value == 12
            },
            new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(80, 64, 16, 16), 4f)
            {
                myID = 100004,
                upNeighborID = -99998,
                downNeighborID = -99998,
                rightNeighborID = 3546,
                Filter = (ISalable item) => item is Furniture furniture && (furniture.furniture_type.Value == 7 || furniture.furniture_type.Value == 17 || furniture.furniture_type.Value == 10 || furniture.furniture_type.Value == 8 || furniture.furniture_type.Value == 9 || furniture.furniture_type.Value == 14)
            }
        };
        this.repositionTabs();
    }

    /// <summary>Remove the filter tabs, if any.</summary>
    public void UseNoTabs()
    {
        this.tabButtons = new List<ShopTabClickableTextureComponent>();
        this.repositionTabs();
    }
}
#endif
