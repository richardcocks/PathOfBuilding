using PathOfBuilding.Core.Items;
using PathOfBuilding.Core.Import.Sections;

namespace PathOfBuilding.Core.Tests.Items;

public class ItemParserTests
{
    // ─── Rarity extraction ───

    [Fact]
    public void Parse_NormalRarity()
    {
        var item = ItemParser.Parse(1, @"
Rarity: NORMAL
Iron Ring
Implicits: 0
");
        Assert.Equal(ItemRarity.Normal, item.Rarity);
    }

    [Fact]
    public void Parse_MagicRarity()
    {
        var item = ItemParser.Parse(1, @"
Rarity: MAGIC
Perpetual Quicksilver Flask of Staunching
Implicits: 0
31% increased Charge Recovery
");
        Assert.Equal(ItemRarity.Magic, item.Rarity);
    }

    [Fact]
    public void Parse_RareRarity()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Loath Sanctuary
Vaal Spirit Shield
Implicits: 0
");
        Assert.Equal(ItemRarity.Rare, item.Rarity);
    }

    [Fact]
    public void Parse_UniqueRarity()
    {
        var item = ItemParser.Parse(1, @"
Rarity: UNIQUE
Heretic's Veil
Deicide Mask
Implicits: 0
");
        Assert.Equal(ItemRarity.Unique, item.Rarity);
    }

    [Fact]
    public void Parse_RelicRarity()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RELIC
Headhunter
Leather Belt
Implicits: 1
+40 to maximum Life
");
        Assert.Equal(ItemRarity.Relic, item.Rarity);
    }

    // ─── Name/BaseName extraction ───

    [Fact]
    public void Parse_RareItem_NameAndBaseName()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Loath Sanctuary
Vaal Spirit Shield
Implicits: 0
");
        Assert.Equal("Loath Sanctuary", item.Name);
        Assert.Equal("Vaal Spirit Shield", item.BaseName);
    }

    [Fact]
    public void Parse_UniqueItem_NameAndBaseName()
    {
        var item = ItemParser.Parse(1, @"
Rarity: UNIQUE
Heretic's Veil
Deicide Mask
Implicits: 0
");
        Assert.Equal("Heretic's Veil", item.Name);
        Assert.Equal("Deicide Mask", item.BaseName);
    }

    [Fact]
    public void Parse_MagicItem_NameEqualsBaseName()
    {
        var item = ItemParser.Parse(1, @"
Rarity: MAGIC
Perpetual Quicksilver Flask of Staunching
Implicits: 0
");
        Assert.Equal("Perpetual Quicksilver Flask of Staunching", item.Name);
        Assert.Equal("Perpetual Quicksilver Flask of Staunching", item.BaseName);
    }

    [Fact]
    public void Parse_NormalItem_NameEqualsBaseName()
    {
        var item = ItemParser.Parse(1, @"
Rarity: NORMAL
Iron Ring
Implicits: 0
");
        Assert.Equal("Iron Ring", item.Name);
        Assert.Equal("Iron Ring", item.BaseName);
    }

    // ─── Property extraction ───

    [Fact]
    public void Parse_ItemLevel()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Item Level: 71
Implicits: 0
");
        Assert.Equal(71, item.ItemLevel);
    }

    [Fact]
    public void Parse_Quality()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Quality: 20
Implicits: 0
");
        Assert.Equal(20, item.Quality);
    }

    [Fact]
    public void Parse_QualityZero()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Quality: 0
Implicits: 0
");
        Assert.Equal(0, item.Quality);
    }

    [Fact]
    public void Parse_Sockets()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Sockets: B-B-B
Implicits: 0
");
        Assert.Equal("B-B-B", item.Sockets);
    }

    [Fact]
    public void Parse_LevelReq()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
LevelReq: 70
Implicits: 0
");
        Assert.Equal(70, item.LevelReq);
    }

    [Fact]
    public void Parse_ImplicitCount_One()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 1
9% increased Spell Damage
+50 to maximum Life
");
        Assert.Equal(1, item.ImplicitCount);
    }

    [Fact]
    public void Parse_ImplicitCount_Two()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Citrine Amulet
Implicits: 2
{crafted}Allocates Prodigal Perfection
+16 to Strength and Dexterity
+43 to Strength
");
        Assert.Equal(2, item.ImplicitCount);
    }

    // ─── Mod line parsing ───

    [Fact]
    public void Parse_ImplicitsCountedCorrectly()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 1
