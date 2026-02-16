using PathOfBuilding.Core.Items;
using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Tests.Items;

public class ItemSlotResolverTests
{
    private static ParsedItem MakeItem(int id, ItemRarity rarity = ItemRarity.Rare)
    {
        return new ParsedItem { Id = id, Rarity = rarity, Name = $"Item{id}" };
    }

    [Fact]
    public void Resolve_ActiveItemSet_Found()
    {
        var items = new Dictionary<int, ParsedItem>
        {
            [1] = MakeItem(1),
        };
        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData
                {
                    Id = 1,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Helmet", ItemId = 1 },
                    }
                }
            }
        };

        var result = ItemSlotResolver.Resolve(build, items);
        Assert.Single(result);
        Assert.Same(items[1], result["Helmet"]);
    }

    [Fact]
    public void Resolve_CorrectSlotMapping()
    {
        var items = new Dictionary<int, ParsedItem>
        {
            [1] = MakeItem(1),
            [2] = MakeItem(2),
        };
        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData
                {
                    Id = 1,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Helmet", ItemId = 1 },
                        new SlotAssignment { SlotName = "Gloves", ItemId = 2 },
                    }
                }
            }
        };

        var result = ItemSlotResolver.Resolve(build, items);
        Assert.Equal(2, result.Count);
        Assert.Same(items[1], result["Helmet"]);
        Assert.Same(items[2], result["Gloves"]);
    }

    [Fact]
    public void Resolve_EmptyItemSet_EmptyResult()
    {
        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData { Id = 1, Slots = new List<SlotAssignment>() }
            }
        };

        var result = ItemSlotResolver.Resolve(build, new Dictionary<int, ParsedItem>());
        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_ItemIdZero_SlotSkipped()
    {
        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData
                {
                    Id = 1,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Helmet", ItemId = 0 },
                    }
                }
            }
        };

        var result = ItemSlotResolver.Resolve(build, new Dictionary<int, ParsedItem>());
        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_MissingItemId_SlotSkipped()
    {
        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData
                {
                    Id = 1,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Helmet", ItemId = 999 },
                    }
                }
            }
        };

        var result = ItemSlotResolver.Resolve(build, new Dictionary<int, ParsedItem>());
        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_FallsBackToDefaultSlots()
    {
        var items = new Dictionary<int, ParsedItem>
        {
            [5] = MakeItem(5),
        };
        var build = new BuildData
        {
            ActiveItemSet = 99, // No matching ItemSet
            ItemSets = new List<ItemSetData>(),
            DefaultSlots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Belt", ItemId = 5 },
            }
        };

        var result = ItemSlotResolver.Resolve(build, items);
        Assert.Single(result);
        Assert.Same(items[5], result["Belt"]);
    }

    [Fact]
    public void Resolve_MultipleItemSets_OnlyActiveUsed()
    {
        var items = new Dictionary<int, ParsedItem>
        {
            [1] = MakeItem(1),
            [2] = MakeItem(2),
        };
        var build = new BuildData
        {
            ActiveItemSet = 2,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData
                {
                    Id = 1,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Helmet", ItemId = 1 },
                    }
                },
                new ItemSetData
                {
                    Id = 2,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Helmet", ItemId = 2 },
                    }
                }
            }
        };

        var result = ItemSlotResolver.Resolve(build, items);
        Assert.Same(items[2], result["Helmet"]);
    }

    [Fact]
    public void Resolve_FlaskSlots_Excluded()
    {
        var items = new Dictionary<int, ParsedItem>
        {
            [1] = MakeItem(1),
            [2] = MakeItem(2),
        };
        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData
                {
                    Id = 1,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Flask 1", ItemId = 1 },
                        new SlotAssignment { SlotName = "Helmet", ItemId = 2 },
                    }
                }
            }
        };

        var result = ItemSlotResolver.Resolve(build, items);
        Assert.Single(result);
        Assert.True(result.ContainsKey("Helmet"));
        Assert.False(result.ContainsKey("Flask 1"));
    }

    [Fact]
    public void Resolve_AbyssalSocketSlots_Excluded()
    {
        var items = new Dictionary<int, ParsedItem>
        {
            [1] = MakeItem(1),
        };
        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData
                {
                    Id = 1,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Helmet Abyssal Socket 1", ItemId = 1 },
                    }
                }
            }
        };

        var result = ItemSlotResolver.Resolve(build, items);
        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_WeaponSwapSlots_Excluded()
    {
        var items = new Dictionary<int, ParsedItem>
        {
            [1] = MakeItem(1),
        };
        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData
                {
                    Id = 1,
                    Slots = new List<SlotAssignment>
                    {
                        new SlotAssignment { SlotName = "Weapon 1 Swap", ItemId = 1 },
                        new SlotAssignment { SlotName = "Weapon 2 Swap", ItemId = 1 },
                    }
                }
            }
        };

        var result = ItemSlotResolver.Resolve(build, items);
        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_AllEquipmentSlots()
    {
        var items = new Dictionary<int, ParsedItem>();
        var slots = new List<SlotAssignment>();
        for (int i = 0; i < ItemSlotResolver.EquipmentSlots.Length; i++)
        {
            items[i + 1] = MakeItem(i + 1);
            slots.Add(new SlotAssignment { SlotName = ItemSlotResolver.EquipmentSlots[i], ItemId = i + 1 });
        }

        var build = new BuildData
        {
            ActiveItemSet = 1,
            ItemSets = new List<ItemSetData>
            {
                new ItemSetData { Id = 1, Slots = slots }
            }
        };

        var result = ItemSlotResolver.Resolve(build, items);
        Assert.Equal(ItemSlotResolver.EquipmentSlots.Length, result.Count);
    }

    [Fact]
    public void Resolve_NoItemSets_NoDefaultSlots_EmptyResult()
    {
        var build = new BuildData();
        var result = ItemSlotResolver.Resolve(build, new Dictionary<int, ParsedItem>());
        Assert.Empty(result);
    }
}
