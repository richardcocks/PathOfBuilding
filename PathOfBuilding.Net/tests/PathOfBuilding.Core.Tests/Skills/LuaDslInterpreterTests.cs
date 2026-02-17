using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Tests.Skills;

public class LuaDslInterpreterTests
{
    // --- Identifier resolution ---

    [Fact]
    public void Resolve_SkillType()
    {
        var ident = new DottedIdentifier("SkillType", "Spell");
        var result = LuaDslInterpreter.Resolve(ident);
        Assert.Equal((double)(int)SkillType.Spell, result);
    }

    [Fact]
    public void Resolve_ModFlag()
    {
        var ident = new DottedIdentifier("ModFlag", "Attack");
        var result = LuaDslInterpreter.Resolve(ident);
        Assert.Equal((double)(uint)ModFlag.Attack, result);
    }

    [Fact]
    public void Resolve_KeywordFlag()
    {
        var ident = new DottedIdentifier("KeywordFlag", "Hit");
        var result = LuaDslInterpreter.Resolve(ident);
        Assert.Equal((double)(uint)KeywordFlag.Hit, result);
    }

    [Fact]
    public void Resolve_UnknownNamespace_Preserved()
    {
        var ident = new DottedIdentifier("SomeOther", "Value");
        var result = LuaDslInterpreter.Resolve(ident);
        Assert.IsType<DottedIdentifier>(result);
    }

    // --- mod() interpretation ---

    [Fact]
    public void InterpretMod_Basic()
    {
        var args = new List<object?> { "Damage", "MORE", 20.0 };
        var mod = LuaDslInterpreter.InterpretMod(args);

        Assert.Equal("Damage", mod.Name);
        Assert.Equal(ModType.More, mod.Type);
        Assert.Equal(20.0, mod.Value.AsNumber());
        Assert.Equal(ModFlag.None, mod.Flags);
        Assert.Equal(KeywordFlag.None, mod.KeywordFlags);
        Assert.Empty(mod.Tags);
    }

    [Fact]
    public void InterpretMod_WithFlags()
    {
        var args = new List<object?>
        {
            "Damage", "MORE", 30.0,
            (double)(uint)ModFlag.Spell,
            (double)(uint)KeywordFlag.Hit,
        };
        var mod = LuaDslInterpreter.InterpretMod(args);

        Assert.Equal(ModFlag.Spell, mod.Flags);
        Assert.Equal(KeywordFlag.Hit, mod.KeywordFlags);
    }

    [Fact]
    public void InterpretMod_NilValue_BecomesZero()
    {
        var args = new List<object?> { "Damage", "MORE", null };
        var mod = LuaDslInterpreter.InterpretMod(args);
        Assert.Equal(0.0, mod.Value.AsNumber());
    }

    [Fact]
    public void InterpretMod_WithConditionTag()
    {
        var tagTable = new Dictionary<string, object?>
        {
            ["type"] = "Condition",
            ["var"] = "LowLife",
        };
        var args = new List<object?> { "Damage", "MORE", null, 0.0, 0.0, tagTable };
        var mod = LuaDslInterpreter.InterpretMod(args);

        Assert.Single(mod.Tags);
        var tag = Assert.IsType<ConditionTag>(mod.Tags[0]);
        Assert.Equal("LowLife", tag.Var);
    }

    [Fact]
    public void InterpretMod_WithMultipleTags()
    {
        var tag1 = new Dictionary<string, object?> { ["type"] = "Condition", ["var"] = "LowLife" };
        var tag2 = new Dictionary<string, object?> { ["type"] = "SkillType", ["skillType"] = (double)(int)SkillType.Spell };
        var args = new List<object?> { "Damage", "INC", 10.0, 0.0, 0.0, tag1, tag2 };
        var mod = LuaDslInterpreter.InterpretMod(args);

        Assert.Equal(2, mod.Tags.Count);
        Assert.IsType<ConditionTag>(mod.Tags[0]);
        Assert.IsType<SkillTypeTag>(mod.Tags[1]);
    }

    // --- skill() interpretation ---

