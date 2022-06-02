using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Mods.ConsoleCommands.Framework.ItemData;
using StardewValley;
using StardewValley.GameData.FishPond;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework;

/// <summary>Provides methods for searching and constructing items.</summary>
internal class ItemRepository
{
    /*********
    ** Fields
    *********/
    /// <summary>The custom ID offset for items don't have a unique ID in the game.</summary>
    private readonly int CustomIDOffset = 1000;


    /*********
    ** Public methods
    *********/
    /// <summary>Get all spawnable items.</summary>
    /// <param name="itemTypes">The item types to fetch (or null for any type).</param>
    /// <param name="includeVariants">Whether to include flavored variants like "Sunflower Honey".</param>
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "TryCreate invokes the lambda immediately.")]
    public IEnumerable<SearchableItem> GetAll(ItemType[]? itemTypes = null, bool includeVariants = true)
    {
        //
        //
        // Be careful about closure variable capture here!
        //
        // SearchableItem stores the Func<Item> to create new instances later. Loop variables passed into the
        // function will be captured, so every func in the loop will use the value from the last iteration. Use the
        // TryCreate(type, id, entity => item) form to avoid the issue, or create a local variable to pass in.
        //
        //

        IEnumerable<SearchableItem?> GetAllRaw()
        {
            HashSet<ItemType>? types = itemTypes?.Any() == true ? new HashSet<ItemType>(itemTypes) : null;
            bool ShouldGet(ItemType type) => types == null || types.Contains(type);

            // get tools
            if (ShouldGet(ItemType.Tool))
            {
                for (int q = Tool.stone; q <= Tool.iridium; q++)
                {
                    int quality = q;

                    yield return this.TryCreate(ItemType.Tool, ToolFactory.axe, _ => ToolFactory.getToolFromDescription(ToolFactory.axe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.hoe, _ => ToolFactory.getToolFromDescription(ToolFactory.hoe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.pickAxe, _ => ToolFactory.getToolFromDescription(ToolFactory.pickAxe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.wateringCan, _ => ToolFactory.getToolFromDescription(ToolFactory.wateringCan, quality));
                    if (quality != Tool.iridium)
                        yield return this.TryCreate(ItemType.Tool, ToolFactory.fishingRod, _ => ToolFactory.getToolFromDescription(ToolFactory.fishingRod, quality));
                }
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset, _ => new MilkPail()); // these don't have any sort of ID, so we'll just assign some arbitrary ones
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 1, _ => new Shears());
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 2, _ => new Pan());
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 3, _ => new Wand());
            }

            // clothing
            if (ShouldGet(ItemType.Clothing))
            {
                foreach (int id in this.GetShirtIds())
                    yield return this.TryCreate(ItemType.Clothing, id, p => new Clothing(p.ID));
            }

            // wallpapers
            if (ShouldGet(ItemType.Wallpaper))
            {
                for (int id = 0; id < 112; id++)
                    yield return this.TryCreate(ItemType.Wallpaper, id, p => new Wallpaper(p.ID) { Category = SObject.furnitureCategory });
            }

            // flooring
            if (ShouldGet(ItemType.Flooring))
            {
                for (int id = 0; id < 56; id++)
                    yield return this.TryCreate(ItemType.Flooring, id, p => new Wallpaper(p.ID, isFloor: true) { Category = SObject.furnitureCategory });
            }

            // equipment
            if (ShouldGet(ItemType.Boots))
            {
                foreach (int id in this.TryLoad<int, string>("Data\\Boots").Keys)
                    yield return this.TryCreate(ItemType.Boots, id, p => new Boots(p.ID));
            }
            if (ShouldGet(ItemType.Hat))
            {
                foreach (int id in this.TryLoad<int, string>("Data\\hats").Keys)
                    yield return this.TryCreate(ItemType.Hat, id, p => new Hat(p.ID));
            }

            // weapons
            if (ShouldGet(ItemType.Weapon))
            {
                foreach (int id in this.TryLoad<int, string>("Data\\weapons").Keys)
                {
                    yield return this.TryCreate(ItemType.Weapon, id, p => p.ID is >= 32 and <= 34
                        ? new Slingshot(p.ID)
                        : new MeleeWeapon(p.ID)
                    );
                }
            }

            // furniture
            if (ShouldGet(ItemType.Furniture))
            {
                foreach (int id in this.TryLoad<int, string>("Data\\Furniture").Keys)
                    yield return this.TryCreate(ItemType.Furniture, id, p => Furniture.GetFurnitureInstance(p.ID));
            }

            // craftables
            if (ShouldGet(ItemType.BigCraftable))
            {
                foreach (int id in Game1.bigCraftablesInformation.Keys)
                    yield return this.TryCreate(ItemType.BigCraftable, id, p => new SObject(Vector2.Zero, p.ID));
            }

            // objects
            if (ShouldGet(ItemType.Object) || ShouldGet(ItemType.Ring))
            {
                foreach (int id in Game1.objectInformation.Keys)
                {
                    string[]? fields = Game1.objectInformation[id]?.Split('/');

                    // ring
                    if (id != 801 && fields?.Length >= 4 && fields[3] == "Ring") // 801 = wedding ring, which isn't an equippable ring
                    {
                        if (ShouldGet(ItemType.Ring))
                            yield return this.TryCreate(ItemType.Ring, id, p => new Ring(p.ID));
                    }

                    // journal scrap
                    else if (id == 842)
                    {
                        if (ShouldGet(ItemType.Object))
                        {
                            foreach (SearchableItem? journalScrap in this.GetSecretNotes(isJournalScrap: true))
                                yield return journalScrap;
                        }
                    }

                    // secret notes
                    else if (id == 79)
                    {
                        if (ShouldGet(ItemType.Object))
                        {
                            foreach (SearchableItem? secretNote in this.GetSecretNotes(isJournalScrap: false))
                                yield return secretNote;
                        }
                    }

                    // object
                    else if (ShouldGet(ItemType.Object))
                    {
                        // spawn main item
                        SObject? item = null;
                        yield return this.TryCreate(ItemType.Object, id, p =>
                        {
                            return item = (p.ID == 812 // roe
                                    ? new ColoredObject(p.ID, 1, Color.White)
                                    : new SObject(p.ID, 1)
                                );
                        });
                        if (item == null)
                            continue;

                        // flavored items
                        if (includeVariants)
                        {
                            foreach (SearchableItem? variant in this.GetFlavoredObjectVariants(item))
                                yield return variant;
                        }
                    }
                }
            }
        }

        return (
            from item in GetAllRaw()
            where item != null
            select item
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get the individual secret note or journal scrap items.</summary>
    /// <param name="isJournalScrap">Whether to get journal scraps.</param>
    /// <remarks>Derived from <see cref="GameLocation.tryToCreateUnseenSecretNote"/>.</remarks>
    private IEnumerable<SearchableItem?> GetSecretNotes(bool isJournalScrap)
    {
        // get base item ID
        int baseId = isJournalScrap ? 842 : 79;

        // get secret note IDs
        var ids = this
            .TryLoad<int, string>("Data\\SecretNotes")
            .Keys
            .Where(isJournalScrap
                ? id => (id >= GameLocation.JOURNAL_INDEX)
                : id => (id < GameLocation.JOURNAL_INDEX)
            )
            .Select<int, int>(isJournalScrap
                ? id => (id - GameLocation.JOURNAL_INDEX)
                : id => id
            );

        // build items
        foreach (int id in ids)
        {
            int fakeId = this.CustomIDOffset * 8 + id;
            if (isJournalScrap)
                fakeId += GameLocation.JOURNAL_INDEX;

            yield return this.TryCreate(ItemType.Object, fakeId, _ =>
            {
                SObject note = new(baseId, 1);
                note.Name = $"{note.Name} #{id}";
                return note;
            });
        }
    }

    /// <summary>Get flavored variants of a base item (like Blueberry Wine for Blueberry), if any.</summary>
    /// <param name="item">A sample of the base item.</param>
    private IEnumerable<SearchableItem?> GetFlavoredObjectVariants(SObject item)
    {
        int id = item.ParentSheetIndex;

        switch (item.Category)
        {
            // fruit products
            case SObject.FruitsCategory:
                // wine
                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 2 + id, _ => new SObject(348, 1)
                {
                    Name = $"{item.Name} Wine",
                    Price = item.Price * 3,
                    preserve = { SObject.PreserveType.Wine },
                    preservedParentSheetIndex = { id }
                });

                // jelly
                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 3 + id, _ => new SObject(344, 1)
                {
                    Name = $"{item.Name} Jelly",
                    Price = 50 + item.Price * 2,
                    preserve = { SObject.PreserveType.Jelly },
                    preservedParentSheetIndex = { id }
                });
                break;

            // vegetable products
            case SObject.VegetableCategory:
                // juice
                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 4 + id, _ => new SObject(350, 1)
                {
                    Name = $"{item.Name} Juice",
                    Price = (int)(item.Price * 2.25d),
                    preserve = { SObject.PreserveType.Juice },
                    preservedParentSheetIndex = { id }
                });

                // pickled
                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 5 + id, _ => new SObject(342, 1)
                {
                    Name = $"Pickled {item.Name}",
                    Price = 50 + item.Price * 2,
                    preserve = { SObject.PreserveType.Pickle },
                    preservedParentSheetIndex = { id }
                });
                break;

            // flower honey
            case SObject.flowersCategory:
                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 5 + id, _ =>
                {
                    SObject honey = new(Vector2.Zero, 340, $"{item.Name} Honey", false, true, false, false)
                    {
                        Name = $"{item.Name} Honey",
                        preservedParentSheetIndex = { id }
                    };
                    honey.Price += item.Price * 2;
                    return honey;
                });
                break;

            // roe and aged roe (derived from FishPond.GetFishProduce)
            case SObject.sellAtFishShopCategory when id == 812:
            {
                this.GetRoeContextTagLookups(out HashSet<string> simpleTags, out List<List<string>> complexTags);

                foreach (var pair in Game1.objectInformation)
                {
                    // get input
                    SObject? input = this.TryCreate(ItemType.Object, pair.Key, p => new SObject(p.ID, 1))?.Item as SObject;
                    if (input == null)
                        continue;

                    HashSet<string> inputTags = input.GetContextTags();
                    if (!inputTags.Any())
                        continue;

                    // check if roe-producing fish
                    if (!inputTags.Any(tag => simpleTags.Contains(tag)) && !complexTags.Any(set => set.All(tag => input.HasContextTag(tag))))
                        continue;

                    // yield roe
                    SObject? roe = null;
                    Color color = this.GetRoeColor(input);
                    yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 7 + id, _ =>
                    {
                        roe = new ColoredObject(812, 1, color)
                        {
                            name = $"{input.Name} Roe",
                            preserve = { Value = SObject.PreserveType.Roe },
                            preservedParentSheetIndex = { Value = input.ParentSheetIndex }
                        };
                        roe.Price += input.Price / 2;
                        return roe;
                    });

                    // aged roe
                    if (roe != null && pair.Key != 698) // aged sturgeon roe is caviar, which is a separate item
                    {
                        yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 7 + id, _ => new ColoredObject(447, 1, color)
                        {
                            name = $"Aged {input.Name} Roe",
                            Category = -27,
                            preserve = { Value = SObject.PreserveType.AgedRoe },
                            preservedParentSheetIndex = { Value = input.ParentSheetIndex },
                            Price = roe.Price * 2
                        });
                    }
                }
            }
                break;
        }
    }

    /// <summary>Get optimized lookups to match items which produce roe in a fish pond.</summary>
    /// <param name="simpleTags">A lookup of simple singular tags which match a roe-producing fish.</param>
    /// <param name="complexTags">A list of tag sets which match roe-producing fish.</param>
    private void GetRoeContextTagLookups(out HashSet<string> simpleTags, out List<List<string>> complexTags)
    {
        simpleTags = new HashSet<string>();
        complexTags = new List<List<string>>();

        foreach (FishPondData data in Game1.content.Load<List<FishPondData>>("Data\\FishPondData"))
        {
            if (data.ProducedItems.All(p => p.ItemID != 812))
                continue; // doesn't produce roe

            if (data.RequiredTags.Count == 1 && !data.RequiredTags[0].StartsWith("!"))
                simpleTags.Add(data.RequiredTags[0]);
            else
                complexTags.Add(data.RequiredTags);
        }
    }

    /// <summary>Try to load a data file, and return empty data if it's invalid.</summary>
    /// <typeparam name="TKey">The asset key type.</typeparam>
    /// <typeparam name="TValue">The asset value type.</typeparam>
    /// <param name="assetName">The data asset name.</param>
    private Dictionary<TKey, TValue> TryLoad<TKey, TValue>(string assetName)
        where TKey : notnull
    {
        try
        {
            return Game1.content.Load<Dictionary<TKey, TValue>>(assetName);
        }
        catch (ContentLoadException)
        {
            // generally due to a player incorrectly replacing a data file with an XNB mod
            return new Dictionary<TKey, TValue>();
        }
    }

    /// <summary>Create a searchable item if valid.</summary>
    /// <param name="type">The item type.</param>
    /// <param name="id">The unique ID (if different from the item's parent sheet index).</param>
    /// <param name="createItem">Create an item instance.</param>
    private SearchableItem? TryCreate(ItemType type, int id, Func<SearchableItem, Item> createItem)
    {
        try
        {
            var item = new SearchableItem(type, id, createItem);
            item.Item.getDescription(); // force-load item data, so it crashes here if it's invalid
            return item;
        }
        catch
        {
            return null; // if some item data is invalid, just don't include it
        }
    }

    /// <summary>Get the color to use a given fish's roe.</summary>
    /// <param name="fish">The fish whose roe to color.</param>
    /// <remarks>Derived from <see cref="StardewValley.Buildings.FishPond.GetFishProduce"/>.</remarks>
    private Color GetRoeColor(SObject fish)
    {
        return fish.ParentSheetIndex == 698 // sturgeon
            ? new Color(61, 55, 42)
            : (TailoringMenu.GetDyeColor(fish) ?? Color.Orange);
    }

    /// <summary>Get valid shirt IDs.</summary>
    /// <remarks>
    /// Shirts have a possible range of 1000–1999, but not all of those IDs are valid. There are two sets of IDs:
    ///
    /// <list type="number">
    ///   <item>
    ///     Shirts which exist in <see cref="Game1.clothingInformation"/>.
    ///   </item>
    ///   <item>
    ///     Shirts with a dynamic ID and no entry in <see cref="Game1.clothingInformation"/>. These automatically
    ///     use the generic shirt entry with ID <c>-1</c> and are mapped to a calculated position in the
    ///     <c>Characters/Farmer/shirts</c> spritesheet. There's no constant we can use, but some known valid
    ///     ranges are 1000–1111 (used in <see cref="Farmer.changeShirt"/> for the customization screen and
    ///     1000–1127 (used in <see cref="Utility.getShopStock"/> and <see cref="GameLocation.sandyShopStock"/>).
    ///     Based on the spritesheet, the max valid ID is 1299.
    ///   </item>
    /// </list>
    /// </remarks>
    private IEnumerable<int> GetShirtIds()
    {
        // defined shirt items
        foreach (int id in Game1.clothingInformation.Keys)
        {
            if (id < 0)
                continue; // placeholder data for character customization clothing below

            yield return id;
        }

        // dynamic shirts
        HashSet<int> clothingIds = new HashSet<int>(Game1.clothingInformation.Keys);
        for (int id = 1000; id <= 1299; id++)
        {
            if (!clothingIds.Contains(id))
                yield return id;
        }
    }
}