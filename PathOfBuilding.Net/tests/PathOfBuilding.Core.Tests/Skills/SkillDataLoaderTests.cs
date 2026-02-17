using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Tests.Skills;

public class SkillDataLoaderTests
{
    // --- Synthetic tests ---

    [Fact]
    public void LoadFromText_SimpleSkill()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["TestArc"] = {
                name = "Arc",
                color = 3,
                baseEffectiveness = 1.5849,
                incrementalEffectiveness = 0.0395,
                description = "An arc of lightning",
                castTime = 0.7,
                baseFlags = { spell = true, chaining = true },
                stats = { "spell_minimum_base_lightning_damage", "spell_maximum_base_lightning_damage" },
                levels = {
                    [1] = { 0.3, 1.7, 4, critChance = 6, damageEffectiveness = 1.2, levelRequirement = 12, statInterpolation = { 3, 3, 1 }, cost = { Mana = 8 } },
                },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        Assert.Single(skills);

        var skill = skills["TestArc"];
        Assert.Equal("Arc", skill.Name);
        Assert.Equal(3, skill.Color);
        Assert.Equal(1.5849, skill.BaseEffectiveness, 4);
        Assert.Equal(0.7, skill.CastTime);
        Assert.False(skill.Support);
        Assert.True(skill.BaseFlags.ContainsKey("spell"));
        Assert.True(skill.BaseFlags.ContainsKey("chaining"));
        Assert.Equal(2, skill.Stats.Count);
        Assert.Equal("spell_minimum_base_lightning_damage", skill.Stats[0]);
    }

    [Fact]
    public void LoadFromText_LevelData()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["X"] = {
                name = "X",
                levels = {
                    [1] = { 0.3, 1.7, 4, critChance = 6, damageEffectiveness = 1.2, levelRequirement = 12, statInterpolation = { 3, 3, 1 }, cost = { Mana = 8 } },
                    [20] = { 0.3, 1.7, 7, critChance = 6, damageEffectiveness = 1.2, levelRequirement = 70, statInterpolation = { 3, 3, 1 }, cost = { Mana = 23 } },
                },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var skill = skills["X"];

        Assert.Equal(2, skill.Levels.Count);

        var lvl1 = skill.Levels[1];
        Assert.Equal(1, lvl1.Level);
        Assert.Equal(3, lvl1.Values.Count);
        Assert.Equal(0.3, lvl1.Values[0], 4);
        Assert.Equal(1.7, lvl1.Values[1], 4);
        Assert.Equal(4.0, lvl1.Values[2]);
        Assert.Equal(6.0, lvl1.CritChance);
        Assert.Equal(1.2, lvl1.DamageEffectiveness);
        Assert.Equal(12, lvl1.LevelRequirement);
        Assert.Equal(3, lvl1.StatInterpolation.Count);
        Assert.Equal(8, lvl1.Cost["Mana"]);

        var lvl20 = skill.Levels[20];
        Assert.Equal(70, lvl20.LevelRequirement);
        Assert.Equal(23, lvl20.Cost["Mana"]);
    }

    [Fact]
    public void LoadFromText_SupportSkill()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["SupportTest"] = {
                name = "Test Support",
                support = true,
                requireSkillTypes = { SkillType.Damage, SkillType.Attack },
                addSkillTypes = { },
                excludeSkillTypes = { },
                statMap = {
                    ["test_stat_+%_final"] = {
                        mod("Damage", "MORE", nil, ModFlag.Spell),
                    },
                },
                levels = {
                    [1] = { 25, levelRequirement = 18, manaMultiplier = 40, statInterpolation = { 1 } },
                },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var skill = skills["SupportTest"];

        Assert.True(skill.Support);
        Assert.Equal(2, skill.RequireSkillTypes.Count);
        Assert.Contains(SkillType.Damage, skill.RequireSkillTypes);
        Assert.Contains(SkillType.Attack, skill.RequireSkillTypes);
        Assert.Empty(skill.AddSkillTypes);
        Assert.Empty(skill.ExcludeSkillTypes);

        Assert.Single(skill.StatMap);
        var entry = skill.StatMap["test_stat_+%_final"];
        Assert.Single(entry.Mods);
        Assert.Equal("Damage", entry.Mods[0].Name);
        Assert.Equal(ModType.More, entry.Mods[0].Type);
        Assert.Equal(ModFlag.Spell, entry.Mods[0].Flags);

        Assert.Equal(40.0, skill.Levels[1].ManaMultiplier);
    }

