using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Tests.Modifiers;

public class TagEvaluationTests
{
    // ─── Condition Tag ───

    [Fact]
    public void ConditionTag_Passes_WhenConditionTrue()
    {
        var db = new ModDB();
        db.Conditions["LowLife"] = true;

        var mod = ModHelper.CreateMod("Life", ModType.Inc, 30,
            tags: new ConditionTag { Var = "LowLife" });
        db.AddMod(mod);

        Assert.Equal(30, db.Sum(ModType.Inc, null, "Life"));
    }

    [Fact]
    public void ConditionTag_Fails_WhenConditionFalse()
    {
        var db = new ModDB();

        var mod = ModHelper.CreateMod("Life", ModType.Inc, 30,
            tags: new ConditionTag { Var = "LowLife" });
        db.AddMod(mod);

        Assert.Equal(0, db.Sum(ModType.Inc, null, "Life"));
    }

    [Fact]
    public void ConditionTag_Neg_InvertsResult()
    {
        var db = new ModDB();
        // Condition is NOT set, so Neg should make it pass
        var mod = ModHelper.CreateMod("Life", ModType.Inc, 30,
            tags: new ConditionTag { Var = "LowLife", Neg = true });
        db.AddMod(mod);

        Assert.Equal(30, db.Sum(ModType.Inc, null, "Life"));
    }

    [Fact]
    public void ConditionTag_VarList_MatchesAny()
    {
        var db = new ModDB();
        db.Conditions["Shocked"] = true;

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 20,
            tags: new ConditionTag { VarList = ["Frozen", "Shocked", "Ignited"] });
        db.AddMod(mod);