    [Fact]
    public void InterpretSkill_Basic()
    {
        var args = new List<object?> { "ColdMin", null };
        var mod = LuaDslInterpreter.InterpretSkill(args);

        Assert.Equal("SkillData", mod.Name);
        Assert.Equal(ModType.List, mod.Type);
        var complex = Assert.IsType<Dictionary<string, object?>>(mod.Value.Complex);
        Assert.Equal("ColdMin", complex["key"]);
        Assert.Null(complex["value"]);
    }

    [Fact]
    public void InterpretSkill_WithValue()
    {
        var args = new List<object?> { "duration", 5000.0 };
        var mod = LuaDslInterpreter.InterpretSkill(args);

        var complex = Assert.IsType<Dictionary<string, object?>>(mod.Value.Complex);
        Assert.Equal("duration", complex["key"]);
        Assert.Equal(5000.0, complex["value"]);
    }

    [Fact]
    public void InterpretSkill_WithTag()
    {
        var tag = new Dictionary<string, object?> { ["type"] = "SkillType", ["skillType"] = (double)(int)SkillType.Aura };
        var args = new List<object?> { "manaReservationFlat", 0.0, tag };
        var mod = LuaDslInterpreter.InterpretSkill(args);

        Assert.Single(mod.Tags);
        var st = Assert.IsType<SkillTypeTag>(mod.Tags[0]);
        Assert.Equal(SkillType.Aura, st.SkillTypeValue);
    }

    // --- flag() interpretation ---

    [Fact]
    public void InterpretFlag_Basic()
    {
        var args = new List<object?> { "dotIsCorruptingBlood" };
        var mod = LuaDslInterpreter.InterpretFlag(args);

        Assert.Equal("dotIsCorruptingBlood", mod.Name);
        Assert.Equal(ModType.Flag, mod.Type);
        Assert.True(mod.Value.AsBoolean());
    }

    [Fact]
    public void InterpretFlag_WithTag()
    {
        var tag = new Dictionary<string, object?> { ["type"] = "GlobalEffect", ["effectType"] = "Buff" };
        var args = new List<object?> { "Condition:CanBeElusive", tag };
        var mod = LuaDslInterpreter.InterpretFlag(args);

        Assert.Equal("Condition:CanBeElusive", mod.Name);
        Assert.Single(mod.Tags);
        Assert.IsType<GlobalEffectTag>(mod.Tags[0]);
    }

    // --- bit.bor() ---

    [Fact]
    public void Resolve_BitBor()
    {
        var fc = new FunctionCall("bit.bor", new List<object?>
        {
            (double)(uint)KeywordFlag.Hit,
            (double)(uint)KeywordFlag.Ailment,
        });
        var result = LuaDslInterpreter.Resolve(fc);
        var expected = (double)((uint)KeywordFlag.Hit | (uint)KeywordFlag.Ailment);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_BitBor_WithModFlags()
    {
        var fc = new FunctionCall("bit.bor", new List<object?>
        {
            (double)(uint)ModFlag.Attack,
            (double)(uint)ModFlag.Melee,
        });
        var result = LuaDslInterpreter.Resolve(fc);
        var expected = (double)((uint)ModFlag.Attack | (uint)ModFlag.Melee);
        Assert.Equal(expected, result);
    }

    // --- Tag building ---

    [Fact]
    public void BuildTag_Condition()
    {
        var d = new Dictionary<string, object?> { ["type"] = "Condition", ["var"] = "LowLife" };
        var tag = LuaDslInterpreter.BuildTag(d);
        var ct = Assert.IsType<ConditionTag>(tag);
        Assert.Equal("LowLife", ct.Var);
    }

    [Fact]
    public void BuildTag_ActorCondition()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "ActorCondition",
            ["actor"] = "enemy",
            ["var"] = "LowLife",
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var ct = Assert.IsType<ActorConditionTag>(tag);
        Assert.Equal("LowLife", ct.Var);
        Assert.Equal("enemy", ct.Actor);
    }