    [Fact]
    public void LoadFromText_SkillWithParts()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["TestParts"] = {
                name = "Multi-Part",
                parts = {
                    { name = "Projectile", projectile = true },
                    { name = "Explosion", area = true },
                },
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var skill = skills["TestParts"];

        Assert.Equal(2, skill.Parts.Count);
        Assert.Equal("Projectile", skill.Parts[0].Name);
        Assert.True(skill.Parts[0].Flags["projectile"]);
        Assert.Equal("Explosion", skill.Parts[1].Name);
        Assert.True(skill.Parts[1].Flags["area"]);
    }

    [Fact]
    public void LoadFromText_SkillTypes()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["TestTypes"] = {
                name = "Typed",
                skillTypes = { [SkillType.Spell] = true, [SkillType.Damage] = true },
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var skill = skills["TestTypes"];

        Assert.True(skill.SkillTypes.ContainsKey(SkillType.Spell));
        Assert.True(skill.SkillTypes.ContainsKey(SkillType.Damage));
    }

    [Fact]
    public void LoadFromText_BaseMods()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["TestBaseMods"] = {
                name = "WithBaseMods",
                baseMods = {
                    skill("debuff", true),
                    flag("dotIsCorruptingBlood"),
                    mod("Multiplier:CorruptingCryMaxStages", "BASE", 10),
                },
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var skill = skills["TestBaseMods"];

        Assert.Equal(3, skill.BaseMods.Count);
        Assert.Equal("SkillData", skill.BaseMods[0].Name); // skill() → SkillData
        Assert.Equal("dotIsCorruptingBlood", skill.BaseMods[1].Name); // flag()
        Assert.Equal(ModType.Flag, skill.BaseMods[1].Type);
        Assert.Equal("Multiplier:CorruptingCryMaxStages", skill.BaseMods[2].Name);
        Assert.Equal(10.0, skill.BaseMods[2].Value.AsNumber());
    }

    [Fact]
    public void LoadFromText_ConstantStats()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["X"] = {
                name = "X",
                constantStats = {
                    { "stat1", 15 },
                    { "stat2", 35 },
                },
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var skill = skills["X"];

        Assert.Equal(2, skill.ConstantStats.Count);
        Assert.Equal(("stat1", 15.0), skill.ConstantStats[0]);
        Assert.Equal(("stat2", 35.0), skill.ConstantStats[1]);
    }

    [Fact]
    public void LoadFromText_QualityStats()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["X"] = {
                name = "X",
                qualityStats = {
                    Default = {
                        { "number_of_chains", 0.05 },
                    },
                },
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var skill = skills["X"];

        Assert.Single(skill.QualityStats);
        Assert.True(skill.QualityStats.ContainsKey("Default"));
        var pairs = skill.QualityStats["Default"];
        Assert.Single(pairs);
        Assert.Equal("number_of_chains", pairs[0].stat);
        Assert.Equal(0.05, pairs[0].value);
    }

    [Fact]
    public void LoadFromText_PlusVersionOf()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["TestPlus"] = {
                name = "Awakened Test",
                support = true,
                plusVersionOf = "SupportTest",
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        Assert.Equal("SupportTest", skills["TestPlus"].PlusVersionOf);
    }

    [Fact]
    public void LoadFromText_FunctionLiteralSkipped()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["X"] = {
                name = "WithFunc",
                preDamageFunc = function(activeSkill, output)
                    if true then
                        local x = 1
                    end
                end,
                castTime = 0.8,
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var skill = skills["X"];
        Assert.True(skill.HasPreDamageFunc);
        Assert.Equal(0.8, skill.CastTime);
    }

    [Fact]
    public void LoadFromText_StatMapWithDiv()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["X"] = {
                name = "X",
                statMap = {
                    ["test_stat"] = {
                        skill("duration", nil),
                        div = 1000,
                    },
                },
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var entry = skills["X"].StatMap["test_stat"];
        Assert.Single(entry.Mods);
        Assert.Equal(1000.0, entry.Div);
    }

    [Fact]
    public void LoadFromText_BitBorInStatMap()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["X"] = {
                name = "X",
                statMap = {
                    ["damage_stat"] = {
                        mod("Damage", "MORE", nil, 0, bit.bor(KeywordFlag.Hit, KeywordFlag.Ailment), { type = "PerStat", stat = "ChainRemaining" }),
                    },
                },
                levels = { [1] = { levelRequirement = 1, statInterpolation = { } } },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var entry = skills["X"].StatMap["damage_stat"];
        Assert.Single(entry.Mods);
        var mod = entry.Mods[0];
        Assert.Equal(KeywordFlag.Hit | KeywordFlag.Ailment, mod.KeywordFlags);
        Assert.Single(mod.Tags);
        Assert.IsType<PerStatTag>(mod.Tags[0]);
    }

