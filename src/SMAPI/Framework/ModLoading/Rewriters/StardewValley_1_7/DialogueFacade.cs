using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_7;

/// <summary>Maps Stardew Valley 1.6.16's <see cref="Dialogue"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class DialogueFacade_1_7 : Dialogue, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static string applyGenderSwitchBlocks(Gender gender, string str)
    {
        return Dialogue.ApplySwitchBlocks(gender, str, null);
    }


    /*********
    ** Private methods
    *********/
    private DialogueFacade_1_7()
        : base(null, null)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
