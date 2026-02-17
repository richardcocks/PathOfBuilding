using PathOfBuilding.Core.Import;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class ActiveSkillBuilderTests
{
    private static SkillDefinition MakeActiveSkillDef(
        string id, string name,
        Dictionary<SkillType, bool>? skillTypes = null,
        Dictionary<string, bool>? baseFlags = null)
    {
        return new SkillDefinition
        {
            Id = id,
            Name = name,
            Support = false,
            SkillTypes = skillTypes ?? new(),
            BaseFlags = baseFlags ?? new(),
        };
    }

    private static SkillDefinition MakeSupportDef(
        string id, string name,
        List<SkillType>? requireTypes = null,
        List<SkillType>? excludeTypes = null,
        List<SkillType>? addTypes = null)
    {
        return new SkillDefinition
        {
            Id = id,
            Name = name,
            Support = true,
            RequireSkillTypes = requireTypes ?? new(),
            ExcludeSkillTypes = excludeTypes ?? new(),
            AddSkillTypes = addTypes ?? new(),
        };
    }

    private static SkillDataCache MakeCache(
        Dictionary<string, SkillDefinition>? skills = null,
        Dictionary<string, GemDefinition>? gems = null)
    {
        var cache = new SkillDataCache();
        cache.SetSkills(skills ?? new());
        cache.SetGems(gems ?? new());
        cache.SetSkillStatMap(new());
        return cache;
    }

    private static BuildData MakeBuild(List<SocketGroupData> groups, int mainSocketGroup = 1)
    {
        return new BuildData
        {
            Metadata = new BuildMetadata
            {
                Level = 90,
                ClassName = "Witch",
                MainSocketGroup = mainSocketGroup,
            },
            SkillSets = new List<SkillSetData>
            {
                new SkillSetData
                {
                    Id = 1,
                    SocketGroups = groups,
                },
            },
        };
    }

    // ─── Gem resolution ───

    [Fact]
    public void ResolveGem_BySkillId()
    {
        var skill = MakeActiveSkillDef("Fireball", "Fireball");
        var cache = MakeCache(new() { ["Fireball"] = skill });

        var gem = new GemInstanceData { SkillId = "Fireball" };
        var (resolved, _) = ActiveSkillBuilder.ResolveGem(gem, cache);

        Assert.NotNull(resolved);
        Assert.Equal("Fireball", resolved!.Name);
    }

    [Fact]
    public void ResolveGem_ByGemId_Fallback()
    {
        var skill = MakeActiveSkillDef("Fireball", "Fireball");
        var gemDef = new GemDefinition
        {
            Id = "FireballGem", Name = "Fireball", GameId = "G1",
            VariantId = "V1", GrantedEffectId = "Fireball",
        };
        var cache = MakeCache(
            new() { ["Fireball"] = skill },
            new() { ["FireballGem"] = gemDef });

        var gem = new GemInstanceData { GemId = "FireballGem" };
        var (resolved, resolvedGem) = ActiveSkillBuilder.ResolveGem(gem, cache);

        Assert.NotNull(resolved);
        Assert.Equal("Fireball", resolved!.Name);
        Assert.NotNull(resolvedGem);
    }

    [Fact]
    public void ResolveGem_Unknown_ReturnsNull()
    {
        var cache = MakeCache();
        var gem = new GemInstanceData { SkillId = "Nonexistent" };
        var (resolved, _) = ActiveSkillBuilder.ResolveGem(gem, cache);

        Assert.Null(resolved);
    }

    // ─── Single active gem ───

    [Fact]
    public void SingleActiveGem_CreatesOneSkill()
    {
        var skill = MakeActiveSkillDef("Fireball", "Fireball",
            skillTypes: new() { [SkillType.Spell] = true, [SkillType.Projectile] = true });
        var cache = MakeCache(new() { ["Fireball"] = skill });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball", Level = 20 },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Single(player.ActiveSkillList);
        Assert.Equal("Fireball", player.ActiveSkillList[0].GrantedEffect.Name);
        Assert.NotNull(player.MainSkill);
        Assert.Equal("Fireball", player.MainSkillName);
    }

    // ─── Active + supports ───

    [Fact]
    public void ActiveWithSupport_FiltersCompatible()
    {
        var active = MakeActiveSkillDef("Fireball", "Fireball",
            skillTypes: new()
            {
                [SkillType.Spell] = true,
                [SkillType.Projectile] = true,
                [SkillType.Damage] = true,
            });
        var support = MakeSupportDef("ControlledDestruction", "Controlled Destruction",
            requireTypes: new List<SkillType> { SkillType.Damage });
        var cache = MakeCache(new()
        {
            ["Fireball"] = active,
            ["ControlledDestruction"] = support,
        });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball" },
                    new GemInstanceData { SkillId = "ControlledDestruction" },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Single(player.ActiveSkillList);
        // effectList: [active, support]
        Assert.Equal(2, player.ActiveSkillList[0].EffectList.Count);
    }

    [Fact]
    public void ActiveWithIncompatibleSupport_Filtered()
    {
        var active = MakeActiveSkillDef("Fireball", "Fireball",
            skillTypes: new() { [SkillType.Spell] = true });
        var support = MakeSupportDef("MeleePhys", "Melee Physical Damage",
            requireTypes: new List<SkillType> { SkillType.Attack });
        var cache = MakeCache(new()
        {
            ["Fireball"] = active,
            ["MeleePhys"] = support,
        });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball" },
                    new GemInstanceData { SkillId = "MeleePhys" },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Single(player.ActiveSkillList);
        Assert.Single(player.ActiveSkillList[0].EffectList); // Only active, no support
    }

    // ─── Disabled groups ───

    [Fact]
    public void DisabledGroup_Skipped()
    {
        var skill = MakeActiveSkillDef("Fireball", "Fireball");
        var cache = MakeCache(new() { ["Fireball"] = skill });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Enabled = false,
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball" },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Empty(player.ActiveSkillList);
    }

    [Fact]
    public void DisabledGem_Skipped()
    {
        var skill = MakeActiveSkillDef("Fireball", "Fireball");
        var cache = MakeCache(new() { ["Fireball"] = skill });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball", Enabled = false },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Empty(player.ActiveSkillList);
    }

    // ─── Support cascading ───

    [Fact]
    public void SupportCascading_AddsTypesEnablingMoreSupports()
    {
        var active = MakeActiveSkillDef("Smite", "Smite",
            skillTypes: new() { [SkillType.Attack] = true });

        // Support A adds Triggered type
        var supportA = MakeSupportDef("TriggerSupport", "Trigger Support",
            requireTypes: new List<SkillType> { SkillType.Attack },
            addTypes: new List<SkillType> { SkillType.Triggered });

        // Support B requires Triggered (can't support until A adds it)
        var supportB = MakeSupportDef("TriggeredBonus", "Triggered Bonus",
            requireTypes: new List<SkillType> { SkillType.Triggered });

        var cache = MakeCache(new()
        {
            ["Smite"] = active,
            ["TriggerSupport"] = supportA,
            ["TriggeredBonus"] = supportB,
        });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Smite" },
                    new GemInstanceData { SkillId = "TriggerSupport" },
                    new GemInstanceData { SkillId = "TriggeredBonus" },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Single(player.ActiveSkillList);
        // Both supports should be in the effect list
        Assert.Equal(3, player.ActiveSkillList[0].EffectList.Count);
        // SkillTypes should include Triggered
        Assert.True(player.ActiveSkillList[0].SkillTypes.ContainsKey(SkillType.Triggered));
    }

    // ─── Main skill selection ───

    [Fact]
    public void MainSkill_SelectedFromMainSocketGroup()
    {
        var skill1 = MakeActiveSkillDef("Fireball", "Fireball");
        var skill2 = MakeActiveSkillDef("Vortex", "Vortex");
        var cache = MakeCache(new()
        {
            ["Fireball"] = skill1,
            ["Vortex"] = skill2,
        });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball" },
                },
            },
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Vortex" },
                },
            },
        }, mainSocketGroup: 2);

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Equal(2, player.ActiveSkillList.Count);
        Assert.NotNull(player.MainSkill);
        Assert.Equal("Vortex", player.MainSkill!.GrantedEffect.Name);
    }

    [Fact]
    public void MainSkill_FallbackToFirst()
    {
        var skill = MakeActiveSkillDef("Fireball", "Fireball");
        var cache = MakeCache(new() { ["Fireball"] = skill });
        var player = new Actor();

        // MainSocketGroup = 99 (doesn't exist)
        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball" },
                },
            },
        }, mainSocketGroup: 99);

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.NotNull(player.MainSkill);
        Assert.Equal("Fireball", player.MainSkill!.GrantedEffect.Name);
    }

    // ─── Hit flag inference ───

    [Fact]
    public void HitFlag_SetFromAttackType()
    {
        var skill = MakeActiveSkillDef("Sweep", "Sweep",
            skillTypes: new() { [SkillType.Attack] = true });
        var cache = MakeCache(new() { ["Sweep"] = skill });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Sweep" },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.True(player.ActiveSkillList[0].SkillFlags.ContainsKey("hit"));
    }

    [Fact]
    public void HitFlag_SetFromDamageType()
    {
        var skill = MakeActiveSkillDef("Fireball", "Fireball",
            skillTypes: new() { [SkillType.Damage] = true, [SkillType.Spell] = true });
        var cache = MakeCache(new() { ["Fireball"] = skill });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball" },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.True(player.ActiveSkillList[0].SkillFlags.ContainsKey("hit"));
    }

    // ─── Multiple active gems in one group ───

    [Fact]
    public void MultipleActiveGems_InOneGroup()
    {
        var skill1 = MakeActiveSkillDef("Fireball", "Fireball");
        var skill2 = MakeActiveSkillDef("IceBolt", "Ice Bolt");
        var cache = MakeCache(new()
        {
            ["Fireball"] = skill1,
            ["IceBolt"] = skill2,
        });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball" },
                    new GemInstanceData { SkillId = "IceBolt" },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Equal(2, player.ActiveSkillList.Count);
    }

    // ─── Support adds flags ───

    [Fact]
    public void Support_AddsFlags()
    {
        var active = MakeActiveSkillDef("Fireball", "Fireball",
            skillTypes: new() { [SkillType.Spell] = true, [SkillType.Damage] = true });
        var support = new SkillDefinition
        {
            Id = "RemoteMine",
            Name = "Remote Mine",
            Support = true,
            RequireSkillTypes = new List<SkillType> { SkillType.Spell },
            BaseFlags = new Dictionary<string, bool> { ["mine"] = true },
        };

        var cache = MakeCache(new()
        {
            ["Fireball"] = active,
            ["RemoteMine"] = support,
        });
        var player = new Actor();

        var build = MakeBuild(new List<SocketGroupData>
        {
            new SocketGroupData
            {
                Gems = new List<GemInstanceData>
                {
                    new GemInstanceData { SkillId = "Fireball" },
                    new GemInstanceData { SkillId = "RemoteMine" },
                },
            },
        });

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.True(player.ActiveSkillList[0].SkillFlags.ContainsKey("mine"));
    }

    // ─── Empty build ───

    [Fact]
    public void EmptyBuild_NoSkills()
    {
        var cache = MakeCache();
        var player = new Actor();
        var build = MakeBuild(new List<SocketGroupData>());

        ActiveSkillBuilder.BuildFromSocketGroups(build, player, cache);

        Assert.Empty(player.ActiveSkillList);
        Assert.Null(player.MainSkill);
    }
}
