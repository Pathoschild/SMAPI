using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_7;

/// <summary>Maps SMAPI 4.3.x's <see cref="Translation"/> members to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class SmapiTranslationFacade : Translation, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public Translation ApplyGenderSwitchBlocks(bool apply)
    {
        return base.ApplySwitchBlocks(apply);
    }


    /*********
    ** Private methods
    *********/
    private SmapiTranslationFacade()
        : base("en", "key", "text")
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
