using System.Reflection;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mobile.Facade;

public class ToolbarFacade : Toolbar, IRewriteFacade
{
    static FieldInfo hoverItem_Field = typeof(Toolbar).GetField(nameof(hoverItem),
            BindingFlags.Instance | BindingFlags.NonPublic);

    public Item hoverItem
    {
        get => hoverItem_Field.GetValue(this) as Item;
        set => hoverItem_Field.SetValue(this, value);
    }
}
