using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class BuildPipelineSkillTests
{
    private static SkillDataCache MakeMinimalCache()
    {
        var cache = new SkillDataCache();
        cache.SetSkills(new());
        cache.SetGems(new());
        cache.SetSkillStatMap(new());
        return cache;
    }

    private static BuildData MakeMinimalBuild()
    {
        return new BuildData
        {
            Metadata = new BuildMetadata
            {
                Level = 90,
                ClassName = "Witch",
                AscendClassName = "Occultist",
                MainSocketGroup = 1,
            },
            ConfigSets = new List<ConfigSetData>
            {
                new ConfigSetData { Inputs = new List<ConfigInput>() },
            },
        };
    }

    [Fact]
    public void CreateActors_WithoutSkillCache_NoActiveSkills()
    {
        var build = MakeMinimalBuild();
        var (player, _) = BuildPipeline.CreateActors(build);

        Assert.Empty(player.ActiveSkillList);
        Assert.Null(player.MainSkill);
    }

    [Fact]
    public void CreateActors_WithSkillCache_BuildsSkills()
    {
        var vortex = new SkillDefinition
        {
            Id = "Vortex",
            Name = "Vortex",
            SkillTypes = new()
            {
                [SkillType.Spell] = true,
                [SkillType.Damage] = true,
                [SkillType.Area] = true,
                [SkillType.Cold] = true,
                [SkillType.Duration] = true,
            },
            BaseFlags = new() { ["spell"] = true, ["area"] = true, ["duration"] = true },
            Levels = new Dictionary<int, SkillLevelData>
            {
                [20] = new SkillLevelData
                {
                    Level = 20,
                    CritChance = 6.5,
                    LevelRequirement = 70,
                },
            },
        };

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["Vortex"] = vortex });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        var build = MakeMinimalBuild();
        build.SkillSets = new List<SkillSetData>
        {
            new SkillSetData
            {
                Id = 1,
                SocketGroups = new List<SocketGroupData>
                {
                    new SocketGroupData
                    {
                        Gems = new List<GemInstanceData>
                        {
                            new GemInstanceData { SkillId = "Vortex", Level = 20, Quality = 20 },
                        },
                    },
                },
            },
        };

        var (player, _) = BuildPipeline.CreateActors(build, skillCache: cache);

        Assert.Single(player.ActiveSkillList);
        Assert.NotNull(player.MainSkill);
        Assert.Equal("Vortex", player.MainSkill!.GrantedEffect.Name);
        Assert.NotNull(player.MainSkill.SkillCfg);
    }

    [Fact]
    public void CreateActors_MultipleGroups_SetsCorrectMainSkill()
    {
        var fireball = new SkillDefinition
        {
            Id = "Fireball",
            Name = "Fireball",
            SkillTypes = new() { [SkillType.Spell] = true },
        };
        var vortex = new SkillDefinition
        {
            Id = "Vortex",
            Name = "Vortex",
            SkillTypes = new() { [SkillType.Spell] = true },
        };

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["Fireball"] = fireball, ["Vortex"] = vortex });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        var build = MakeMinimalBuild();
        build.Metadata.MainSocketGroup = 2;
        build.SkillSets = new List<SkillSetData>
        {
            new SkillSetData
            {
                Id = 1,
                SocketGroups = new List<SocketGroupData>
                {
                    new SocketGroupData
                    {
                        Gems = new List<GemInstanceData>
                        {
                            new GemInstanceData { SkillId = "Fireball", Level = 20 },
                        },
                    },
                    new SocketGroupData
                    {
                        Gems = new List<GemInstanceData>
                        {
                            new GemInstanceData { SkillId = "Vortex", Level = 20 },
                        },
                    },
                },
            },
        };

        var (player, _) = BuildPipeline.CreateActors(build, skillCache: cache);

        Assert.Equal(2, player.ActiveSkillList.Count);
        Assert.Equal("Vortex", player.MainSkill!.GrantedEffect.Name);
    }

    [Fact]
    public void CreateActors_WithSupport_MergesEffects()
    {
        var fireball = new SkillDefinition
        {
            Id = "Fireball",
            Name = "Fireball",
            SkillTypes = new() { [SkillType.Spell] = true, [SkillType.Damage] = true },
        };
        var support = new SkillDefinition
        {
            Id = "ControlledDestruction",
            Name = "Controlled Destruction",
            Support = true,
            RequireSkillTypes = new List<SkillType> { SkillType.Damage },
        };

        var cache = new SkillDataCache();
        cache.SetSkills(new()
        {
            ["Fireball"] = fireball,
            ["ControlledDestruction"] = support,
        });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        var build = MakeMinimalBuild();
        build.SkillSets = new List<SkillSetData>
        {
            new SkillSetData
            {
                Id = 1,
                SocketGroups = new List<SocketGroupData>
                {
                    new SocketGroupData
                    {
                        Gems = new List<GemInstanceData>
                        {
                            new GemInstanceData { SkillId = "Fireball", Level = 20 },
                            new GemInstanceData { SkillId = "ControlledDestruction", Level = 20 },
                        },
                    },
                },
            },
        };

        var (player, _) = BuildPipeline.CreateActors(build, skillCache: cache);

        Assert.Single(player.ActiveSkillList);
        Assert.Equal(2, player.ActiveSkillList[0].EffectList.Count);
    }

    [Fact]
    public void Calculate_WithSkills_StillComputesDefence()
    {
        var skill = new SkillDefinition
        {
            Id = "Fireball",
            Name = "Fireball",
            SkillTypes = new() { [SkillType.Spell] = true },
        };

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["Fireball"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        var build = MakeMinimalBuild();
        build.SkillSets = new List<SkillSetData>
        {
            new SkillSetData
            {
                Id = 1,
                SocketGroups = new List<SocketGroupData>
                {
                    new SocketGroupData
                    {
                        Gems = new List<GemInstanceData>
                        {
                            new GemInstanceData { SkillId = "Fireball", Level = 20 },
                        },
                    },
                },
            },
        };

        var (player, _) = BuildPipeline.Calculate(build, skillCache: cache);

        // Calculation pipeline ran successfully
        Assert.True(player.Output.ContainsKey("Life"));
        Assert.True(player.Output["Life"] > 0);

        // Skills were created
        Assert.Single(player.ActiveSkillList);
        Assert.NotNull(player.MainSkill);
    }

    [Fact]
    public void Calculate_WithSkills_SetsMainSkillName()
    {
        var skill = new SkillDefinition
        {
            Id = "Vortex",
            Name = "Vortex",
            SkillTypes = new() { [SkillType.Spell] = true },
        };

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["Vortex"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        var build = MakeMinimalBuild();
        build.SkillSets = new List<SkillSetData>
        {
            new SkillSetData
            {
                Id = 1,
                SocketGroups = new List<SocketGroupData>
                {
                    new SocketGroupData
                    {
                        Gems = new List<GemInstanceData>
                        {
                            new GemInstanceData { SkillId = "Vortex", Level = 20 },
                        },
                    },
                },
            },
        };

        var (player, _) = BuildPipeline.Calculate(build, skillCache: cache);

        Assert.Equal("Vortex", player.MainSkillName);
    }

    [Fact]
    public void CreateActors_SkillModList_HasParent()
    {
        var skill = new SkillDefinition
        {
            Id = "Fireball",
            Name = "Fireball",
            SkillTypes = new() { [SkillType.Spell] = true },
        };

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["Fireball"] = skill });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        var build = MakeMinimalBuild();
        build.SkillSets = new List<SkillSetData>
        {
            new SkillSetData
            {
                Id = 1,
                SocketGroups = new List<SocketGroupData>
                {
                    new SocketGroupData
                    {
                        Gems = new List<GemInstanceData>
                        {
                            new GemInstanceData { SkillId = "Fireball", Level = 20 },
                        },
                    },
                },
            },
        };

        var (player, _) = BuildPipeline.CreateActors(build, skillCache: cache);

        // SkillModList should have playerModDB as parent
        var activeSkill = player.ActiveSkillList[0];
        Assert.NotNull(activeSkill.SkillModList);

        // Verify the skill can query through the parent chain
        // Add a mod to player ModDB and verify it's visible through the skill mod list
        player.ModDB.AddMod(new Mod
        {
            Name = "TestStat",
            Type = ModType.Base,
            Value = 42,
            Source = "Test",
        });

        double sum = activeSkill.SkillModList.Sum(ModType.Base, null, "TestStat");
        Assert.Equal(42, sum);
    }

    [Fact]
    public void CreateActors_EmptySkillSets_NoError()
    {
        var cache = MakeMinimalCache();
        var build = MakeMinimalBuild();

        var (player, _) = BuildPipeline.CreateActors(build, skillCache: cache);

        Assert.Empty(player.ActiveSkillList);
    }

    [Fact]
    public void CreateActors_SkillCfg_HasCorrectTypes()
    {
        var vortex = new SkillDefinition
        {
            Id = "Vortex",
            Name = "Vortex",
            SkillTypes = new()
            {
                [SkillType.Spell] = true,
                [SkillType.Damage] = true,
                [SkillType.Area] = true,
                [SkillType.Cold] = true,
            },
            BaseFlags = new() { ["spell"] = true, ["area"] = true, ["hit"] = true },
        };

        var cache = new SkillDataCache();
        cache.SetSkills(new() { ["Vortex"] = vortex });
        cache.SetGems(new());
        cache.SetSkillStatMap(new());

        var build = MakeMinimalBuild();
        build.SkillSets = new List<SkillSetData>
        {
            new SkillSetData
            {
                Id = 1,
                SocketGroups = new List<SocketGroupData>
                {
                    new SocketGroupData
                    {
                        Gems = new List<GemInstanceData>
                        {
                            new GemInstanceData { SkillId = "Vortex", Level = 20 },
                        },
                    },
                },
            },
        };

        var (player, _) = BuildPipeline.CreateActors(build, skillCache: cache);

        var cfg = player.ActiveSkillList[0].SkillCfg!;
        Assert.Equal("Vortex", cfg.SkillName);
        Assert.True(cfg.Flags.HasFlag(ModFlag.Spell));
        Assert.True(cfg.Flags.HasFlag(ModFlag.Area));
        Assert.True(cfg.Flags.HasFlag(ModFlag.Hit));
        Assert.True(cfg.Flags.HasFlag(ModFlag.Cast));
        Assert.True(cfg.KeywordFlags.HasFlag(KeywordFlag.Cold));
        Assert.True(cfg.KeywordFlags.HasFlag(KeywordFlag.Spell));
        Assert.True(cfg.KeywordFlags.HasFlag(KeywordFlag.Hit));
        Assert.Contains(SkillType.Spell, cfg.SkillTypes!);
        Assert.Contains(SkillType.Area, cfg.SkillTypes!);
        Assert.Contains(SkillType.Cold, cfg.SkillTypes!);
    }
}
