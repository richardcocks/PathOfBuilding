using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class SkillDataCacheTests
{
    [Fact]
    public void SetSkills_PreloadsData()
    {
        var cache = new SkillDataCache();
        var skills = new Dictionary<string, SkillDefinition>
        {
            ["TestSkill"] = new SkillDefinition { Id = "TestSkill", Name = "Test" },
        };

        cache.SetSkills(skills);

        Assert.Single(cache.Skills);
        Assert.Equal("Test", cache.Skills["TestSkill"].Name);
    }

    [Fact]
    public void SetGems_PreloadsData()
    {
        var cache = new SkillDataCache();
        var gems = new Dictionary<string, GemDefinition>
        {
            ["G1"] = new GemDefinition
            {
                Id = "G1", Name = "Gem1", GameId = "G1", VariantId = "G1", GrantedEffectId = "S1",
            },
        };

        cache.SetGems(gems);

        Assert.Single(cache.Gems);
        Assert.Equal("Gem1", cache.Gems["G1"].Name);
    }

    [Fact]
    public void GetSkillById_ReturnsNull_WhenNotFound()
    {
        var cache = new SkillDataCache();
        cache.SetSkills(new Dictionary<string, SkillDefinition>());

        Assert.Null(cache.GetSkillById("nonexistent"));
    }

    [Fact]
    public void GetSkillForGem_LinksCorrectly()
    {
        var cache = new SkillDataCache();
        cache.SetSkills(new Dictionary<string, SkillDefinition>
        {
            ["Fireball"] = new SkillDefinition { Id = "Fireball", Name = "Fireball" },
        });
        cache.SetGems(new Dictionary<string, GemDefinition>
        {
            ["Gem1"] = new GemDefinition
            {
                Id = "Gem1", Name = "Fireball Gem", GameId = "Gem1",
                VariantId = "Fireball", GrantedEffectId = "Fireball",
            },
        });

        var gem = cache.GetGemById("Gem1")!;
        var skill = cache.GetSkillForGem(gem);
        Assert.NotNull(skill);
        Assert.Equal("Fireball", skill.Name);
    }

    [Fact]
    public void Skills_ThrowsWhenNoPathAndNotPreloaded()
    {
        var cache = new SkillDataCache();
        Assert.Throws<InvalidOperationException>(() => _ = cache.Skills);
    }

    [Fact]
    public void LazyLoading_FromRealFiles()
    {
        string dataPath;
        try
        {
            dataPath = TestDataHelper.GetDataBasePath();
        }
        catch (DirectoryNotFoundException)
        {
            return; // Skip if not available
        }

        var cache = new SkillDataCache { DataBasePath = dataPath };

        // First access triggers lazy loading
        Assert.True(cache.Gems.Count > 700);
        Assert.True(cache.Skills.Count > 200);
        Assert.True(cache.SkillStatMap.Count > 100);
    }

    [Fact]
    public void SetSkillStatMap_PreloadsData()
    {
        var cache = new SkillDataCache();
        cache.SetSkillStatMap(new Dictionary<string, StatMapEntry>
        {
            ["test_stat"] = new StatMapEntry { Div = 1000 },
        });

        Assert.Single(cache.SkillStatMap);
        Assert.Equal(1000.0, cache.SkillStatMap["test_stat"].Div);
    }
}