9% increased Spell Damage
+50 to maximum Life
");
        var implicits = item.Implicits.ToList();
        Assert.Single(implicits);
        Assert.Equal("9% increased Spell Damage", implicits[0].RawText);
        Assert.Equal(ModLineCategory.Implicit, implicits[0].Category);
    }

    [Fact]
    public void Parse_ExplicitsFollowImplicits()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 1
9% increased Spell Damage
+50 to maximum Life
30% increased Energy Shield
");
        var explicits = item.Explicits.ToList();
        Assert.Equal(2, explicits.Count);
        Assert.Equal("+50 to maximum Life", explicits[0].RawText);
        Assert.Equal(ModLineCategory.Explicit, explicits[0].Category);
    }

    [Fact]
    public void Parse_CraftedPrefix_SetsCategory()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 0
+50 to maximum Life
{crafted}+19% to Cold Damage over Time Multiplier
");
        var modLines = item.ModLines;
        Assert.Equal(ModLineCategory.Explicit, modLines[0].Category);
        Assert.Equal(ModLineCategory.Crafted, modLines[1].Category);
        Assert.Equal("+19% to Cold Damage over Time Multiplier", modLines[1].RawText);
    }

    [Fact]
    public void Parse_FracturedPrefix_SetsCategory()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 0
{fractured}+50 to maximum Life
");
        Assert.Equal(ModLineCategory.Fractured, item.ModLines[0].Category);
        Assert.Equal("+50 to maximum Life", item.ModLines[0].RawText);
    }

    [Fact]
    public void Parse_EnchantPrefix_SetsCategory()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Deicide Mask
Implicits: 1
{enchant}24% increased Earthquake Area of Effect
+50 to maximum Life
");
        Assert.Equal(ModLineCategory.Enchant, item.ModLines[0].Category);
    }

    [Fact]
    public void Parse_ImplicitPrefix_InCraftedImplicit()
    {
        // {crafted} in implicit position → Crafted category (it's an enchant crafted implicit)
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Citrine Amulet
Implicits: 2
{crafted}Allocates Prodigal Perfection
+16 to Strength and Dexterity
+43 to Strength
");
        Assert.Equal(ModLineCategory.Crafted, item.ModLines[0].Category);
        // Second implicit by position
        Assert.Equal(ModLineCategory.Implicit, item.ModLines[1].Category);
        // Third line is explicit
        Assert.Equal(ModLineCategory.Explicit, item.ModLines[2].Category);
    }

    [Fact]
    public void Parse_ModPassedToModParser()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 0
+50 to maximum Life
");
        var modLine = item.ModLines[0];
        Assert.False(modLine.ParseFailed);
        Assert.NotEmpty(modLine.Mods);
        Assert.Equal("Life", modLine.Mods[0].Name);
    }

    [Fact]
    public void Parse_UnparseableMod_ParseFailed()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 0
This is total gibberish that cannot be parsed
");
        var modLine = item.ModLines[0];
        Assert.True(modLine.ParseFailed);
        Assert.Empty(modLine.Mods);
    }

    [Fact]
    public void Parse_ScourgePrefix_SetsCategory()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 0
{scourge}+50 to maximum Life
");
        Assert.Equal(ModLineCategory.Scourge, item.ModLines[0].Category);
    }

    [Fact]
    public void Parse_CruciblePrefix_SetsCategory()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 0
{crucible}+50 to maximum Life
");
        Assert.Equal(ModLineCategory.Crucible, item.ModLines[0].Category);
    }

    [Fact]
    public void Parse_CustomPrefix_StrippedNoCategory()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 0
{custom}+50 to maximum Life
");
        // {custom} should be stripped but default to Explicit (first mod after 0 implicits)
        Assert.Equal(ModLineCategory.Explicit, item.ModLines[0].Category);
        Assert.Equal("+50 to maximum Life", item.ModLines[0].RawText);
    }

    [Fact]
    public void Parse_Corrupted_DetectedAndSkipped()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Vaal Shield
