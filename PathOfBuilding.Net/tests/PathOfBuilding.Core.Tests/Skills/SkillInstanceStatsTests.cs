using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class SkillInstanceStatsTests
{
    private static SkillDefinition MakeSkill(
        string id = "Test",
        List<string>? stats = null,
        Dictionary<int, SkillLevelData>? levels = null,
        List<(string, double)>? constantStats = null,
        Dictionary<string, List<(string, double)>>? qualityStats = null,
        double baseEffectiveness = 0,
        double incrementalEffectiveness = 0)
    {
        return new SkillDefinition
        {
            Id = id,
            Name = id,
            Stats = stats ?? new(),
            Levels = levels ?? new(),
            ConstantStats = constantStats ?? new(),
            QualityStats = qualityStats ?? new(),
            BaseEffectiveness = baseEffectiveness,
            IncrementalEffectiveness = incrementalEffectiveness,
        };
    }

    private static GemInstanceData MakeGem(int level = 1, int quality = 0, string qualityId = "Default")
    {
        return new GemInstanceData
        {
            Level = level,
            Quality = quality,
            QualityId = qualityId,
        };
    }

    // ─── Step interpolation (type 1 / default) ───

    [Fact]
    public void StepInterpolation_UsesRawValue()
    {
        var skill = MakeSkill(
            stats: new List<string> { "base_damage_min", "base_damage_max" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 10, 15 },
                    StatInterpolation = new List<int> { 1, 1 },
                    LevelRequirement = 1,
                },
            });

        var stats = SkillInstanceStats.Build(MakeGem(1), skill);

        Assert.Equal(10, stats["base_damage_min"]);
        Assert.Equal(15, stats["base_damage_max"]);
    }

    [Fact]
    public void StepInterpolation_DefaultWithoutStatInterpolation()
    {
        var skill = MakeSkill(
            stats: new List<string> { "base_damage" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [5] = new SkillLevelData
                {
                    Level = 5,
                    Values = new List<double> { 42 },
                    StatInterpolation = new List<int>(), // empty
                    LevelRequirement = 12,
                },
            });

        var stats = SkillInstanceStats.Build(MakeGem(5), skill);

        Assert.Equal(42, stats["base_damage"]);
    }

    [Fact]
    public void MultipleStats_AtDifferentLevels()
    {
        var skill = MakeSkill(
            stats: new List<string> { "damage", "mana_cost" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData { Level = 1, Values = new List<double> { 10, 5 }, StatInterpolation = new List<int> { 1, 1 }, LevelRequirement = 1 },
                [10] = new SkillLevelData { Level = 10, Values = new List<double> { 50, 15 }, StatInterpolation = new List<int> { 1, 1 }, LevelRequirement = 28 },
            });

        var stats1 = SkillInstanceStats.Build(MakeGem(1), skill);
        Assert.Equal(10, stats1["damage"]);
        Assert.Equal(5, stats1["mana_cost"]);

        var stats10 = SkillInstanceStats.Build(MakeGem(10), skill);
        Assert.Equal(50, stats10["damage"]);
        Assert.Equal(15, stats10["mana_cost"]);
    }

    // ─── Effectiveness interpolation (type 3) ───

    [Fact]
    public void EffectivenessInterpolation_ComputesCorrectValue()
    {
        // At level req 1: availableEff = (3.885209 + 0.360246*0) * 0.5 * (1+0.03)^0 = 1.9426045
        // statValue = round(1.9426045 * 100) = round(194.26045) = 194
        var skill = MakeSkill(
            stats: new List<string> { "base_damage" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 100 },
                    StatInterpolation = new List<int> { 3 },
                    LevelRequirement = 1,
                },
            },
            baseEffectiveness: 0.5,
            incrementalEffectiveness: 0.03);

        var stats = SkillInstanceStats.Build(MakeGem(1), skill);

        // (3.885209 + 0.360246 * (1-1)) * 0.5 * (1 + 0.03)^(1-1) = 3.885209 * 0.5 * 1 = 1.9426045
        // round(1.9426045 * 100) = round(194.26045) = 194
        Assert.Equal(194, stats["base_damage"]);
    }

    [Fact]
    public void EffectivenessInterpolation_HigherLevel()
    {
        // At level req 70:
        // baseEff = 3.885209, incrEff = 0.360246
        // availableEff = (3.885209 + 0.360246 * 69) * 1.0 * (1 + 0.05)^69
        var skill = MakeSkill(
            stats: new List<string> { "base_damage" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [20] = new SkillLevelData
                {
                    Level = 20,
                    Values = new List<double> { 1 }, // 1 unit of effectiveness
                    StatInterpolation = new List<int> { 3 },
                    LevelRequirement = 70,
                },
            },
            baseEffectiveness: 1.0,
            incrementalEffectiveness: 0.05);

        var stats = SkillInstanceStats.Build(MakeGem(20), skill);

        // (3.885209 + 0.360246 * 69) * 1.0 * (1.05)^69
        double expected = (3.885209 + 0.360246 * 69) * Math.Pow(1.05, 69);
        double rounded = Math.Floor(expected + 0.5);
        Assert.Equal(rounded, stats["base_damage"]);
    }

    // ─── Quality stats ───

    [Fact]
    public void QualityStats_AppliedWithTruncation()
    {
        var skill = MakeSkill(
            stats: new List<string> { "base_damage" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 100 },
                    StatInterpolation = new List<int> { 1 },
                    LevelRequirement = 1,
                },
            },
            qualityStats: new Dictionary<string, List<(string, double)>>
            {
                ["Default"] = new List<(string, double)>
                {
                    ("base_damage", 0.5), // Each quality adds 0.5
                },
            });

        // At quality 20: Math.Truncate(0.5 * 20) = 10
        var stats = SkillInstanceStats.Build(MakeGem(1, quality: 20), skill);

        // 100 (base) + 10 (quality) = 110
        Assert.Equal(110, stats["base_damage"]);
    }

    [Fact]
    public void QualityStats_TruncateTowardZero()
    {
        var skill = MakeSkill(
            qualityStats: new Dictionary<string, List<(string, double)>>
            {
                ["Default"] = new List<(string, double)>
                {
                    ("bonus", 0.3), // 0.3 * 7 = 2.1 → truncate to 2
                },
            });

        var stats = SkillInstanceStats.Build(MakeGem(1, quality: 7), skill);

        Assert.Equal(2, stats["bonus"]);
    }

    [Fact]
    public void QualityStats_NonDefaultQualityId()
    {
        var skill = MakeSkill(
            qualityStats: new Dictionary<string, List<(string, double)>>
            {
                ["Default"] = new List<(string, double)> { ("bonus", 1.0) },
                ["Anomalous"] = new List<(string, double)> { ("bonus", 2.0) },
            });

        var stats = SkillInstanceStats.Build(
            MakeGem(1, quality: 10, qualityId: "Anomalous"), skill);

        // 2.0 * 10 = 20
        Assert.Equal(20, stats["bonus"]);
    }

    [Fact]
    public void QualityStats_FallbackToDefault()
    {
        var skill = MakeSkill(
            qualityStats: new Dictionary<string, List<(string, double)>>
            {
                ["Default"] = new List<(string, double)> { ("bonus", 1.0) },
            });

        // qualityId "Phantasmal" not found → falls back to "Default"
        var stats = SkillInstanceStats.Build(
            MakeGem(1, quality: 5, qualityId: "Phantasmal"), skill);

        Assert.Equal(5, stats["bonus"]);
    }

    [Fact]
    public void QualityStats_ZeroQuality_NotApplied()
    {
        var skill = MakeSkill(
            qualityStats: new Dictionary<string, List<(string, double)>>
            {
                ["Default"] = new List<(string, double)> { ("bonus", 1.0) },
            });

        var stats = SkillInstanceStats.Build(MakeGem(1, quality: 0), skill);

        Assert.False(stats.ContainsKey("bonus"));
    }

    // ─── Constant stats ───

    [Fact]
    public void ConstantStats_Added()
    {
        var skill = MakeSkill(
            constantStats: new List<(string, double)>
            {
                ("is_area_damage", 1),
                ("base_skill_is_multicastable", 1),
            });

        var stats = SkillInstanceStats.Build(MakeGem(1), skill);

        Assert.Equal(1, stats["is_area_damage"]);
        Assert.Equal(1, stats["base_skill_is_multicastable"]);
    }

    [Fact]
    public void ConstantStats_AddedToExistingValues()
    {
        var skill = MakeSkill(
            stats: new List<string> { "base_damage" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 50 },
                    StatInterpolation = new List<int> { 1 },
                    LevelRequirement = 1,
                },
            },
            constantStats: new List<(string, double)>
            {
                ("base_damage", 10),
            });

        var stats = SkillInstanceStats.Build(MakeGem(1), skill);

        Assert.Equal(60, stats["base_damage"]); // 50 + 10
    }

    // ─── Missing level ───

    [Fact]
    public void MissingLevel_ReturnsOnlyQualityAndConstantStats()
    {
        var skill = MakeSkill(
            stats: new List<string> { "damage" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 10 },
                    StatInterpolation = new List<int> { 1 },
                    LevelRequirement = 1,
                },
            },
            constantStats: new List<(string, double)> { ("flag", 1) });

        // Level 99 doesn't exist
        var stats = SkillInstanceStats.Build(MakeGem(99), skill);

        Assert.False(stats.ContainsKey("damage")); // No level data
        Assert.Equal(1, stats["flag"]); // Constants still added
    }

    // ─── Linear interpolation (type 2) ───

    [Fact]
    public void LinearInterpolation_Computes()
    {
        var skill = MakeSkill(
            stats: new List<string> { "mana_cost" },
            levels: new Dictionary<int, SkillLevelData>
            {
                [1] = new SkillLevelData
                {
                    Level = 1,
                    Values = new List<double> { 10 },
                    StatInterpolation = new List<int> { 2 },
                    LevelRequirement = 1,
                },
                [20] = new SkillLevelData
                {
                    Level = 20,
                    Values = new List<double> { 100 },
                    StatInterpolation = new List<int> { 2 },
                    LevelRequirement = 70,
                },
            });

        // At level 1, actorLevel = 1, prevReq = 1, nextReq = 70
        // prevStat = 10, nextStat = 100
        // value = round(10 + (100 - 10) * (1 - 1) / (70 - 1)) = round(10) = 10
        var stats = SkillInstanceStats.Build(MakeGem(1), skill);
        Assert.Equal(10, stats["mana_cost"]);
    }

    [Fact]
    public void EmptySkill_ReturnsEmptyStats()
    {
        var skill = MakeSkill();
        var stats = SkillInstanceStats.Build(MakeGem(1), skill);
        Assert.Empty(stats);
    }
}
