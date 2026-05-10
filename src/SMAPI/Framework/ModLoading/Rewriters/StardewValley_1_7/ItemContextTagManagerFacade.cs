using System.Collections.Generic;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_7;

/// <summary>Maps Stardew Valley 1.6.16's <see cref="ItemContextTagManager"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ItemContextTagManagerFacade : IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    /// <inheritdoc cref="ItemContextTagManager.DoesTagQueryMatch" />
    public static bool DoesTagQueryMatch(string tagQueryString, HashSet<string> tags)
    {
        return ItemContextTagManager.DoesTagQueryMatch(tagQueryString, tags);
    }

    /// <inheritdoc cref="ItemContextTagManager.DoAllTagsMatch" />
    public static bool DoAllTagsMatch(IList<string> requiredTags, HashSet<string> actualTags)
    {
        return ItemContextTagManager.DoAllTagsMatch(requiredTags, actualTags);
    }

    /// <inheritdoc cref="ItemContextTagManager.DoesTagMatch" />
    public static bool DoesTagMatch(string tag, HashSet<string> tags)
    {
        return ItemContextTagManager.DoesTagMatch(tag, tags);
    }


    /*********
    ** Private methods
    *********/
    private ItemContextTagManagerFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
