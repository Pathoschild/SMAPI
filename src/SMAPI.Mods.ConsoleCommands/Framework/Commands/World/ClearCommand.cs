using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.World;

/// <summary>A command which clears in-game objects.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Loaded using reflection")]
internal class ClearCommand : ConsoleCommand
{
    /*********
    ** Fields
    *********/
    /// <summary>The valid types that can be cleared.</summary>
    private readonly string[] ValidTypes = { "crops", "debris", "fruit-trees", "furniture", "grass", "trees", "removable", "everything" };

    /// <summary>The resource clump IDs to consider debris.</summary>
    private readonly int[] DebrisClumps = { ResourceClump.stumpIndex, ResourceClump.hollowLogIndex, ResourceClump.meteoriteIndex, ResourceClump.boulderIndex };


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public ClearCommand()
        : base(
            name: "world_clear",
            description: "Clears in-game entities in a given location.\n\n"
                         + "Usage: world_clear <location> <object type>\n"
                         + " - location: the location name for which to clear objects (like Farm), or 'current' for the current location.\n"
                         + " - object type: the type of object clear. You can specify 'crops', 'debris' (stones/twigs/weeds and dead crops), 'furniture', 'grass', and 'trees' / 'fruit-trees'. You can also specify 'removable' (remove everything that can be removed or destroyed during normal gameplay) or 'everything' (remove everything including permanent bushes)."
        )
    { }

