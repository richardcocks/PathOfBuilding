using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Tests.Modifiers;

public class ModDBTests
{
    [Fact]
    public void Sum_SingleBaseMod()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Sum_MultipleBaseMods()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));
        db.AddMod(ModHelper.CreateMod("Life", ModType.Base, 30));
        db.AddMod(ModHelper.CreateMod("Life", ModType.Base, 20));

        Assert.Equal(100, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Sum_MultipleStatNames()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Str", ModType.Base, 10));
        db.AddMod(ModHelper.CreateMod("Dex", ModType.Base, 20));
        db.AddMod(ModHelper.CreateMod("Int", ModType.Base, 30));

        Assert.Equal(60, db.Sum(ModType.Base, null, "Str", "Dex", "Int"));
    }

    [Fact]
    public void Sum_IgnoresDifferentTypes()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));
        db.AddMod(ModHelper.CreateMod("Life", ModType.Inc, 10));

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
        Assert.Equal(10, db.Sum(ModType.Inc, null, "Life"));
    }

    [Fact]
    public void Sum_IgnoresUnrelatedStats()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));
        db.AddMod(ModHelper.CreateMod("Mana", ModType.Base, 30));

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Sum_WithFlagFiltering()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Damage", ModType.Base, 10, flags: ModFlag.Attack));
        db.AddMod(ModHelper.CreateMod("Damage", ModType.Base, 20, flags: ModFlag.Spell));
        db.AddMod(ModHelper.CreateMod("Damage", ModType.Base, 5));

        var attackCfg = new ModConfig { Flags = ModFlag.Attack };
        var spellCfg = new ModConfig { Flags = ModFlag.Spell };

        // Attack config sees attack mod + unflagged mod
        Assert.Equal(15, db.Sum(ModType.Base, attackCfg, "Damage"));
        // Spell config sees spell mod + unflagged mod
        Assert.Equal(25, db.Sum(ModType.Base, spellCfg, "Damage"));
    }

    [Fact]
    public void Sum_WithKeywordFiltering()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Damage", ModType.Inc, 10, keywordFlags: KeywordFlag.Fire));
        db.AddMod(ModHelper.CreateMod("Damage", ModType.Inc, 20, keywordFlags: KeywordFlag.Cold));

        var fireCfg = new ModConfig { KeywordFlags = KeywordFlag.Fire };
        Assert.Equal(10, db.Sum(ModType.Inc, fireCfg, "Damage"));
    }

    [Fact]
    public void More_SingleMod()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Damage", ModType.More, 50));

        Assert.Equal(1.5, db.More(null, "Damage"));
    }

    [Fact]
    public void More_MultipleMods_Multiplicative()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Damage", ModType.More, 50));
        db.AddMod(ModHelper.CreateMod("Damage", ModType.More, 20));

        // (1 + 50/100) * (1 + 20/100) = 1.5 * 1.2 = 1.8
        Assert.Equal(1.8, db.More(null, "Damage"), 4);
    }

    [Fact]
    public void More_NoMods_ReturnsOne()
    {
        var db = new ModDB();
        Assert.Equal(1.0, db.More(null, "Damage"));
    }

    [Fact]
    public void Flag_ReturnsTrue_WhenPresent()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Condition:LowLife", ModType.Flag, true));

        Assert.True(db.Flag(null, "Condition:LowLife"));
    }

    [Fact]
    public void Flag_ReturnsFalse_WhenAbsent()
    {
        var db = new ModDB();
        Assert.False(db.Flag(null, "Condition:LowLife"));
    }

    [Fact]
    public void Override_ReturnsFirstMatch()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("MaxResist", ModType.Override, 78.0));
        db.AddMod(ModHelper.CreateMod("MaxResist", ModType.Override, 80.0));

        var result = db.Override(null, "MaxResist");
        Assert.NotNull(result);
        Assert.Equal(78.0, result.Value.AsNumber());
    }

    [Fact]
    public void Override_ReturnsNull_WhenAbsent()
    {
        var db = new ModDB();
        Assert.Null(db.Override(null, "MaxResist"));
    }

    [Fact]
    public void List_CollectsAllValues()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Keystone", ModType.List, ModValue.FromComplex("IronReflexes")));
        db.AddMod(ModHelper.CreateMod("Keystone", ModType.List, ModValue.FromComplex("AvatarOfFire")));

        var result = db.List(null, "Keystone");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParentChaining_Sum()
    {
        var parent = new ModDB();
        parent.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));

        var child = new ModDB(parent);
        child.AddMod(ModHelper.CreateMod("Life", ModType.Base, 30));

        Assert.Equal(80, child.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void ParentChaining_More()
    {
        var parent = new ModDB();
        parent.AddMod(ModHelper.CreateMod("Damage", ModType.More, 50));

        var child = new ModDB(parent);
        child.AddMod(ModHelper.CreateMod("Damage", ModType.More, 20));

        // child: 1.2 * parent: 1.5 = 1.8
        Assert.Equal(1.8, child.More(null, "Damage"), 4);
    }

    [Fact]
    public void ParentChaining_Flag()
    {
        var parent = new ModDB();
        parent.AddMod(ModHelper.CreateMod("Condition:LowLife", ModType.Flag, true));

        var child = new ModDB(parent);

        Assert.True(child.Flag(null, "Condition:LowLife"));
    }

    [Fact]
    public void HasMod_ReturnsTrueForExisting()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));

        Assert.True(db.HasMod(ModType.Base, null, "Life"));
        Assert.False(db.HasMod(ModType.Inc, null, "Life"));
        Assert.False(db.HasMod(ModType.Base, null, "Mana"));
    }

    [Fact]
    public void AddDB_MergesAll()
    {
        var db1 = new ModDB();
        db1.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50));

        var db2 = new ModDB();
        db2.AddMod(ModHelper.CreateMod("Life", ModType.Base, 30));
        db2.AddMod(ModHelper.CreateMod("Mana", ModType.Base, 40));

        db1.AddDB(db2);

        Assert.Equal(80, db1.Sum(ModType.Base, null, "Life"));
        Assert.Equal(40, db1.Sum(ModType.Base, null, "Mana"));
    }

    [Fact]
    public void ReplaceMod_ReplacesExisting()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Life", ModType.Base, 50, source: "Item:Ring1"));

        db.ReplaceMod("Life", ModType.Base, 100, source: "Item:Ring1");

        Assert.Equal(100, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void ReplaceMod_AddsIfNotFound()
    {
        var db = new ModDB();
        db.ReplaceMod("Life", ModType.Base, 100, source: "Item:Ring1");

        Assert.Equal(100, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void Tabulate_ReturnsModsWithValues()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Damage", ModType.Inc, 10));
        db.AddMod(ModHelper.CreateMod("Damage", ModType.Inc, 20));

        var result = db.Tabulate(ModType.Inc, null, "Damage");
        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].Value.AsNumber());
        Assert.Equal(20, result[1].Value.AsNumber());
    }

    [Fact]
    public void Combine_RoutesToCorrectMethod()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Damage", ModType.More, 50));
        db.AddMod(ModHelper.CreateMod("Damage", ModType.Base, 100));
        db.AddMod(ModHelper.CreateMod("Condition:LowLife", ModType.Flag, true));

        Assert.Equal(1.5, (double)db.Combine(ModType.More, null, "Damage")!);
        Assert.Equal(100.0, (double)db.Combine(ModType.Base, null, "Damage")!);
        Assert.True((bool)db.Combine(ModType.Flag, null, "Condition:LowLife")!);
    }
}
