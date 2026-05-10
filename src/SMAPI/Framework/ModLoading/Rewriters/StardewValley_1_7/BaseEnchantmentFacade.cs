using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.GameData.Enchantments;
using Object = StardewValley.Object;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_7;

/// <summary>Maps Stardew Valley 1.6.16's <c>BaseEnchantment</c> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class BaseEnchantmentFacade : Enchantment, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public static bool hideEnchantmentName
    {
        get => EnchantmentManager.HideEnchantmentName;
        set => EnchantmentManager.HideEnchantmentName = value;
    }

    public static bool hideSecondaryEnchantName
    {
        get => EnchantmentManager.HideSecondaryEnchantName;
        set => EnchantmentManager.HideSecondaryEnchantName = value;
    }


    /*********
    ** Public methods
    *********/
    public int GetLevel()
    {
        return this.Level;
    }

    public string GetName()
    {
        switch (this.Id)
        {
            // had no name
            case EnchantmentIds.Amethyst:
            case EnchantmentIds.Aquamarine:
            case EnchantmentIds.Diamond:
            case EnchantmentIds.Emerald:
            case EnchantmentIds.GalaxySoul:
            case EnchantmentIds.Jade:
            case EnchantmentIds.Ruby:
            case EnchantmentIds.Topaz:
                return "Unknown Enchantment";

            // returned display name
            case EnchantmentIds.Attack:
            case EnchantmentIds.CritChance:
            case EnchantmentIds.CritPower:
            case EnchantmentIds.Defense:
            case EnchantmentIds.Lightweight:
            case EnchantmentIds.SlimeGatherer:
            case EnchantmentIds.SlimeSlayer:
            case EnchantmentIds.Speed:
                return Game1.content.LoadString($"Strings/EnchantmentNames:{this.Id}", this.Level);

            // name doesn't match ID
            case EnchantmentIds.AutoHook:
                return "Auto-Hook";
            case EnchantmentIds.BugKiller:
                return "Bug Killer";

            // any others match ID
            default:
                return this.Id;
        }
    }

    public static Enchantment GetEnchantmentFromItem(Item? base_item, Item? item)
    {
        return EnchantmentManager.GetEnchantmentFromItem(base_item, item);
    }

    public static List<Enchantment> GetAvailableEnchantmentsForItem(Tool? item)
    {
        var contextTags = item?.GetContextTags();

        return BaseEnchantmentFacade.GetPrismaticShardEnchantments(
            filter: contextTags != null
                ? data => EnchantmentManager.MatchesTags(contextTags, data.AppliesTo)
                : null
        );
    }

    public static List<Enchantment> GetAvailableEnchantments()
    {
        return BaseEnchantmentFacade.GetPrismaticShardEnchantments();
    }

    public static void ResetEnchantments()
    {
        // does nothing, since we no longer have cached enchantment instances to reset
    }


    /*********
    ** Private methods
    *********/
    private BaseEnchantmentFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }

    /// <summary>Get the enchantments applied at the forge using a prismatic shard.</summary>
    private static List<Enchantment> GetPrismaticShardEnchantments(Func<EnchantmentData, bool>? filter = null)
    {
        var dataAsset = DataLoader.Enchantments(Game1.content);

        List<Enchantment> enchantments = [];
        foreach ((string id, EnchantmentData data) in dataAsset)
        {
            if (data.ForgeWith == Object.prismaticShardQID && filter?.Invoke(data) != false)
                enchantments.Add(EnchantmentManager.CreateEnchantment(dataAsset, id));
        }

        return enchantments;
    }
}
