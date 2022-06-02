namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.ItemData;

/// <summary>An item type that can be searched and added to the player through the console.</summary>
internal enum ItemType
{
    /// <summary>A big craftable object in <see cref="StardewValley.Game1.bigCraftablesInformation"/></summary>
    BigCraftable,

    /// <summary>A <see cref="StardewValley.Objects.Boots"/> item.</summary>
    Boots,

    /// <summary>A <see cref="StardewValley.Objects.Clothing"/> item.</summary>
    Clothing,

    /// <summary>A <see cref="StardewValley.Objects.Wallpaper"/> flooring item.</summary>
    Flooring,

    /// <summary>A <see cref="StardewValley.Objects.Furniture"/> item.</summary>
    Furniture,

    /// <summary>A <see cref="StardewValley.Objects.Hat"/> item.</summary>
    Hat,

    /// <summary>Any object in <see cref="StardewValley.Game1.objectInformation"/> (except rings).</summary>
    Object,

    /// <summary>A <see cref="StardewValley.Objects.Ring"/> item.</summary>
    Ring,

    /// <summary>A <see cref="StardewValley.Tool"/> tool.</summary>
    Tool,

    /// <summary>A <see cref="StardewValley.Objects.Wallpaper"/> wall item.</summary>
    Wallpaper,

    /// <summary>A <see cref="StardewValley.Tools.MeleeWeapon"/> or <see cref="StardewValley.Tools.Slingshot"/> item.</summary>
    Weapon
}