        Assert.Equal(20, db.Sum(ModType.Inc, null, "Damage"));
    }

    [Fact]
    public void ConditionTag_SkillCond()
    {
        var db = new ModDB();
        var cfg = new ModConfig
        {
            SkillCond = new Dictionary<string, bool> { ["CritRecently"] = true }
        };

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 40,
            tags: new ConditionTag { Var = "CritRecently" });
        db.AddMod(mod);

        Assert.Equal(40, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    // ─── Multiplier Tag ───

    [Fact]
    public void MultiplierTag_ScalesValue()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 3;

        var mod = ModHelper.CreateMod("CritChance", ModType.Base, 50,
            tags: new MultiplierTag { Var = "PowerCharge" });
        db.AddMod(mod);

        // 50 * 3 = 150
        Assert.Equal(150, db.Sum(ModType.Base, null, "CritChance"));
    }

    [Fact]
    public void MultiplierTag_WithDiv()
    {
        var db = new ModDB();
        var actor = new Actor();
        actor.Output["Str"] = 200;
        db.Actor = actor;
        db.Multipliers["Str"] = 200;

        var mod = ModHelper.CreateMod("Life", ModType.Base, 1,
            tags: new MultiplierTag { Var = "Str", Div = 10 });
        db.AddMod(mod);

        // 1 * floor(200 / 10) = 20
        Assert.Equal(20, db.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void MultiplierTag_WithLimit()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 10;

        var mod = ModHelper.CreateMod("CritChance", ModType.Base, 50,
            tags: new MultiplierTag { Var = "PowerCharge", Limit = 5 });
        db.AddMod(mod);

        // 50 * min(10, 5) = 250
        Assert.Equal(250, db.Sum(ModType.Base, null, "CritChance"));
    }

    [Fact]
    public void MultiplierTag_ZeroMultiplier_ZerosValue()
    {
        var db = new ModDB();
        // PowerCharge multiplier defaults to 0

        var mod = ModHelper.CreateMod("CritChance", ModType.Base, 50,
            tags: new MultiplierTag { Var = "PowerCharge" });
        db.AddMod(mod);

        Assert.Equal(0, db.Sum(ModType.Base, null, "CritChance"));
    }

    [Fact]
    public void MultiplierTag_WithBase()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 3;

        var mod = ModHelper.CreateMod("CritChance", ModType.Base, 10,
            tags: new MultiplierTag { Var = "PowerCharge", Base = 5 });
        db.AddMod(mod);

        // 10 * 3 + 5 = 35
        Assert.Equal(35, db.Sum(ModType.Base, null, "CritChance"));
    }

    // ─── MultiplierThreshold Tag ───

    [Fact]
    public void MultiplierThresholdTag_Passes_WhenAboveThreshold()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 5;

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 30,
            tags: new MultiplierThresholdTag { Var = "PowerCharge", Threshold = 3 });
        db.AddMod(mod);

        Assert.Equal(30, db.Sum(ModType.Inc, null, "Damage"));
    }

    [Fact]
    public void MultiplierThresholdTag_Fails_WhenBelowThreshold()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 2;

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 30,
            tags: new MultiplierThresholdTag { Var = "PowerCharge", Threshold = 3 });
        db.AddMod(mod);

        Assert.Equal(0, db.Sum(ModType.Inc, null, "Damage"));
    }

    [Fact]
    public void MultiplierThresholdTag_Upper_FailsWhenAbove()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 5;

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 30,
            tags: new MultiplierThresholdTag { Var = "PowerCharge", Threshold = 3, Upper = true });
        db.AddMod(mod);

        Assert.Equal(0, db.Sum(ModType.Inc, null, "Damage"));
    }

    // ─── PerStat Tag ───

    [Fact]
    public void PerStatTag_ScalesByStatValue()
    {
        var db = new ModDB();
        var actor = new Actor();
        actor.Output["Str"] = 100;
        db.Actor = actor;

        var mod = ModHelper.CreateMod("Life", ModType.Base, 1,
            tags: new PerStatTag { Stat = "Str", Div = 10 });
        db.AddMod(mod);

        // 1 * floor(100 / 10) = 10
        Assert.Equal(10, db.Sum(ModType.Base, null, "Life"));
    }

    // ─── StatThreshold Tag ───

    [Fact]
    public void StatThresholdTag_Passes_WhenAbove()
    {
        var db = new ModDB();
        var actor = new Actor();
        actor.Output["Str"] = 200;
        db.Actor = actor;

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 40,
            tags: new StatThresholdTag { Stat = "Str", Threshold = 150 });
        db.AddMod(mod);

        Assert.Equal(40, db.Sum(ModType.Inc, null, "Damage"));
    }

    [Fact]
    public void StatThresholdTag_Fails_WhenBelow()
    {
        var db = new ModDB();
        var actor = new Actor();
        actor.Output["Str"] = 100;
        db.Actor = actor;

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 40,
            tags: new StatThresholdTag { Stat = "Str", Threshold = 150 });
        db.AddMod(mod);

        Assert.Equal(0, db.Sum(ModType.Inc, null, "Damage"));
    }

    // ─── DistanceRamp Tag ───

    [Fact]
    public void DistanceRampTag_Interpolates()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 100,
            tags: new DistanceRampTag
            {
                Ramp = [(0, 1.0), (100, 0.0)]
            });
        db.AddMod(mod);

        var cfg = new ModConfig { SkillDist = 50 };
        // 100 * (1.0 + (0.0 - 1.0) * (50 - 0) / (100 - 0)) = 100 * 0.5 = 50
        Assert.Equal(50, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    [Fact]
    public void DistanceRampTag_Fails_WithoutSkillDist()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 100,
            tags: new DistanceRampTag
            {
                Ramp = [(0, 1.0), (100, 0.0)]
            });
        db.AddMod(mod);

        Assert.Equal(0, db.Sum(ModType.Inc, null, "Damage"));
    }

    // ─── MeleeProximity Tag ───

    [Fact]
    public void MeleeProximityTag_FullAtClose()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 60,
            tags: new MeleeProximityTag { Near = 1.0, Far = 0.0 });
        db.AddMod(mod);

        var cfg = new ModConfig { SkillDist = 10 };
        Assert.Equal(60, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    [Fact]
    public void MeleeProximityTag_ZeroAtFar()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 60,
            tags: new MeleeProximityTag { Near = 1.0, Far = 0.0 });
        db.AddMod(mod);

        var cfg = new ModConfig { SkillDist = 50 };
        Assert.Equal(0, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    // ─── SkillType Tag ───

    [Fact]
    public void SkillTypeTag_Passes_WhenTypePresent()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 20,
            tags: new SkillTypeTag { SkillTypeValue = SkillType.Attack });
        db.AddMod(mod);

        var cfg = new ModConfig
        {
            SkillTypes = new HashSet<SkillType> { SkillType.Attack, SkillType.Melee }
        };

        Assert.Equal(20, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    [Fact]
    public void SkillTypeTag_Fails_WhenTypeAbsent()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 20,
            tags: new SkillTypeTag { SkillTypeValue = SkillType.Spell });
        db.AddMod(mod);

        var cfg = new ModConfig
        {
            SkillTypes = new HashSet<SkillType> { SkillType.Attack }
        };

        Assert.Equal(0, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    // ─── SkillName Tag ───

    [Fact]
    public void SkillNameTag_CaseInsensitiveMatch()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 50,
            tags: new SkillNameTag { SkillName = "Fireball" });
        db.AddMod(mod);

        var cfg = new ModConfig { SkillName = "fireball" };
        Assert.Equal(50, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    [Fact]
    public void SkillNameTag_Fails_WhenDifferent()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 50,
            tags: new SkillNameTag { SkillName = "Fireball" });
        db.AddMod(mod);

        var cfg = new ModConfig { SkillName = "Frostbolt" };
        Assert.Equal(0, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    // ─── SkillPart Tag ───

    [Fact]
    public void SkillPartTag_MatchesPart()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 25,
            tags: new SkillPartTag { SkillPart = 2 });
        db.AddMod(mod);

        var cfg = new ModConfig { SkillPart = 2 };
        Assert.Equal(25, db.Sum(ModType.Inc, cfg, "Damage"));

        cfg.SkillPart = 1;
        Assert.Equal(0, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    // ─── SlotName Tag ───

    [Fact]
    public void SlotNameTag_MatchesSlot()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 15,
            tags: new SlotNameTag { SlotName = "Helmet" });
        db.AddMod(mod);

        var cfg = new ModConfig { SlotName = "Helmet" };
        Assert.Equal(15, db.Sum(ModType.Inc, cfg, "Damage"));

        cfg.SlotName = "Gloves";
        Assert.Equal(0, db.Sum(ModType.Inc, cfg, "Damage"));
    }

    // ─── Limit Tag ───

    [Fact]
    public void LimitTag_CapsValue()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 10;

        var mod = ModHelper.CreateMod("CritChance", ModType.Base, 50,
            tags: [
                new MultiplierTag { Var = "PowerCharge" },
                new LimitTag { Limit = 200 }
            ]);
        db.AddMod(mod);

        // 50 * 10 = 500 -> capped at 200
        Assert.Equal(200, db.Sum(ModType.Base, null, "CritChance"));
    }

    // ─── ModFlagOr Tag ───

    [Fact]
    public void ModFlagOrTag_MatchesAny()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 20,
            tags: new ModFlagOrTag { ModFlags = ModFlag.Axe | ModFlag.Sword });
        db.AddMod(mod);

        var axeCfg = new ModConfig { Flags = ModFlag.Attack | ModFlag.Axe };
        Assert.Equal(20, db.Sum(ModType.Inc, axeCfg, "Damage"));

        var bowCfg = new ModConfig { Flags = ModFlag.Attack | ModFlag.Bow };
        Assert.Equal(0, db.Sum(ModType.Inc, bowCfg, "Damage"));
    }

    // ─── KeywordFlagAnd Tag ───

    [Fact]
    public void KeywordFlagAndTag_RequiresAll()
    {
        var db = new ModDB();
        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 30,
            tags: new KeywordFlagAndTag { KeywordFlags = KeywordFlag.Fire | KeywordFlag.Spell });
        db.AddMod(mod);

        var bothCfg = new ModConfig { KeywordFlags = KeywordFlag.Fire | KeywordFlag.Spell };
        Assert.Equal(30, db.Sum(ModType.Inc, bothCfg, "Damage"));

        var oneCfg = new ModConfig { KeywordFlags = KeywordFlag.Fire };
        Assert.Equal(0, db.Sum(ModType.Inc, oneCfg, "Damage"));
    }

    // ─── Multiple Tags Combined ───

    [Fact]
    public void MultipleTags_AllMustPass()
    {
        var db = new ModDB();
        db.Conditions["LowLife"] = true;
        db.Multipliers["PowerCharge"] = 3;

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 10,
            tags: [
                new ConditionTag { Var = "LowLife" },
                new MultiplierTag { Var = "PowerCharge" }
            ]);
        db.AddMod(mod);

        // Condition passes, multiplier = 3, so 10 * 3 = 30
        Assert.Equal(30, db.Sum(ModType.Inc, null, "Damage"));
    }

    [Fact]
    public void MultipleTags_OneFailsGatesAll()
    {
        var db = new ModDB();
        // LowLife is NOT set
        db.Multipliers["PowerCharge"] = 3;

        var mod = ModHelper.CreateMod("Damage", ModType.Inc, 10,
            tags: [
                new ConditionTag { Var = "LowLife" },
                new MultiplierTag { Var = "PowerCharge" }
            ]);
        db.AddMod(mod);

        // Condition fails, so entire mod is gated
        Assert.Equal(0, db.Sum(ModType.Inc, null, "Damage"));
    }

    // ─── Global Limits ───

    [Fact]
    public void GlobalLimit_CapsAcrossMods()
    {
        var db = new ModDB();
        db.Multipliers["PowerCharge"] = 1;

        // Two mods with the same global limit key
        db.AddMod(ModHelper.CreateMod("CritChance", ModType.Base, 60,
            tags: new MultiplierTag { Var = "PowerCharge", GlobalLimit = 100, GlobalLimitKey = "CritCap" }));
        db.AddMod(ModHelper.CreateMod("CritChance", ModType.Base, 60,
            tags: new MultiplierTag { Var = "PowerCharge", GlobalLimit = 100, GlobalLimitKey = "CritCap" }));

        // First gives 60, second would give 60 but global limit is 100, so second gives 40
        Assert.Equal(100, db.Sum(ModType.Base, null, "CritChance"));
    }
}
