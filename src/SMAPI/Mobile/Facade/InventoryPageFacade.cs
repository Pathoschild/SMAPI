using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;
public class InventoryPageFacade : InventoryPage, IRewriteFacade
{
    public InventoryPageFacade(int x, int y, int width, int height) : base(x, y, width, height)
    {
    }
    static FieldInfo hoveredItem_Field = typeof(InventoryPage).GetField("hoveredItem",
            BindingFlags.Instance | BindingFlags.NonPublic);

    public Item hoveredItem
    {
        get => hoveredItem_Field.GetValue(this) as Item;
        set => hoveredItem_Field.SetValue(this, value);
    }
}
