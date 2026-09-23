using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade
{
    internal class IClickableMenuFacadeAndroid : IClickableMenu, IRewriteFacade
    {
        //Fix additionalCraftMaterials parameter
        public static void drawToolTip(SpriteBatch b, string hoverText,
            string hoverTitle, Item hoveredItem, bool heldItem = false,
            int healAmountToDisplay = -1, int currencySymbol = 0,
            string extraItemToShowIndex = null, int extraItemToShowAmount = -1,
            CraftingRecipe craftingIngredients = null,
            int moneyAmountToShowAtBottom = -1,
            IList<Item> additionalCraftMaterials = null)
        {
            bool flag = hoveredItem is StardewValley.Object @object && @object.edibility.Value != -300;
            string[] buffIconsToDisplay = null;
            if (flag && Game1.objectData.TryGetValue(hoveredItem.ItemId, out var value))
            {
                buffIconsToDisplay = GetBuffIcons(hoveredItem, value);
            }

            drawHoverText(b, hoverText, Game1.smallFont,
                heldItem ? 40 : 0, heldItem ? 40 : 0,
                moneyAmountToShowAtBottom, hoverTitle,
                flag ? (hoveredItem as StardewValley.Object).edibility.Value : (-1),
                buffIconsToDisplay, hoveredItem, currencySymbol,
                extraItemToShowIndex, extraItemToShowAmount,
                -1, -1, 1f, craftingIngredients,
                //fix additionalCraftMaterials
                additional_craft_materials: additionalCraftMaterials);
        }
    }
}