Implicits: 0
+50 to maximum Life
Corrupted
");
        Assert.True(item.Corrupted);
        Assert.Single(item.ModLines); // "Corrupted" not treated as a mod line
    }

    // ─── Real item parsing (OccVortex) ───

    [Fact]
    public void Parse_OccVortex_Wand_Item3()
    {
        var item = ItemParser.Parse(3, @"Rarity: RARE
Maelstrom Cry
Imbued Wand
Unique ID: ced098a69068af80586640953702e39c51e8ab027e80b7b3a9097b446b93cc06
Item Level: 71
Quality: 0
Sockets: B-B-B
LevelReq: 70
Implicits: 1
37% increased Spell Damage
+9% to Damage over Time Multiplier
Adds 8 to 12 Fire Damage to Spells
5% increased Cast Speed
+1 to Level of all Cold Spell Skill Gems
+6 Life gained on Kill
{crafted}+19% to Cold Damage over Time Multiplier");

        Assert.Equal(ItemRarity.Rare, item.Rarity);
        Assert.Equal("Maelstrom Cry", item.Name);
        Assert.Equal("Imbued Wand", item.BaseName);
        Assert.Equal(71, item.ItemLevel);
        Assert.Equal(1, item.ImplicitCount);

        var implicits = item.Implicits.ToList();
        Assert.Single(implicits);
        Assert.Equal("37% increased Spell Damage", implicits[0].RawText);

        var explicits = item.Explicits.ToList();
        Assert.Equal(6, explicits.Count); // 5 explicit + 1 crafted
        Assert.Equal(ModLineCategory.Crafted, explicits[5].Category);
        Assert.Equal("+19% to Cold Damage over Time Multiplier", explicits[5].RawText);
    }

    [Fact]
    public void Parse_OccVortex_Amulet_Item2()
    {
        var item = ItemParser.Parse(2, @"Rarity: RARE
Apocalypse Noose
Citrine Amulet
Unique ID: f6eb3e5726a37508693267904e020215c25f9267e1fe4df45f2455e2220808c9
Item Level: 83
LevelReq: 59
Implicits: 2
{crafted}Allocates Prodigal Perfection
+16 to Strength and Dexterity
+43 to Strength
+44 to maximum Energy Shield
+9% to all Elemental Resistances
+11% to Fire Resistance
{crafted}+46 to maximum Life");

        Assert.Equal(2, item.ImplicitCount);
        // First implicit is crafted
        Assert.Equal(ModLineCategory.Crafted, item.ModLines[0].Category);
        // Second implicit by position count
        Assert.Equal(ModLineCategory.Implicit, item.ModLines[1].Category);
        // Remaining are explicit
        Assert.Equal(ModLineCategory.Explicit, item.ModLines[2].Category);
        // Last is crafted explicit
        Assert.Equal(ModLineCategory.Crafted, item.ModLines[^1].Category);
    }

    [Fact]
    public void Parse_OccVortex_BodyArmour_Item10()
    {
        var item = ItemParser.Parse(10, @"Rarity: RARE
Gale Ward
Quilted Jacket
Unique ID: dafc394c025c87f7df806c8c92c938c8125b923c3db138e39f82e8503e932a50
Item Level: 61
Quality: 20
Sockets: G-B-G-B-B-B
LevelReq: 72
Implicits: 0
32% reduced Attribute Requirements
+124 to maximum Life
+58 to maximum Mana
+33% to Cold Resistance
+40% to Lightning Resistance
Reflects 10 Physical Damage to Melee Attackers");

        Assert.Equal(0, item.ImplicitCount);
        Assert.Equal(6, item.ModLines.Count);
        Assert.All(item.ModLines, ml => Assert.Equal(ModLineCategory.Explicit, ml.Category));
    }

    [Fact]
    public void Parse_OccVortex_Flask_Item15()
    {
        var item = ItemParser.Parse(15, @"Rarity: MAGIC
Bubbling Eternal Life Flask of Acceleration
Unique ID: 2d887378e1162109008ca16d742df9828074f9901adfd8ccd69503c1aa36d391
Item Level: 65
Quality: 0
LevelReq: 65
Implicits: 0
50% reduced Amount Recovered
135% increased Recovery rate
50% of Recovery applied Instantly
11% increased Attack Speed during Flask effect");

        Assert.Equal(ItemRarity.Magic, item.Rarity);
        Assert.Equal(0, item.ImplicitCount);
        Assert.Equal(4, item.ModLines.Count);
    }

    [Fact]
    public void Parse_OccVortex_UniqueHelmet_Item11()
    {
        var item = ItemParser.Parse(11, @"Rarity: UNIQUE
Heretic's Veil
Deicide Mask
Unique ID: 9441019ae9c688a0c2be333c1c14d036ee380552ff7b1aa9302da663729aa481
Item Level: 70
Quality: 20
Sockets: R-B-G-B
LevelReq: 67
Implicits: 1
{crafted}24% increased Earthquake Area of Effect
+1 to Level of Socketed Curse Gems
Socketed Gems are Supported by Level 22 Blasphemy
Socketed Curse Gems have 12% reduced Mana Reservation
110% increased Evasion and Energy Shield
+44 to maximum Energy Shield");

        Assert.Equal(ItemRarity.Unique, item.Rarity);
        Assert.Equal("Heretic's Veil", item.Name);
        Assert.Equal("Deicide Mask", item.BaseName);
        Assert.Equal(1, item.ImplicitCount);
        Assert.Equal(ModLineCategory.Crafted, item.ModLines[0].Category);
    }

    [Fact]
    public void Parse_OccVortex_Shield_Item1_Corrupted()
    {
        var item = ItemParser.Parse(1, @"Rarity: RARE
Loath Sanctuary
Vaal Spirit Shield
Unique ID: fc24691eb70b5a54708e71b8142ebbbc86143ec938445222927b525295777c27
Item Level: 83
Quality: 0
Sockets: B-B-B
LevelReq: 70
Implicits: 1
9% increased Spell Damage
+54 to Intelligence
30% increased Energy Shield
+87 to maximum Life
+1 to Level of all Cold Spell Skill Gems
+11% to all Elemental Resistances
+43% to Cold Resistance
Corrupted");

        Assert.True(item.Corrupted);
        Assert.Equal(1, item.ImplicitCount);
        Assert.Single(item.Implicits);
        Assert.Equal(6, item.Explicits.Count());
    }

    // ─── Edge cases ───

    [Fact]
    public void Parse_EmptyRawText_DefaultItem()
    {
        var item = ItemParser.Parse(1, "");
        Assert.Equal(1, item.Id);
        Assert.Equal(ItemRarity.Normal, item.Rarity);
        Assert.Empty(item.ModLines);
    }

    [Fact]
    public void Parse_WhitespaceOnly_DefaultItem()
    {
        var item = ItemParser.Parse(1, "   \n  \n  ");
        Assert.Equal(ItemRarity.Normal, item.Rarity);
        Assert.Empty(item.ModLines);
    }

    [Fact]
    public void Parse_MissingRarity_NormalDefault()
    {
        var item = ItemParser.Parse(1, @"
Iron Ring
Implicits: 0
");
        Assert.Equal(ItemRarity.Normal, item.Rarity);
    }

    [Fact]
    public void Parse_NoModLines_EmptyModList()
    {
        var item = ItemParser.Parse(1, @"
Rarity: NORMAL
Iron Ring
Implicits: 0
");
        Assert.Empty(item.ModLines);
    }

    [Fact]
    public void Parse_ItemData_OverloadWorks()
    {
        var itemData = new ItemData
        {
            Id = 42,
            RawText = @"Rarity: RARE
Test Name
Test Base
Implicits: 0
+50 to maximum Life"
        };
        var item = ItemParser.Parse(itemData);
        Assert.Equal(42, item.Id);
        Assert.Equal("Test Name", item.Name);
        Assert.Single(item.ModLines);
    }

    [Fact]
    public void Parse_AllProperties_Together()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Test Base
Unique ID: abc123
Item Level: 83
Quality: 20
Sockets: R-G-B-B
LevelReq: 70
Implicits: 1
9% increased Spell Damage
+50 to maximum Life
");
        Assert.Equal(83, item.ItemLevel);
        Assert.Equal(20, item.Quality);
        Assert.Equal("R-G-B-B", item.Sockets);
        Assert.Equal(70, item.LevelReq);
        Assert.Equal(1, item.ImplicitCount);
        Assert.Equal(2, item.ModLines.Count);
    }

    [Fact]
    public void Parse_ZeroImplicits_AllExplicit()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Test Base
Implicits: 0
+50 to maximum Life
+30 to Strength
");
        Assert.Equal(0, item.ImplicitCount);
        Assert.All(item.ModLines, ml => Assert.Equal(ModLineCategory.Explicit, ml.Category));
    }

    [Fact]
    public void Parse_RangeTagStripped()
    {
        var item = ItemParser.Parse(1, @"
Rarity: RARE
Test Item
Test Base
Implicits: 0
{range:0.5}+50 to maximum Life
");
        // {range:0.5} stripped, line still parsed as explicit
        Assert.Equal(ModLineCategory.Explicit, item.ModLines[0].Category);
        Assert.Equal("+50 to maximum Life", item.ModLines[0].RawText);
    }

    [Fact]
    public void Parse_Jewel_NoSockets()
    {
        var item = ItemParser.Parse(17, @"Rarity: RARE
Cataclysm Stone
Cobalt Jewel
Unique ID: 949f96690b56595beceb512b5d9977d4fffd742690bc631defc0187caa6d8640
Item Level: 72
Implicits: 0
+4% to Cold Damage over Time Multiplier
7% increased maximum Life
3% reduced Mana Cost of Skills");

        Assert.Equal("Cataclysm Stone", item.Name);
        Assert.Equal("Cobalt Jewel", item.BaseName);
        Assert.Equal("", item.Sockets);
        Assert.Equal(3, item.ModLines.Count);
    }
}
