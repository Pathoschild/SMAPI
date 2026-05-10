using System;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.Enchantments.Obsolete;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_7;

/// <summary>Maps Stardew Valley 1.6.16's <c>Tool</c> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ToolFacade : Tool, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public bool hasEnchantmentOfType<T>()
    {
        // types which don't have their own type anymore
        if (this.TryGetEnchantmentIdForRemovedType<T>(out string? enchantmentId))
            return this.HasEnchantment(enchantmentId);

        // else reimplement old logic
        foreach (Enchantment? enchantment in this.enchantments)
        {
            if (enchantment is T)
                return true;
        }

        return false;

    }


    /*********
    ** Private methods
    *********/
    private ToolFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }

    protected override Item GetOneNew()
    {
        throw new NotImplementedException();
    }

    /// <summary>Get the ID for an enchantment which no longer has its own type.</summary>
    /// <typeparam name="T">The enchantment type.</typeparam>
    /// <param name="enchantmentId">The enchantment ID, if found.</param>
    private bool TryGetEnchantmentIdForRemovedType<T>([NotNullWhen(true)] out string? enchantmentId)
    {
        // skip custom enchantments
        if (typeof(T).Namespace != typeof(Enchantment).Namespace)
        {
            enchantmentId = null;
            return false;
        }

        // get ID if it's a removed type
        enchantmentId = typeof(T).Name switch
        {
#pragma warning disable CS0618 // Type or member is obsolete -- deliberate to rewrite them
            nameof(ArchaeologistEnchantment) => EnchantmentIds.Archaeologist,
            nameof(ArtfulEnchantment) => EnchantmentIds.Artful,
            nameof(AttackEnchantment) => EnchantmentIds.Attack,
            nameof(AutoHookEnchantment) => EnchantmentIds.AutoHook,
            nameof(CritEnchantment) => EnchantmentIds.CritChance,
            nameof(DefenseEnchantment) => EnchantmentIds.Defense,
            nameof(DiamondEnchantment) => EnchantmentIds.Diamond,
            nameof(FisherEnchantment) => EnchantmentIds.Fisher,
            nameof(GalaxySoulEnchantment) => EnchantmentIds.GalaxySoul,
            nameof(GenerousEnchantment) => EnchantmentIds.Generous,
            nameof(LightweightEnchantment) => EnchantmentIds.Lightweight,
            nameof(MasterEnchantment) => EnchantmentIds.Master,
            nameof(PreservingEnchantment) => EnchantmentIds.Preserving,
            nameof(ReachingToolEnchantment) => EnchantmentIds.Expansive,
            nameof(ShavingEnchantment) => EnchantmentIds.Shaving,
            nameof(WeaponSpeedEnchantment) => EnchantmentIds.Speed,
#pragma warning restore CS0618
            _ => null
        };
        return enchantmentId != null;
    }
}