    [Fact]
    public void LoadFromText_LevelWithCooldownAndStoredUses()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["X"] = {
                name = "X",
                levels = {
                    [1] = { 45, 10, cooldown = 4, levelRequirement = 16, storedUses = 1, statInterpolation = { 1, 1 } },
                },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var lvl = skills["X"].Levels[1];
        Assert.Equal(4.0, lvl.Cooldown);
        Assert.Equal(1, lvl.StoredUses);
    }

    [Fact]
    public void LoadFromText_LevelWithBaseMultiplier()
    {
        var lua = """
            local skills, mod, flag, skill = ...
            skills["X"] = {
                name = "X",
                levels = {
                    [1] = { 0, 0, baseMultiplier = 2.307, cooldown = 1, damageEffectiveness = 2.307, levelRequirement = 18, storedUses = 1, statInterpolation = { 1, 1 } },
                },
            }
        """;

        var skills = SkillDataLoader.LoadFromText(lua);
        var lvl = skills["X"].Levels[1];
        Assert.Equal(2.307, lvl.BaseMultiplier);
    }

    // --- Real file integration tests ---

    [Fact]
    public void LoadFromFile_RealActInt_ParsesAllSkills()
    {
        var path = TestDataHelper.GetSkillFile("act_int.lua");
        if (!File.Exists(path))
            return;

        var skills = SkillDataLoader.LoadFromFile(path);
        Assert.True(skills.Count > 50, $"Expected > 50 skills, got {skills.Count}");
    }

    [Fact]
    public void LoadFromFile_RealActInt_ArcFields()
    {
        var path = TestDataHelper.GetSkillFile("act_int.lua");
        if (!File.Exists(path))
            return;

        var skills = SkillDataLoader.LoadFromFile(path);
        var arc = skills["Arc"];

        Assert.Equal("Arc", arc.Name);
        Assert.Equal(3, arc.Color);
        Assert.True(arc.BaseEffectiveness > 1.0);
        Assert.Equal(0.7, arc.CastTime);
        Assert.False(arc.Support);
        Assert.True(arc.SkillTypes.ContainsKey(SkillType.Spell));
        Assert.True(arc.BaseFlags.ContainsKey("spell"));
        Assert.True(arc.Stats.Count >= 3);
        Assert.True(arc.Levels.Count >= 20);

        var lvl1 = arc.Levels[1];
        Assert.Equal(6.0, lvl1.CritChance);
        Assert.Equal(1.2, lvl1.DamageEffectiveness);
        Assert.Equal(12, lvl1.LevelRequirement);
    }

    [Fact]
    public void LoadFromFile_RealSupInt_ControlledDestruction()
    {
        var path = TestDataHelper.GetSkillFile("sup_int.lua");
        if (!File.Exists(path))
            return;

        var skills = SkillDataLoader.LoadFromFile(path);
        var cd = skills["SupportControlledDestruction"];

        Assert.True(cd.Support);
        Assert.Equal("Controlled Destruction", cd.Name);
        Assert.Contains(SkillType.Damage, cd.RequireSkillTypes);
        Assert.True(cd.StatMap.ContainsKey("support_controlled_destruction_spell_damage_+%_final"));

        var entry = cd.StatMap["support_controlled_destruction_spell_damage_+%_final"];
        Assert.Single(entry.Mods);
        Assert.Equal("Damage", entry.Mods[0].Name);
        Assert.Equal(ModType.More, entry.Mods[0].Type);
        Assert.Equal(ModFlag.Spell, entry.Mods[0].Flags);

        Assert.Equal(40.0, cd.Levels[1].ManaMultiplier);
    }

    [Fact]
    public void LoadFromFile_RealSupInt_AwakenedHasPlusVersionOf()
    {
        var path = TestDataHelper.GetSkillFile("sup_int.lua");
        if (!File.Exists(path))
            return;

        var skills = SkillDataLoader.LoadFromFile(path);
        var awakened = skills["SupportAwakenedControlledDestruction"];
        Assert.Equal("SupportControlledDestruction", awakened.PlusVersionOf);
    }

    [Fact]
    public void LoadAllFromDirectory_RealSkills()
    {
        var dir = TestDataHelper.GetSkillsDir();
        if (!Directory.Exists(dir))
            return;

        var skills = SkillDataLoader.LoadAllFromDirectory(dir);
        Assert.True(skills.Count > 200, $"Expected > 200 skills, got {skills.Count}");
    }
}