    [Fact]
    public void BuildTag_PerStat()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "PerStat",
            ["stat"] = "ChainRemaining",
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var pt = Assert.IsType<PerStatTag>(tag);
        Assert.Equal("ChainRemaining", pt.Stat);
    }

    [Fact]
    public void BuildTag_GlobalEffect()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "GlobalEffect",
            ["effectType"] = "Buff",
            ["effectName"] = "Onslaught",
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var ge = Assert.IsType<GlobalEffectTag>(tag);
        Assert.Equal("Buff", ge.EffectType);
        Assert.Equal("Onslaught", ge.EffectName);
    }

    [Fact]
    public void BuildTag_GlobalEffect_WithCond()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "GlobalEffect",
            ["effectType"] = "Buff",
            ["effectName"] = "CombatRush",
            ["effectCond"] = "CombatRushActive",
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var ge = Assert.IsType<GlobalEffectTag>(tag);
        Assert.Equal("CombatRushActive", ge.EffectCond);
    }

    [Fact]
    public void BuildTag_SkillType()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "SkillType",
            ["skillType"] = (double)(int)SkillType.Aura,
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var st = Assert.IsType<SkillTypeTag>(tag);
        Assert.Equal(SkillType.Aura, st.SkillTypeValue);
    }

    [Fact]
    public void BuildTag_Multiplier()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "Multiplier",
            ["var"] = "PowerCharge",
            ["limit"] = 10.0,
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var mt = Assert.IsType<MultiplierTag>(tag);
        Assert.Equal("PowerCharge", mt.Var);
        Assert.Equal(10.0, mt.Limit);
    }

    [Fact]
    public void BuildTag_ModFlagOr()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "ModFlagOr",
            ["modFlags"] = (double)((uint)ModFlag.WeaponMelee | (uint)ModFlag.Unarmed),
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var mf = Assert.IsType<ModFlagOrTag>(tag);
        Assert.Equal(ModFlag.WeaponMelee | ModFlag.Unarmed, mf.ModFlags);
    }

    [Fact]
    public void BuildTag_ConditionWithNeg()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "Condition",
            ["var"] = "SomeCondition",
            ["neg"] = true,
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var ct = Assert.IsType<ConditionTag>(tag);
        Assert.True(ct.Neg);
    }

    [Fact]
    public void BuildTag_UnknownType_ReturnsNull()
    {
        var d = new Dictionary<string, object?> { ["type"] = "SomeUnknownTag" };
        var tag = LuaDslInterpreter.BuildTag(d);
        Assert.Null(tag);
    }

    // --- Full DSL round-trip ---

    [Fact]
    public void Resolve_FullModWithIdentifiers()
    {
        // Simulate: mod("Damage", "MORE", nil, ModFlag.Spell)
        var fc = new FunctionCall("mod", new List<object?>
        {
            "Damage",
            "MORE",
            null,
            new DottedIdentifier("ModFlag", "Spell"),
        });
        var result = LuaDslInterpreter.Resolve(fc);
        var mod = Assert.IsType<Mod>(result);
        Assert.Equal("Damage", mod.Name);
        Assert.Equal(ModType.More, mod.Type);
        Assert.Equal(ModFlag.Spell, mod.Flags);
    }

    [Fact]
    public void Resolve_BitBor_NestedInMod()
    {
        // Simulate: mod("Damage", "MORE", nil, 0, bit.bor(KeywordFlag.Hit, KeywordFlag.Ailment))
        var borCall = new FunctionCall("bit.bor", new List<object?>
        {
            new DottedIdentifier("KeywordFlag", "Hit"),
            new DottedIdentifier("KeywordFlag", "Ailment"),
        });
        var modCall = new FunctionCall("mod", new List<object?>
        {
            "Damage",
            "MORE",
            null,
            0.0,
            borCall,
        });
        var result = LuaDslInterpreter.Resolve(modCall);
        var mod = Assert.IsType<Mod>(result);
        Assert.Equal(KeywordFlag.Hit | KeywordFlag.Ailment, mod.KeywordFlags);
    }

    [Fact]
    public void Resolve_PercentStatTag()
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = "PercentStat",
            ["stat"] = "Life",
            ["percent"] = 1.0,
        };
        var tag = LuaDslInterpreter.BuildTag(d);
        var pt = Assert.IsType<PercentStatTag>(tag);
        Assert.Equal("Life", pt.Stat);
        Assert.Equal(1.0, pt.Percent);
    }
}