    /// <summary>Handle the command.</summary>
    /// <param name="monitor">Writes messages to the console and log file.</param>
    /// <param name="command">The command name.</param>
    /// <param name="args">The command arguments.</param>
    public override void Handle(IMonitor monitor, string command, ArgumentParser args)
    {
        // check context
        if (!Context.IsWorldReady)
        {
            monitor.Log("You need to load a save to use this command.", LogLevel.Error);
            return;
        }

        // parse arguments
        if (!args.TryGet(0, "location", out string? locationName, required: true))
            return;
        if (!args.TryGet(1, "object type", out string? type, required: true, oneOf: this.ValidTypes))
            return;

        // get target location
        GameLocation? location = Game1.locations.FirstOrDefault(p => p.Name != null && p.Name.Equals(locationName, StringComparison.OrdinalIgnoreCase));
        if (location == null && locationName == "current")
            location = Game1.currentLocation;
        if (location == null)
        {
            string[] locationNames = (from loc in Game1.locations where !string.IsNullOrWhiteSpace(loc.Name) orderby loc.Name select loc.Name).ToArray();
            monitor.Log($"Could not find a location with that name. Must be one of [{string.Join(", ", locationNames)}].", LogLevel.Error);
            return;
        }

        // apply
        switch (type)
        {
            case "crops":
            {
                int removed =
                    this.RemoveTerrainFeatures(location, p => p is HoeDirt)
                    + this.RemoveResourceClumps(location, p => p is GiantCrop);
                monitor.Log($"Done! Removed {removed} entities from {location.Name}.", LogLevel.Info);
                break;
            }

            case "debris":
            {
                int removed = 0;
                foreach (var pair in location.terrainFeatures.Pairs.ToArray())
                {
                    TerrainFeature feature = pair.Value;
                    if (feature is HoeDirt dirt && dirt.crop?.dead == true)
                    {
                        dirt.crop = null;
                        removed++;
                    }
                }

                removed +=
                    this.RemoveObjects(location, obj =>
                        obj is not Chest
                        && (
                            obj.Name is "Weeds" or "Stone"
                            || obj.ParentSheetIndex is 294 or 295
                        )
                    )
                    + this.RemoveResourceClumps(location, clump => this.DebrisClumps.Contains(clump.parentSheetIndex.Value));

                monitor.Log($"Done! Removed {removed} entities from {location.Name}.", LogLevel.Info);
                break;
            }

            case "fruit-trees":
            {
                int removed = this.RemoveTerrainFeatures(location, feature => feature is FruitTree);
                monitor.Log($"Done! Removed {removed} entities from {location.Name}.", LogLevel.Info);
                break;
            }

            case "furniture":
            {
                int removed = this.RemoveFurniture(location, _ => true);
                monitor.Log($"Done! Removed {removed} entities from {location.Name}.", LogLevel.Info);
                break;
            }

            case "grass":
            {
                int removed = this.RemoveTerrainFeatures(location, feature => feature is Grass);
                monitor.Log($"Done! Removed {removed} entities from {location.Name}.", LogLevel.Info);
                break;
            }

            case "trees":
            {
                int removed = this.RemoveTerrainFeatures(location, feature => feature is Tree);
                monitor.Log($"Done! Removed {removed} entities from {location.Name}.", LogLevel.Info);
                break;
            }

            case "removable":
            case "everything":
            {
                bool everything = type == "everything";
                int removed =
                    this.RemoveFurniture(location, _ => true)
                    + this.RemoveObjects(location, _ => true)
                    + this.RemoveTerrainFeatures(location, _ => true)
                    + this.RemoveLargeTerrainFeatures(location, p => everything || p is not Bush bush || bush.isDestroyable(location, p.currentTileLocation))
                    + this.RemoveResourceClumps(location, _ => true);
                monitor.Log($"Done! Removed {removed} entities from {location.Name}.", LogLevel.Info);
                break;
            }

            default:
                monitor.Log($"Unknown type '{type}'. Must be one [{string.Join(", ", this.ValidTypes)}].", LogLevel.Error);
                break;
        }
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Remove objects from a location matching a lambda.</summary>
    /// <param name="location">The location to search.</param>
    /// <param name="shouldRemove">Whether an entity should be removed.</param>
    /// <returns>Returns the number of removed entities.</returns>
    private int RemoveObjects(GameLocation location, Func<SObject, bool> shouldRemove)
    {
        int removed = 0;

        foreach ((Vector2 tile, SObject? obj) in location.Objects.Pairs.ToArray())
        {
            if (shouldRemove(obj))
            {
                location.Objects.Remove(tile);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>Remove terrain features from a location matching a lambda.</summary>
    /// <param name="location">The location to search.</param>
    /// <param name="shouldRemove">Whether an entity should be removed.</param>
    /// <returns>Returns the number of removed entities.</returns>
    private int RemoveTerrainFeatures(GameLocation location, Func<TerrainFeature, bool> shouldRemove)
    {
        int removed = 0;

        foreach ((Vector2 tile, TerrainFeature? feature) in location.terrainFeatures.Pairs.ToArray())
        {
            if (shouldRemove(feature))
            {
                location.terrainFeatures.Remove(tile);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>Remove large terrain features from a location matching a lambda.</summary>
    /// <param name="location">The location to search.</param>
    /// <param name="shouldRemove">Whether an entity should be removed.</param>
    /// <returns>Returns the number of removed entities.</returns>
    private int RemoveLargeTerrainFeatures(GameLocation location, Func<LargeTerrainFeature, bool> shouldRemove)
    {
        int removed = 0;

        foreach (LargeTerrainFeature feature in location.largeTerrainFeatures.ToArray())
        {
            if (shouldRemove(feature))
            {
                location.largeTerrainFeatures.Remove(feature);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>Remove resource clumps from a location matching a lambda.</summary>
    /// <param name="location">The location to search.</param>
    /// <param name="shouldRemove">Whether an entity should be removed.</param>
    /// <returns>Returns the number of removed entities.</returns>
    private int RemoveResourceClumps(GameLocation location, Func<ResourceClump, bool> shouldRemove)
    {
        int removed = 0;

        foreach (ResourceClump clump in location.resourceClumps.Where(shouldRemove).ToArray())
        {
            location.resourceClumps.Remove(clump);
            removed++;
        }

        if (location is Woods woods)
        {
            foreach (ResourceClump clump in woods.stumps.Where(shouldRemove).ToArray())
            {
                woods.stumps.Remove(clump);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>Remove furniture from a location matching a lambda.</summary>
    /// <param name="location">The location to search.</param>
    /// <param name="shouldRemove">Whether an entity should be removed.</param>
    /// <returns>Returns the number of removed entities.</returns>
    private int RemoveFurniture(GameLocation location, Func<Furniture, bool> shouldRemove)
    {
        int removed = 0;

        foreach (Furniture furniture in location.furniture.ToArray())
        {
            if (shouldRemove(furniture))
            {
                location.furniture.Remove(furniture);
                removed++;
            }
        }

        return removed;
    }
}
