using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Java.Util;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;

namespace StardewModdingAPI.Mobile.Mods;

[HarmonyPatch]
internal static class SveFix
{
    public static void Init(AndroidModFixManager modFix)
    {
        modFix.RegisterOnModLoaded("StardewValleyExpanded.dll", OnModLoaded);
    }
    static Type TMXLLoadMapFacingDirection;
    private static IMonitor Monitor { get; set; } = null;

    static void OnModLoaded(Assembly asm)
    {
        var harmony = AndroidPatcher.harmony;
        TMXLLoadMapFacingDirection = asm.GetType("StardewValleyExpanded.HarmonyPatch_TMXLLoadMapFacingDirection");
        harmony.Patch(
            original: AccessTools.Method(TMXLLoadMapFacingDirection, "ApplyPatch"),
            prefix: new(typeof(SveFix), nameof(TMXLLoadMapFacingDirection_ApplyPatch))
        );
        Monitor = SCore.Instance.SMAPIMonitor;
    }
    static bool TMXLLoadMapFacingDirection_ApplyPatch(Harmony harmony, IMonitor monitor)
    {
        monitor.Log("Try fix TMXLLoadMapFacingDirection_ApplyPatch", LogLevel.Trace);
        monitor.Log("Applying Harmony patch \"Sve_TXMLMapFacingDir_warpFarmer\": " +
            "prefixing SDV method \"Game1.warpFarmer(LocationRequest, int, int, int)\".");
        harmony.Patch(
            original: AccessTools.Method(typeof(Game1), "warpFarmer",
            [
                typeof(LocationRequest), typeof(int),
                typeof(int), typeof(int),
            ]),
            prefix: new(typeof(SveFix), nameof(Sve_TXMLMapFacingDir_warpFarmer))
        );

        return false;
    }

    public static int WhichParameterIsFacingDirection { get; set; } = 4;

    //src code
    //https://github.com/FlashShifter/StardewValleyExpanded/blob/be7e471a8090fced91007d6caf91380097d3a0b3/Code/HarmonyPatch_TMXLLoadMapFacingDirection.cs#L68
    /// <summary>Detects the "facing direction" parameter in a TMXL LoadMap tile property and, if found, modifies the local player's facing direction after warping.</summary>
    /// <param name="__instance">The instance calling the original method.</param>
    /// <param name="facingDirectionAfterWarp">The direction the player will face after warping.</param>

    static void Sve_TXMLMapFacingDir_warpFarmer(LocationRequest locationRequest, int tileX, int tileY,
       ref int facingDirectionAfterWarp)
    {
        try
        {
            //Console.WriteLine("Skip code Sve_TXMLMapFacingDir_warpFarmer");
            //check the player's current tile for a LoadMap property and, if found, get its value
            Vector2 tile = Game1.player.Tile;
            string property = Game1.player.currentLocation?.doesTileHaveProperty((int)tile.X, (int)tile.Y, "TouchAction", "Back");
            if (property?.StartsWith("LoadMap", StringComparison.OrdinalIgnoreCase) == true) //if this is a TMXL LoadMap property
            {
                string[] args = property.Split(' '); //split into separate arguments

                if (args.Length > WhichParameterIsFacingDirection) //if the facing direction argument exists
                {
                    if (int.TryParse(args[WhichParameterIsFacingDirection], out int facingDirection) && facingDirection >= 0 && facingDirection <= 3) //if the value is valid
                    {
                        Monitor.VerboseLog($"SveFix: Applying custom facing direction for LoadMap warp: {facingDirection}");
                        facingDirectionAfterWarp = facingDirection; //edit the original method's argument
                    }
                    else if (string.IsNullOrWhiteSpace(args[WhichParameterIsFacingDirection]) == false) //if the argument was invalid but NOT blank
                    {
                        Monitor.LogOnce($"SveFix: Couldn't parse the custom 'facing direction' value for a TMXL LoadMap property; ignoring it. Debug information will be displayed below.\nLocation: {Game1.currentLocation?.Name ?? "null"}.\nTile: {$"{tile.X},{tile.Y}"}.\nFacing direction value: \"{args[WhichParameterIsFacingDirection]}\".\nFull property value: \"{property}\".", LogLevel.Debug);
                    }
                }

            }
            Console.WriteLine("End Sve_TXMLMapFacingDir_warpFarmer");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error on Sve FacingDir warpFarmer");
            Console.WriteLine(ex);
            Monitor.LogOnce($"SveFix: Harmony patch \"{nameof(Sve_TXMLMapFacingDir_warpFarmer)}\" has encountered an error. LoadMap tile properties might ignore their custom \"facing direction\" values. Full error message: \n{ex.ToString()}", LogLevel.Error);
            return; //run the original method
        }
    }
}


