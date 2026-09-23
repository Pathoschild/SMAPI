using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.Menus;
using static StardewValley.Menus.InventoryMenu;

namespace StardewModdingAPI.Mobile.Facade;


public class ItemGrabMenuFacade : ItemGrabMenu, IRewriteFacade
{
    public ItemGrabMenuFacade(IList<Item> inventory,
        bool reverseGrab,
        bool showReceivingMenu,
        InventoryMenu.highlightThisItem highlightFunction,
        behaviorOnItemSelect behaviorOnItemSelectFunction,
        string message,
        behaviorOnItemSelect behaviorOnItemGrab = null,
        bool snapToBottom = false,
        bool canBeExitedWithKey = false,
        bool playRightClickSound = true,
        bool allowRightClick = true,
        bool showOrganizeButton = false,
        int source = 0,
        Item sourceItem = null,
        int whichSpecialButton = -1,
        object context = null,
        ItemExitBehavior heldItemExitBehavior = ItemExitBehavior.ReturnToPlayer,
        bool allowExitWithHeldItem = false)
            : base(
                inventory: inventory,
                reverseGrab: reverseGrab,
                showReceivingMenu: showReceivingMenu,
                highlightFunction: highlightFunction,
                behaviorOnItemSelectFunction, message: message,
                behaviorOnItemGrab,
                snapToBottom,
                canBeExitedWithKey,
                playRightClickSound,
                allowRightClick,
                showOrganizeButton,
                source,
                sourceItem,
                whichSpecialButton,
                allowExitWithHeldItem
                )
    {

    }
}
