using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="CarpenterMenu"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class CarpenterMenuFacade : CarpenterMenu, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static CarpenterMenu Constructor(bool magicalConstruction = false)
    {
        return new CarpenterMenu(magicalConstruction ? Game1.builder_wizard : Game1.builder_robin);
    }

#if !SMAPI_FOR_ANDROID
    public void setNewActiveBlueprint()
    {
        base.SetNewActiveBlueprint(base.Blueprint);
    }
#endif


    /*********
    ** Private methods
    *********/
    private CarpenterMenuFacade()
        : base(null)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
