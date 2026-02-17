using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class SkillStatMapLoaderTests
{
    // --- Synthetic tests ---

    [Fact]
    public void LoadFromText_SingleEntry()
    {
        var lua = """
            local mod, flag, skill = ...
            return {
                ["base_skill_effect_duration"] = {
                    skill("duration", nil),
                    div = 1000,
                },
            }
        """;

        var map = SkillStatMapLoader.LoadFromText(lua);
        Assert.Single(map);
        var entry = map["base_skill_effect_duration"];
        Assert.Single(entry.Mods);
        Assert.Equal("SkillData", entry.Mods[0].Name);
        Assert.Equal(ModType.List, entry.Mods[0].Type);
        Assert.Equal(1000.0, entry.Div);
    }

    [Fact]
    public void LoadFromText_EntryWithMod()
    {
        var lua = """
            local mod, flag, skill = ...
            return {
                ["attack_speed_+%"] = {
                    mod("Speed", "INC", nil, ModFlag.Attack),
                },
            }
        """;

        var map = SkillStatMapLoader.LoadFromText(lua);
        var entry = map["attack_speed_+%"];
        Assert.Single(entry.Mods);
        Assert.Equal("Speed", entry.Mods[0].Name);
        Assert.Equal(ModType.Inc, entry.Mods[0].Type);
        Assert.Equal(ModFlag.Attack, entry.Mods[0].Flags);
        Assert.Null(entry.Div);
    }

    [Fact]
    public void LoadFromText_EntryWithMult()
    {
        var lua = """
            local mod, flag, skill = ...
            return {
                ["test_stat"] = {
                    mod("Damage", "INC", nil),
                    mult = 0.01,
                },
            }
        """;

        var map = SkillStatMapLoader.LoadFromText(lua);
        var entry = map["test_stat"];
        Assert.Equal(0.01, entry.Mult);
    }

    [Fact]
    public void LoadFromText_MultipleEntries()
    {
        var lua = """
            local mod, flag, skill = ...
            return {
                ["stat_a"] = { skill("A", nil) },
                ["stat_b"] = { skill("B", nil), div = 100 },
                ["stat_c"] = { mod("Speed", "INC", nil) },
            }
        """;

        var map = SkillStatMapLoader.LoadFromText(lua);
        Assert.Equal(3, map.Count);
    }

    [Fact]
    public void LoadFromText_BitBorInMod()
    {
        var lua = """
            local mod, flag, skill = ...
            return {
                ["test_stat"] = {
                    mod("Damage", "INC", nil, 0, bit.bor(KeywordFlag.Hit, KeywordFlag.Ailment)),
                },
            }
        """;

        var map = SkillStatMapLoader.LoadFromText(lua);
        var mod = map["test_stat"].Mods[0];
        Assert.Equal(KeywordFlag.Hit | KeywordFlag.Ailment, mod.KeywordFlags);
    }

    [Fact]
    public void LoadFromText_EntryWithFlag()
    {
        var lua = """
            local mod, flag, skill = ...
            return {
                ["test_flag"] = {
                    flag("Condition:CanBeElusive"),
                },
            }
        """;

        var map = SkillStatMapLoader.LoadFromText(lua);
        Assert.Single(map["test_flag"].Mods);
        Assert.Equal("Condition:CanBeElusive", map["test_flag"].Mods[0].Name);
        Assert.Equal(ModType.Flag, map["test_flag"].Mods[0].Type);
    }

    // --- Real file integration tests ---

    [Fact]
    public void LoadFromFile_RealSkillStatMap_CountExceeds100()
    {
        var path = TestDataHelper.GetSkillStatMapPath();
        if (!File.Exists(path))
            return;

        var map = SkillStatMapLoader.LoadFromFile(path);
        Assert.True(map.Count > 100, $"Expected > 100 entries, got {map.Count}");
    }

    [Fact]
    public void LoadFromFile_RealSkillStatMap_DurationEntry()
    {
        var path = TestDataHelper.GetSkillStatMapPath();
        if (!File.Exists(path))
            return;

        var map = SkillStatMapLoader.LoadFromFile(path);
        Assert.True(map.ContainsKey("base_skill_effect_duration"));
        var entry = map["base_skill_effect_duration"];
        Assert.Single(entry.Mods);
        Assert.Equal("SkillData", entry.Mods[0].Name);
        Assert.Equal(1000.0, entry.Div);
    }

    [Fact]
    public void LoadFromFile_RealSkillStatMap_ColdDamageEntry()
    {
        var path = TestDataHelper.GetSkillStatMapPath();
        if (!File.Exists(path))
            return;

        var map = SkillStatMapLoader.LoadFromFile(path);
        Assert.True(map.ContainsKey("spell_minimum_base_cold_damage"));
        var entry = map["spell_minimum_base_cold_damage"];
        Assert.Single(entry.Mods);
        Assert.Equal("SkillData", entry.Mods[0].Name);
    }
}
