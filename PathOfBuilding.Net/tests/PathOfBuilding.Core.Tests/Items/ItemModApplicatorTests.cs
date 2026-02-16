using PathOfBuilding.Core.Items;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Modifiers.Parsing;

namespace PathOfBuilding.Core.Tests.Items;

public class ItemModApplicatorTests
{
    private static ModDB MakeDB()
    {
        var db = new ModDB();
        db.Actor = new Actor();
        db.Actor.ModDB = db;
        return db;
    }

    private static ParsedItem MakeItemWithMods(int id, ItemRarity rarity, params string[] modTexts)
    {
        var modLines = new List<ParsedModLine>();
        foreach (var text in modTexts)
        {
            var mods = ModParser.ParseMod(text);
            modLines.Add(new ParsedModLine
            {
                RawText = text,
                Category = ModLineCategory.Explicit,
                Mods = mods ?? new(),
                ParseFailed = mods == null,
            });
        }
        return new ParsedItem
        {
            Id = id,
            Rarity = rarity,
            Name = $"Item{id}",
            ModLines = modLines,
        };
    }

    // ─── Basic mod application ───

    [Fact]
    public void Apply_SingleItem_OneMod_AddedToModDB()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare, "+50 to maximum Life");
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_SingleItem_MultipleMods_AllAdded()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare,
            "+50 to maximum Life",
            "+30 to Strength");
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
        Assert.Equal(30, db.Sum(ModType.Base, null, "Str"));
    }

    [Fact]
    public void Apply_ImplicitAndExplicit_BothApplied()
    {
        var modLines = new List<ParsedModLine>
        {
            new ParsedModLine
            {
                RawText = "+20 to Strength",
                Category = ModLineCategory.Implicit,
                Mods = ModParser.ParseMod("+20 to Strength") ?? new(),
            },
            new ParsedModLine
            {
                RawText = "+50 to maximum Life",
                Category = ModLineCategory.Explicit,
                Mods = ModParser.ParseMod("+50 to maximum Life") ?? new(),
            },
        };
        var item = new ParsedItem { Id = 1, Rarity = ItemRarity.Rare, ModLines = modLines };
        var db = MakeDB();
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(20, db.Sum(ModType.Base, null, "Str"));
        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_CraftedMods_Applied()
    {
        var modLines = new List<ParsedModLine>
        {
            new ParsedModLine
            {
                RawText = "+46 to maximum Life",
                Category = ModLineCategory.Crafted,
                Mods = ModParser.ParseMod("+46 to maximum Life") ?? new(),
            },
        };
        var item = new ParsedItem { Id = 1, Rarity = ItemRarity.Rare, ModLines = modLines };
        var db = MakeDB();
        var equipped = new Dictionary<string, ParsedItem> { ["Amulet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(46, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_FracturedMods_Applied()
    {
        var modLines = new List<ParsedModLine>
        {
            new ParsedModLine
            {
                RawText = "+50 to maximum Life",
                Category = ModLineCategory.Fractured,
                Mods = ModParser.ParseMod("+50 to maximum Life") ?? new(),
            },
        };
        var item = new ParsedItem { Id = 1, Rarity = ItemRarity.Rare, ModLines = modLines };
        var db = MakeDB();
        var equipped = new Dictionary<string, ParsedItem> { ["Belt"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_SourceSetCorrectly()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare, "+50 to maximum Life");
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        var tabulated = db.Tabulate(ModType.Base, null, "Life");
        Assert.Contains(tabulated, t => t.Mod.Source == "Item:Helmet");
    }

    // ─── Multiple items ───

    [Fact]
    public void Apply_TwoItems_DifferentSlots_BothApplied()
    {
        var db = MakeDB();
        var item1 = MakeItemWithMods(1, ItemRarity.Rare, "+50 to maximum Life");
        var item2 = MakeItemWithMods(2, ItemRarity.Rare, "+30 to maximum Life");
        var equipped = new Dictionary<string, ParsedItem>
        {
            ["Helmet"] = item1,
            ["Gloves"] = item2,
        };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(80, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_SameStatFromDifferentItems_Sums()
    {
        var db = MakeDB();
        var item1 = MakeItemWithMods(1, ItemRarity.Rare, "+100 to maximum Life");
        var item2 = MakeItemWithMods(2, ItemRarity.Rare, "+50 to maximum Life");
        var equipped = new Dictionary<string, ParsedItem>
        {
            ["Body Armour"] = item1,
            ["Belt"] = item2,
        };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(150, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_IncModsFromItems_StackWithBase()
    {
        var db = MakeDB();
        // Add a base mod first
        db.NewMod("Life", ModType.Base, 100);

        var item = MakeItemWithMods(1, ItemRarity.Rare, "30% increased maximum Life");
        var equipped = new Dictionary<string, ParsedItem> { ["Belt"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(100, db.Sum(ModType.Base, null, "Life"));
        Assert.Equal(30, db.Sum(ModType.Inc, null, "Life"));
    }

    // ─── Multiplier tracking ───

    [Fact]
    public void Apply_UniqueItem_IncrementsUniqueMultiplier()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Unique);
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["UniqueItem"]);
    }

    [Fact]
    public void Apply_RareItem_IncrementsRareMultiplier()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare);
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["RareItem"]);
    }

    [Fact]
    public void Apply_MagicItem_IncrementsMagicMultiplier()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Magic);
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["MagicItem"]);
    }

    [Fact]
    public void Apply_TwoUniqueItems_MultiplierIsTwo()
    {
        var db = MakeDB();
        var item1 = MakeItemWithMods(1, ItemRarity.Unique);
        var item2 = MakeItemWithMods(2, ItemRarity.Unique);
        var equipped = new Dictionary<string, ParsedItem>
        {
            ["Helmet"] = item1,
            ["Gloves"] = item2,
        };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(2, db.Multipliers["UniqueItem"]);
    }

    [Fact]
    public void Apply_HelmetItem_SlotTypeKey()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare);
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["HelmetItem"]);
    }

    [Fact]
    public void Apply_BodyArmourItem_SlotTypeKey()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare);
        var equipped = new Dictionary<string, ParsedItem> { ["Body Armour"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["BodyArmourItem"]);
    }

    [Fact]
    public void Apply_WeaponItem_SlotTypeKey()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare);
        var equipped = new Dictionary<string, ParsedItem> { ["Weapon 1"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["WeaponItem"]);
    }

    [Fact]
    public void Apply_Ring1AndRing2_BothIncrementRingItem()
    {
        var db = MakeDB();
        var item1 = MakeItemWithMods(1, ItemRarity.Rare);
        var item2 = MakeItemWithMods(2, ItemRarity.Rare);
        var equipped = new Dictionary<string, ParsedItem>
        {
            ["Ring 1"] = item1,
            ["Ring 2"] = item2,
        };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(2, db.Multipliers["RingItem"]);
    }

    [Fact]
    public void Apply_BeltItem_SlotTypeKey()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare);
        var equipped = new Dictionary<string, ParsedItem> { ["Belt"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["BeltItem"]);
    }

    [Fact]
    public void Apply_CorruptedItem_IncrementsCorruptedMultiplier()
    {
        var db = MakeDB();
        var item = new ParsedItem { Id = 1, Rarity = ItemRarity.Rare, Corrupted = true };
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["CorruptedItem"]);
    }

    [Fact]
    public void Apply_RelicItem_CountsAsUnique()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Relic);
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["UniqueItem"]);
    }

    // ─── Edge cases ───

    [Fact]
    public void Apply_EmptyEquipped_NoChanges()
    {
        var db = MakeDB();
        ItemModApplicator.Apply(new Dictionary<string, ParsedItem>(), db);

        Assert.Equal(0, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_ItemWithNoParseableMods_MultipliersStillSet()
    {
        var db = MakeDB();
        var item = new ParsedItem { Id = 1, Rarity = ItemRarity.Rare, ModLines = new List<ParsedModLine>() };
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(1, db.Multipliers["RareItem"]);
        Assert.Equal(1, db.Multipliers["HelmetItem"]);
    }

    [Fact]
    public void Apply_ParseFailedLines_Skipped()
    {
        var db = MakeDB();
        var modLines = new List<ParsedModLine>
        {
            new ParsedModLine
            {
                RawText = "total gibberish",
                Category = ModLineCategory.Explicit,
                Mods = new(),
                ParseFailed = true,
            },
            new ParsedModLine
            {
                RawText = "+50 to maximum Life",
                Category = ModLineCategory.Explicit,
                Mods = ModParser.ParseMod("+50 to maximum Life") ?? new(),
                ParseFailed = false,
            },
        };
        var item = new ParsedItem { Id = 1, Rarity = ItemRarity.Rare, ModLines = modLines };
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
    }

    // ─── ModParser integration ───

    [Fact]
    public void Apply_PlusLifeMod_ParsedCorrectly()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare, "+50 to maximum Life");
        var equipped = new Dictionary<string, ParsedItem> { ["Helmet"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Apply_IncreasedAttackSpeed_ParsedCorrectly()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare, "10% increased Attack Speed");
        var equipped = new Dictionary<string, ParsedItem> { ["Gloves"] = item };

        ItemModApplicator.Apply(equipped, db);

        var cfg = new ModConfig { Flags = ModFlag.Attack };
        Assert.Equal(10, db.Sum(ModType.Inc, cfg, "Speed"));
    }

    [Fact]
    public void Apply_ColdResistance_ParsedCorrectly()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare, "+43% to Cold Resistance");
        var equipped = new Dictionary<string, ParsedItem> { ["Weapon 2"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(43, db.Sum(ModType.Base, null, "ColdResist"));
    }

    [Fact]
    public void Apply_AllElementalResistances_ParsedCorrectly()
    {
        var db = MakeDB();
        var item = MakeItemWithMods(1, ItemRarity.Rare, "+11% to all Elemental Resistances");
        var equipped = new Dictionary<string, ParsedItem> { ["Belt"] = item };

        ItemModApplicator.Apply(equipped, db);

        Assert.Equal(11, db.Sum(ModType.Base, null, "ElementalResist"));
    }
}
