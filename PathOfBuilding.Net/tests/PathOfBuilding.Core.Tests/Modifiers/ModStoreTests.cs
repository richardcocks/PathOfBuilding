using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Modifiers;

public class ModStoreTests
{
    [Fact]
    public void GetCondition_LocalCondition()
    {
        var db = new ModDB();
        db.Conditions["LowLife"] = true;

        Assert.True(db.GetCondition("LowLife", null));
        Assert.False(db.GetCondition("FullLife", null));
    }

    [Fact]
    public void GetCondition_ParentCondition()
    {
        var parent = new ModDB();
        parent.Conditions["LowLife"] = true;

        var child = new ModDB(parent);

        Assert.True(child.GetCondition("LowLife", null));
    }

    [Fact]
    public void GetCondition_FromFlagMod()
    {
        var db = new ModDB();
        db.AddMod(ModHelper.CreateMod("Condition:LowLife", ModType.Flag, true));

        Assert.True(db.GetCondition("LowLife", null));
    }

    [Fact]
    public void GetMultiplier_LocalValue()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 3;

        Assert.Equal(3, db.GetMultiplier("PowerCharge", null));
    }

    [Fact]
    public void GetMultiplier_ParentAddsToLocal()
    {
        var parent = new ModDB();
        parent.Multipliers["PowerCharge"] = 2;

        var child = new ModDB(parent);
        child.Multipliers["PowerCharge"] = 1;

        Assert.Equal(3, child.GetMultiplier("PowerCharge", null));
    }

    [Fact]
    public void GetMultiplier_WithBaseModContribution()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 3;
        db.AddMod(ModHelper.CreateMod("Multiplier:PowerCharge", ModType.Base, 2));

        Assert.Equal(5, db.GetMultiplier("PowerCharge", null));
    }

    [Fact]
    public void GetMultiplier_OverrideTakesPrecedence()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 3;
        db.AddMod(ModHelper.CreateMod("Multiplier:PowerCharge", ModType.Override, 10.0));

        Assert.Equal(10, db.GetMultiplier("PowerCharge", null));
    }

    [Fact]
    public void GetStat_FromActorOutput()
    {
        var db = new ModDB();
        var actor = new Actor();
        actor.Output["Str"] = 200;
        db.Actor = actor;

        Assert.Equal(200, db.GetStat("Str", null));
    }

    [Fact]
    public void GetStat_FallbackToSkillStats()
    {
        var db = new ModDB();
        var cfg = new ModConfig
        {
            SkillStats = new Dictionary<string, double> { ["CritChance"] = 5.0 }
        };

        Assert.Equal(5.0, db.GetStat("CritChance", cfg));
    }

    [Fact]
    public void GetStat_ReturnsZeroWhenNotFound()
    {
        var db = new ModDB();
        Assert.Equal(0, db.GetStat("NonExistent", null));
    }

    [Fact]
    public void ScaleAddMod_ScalesValue()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Life", ModType.Base, 100);
        db.ScaleAddMod(mod, 0.5);

        Assert.Equal(50, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void ScaleAddMod_Scale1_NoClone()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Life", ModType.Base, 100);
        db.ScaleAddMod(mod, 1.0);

        Assert.Equal(100, db.Sum(ModType.Base, null, "Life"));
    }
}
