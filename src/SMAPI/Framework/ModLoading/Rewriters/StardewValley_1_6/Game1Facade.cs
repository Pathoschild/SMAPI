using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="Game1"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class Game1Facade : Game1, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public bool gamePadControlsImplemented { get; set; }              // never used
    public static bool menuUp { get; set; }                           // mostly unused and always false
    public static Color morningColor { get; set; } = Color.LightBlue; // never used


    /*********
    ** Public methods
    *********/
    public static bool canHaveWeddingOnDay(int day, string season)
    {
        return
            Utility.TryParseEnum(season, out Season parsedSeason)
            && Game1.canHaveWeddingOnDay(day, parsedSeason);
    }

    public static void createMultipleObjectDebris(int index, int xTile, int yTile, int number)
    {
        Game1.createMultipleObjectDebris(index.ToString(), xTile, yTile, number);
    }

    public static void createMultipleObjectDebris(int index, int xTile, int yTile, int number, GameLocation location)
    {
        Game1.createMultipleObjectDebris(index.ToString(), xTile, yTile, number, location);
    }

    public static void createMultipleObjectDebris(int index, int xTile, int yTile, int number, float velocityMultiplier)
    {
        Game1.createMultipleObjectDebris(index.ToString(), xTile, yTile, number, velocityMultiplier);
    }

    public static void createMultipleObjectDebris(int index, int xTile, int yTile, int number, long who)
    {
        Game1.createMultipleObjectDebris(index.ToString(), xTile, yTile, number, who);
    }

    public static void createMultipleObjectDebris(int index, int xTile, int yTile, int number, long who, GameLocation location)
    {
        Game1.createMultipleObjectDebris(index.ToString(), xTile, yTile, number, who, location);
    }

    public static void createObjectDebris(int objectIndex, int xTile, int yTile, long whichPlayer)
    {
        Game1.createObjectDebris(objectIndex.ToString(), xTile, yTile, whichPlayer);
    }

    public static void createObjectDebris(int objectIndex, int xTile, int yTile, long whichPlayer, GameLocation location)
    {
        Game1.createObjectDebris(objectIndex.ToString(), xTile, yTile, whichPlayer, location);
    }

    public static void createObjectDebris(int objectIndex, int xTile, int yTile, GameLocation location)
    {
        Game1.createObjectDebris(objectIndex.ToString(), xTile, yTile, location);
    }

    public static void createObjectDebris(int objectIndex, int xTile, int yTile, int groundLevel = -1, int itemQuality = 0, float velocityMultiplyer = 1f, GameLocation? location = null)
    {
        Game1.createObjectDebris(objectIndex.ToString(), xTile, yTile, groundLevel, itemQuality, velocityMultiplyer, location);
    }

    public static void createRadialDebris(GameLocation location, int debrisType, int xTile, int yTile, int numberOfChunks, bool resource, int groundLevel = -1, bool item = false, int color = -1)
    {
        Game1.createRadialDebris(
            location: location,
            debrisType: debrisType,
            xTile: xTile,
            yTile: yTile,
            numberOfChunks: numberOfChunks,
            resource: resource,
            groundLevel: groundLevel,
            item: item,
            color: Debris.getColorForDebris(color)
        );
    }

    public static void drawDialogue(NPC speaker, string dialogue)
    {
        Game1.DrawDialogue(new Dialogue(speaker, null, dialogue));
    }

    public static void drawDialogue(NPC speaker, string dialogue, Texture2D overridePortrait)
    {
        Game1.DrawDialogue(new Dialogue(speaker, null, dialogue) { overridePortrait = overridePortrait });
    }

    public static void drawObjectQuestionDialogue(string dialogue, List<Response>? choices, int width)
    {
        Game1.drawObjectQuestionDialogue(dialogue, choices?.ToArray(), width);
    }

    public static void drawObjectQuestionDialogue(string dialogue, List<Response>? choices)
    {
        Game1.drawObjectQuestionDialogue(dialogue, choices?.ToArray());
    }

    public static NPC? getCharacterFromName(string name, bool mustBeVillager = true)
    {
        return Game1.getCharacterFromName(name, mustBeVillager);
    }

    public static T? getCharacterFromName<T>(string name, bool mustBeVillager = true) where T : NPC
    {
        return Game1.getCharacterFromName<T>(name, mustBeVillager);
    }

    public static T? GetCharacterOfType<T>() where T : NPC
    {
        return Game1.GetCharacterOfType<T>();
    }

    public static T? GetCharacterWhere<T>(Func<T, bool> check) where T : NPC
    {
        return Game1.GetCharacterWhere(check);
    }

    public static int getModeratelyDarkTime()
    {
        return Game1.getModeratelyDarkTime(Game1.currentLocation);
    }

    public new static string GetSeasonForLocation(GameLocation location)
    {
        Season season = Game1.GetSeasonForLocation(location);
        return season.ToString();
    }

    public static int getStartingToGetDarkTime()
    {
        return Game1.getStartingToGetDarkTime(Game1.currentLocation);
    }

    public static int getTrulyDarkTime()
    {
        return Game1.getTrulyDarkTime(Game1.currentLocation);
    }

    public static bool isDarkOut()
    {
        return Game1.isDarkOut(Game1.currentLocation);
    }

    public static bool isStartingToGetDarkOut()
    {
        return Game1.isStartingToGetDarkOut(Game1.currentLocation);
    }

    public static void playMorningSong()
    {
        Game1.playMorningSong();
    }

    public static void playSound(string cueName)
    {
        Game1.playSound(cueName);
    }

    public static void playSoundPitched(string cueName, int pitch)
    {
        Game1.playSound(cueName, pitch);
    }

#if SMAPI_FOR_ANDROID
    public static IList<IClickableMenu> onScreenMenus
    {
        get => Game1.onScreenMenus;
        set => Game1.onScreenMenus = new List<IClickableMenu>(value);
    }
#endif


    /*********
    ** Private methods
    *********/
    private Game1Facade()
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
