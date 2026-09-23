using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="SObject"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class ObjectFacade : SObject, IRewriteFacade
{
    /*********
    ** Public methods
    *********/
    public static SObject Constructor(Vector2 tileLocation, int parentSheetIndex, bool isRecipe = false)
    {
        return new SObject(tileLocation, parentSheetIndex.ToString(), isRecipe);
    }

    public static SObject Constructor(int parentSheetIndex, int initialStack, bool isRecipe = false, int price = -1, int quality = 0)
    {
        return new SObject(parentSheetIndex.ToString(), initialStack, isRecipe, price, quality);
    }

    public static SObject Constructor(Vector2 tileLocation, int parentSheetIndex, int initialStack)
    {
        SObject obj = Constructor(tileLocation, parentSheetIndex, null, true, true, false, false);
        obj.stack.Value = initialStack;
        return obj;
    }

    public static SObject Constructor(Vector2 tileLocation, int parentSheetIndex, string? Givenname, bool canBeSetDown, bool canBeGrabbed, bool isHoedirt, bool isSpawnedObject)
    {
        SObject obj = new(parentSheetIndex.ToString(), 1);

        if (Givenname != null && obj.name is (null or "Error Item"))
            obj.name = Givenname;

        obj.tileLocation.Value = tileLocation;
        obj.canBeSetDown.Value = canBeSetDown;
        obj.canBeGrabbed.Value = canBeGrabbed;
        obj.isSpawnedObject.Value = isSpawnedObject;

        return obj;
    }

    public void ApplySprinkler(GameLocation location, Vector2 tile)
    {
        base.ApplySprinkler(tile);
    }

    public void ApplySprinklerAnimation(GameLocation location)
    {
        base.ApplySprinklerAnimation();
    }

#if !SMAPI_FOR_ANDROID
    public new void ConsumeInventoryItem(Farmer who, Item drop_in, int amount)
    {
        Object.ConsumeInventoryItem(who, drop_in, amount);
    }
#endif

    public void DayUpdate(GameLocation location)
    {
        base.DayUpdate();
    }

    public void farmerAdjacentAction(GameLocation location)
    {
        base.farmerAdjacentAction(Game1.player);
    }

    public Rectangle getBoundingBox(Vector2 tileLocation)
    {
        return base.GetBoundingBoxAt((int)tileLocation.X, (int)tileLocation.Y);
    }

    public bool isForage(GameLocation location)
    {
        return base.isForage();
    }

    public bool minutesElapsed(int minutes, GameLocation environment)
    {
        return base.minutesElapsed(minutes);
    }

    public bool onExplosion(Farmer who, GameLocation location)
    {
        return base.onExplosion(who);
    }

    public void performRemoveAction(Vector2 tileLocation, GameLocation environment)
    {
        base.performRemoveAction();
    }

    public bool performToolAction(Tool t, GameLocation location)
    {
        return base.performToolAction(t);
    }

    public void updateWhenCurrentLocation(GameTime time, GameLocation environment)
    {
        base.updateWhenCurrentLocation(time);
    }


    /*********
    ** Private methods
    *********/
    private ObjectFacade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
