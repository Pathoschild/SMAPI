using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

public class MenuWithInventoryFacade : MenuWithInventory, IRewriteFacade
{
    private Item _heldItem;

    /// <summary>
    /// What to do with the <see cref="StardewValley.Menus.MenuWithInventory.heldItem" />
    /// if the menu is closed before it can be put down.
    /// </summary>
    public ItemExitBehavior HeldItemExitBehavior;

    /// <summary>
    /// Whether to allow exiting the menu while the player has a held item on their cursor.
    /// The <see cref="StardewValley.Menus.MenuWithInventory.HeldItemExitBehavior" /> will be applied.
    /// </summary>
    public bool AllowExitWithHeldItem
    {
        get => this.m_AllowExitWithHeldItem;
        set => this.m_AllowExitWithHeldItem = value;
    }
    bool m_AllowExitWithHeldItem;

    public Item heldItem
    {
        get
        {
            return this._heldItem;
        }
        set
        {
            value?.onDetachedFromParent();
            this._heldItem = value;
        }
    }

    public MenuWithInventoryFacade(
        InventoryMenu.highlightThisItem highlighterMethod = null,
        bool okButton = false,
        bool trashCan = false,
        int inventoryXOffset = 0,
        int inventoryYOffset = 0,
        int menuOffsetHack = 0,
        ItemExitBehavior heldItemExitBehavior = ItemExitBehavior.ReturnToPlayer,
        bool allowExitWithHeldItem = false)
        : base(
            highlighterMethod,
            okButton,
            trashCan
        )
    {
        this.HeldItemExitBehavior = heldItemExitBehavior;
        this.AllowExitWithHeldItem = allowExitWithHeldItem;
    }

    /// <inheritdoc />
    protected override void cleanupBeforeExit()
    {
        this.RescueHeldItemOnExit();
        base.cleanupBeforeExit();
    }

    public override void emergencyShutDown()
    {
        this.RescueHeldItemOnExit();
        base.emergencyShutDown();
    }


    public override bool readyToClose()
    {
        if (!this.m_AllowExitWithHeldItem)
            return this.heldItem == null;
        return true;
    }

    /// <summary>
    /// Rescue the <see cref="StardewValley.Menus.MenuWithInventory.heldItem" />
    /// if the menu is exiting.
    /// </summary>
    protected void RescueHeldItemOnExit()
    {
        if (this.heldItem != null)
        {
            switch (this.HeldItemExitBehavior)
            {
                case ItemExitBehavior.ReturnToPlayer:
                    this.heldItem = Game1.player.addItemToInventory(this.heldItem);
                    break;
                case ItemExitBehavior.ReturnToMenu:
                    this.heldItem = this.inventory.tryToAddItem(this.heldItem);
                    break;
                case ItemExitBehavior.Discard:
                    this.heldItem = null;
                    break;
            }
            this.DropHeldItem();
        }
    }
    public virtual void DropHeldItem()
    {
        if (this.heldItem != null)
        {
            Game1.playSound("throwDownITem");
            int drop_direction = Game1.player.FacingDirection;
            Console.WriteLine("On DropHeilItem");
            Console.WriteLine("this object: " + this);
            Console.WriteLine("this object class: " + this.GetType());
            //if (grabMenu?.context is LibraryMuseum)
            //{
            //    drop_direction = 2;
            //}

            Game1.createItemDebris(this.heldItem, Game1.player.getStandingPosition(), drop_direction);
            this.inventory.onAddItem?.Invoke(this.heldItem, Game1.player);
            this.heldItem = null;
        }
    }
}
