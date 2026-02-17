using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class SkillModListBuilderTests
{
    // ─── BuildSkillModFlags ───

    [Fact]
    public void BuildSkillModFlags_Hit()
    {
        var flags = SkillModListBuilder.BuildSkillModFlags(new() { ["hit"] = true });
        Assert.True(flags.HasFlag(ModFlag.Hit));
    }

    [Fact]
    public void BuildSkillModFlags_Attack()
    {
        var flags = SkillModListBuilder.BuildSkillModFlags(new() { ["attack"] = true });
        Assert.True(flags.HasFlag(ModFlag.Attack));
        Assert.False(flags.HasFlag(ModFlag.Cast));
    }

    [Fact]
    public void BuildSkillModFlags_NotAttack_SetsCast()
    {
        var flags = SkillModListBuilder.BuildSkillModFlags(new() { ["spell"] = true });
        Assert.True(flags.HasFlag(ModFlag.Cast));
        Assert.True(flags.HasFlag(ModFlag.Spell));
        Assert.False(flags.HasFlag(ModFlag.Attack));
    }

    [Fact]
    public void BuildSkillModFlags_Melee()
    {
        var flags = SkillModListBuilder.BuildSkillModFlags(
            new() { ["attack"] = true, ["melee"] = true, ["hit"] = true });
        Assert.True(flags.HasFlag(ModFlag.Melee));
        Assert.False(flags.HasFlag(ModFlag.Projectile));
    }

    [Fact]
    public void BuildSkillModFlags_Projectile()
    {
        var flags = SkillModListBuilder.BuildSkillModFlags(
            new() { ["attack"] = true, ["projectile"] = true });
        Assert.True(flags.HasFlag(ModFlag.Projectile));
        Assert.False(flags.HasFlag(ModFlag.Melee));
    }

    [Fact]
    public void BuildSkillModFlags_Area()
    {
        var flags = SkillModListBuilder.BuildSkillModFlags(
            new() { ["spell"] = true, ["area"] = true });
        Assert.True(flags.HasFlag(ModFlag.Area));
    }

    // ─── BuildSkillKeywordFlags ───

    [Fact]
    public void BuildSkillKeywordFlags_Hit()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new(), new() { ["hit"] = true });
        Assert.True(kf.HasFlag(KeywordFlag.Hit));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Aura()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Aura] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Aura));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Curse()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.AppliesCurse] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Curse));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Elements()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new()
            {
                [SkillType.Lightning] = true,
                [SkillType.Cold] = true,
                [SkillType.Fire] = true,
            },
            new());
        Assert.True(kf.HasFlag(KeywordFlag.Lightning));
        Assert.True(kf.HasFlag(KeywordFlag.Cold));
        Assert.True(kf.HasFlag(KeywordFlag.Fire));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Totem()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new(), new() { ["totem"] = true });
        Assert.True(kf.HasFlag(KeywordFlag.Totem));
        Assert.False(kf.HasFlag(KeywordFlag.Trap));
        Assert.False(kf.HasFlag(KeywordFlag.Mine));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Trap()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new(), new() { ["trap"] = true });
        Assert.True(kf.HasFlag(KeywordFlag.Trap));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Mine()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new(), new() { ["mine"] = true });
        Assert.True(kf.HasFlag(KeywordFlag.Mine));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Attack()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Attack] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Attack));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Spell()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Spell] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Spell));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Spell_WithCastFlag_NoSpellKeyword()
    {
        // "cast" flag suppresses Spell keyword
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Spell] = true }, new() { ["cast"] = true });
        Assert.False(kf.HasFlag(KeywordFlag.Spell));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Brand()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new(), new() { ["brand"] = true });
        Assert.True(kf.HasFlag(KeywordFlag.Brand));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Warcry()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Warcry] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Warcry));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Vaal()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Vaal] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Vaal));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Movement()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Movement] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Movement));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Chaos()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Chaos] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Chaos));
    }

    [Fact]
    public void BuildSkillKeywordFlags_Physical()
    {
        var kf = SkillModListBuilder.BuildSkillKeywordFlags(
            new() { [SkillType.Physical] = true }, new());
        Assert.True(kf.HasFlag(KeywordFlag.Physical));
    }

    // ─── Build (full integration) ───

    [Fact]
    public void Build_CreatesSkillCfg()
    {
        var skill = new SkillDefinition
        {
            Id = "Fireball",
            Name = "Fireball",
            SkillTypes = new() { [SkillType.Spell] = true, [SkillType.Projectile] = true },
            BaseFlags = new() { ["spell"] = true, ["projectile"] = true, ["hit"] = true },
        };
        var gem = new GemInstanceData { SkillId = "Fireball", Level = 20, Quality = 20 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = new(skill.SkillTypes),
            SkillFlags = new(skill.BaseFlags),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var playerModDB = new ModDB();
        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["Fireball"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, playerModDB, cache);

        Assert.NotNull(activeSkill.SkillCfg);
        Assert.Equal("Fireball", activeSkill.SkillCfg!.SkillName);
        Assert.True(activeSkill.SkillCfg.Flags.HasFlag(ModFlag.Spell));
        Assert.True(activeSkill.SkillCfg.Flags.HasFlag(ModFlag.Hit));
        Assert.True(activeSkill.SkillCfg.Flags.HasFlag(ModFlag.Projectile));
        Assert.True(activeSkill.SkillCfg.Flags.HasFlag(ModFlag.Cast));
        Assert.True(activeSkill.SkillCfg.KeywordFlags.HasFlag(KeywordFlag.Spell));
        Assert.Contains(SkillType.Spell, activeSkill.SkillCfg.SkillTypes!);
    }

    [Fact]
    public void Build_VaalSkillName_StripsPrefix()
    {
        var skill = new SkillDefinition
        {
            Id = "VaalFireball",
            Name = "Vaal Fireball",
            SkillTypes = new() { [SkillType.Vaal] = true },
        };
        var gem = new GemInstanceData { SkillId = "VaalFireball", Level = 20 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = new(skill.SkillTypes),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["VaalFireball"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        Assert.Equal("Fireball", activeSkill.SkillCfg!.SkillName);
    }

    [Fact]
    public void Build_StatMapMods_Merged()
    {
        var skill = new SkillDefinition
        {
            Id = "TestSkill",
            Name = "Test",
            Stats = new List<string> { "base_damage" },
            Levels = new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 100 },
                    StatInterpolation = new List<int> { 1 },
                    LevelRequirement = 1,
                },
            },
            StatMap = new Dictionary<string, StatMapEntry>
            {
                ["base_damage"] = new StatMapEntry
                {
                    Mods = new List<Mod>
                    {
                        new Mod
                        {
                            Name = "PhysicalMin",
                            Type = ModType.Base,
                            Value = 0, // placeholder
                        },
                    },
                },
            },
        };

        var gem = new GemInstanceData { SkillId = "TestSkill", Level = 1 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = new(),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["TestSkill"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        // The stat map should have merged the mod with value 100
        bool found = false;
        foreach (var mod in activeSkill.SkillModList)
        {
            if (mod.Name == "PhysicalMin" && mod.Type == ModType.Base)
            {
                Assert.Equal(100, mod.Value.AsNumber());
                found = true;
                break;
            }
        }
        Assert.True(found, "Expected PhysicalMin mod with value 100");
    }

    [Fact]
    public void Build_StatMap_WithDivMult()
    {
        var skill = new SkillDefinition
        {
            Id = "TestSkill",
            Name = "Test",
            Stats = new List<string> { "base_damage" },
            Levels = new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 200 },
                    StatInterpolation = new List<int> { 1 },
                    LevelRequirement = 1,
                },
            },
            StatMap = new Dictionary<string, StatMapEntry>
            {
                ["base_damage"] = new StatMapEntry
                {
                    Div = 100,
                    Mult = 2,
                    Base = 5,
                    Mods = new List<Mod>
                    {
                        new Mod { Name = "Damage", Type = ModType.Base, Value = 0 },
                    },
                },
            },
        };

        var gem = new GemInstanceData { SkillId = "TestSkill", Level = 1 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = new(),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["TestSkill"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        // value = 200 * 2 / 100 + 5 = 4 + 5 = 9
        bool found = false;
        foreach (var mod in activeSkill.SkillModList)
        {
            if (mod.Name == "Damage" && mod.Type == ModType.Base)
            {
                Assert.Equal(9, mod.Value.AsNumber());
                found = true;
                break;
            }
        }
        Assert.True(found, "Expected Damage mod with value 9");
    }

    [Fact]
    public void Build_GlobalStatMap_Fallback()
    {
        var skill = new SkillDefinition
        {
            Id = "TestSkill",
            Name = "Test",
            Stats = new List<string> { "some_stat" },
            Levels = new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 50 },
                    StatInterpolation = new List<int> { 1 },
                    LevelRequirement = 1,
                },
            },
            // No skill-specific statMap for "some_stat"
        };

        var globalStatMap = new Dictionary<string, StatMapEntry>
        {
            ["some_stat"] = new StatMapEntry
            {
                Mods = new List<Mod>
                {
                    new Mod { Name = "SomeStat", Type = ModType.Base, Value = 0 },
                },
            },
        };

        var gem = new GemInstanceData { SkillId = "TestSkill", Level = 1 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = new(),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["TestSkill"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(globalStatMap);

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        bool found = false;
        foreach (var mod in activeSkill.SkillModList)
        {
            if (mod.Name == "SomeStat" && mod.Value.AsNumber() == 50)
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Expected global stat map fallback to apply");
    }

    [Fact]
    public void Build_BaseMods_Added()
    {
        var baseMod = new Mod
        {
            Name = "SkillFlag",
            Type = ModType.Flag,
            Value = true,
            Source = "Skill",
        };
        var skill = new SkillDefinition
        {
            Id = "TestSkill",
            Name = "Test",
            BaseMods = new List<Mod> { baseMod },
        };

        var gem = new GemInstanceData { SkillId = "TestSkill", Level = 1 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = new(),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["TestSkill"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        bool found = false;
        foreach (var mod in activeSkill.SkillModList)
        {
            if (mod.Name == "SkillFlag" && mod.Type == ModType.Flag)
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Expected base mod to be added to skill mod list");
    }

    [Fact]
    public void Build_SupportMods_Merged()
    {
        var activeSkillDef = new SkillDefinition
        {
            Id = "Fireball",
            Name = "Fireball",
            SkillTypes = new() { [SkillType.Spell] = true },
        };

        var supportDef = new SkillDefinition
        {
            Id = "AddedFire",
            Name = "Added Fire",
            Support = true,
            Stats = new List<string> { "fire_damage_add" },
            Levels = new Dictionary<int, SkillLevelData>
            {
                [20] = new SkillLevelData
                {
                    Level = 20,
                    Values = new List<double> { 39 },
                    StatInterpolation = new List<int> { 1 },
                    LevelRequirement = 70,
                },
            },
            StatMap = new Dictionary<string, StatMapEntry>
            {
                ["fire_damage_add"] = new StatMapEntry
                {
                    Mods = new List<Mod>
                    {
                        new Mod { Name = "FireDamageGainAsExtra", Type = ModType.Base, Value = 0 },
                    },
                },
            },
        };

        var gem = new GemInstanceData { SkillId = "Fireball", Level = 20 };
        var supportGem = new GemInstanceData { SkillId = "AddedFire", Level = 20 };

        var activeSkill = new ActiveSkill
        {
            GrantedEffect = activeSkillDef,
            GemInstance = gem,
            SkillTypes = new(activeSkillDef.SkillTypes),
            SkillFlags = new() { ["spell"] = true },
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = activeSkillDef,
            GemInstance = gem,
        });
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = supportDef,
            GemInstance = supportGem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new()
        {
            ["Fireball"] = activeSkillDef,
            ["AddedFire"] = supportDef,
        });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        bool found = false;
        foreach (var mod in activeSkill.SkillModList)
        {
            if (mod.Name == "FireDamageGainAsExtra" && mod.Type == ModType.Base)
            {
                Assert.Equal(39, mod.Value.AsNumber());
                found = true;
                break;
            }
        }
        Assert.True(found, "Expected support mod to be merged with value 39");
    }

    // ─── SkillData extraction ───

    [Fact]
    public void Build_SkillData_ExtractedFromListMods()
    {
        var skill = new SkillDefinition
        {
            Id = "TestSkill",
            Name = "Test",
            BaseMods = new List<Mod>
            {
                new Mod
                {
                    Name = "SkillData",
                    Type = ModType.List,
                    Value = ModValue.FromComplex(new Dictionary<string, object?>
                    {
                        ["key"] = "cooldown",
                        ["value"] = 3.0,
                    }),
                    Source = "Skill",
                },
            },
        };

        var gem = new GemInstanceData { SkillId = "TestSkill", Level = 1 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = new(),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["TestSkill"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        Assert.True(activeSkill.SkillData.ContainsKey("cooldown"));
        Assert.Equal(3.0, activeSkill.SkillData["cooldown"]);
    }

    [Fact]
    public void Build_LevelData_PopulatesSkillData()
    {
        var skill = new SkillDefinition
        {
            Id = "TestSkill",
            Name = "Test",
            Levels = new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    CritChance = 6.5,
                    Cooldown = 2.0,
                    Duration = 4.0,
                    StoredUses = 3,
                    LevelRequirement = 1,
                },
            },
        };

        var gem = new GemInstanceData { SkillId = "TestSkill", Level = 1 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillTypes = new(),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["TestSkill"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        Assert.Equal(6.5, activeSkill.SkillData["CritChance"]);
        Assert.Equal(2.0, activeSkill.SkillData["cooldown"]);
        Assert.Equal(4.0, activeSkill.SkillData["duration"]);
        Assert.Equal(3, activeSkill.SkillData["storedUses"]);
    }

    // ─── Multipart skills ───

    [Fact]
    public void Build_MultipartSkill_SetsSkillPartName()
    {
        var skill = new SkillDefinition
        {
            Id = "TestSkill",
            Name = "Test",
            Parts = new List<SkillPartDef>
            {
                new SkillPartDef { Name = "Part 1" },
                new SkillPartDef { Name = "Part 2" },
            },
        };

        var gem = new GemInstanceData { SkillId = "TestSkill", Level = 1, SkillPart = 2 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillPart = 2,
            SkillTypes = new(),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["TestSkill"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        Assert.Equal("Part 2", activeSkill.SkillPartName);
        Assert.Equal(2, activeSkill.SkillPart);
    }

    [Fact]
    public void Build_MultipartSkill_ClampsPartIndex()
    {
        var skill = new SkillDefinition
        {
            Id = "TestSkill",
            Name = "Test",
            Parts = new List<SkillPartDef>
            {
                new SkillPartDef { Name = "Part 1" },
                new SkillPartDef { Name = "Part 2" },
            },
        };

        var gem = new GemInstanceData { SkillId = "TestSkill", Level = 1, SkillPart = 99 };
        var activeSkill = new ActiveSkill
        {
            GrantedEffect = skill,
            GemInstance = gem,
            SkillPart = 99,
            SkillTypes = new(),
            SkillFlags = new(),
        };
        activeSkill.EffectList.Add(new ActiveSkillEffect
        {
            GrantedEffect = skill,
            GemInstance = gem,
        });

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["TestSkill"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        SkillModListBuilder.Build(activeSkill, new ModDB(), cache);

        Assert.Equal(2, activeSkill.SkillPart); // Clamped to max
        Assert.Equal("Part 2", activeSkill.SkillPartName);
    }
}
