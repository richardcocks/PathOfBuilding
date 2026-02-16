using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Items;
using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Items;

public class BuildPipelineItemTests
{
    private static BuildData MinimalBuild(string className = "Witch", string ascendancy = "None",
        int level = 1, string bandit = "None")
    {
        return new BuildData
        {
            Metadata = new BuildMetadata
            {
                ClassName = className,
                AscendClassName = ascendancy,
                Level = level,
                Bandit = bandit,
            },
        };
    }

    private static string TestDataPath(string filename) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", filename);

    // ─── Item parsing integration ───

    [Fact]
    public void CreateActors_WithItems_ParsedItemsCreated()
    {
        var build = MinimalBuild();
        build.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Test Item
Iron Ring
Implicits: 0
+50 to maximum Life"
        });
        build.ActiveItemSet = 1;
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Ring 1", ItemId = 1 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.NotNull(player.EquippedItems);
        Assert.Single(player.EquippedItems);
        Assert.True(player.EquippedItems.ContainsKey("Ring 1"));
    }

    [Fact]
    public void CreateActors_NoItems_PipelineSucceeds()
    {
        var build = MinimalBuild();
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.NotNull(player.EquippedItems);
        Assert.Empty(player.EquippedItems);
    }

    [Fact]
    public void CreateActors_EmptyRawText_Skipped()
    {
        var build = MinimalBuild();
        build.Items.Add(new ItemData { Id = 1, RawText = "" });
        build.Items.Add(new ItemData { Id = 2, RawText = "   " });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.NotNull(player.EquippedItems);
        Assert.Empty(player.EquippedItems);
    }

    // ─── Slot resolution integration ───

    [Fact]
    public void CreateActors_ActiveItemSet_CorrectItemsEquipped()
    {
        var build = MinimalBuild();
        build.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Test Belt
Studded Belt
Implicits: 0
+50 to maximum Life"
        });
        build.Items.Add(new ItemData
        {
            Id = 2,
            RawText = @"Rarity: RARE
Test Helmet
Iron Hat
Implicits: 0
+30 to maximum Life"
        });
        build.ActiveItemSet = 1;
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Belt", ItemId = 1 },
                new SlotAssignment { SlotName = "Helmet", ItemId = 2 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(2, player.EquippedItems!.Count);
        Assert.Equal("Test Belt", player.EquippedItems["Belt"].Name);
        Assert.Equal("Test Helmet", player.EquippedItems["Helmet"].Name);
    }

    [Fact]
    public void CreateActors_NoItemSets_EmptyEquipment()
    {
        var build = MinimalBuild();
        build.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Test Item
Iron Ring
Implicits: 0
+50 to maximum Life"
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Empty(player.EquippedItems!);
    }

    // ─── Full pipeline with items ───

    [Fact]
    public void Calculate_ItemLifeMod_IncreasesLife()
    {
        var buildNoItems = MinimalBuild("Marauder", level: 90);
        var (playerNoItems, _) = BuildPipeline.Calculate(buildNoItems);

        var buildWithItems = MinimalBuild("Marauder", level: 90);
        buildWithItems.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Test Belt
Studded Belt
Implicits: 0
+100 to maximum Life"
        });
        buildWithItems.ActiveItemSet = 1;
        buildWithItems.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Belt", ItemId = 1 },
            }
        });
        var (playerWithItems, _) = BuildPipeline.Calculate(buildWithItems);

        Assert.True(playerWithItems.Output["Life"] > playerNoItems.Output["Life"],
            $"Life with items ({playerWithItems.Output["Life"]}) should be > without ({playerNoItems.Output["Life"]})");
    }

    [Fact]
    public void Calculate_ItemResistMod_Applied()
    {
        var build = MinimalBuild("Witch", level: 1);
        build.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Test Ring
Iron Ring
Implicits: 0
+43% to Cold Resistance"
        });
        build.ActiveItemSet = 1;
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Ring 1", ItemId = 1 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        // Cold resist = -60 (penalty) + 43 (item) = -17
        Assert.Equal(-17, player.ModDB.Sum(ModType.Base, null, "ColdResist"));
    }

    [Fact]
    public void Calculate_CraftedMods_Applied()
    {
        var build = MinimalBuild("Witch", level: 1);
        build.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Test Ring
Iron Ring
Implicits: 0
+20 to maximum Life
{crafted}+30 to maximum Life"
        });
        build.ActiveItemSet = 1;
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Ring 1", ItemId = 1 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        // Life from items: 20 + 30 = 50 (plus whatever base life from level/class)
        double lifeFromItems = 50;
        double totalLifeBase = player.ModDB.Sum(ModType.Base, null, "Life");
        Assert.True(totalLifeBase >= lifeFromItems,
            $"Total base life ({totalLifeBase}) should include {lifeFromItems} from items");
    }

    [Fact]
    public void Calculate_ItemMultipliers_Set()
    {
        var build = MinimalBuild("Witch", level: 1);
        build.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: UNIQUE
Heretic's Veil
Deicide Mask
Implicits: 0"
        });
        build.ActiveItemSet = 1;
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Helmet", ItemId = 1 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal(1, player.ModDB.Multipliers.GetValueOrDefault("UniqueItem"));
        Assert.Equal(1, player.ModDB.Multipliers.GetValueOrDefault("HelmetItem"));
    }

    [Fact]
    public void Calculate_WithItems_OutputLifeHigher()
    {
        var buildNoItems = MinimalBuild("Marauder", level: 50);
        var (pNo, _) = BuildPipeline.Calculate(buildNoItems);

        var buildWith = MinimalBuild("Marauder", level: 50);
        buildWith.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Belt
Studded Belt
Implicits: 0
+80 to maximum Life"
        });
        buildWith.Items.Add(new ItemData
        {
            Id = 2,
            RawText = @"Rarity: RARE
Ring
Iron Ring
Implicits: 0
+70 to maximum Life"
        });
        buildWith.ActiveItemSet = 1;
        buildWith.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Belt", ItemId = 1 },
                new SlotAssignment { SlotName = "Ring 1", ItemId = 2 },
            }
        });
        var (pWith, _) = BuildPipeline.Calculate(buildWith);

        Assert.True(pWith.Output["Life"] > pNo.Output["Life"]);
    }

    [Fact]
    public void Calculate_ItemStrMod_AffectsOutput()
    {
        var build = MinimalBuild("Witch", level: 1);
        build.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Test Amulet
Citrine Amulet
Implicits: 0
+43 to Strength"
        });
        build.ActiveItemSet = 1;
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Amulet", ItemId = 1 },
            }
        });

        var (player, _) = BuildPipeline.Calculate(build);

        // Witch base Str = 14, item adds 43 → total 57
        Assert.Equal(57, player.Output["Str"]);
    }

    // ─── Actor state ───

    [Fact]
    public void CreateActors_EquippedItems_PopulatedOnPlayer()
    {
        var build = MinimalBuild();
        build.Items.Add(new ItemData
        {
            Id = 1,
            RawText = @"Rarity: RARE
Test
Iron Ring
Implicits: 0"
        });
        build.ActiveItemSet = 1;
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Ring 1", ItemId = 1 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.NotNull(player.EquippedItems);
        Assert.Single(player.EquippedItems);
        Assert.True(player.EquippedItems.ContainsKey("Ring 1"));
    }

    [Fact]
    public void CreateActors_EquippedItems_CorrectMapping()
    {
        var build = MinimalBuild();
        build.Items.Add(new ItemData
        {
            Id = 5,
            RawText = @"Rarity: RARE
My Belt
Studded Belt
Implicits: 0
+50 to maximum Life"
        });
        build.ActiveItemSet = 1;
        build.ItemSets.Add(new ItemSetData
        {
            Id = 1,
            Slots = new List<SlotAssignment>
            {
                new SlotAssignment { SlotName = "Belt", ItemId = 5 },
            }
        });

        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Equal("My Belt", player.EquippedItems!["Belt"].Name);
        Assert.Equal(5, player.EquippedItems["Belt"].Id);
    }

    [Fact]
    public void CreateActors_Enemy_NoEquippedItems()
    {
        var build = MinimalBuild();
        var (_, enemy) = BuildPipeline.CreateActors(build);

        Assert.Null(enemy.EquippedItems);
    }

    // ─── OccVortex integration ───

    [Fact]
    public void CreateActors_OccVortex_ItemsEquipped()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return;

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.NotNull(player.EquippedItems);
        Assert.True(player.EquippedItems.Count > 0,
            "OccVortex should have equipped items");
    }

    [Fact]
    public void CreateActors_OccVortex_HasUniqueItem()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return;

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.CreateActors(build);

        // Heretic's Veil is unique (item 11, in Helmet slot)
        Assert.True(player.ModDB.Multipliers.GetValueOrDefault("UniqueItem") >= 1,
            "OccVortex has Heretic's Veil (unique helmet)");
    }

    [Fact]
    public void CreateActors_OccVortex_ItemResistancesApplied()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return;

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.CreateActors(build);

        // Multiple items add cold resistance
        double coldResist = player.ModDB.Sum(ModType.Base, null, "ColdResist");
        // -60 penalty + item resists should be > -60
        Assert.True(coldResist > -60,
            $"Cold resist ({coldResist}) should be > -60 (items add cold resist)");
    }

    [Fact]
    public void Calculate_OccVortex_LifeHigherWithItems()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return;

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.Calculate(build);

        // With items, life should be significantly higher than base
        // Baseline for level 99 Witch with no items: ~50 + 12*99+38 ≈ 1226 base life
        // Items add hundreds of flat life, so output should be well above 1226
        Assert.True(player.Output["Life"] > 1500,
            $"OccVortex Life ({player.Output["Life"]}) should be > 1500 with item +life mods");
    }

    [Fact]
    public void Calculate_OccVortex_StrFromItems()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return;

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.Calculate(build);

        // Witch base Str = 14, items add Str (amulet +43, ring +14, etc.)
        Assert.True(player.Output["Str"] > 14,
            $"OccVortex Str ({player.Output["Str"]}) should be > 14 (base Witch) with item +Str");
    }

    [Fact]
    public void Calculate_OccVortex_CorruptedItemTracked()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return;

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.CreateActors(build);

        // Item 1 (shield) is corrupted
        Assert.True(player.ModDB.Multipliers.GetValueOrDefault("CorruptedItem") >= 1,
            "OccVortex should have at least 1 corrupted item");
    }

    [Fact]
    public void CreateActors_OccVortex_AllEquipmentSlotsFilled()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return;

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.CreateActors(build);

        // OccVortex has 10 equipment slots filled
        var expectedSlots = new[] { "Weapon 1", "Weapon 2", "Helmet", "Body Armour",
            "Gloves", "Boots", "Amulet", "Ring 1", "Ring 2", "Belt" };

        foreach (var slot in expectedSlots)
        {
            Assert.True(player.EquippedItems!.ContainsKey(slot),
                $"OccVortex should have item in slot '{slot}'");
        }
    }

    [Fact]
    public void Calculate_OccVortex_MultipleRarityTypes()
    {
        var path = TestDataPath("OccVortex.xml");
        if (!File.Exists(path))
            return;

        var build = PathOfBuilding.Core.Import.BuildXmlLoader.Load(File.ReadAllText(path));
        var (player, _) = BuildPipeline.CreateActors(build);

        // OccVortex has: 1 unique (Heretic's Veil), 9 rare
        Assert.Equal(1, player.ModDB.Multipliers.GetValueOrDefault("UniqueItem"));
        Assert.Equal(9, player.ModDB.Multipliers.GetValueOrDefault("RareItem"));
    }
}
