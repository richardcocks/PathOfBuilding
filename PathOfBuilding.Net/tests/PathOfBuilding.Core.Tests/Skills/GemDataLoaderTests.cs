using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Tests.Skills;

public class GemDataLoaderTests
{
    // --- Synthetic tests ---

    [Fact]
    public void LoadFromText_SingleGem()
    {
        var lua = """
            return {
                ["Metadata/Items/Gems/TestGem"] = {
                    name = "Test Gem",
                    baseTypeName = "Test Gem",
                    gameId = "Metadata/Items/Gems/TestGem",
                    variantId = "TestGem",
                    grantedEffectId = "TestGem",
                    tags = { intelligence = true, spell = true },
                    tagString = "Spell",
                    reqStr = 0,
                    reqDex = 0,
                    reqInt = 100,
                    naturalMaxLevel = 20,
                },
            }
        """;

        var gems = GemDataLoader.LoadFromText(lua);
        Assert.Single(gems);

        var gem = gems["Metadata/Items/Gems/TestGem"];
        Assert.Equal("Test Gem", gem.Name);
        Assert.Equal("Test Gem", gem.BaseTypeName);
        Assert.Equal("TestGem", gem.GrantedEffectId);
        Assert.Equal("TestGem", gem.VariantId);
        Assert.Equal(100, gem.ReqInt);
        Assert.Equal(0, gem.ReqStr);
        Assert.True(gem.Tags.ContainsKey("intelligence"));
        Assert.True(gem.Tags.ContainsKey("spell"));
        Assert.Equal("Spell", gem.TagString);
        Assert.Equal(20, gem.NaturalMaxLevel);
        Assert.False(gem.VaalGem);
    }

    [Fact]
    public void LoadFromText_VaalGem()
    {
        var lua = """
            return {
                ["Metadata/Items/Gems/VaalTestGem"] = {
                    name = "Vaal Test Gem",
                    baseTypeName = "Vaal Test Gem",
                    gameId = "Metadata/Items/Gems/VaalTestGem",
                    variantId = "VaalTestGem",
                    grantedEffectId = "VaalTestGem",
                    secondaryGrantedEffectId = "TestGem",
                    vaalGem = true,
                    tags = { vaal = true },
                    reqStr = 0,
                    reqDex = 0,
                    reqInt = 100,
                },
            }
        """;

        var gems = GemDataLoader.LoadFromText(lua);
        var gem = gems["Metadata/Items/Gems/VaalTestGem"];
        Assert.True(gem.VaalGem);
        Assert.Equal("TestGem", gem.SecondaryGrantedEffectId);
    }

    [Fact]
    public void LoadFromText_MultipleGems()
    {
        var lua = """
            return {
                ["Gem1"] = { name = "First", gameId = "Gem1", variantId = "G1", grantedEffectId = "G1" },
                ["Gem2"] = { name = "Second", gameId = "Gem2", variantId = "G2", grantedEffectId = "G2" },
                ["Gem3"] = { name = "Third", gameId = "Gem3", variantId = "G3", grantedEffectId = "G3" },
            }
        """;

        var gems = GemDataLoader.LoadFromText(lua);
        Assert.Equal(3, gems.Count);
    }

    [Fact]
    public void LoadFromText_DefaultNaturalMaxLevel()
    {
        var lua = """
            return {
                ["Gem1"] = { name = "NoMax", gameId = "Gem1", variantId = "G1", grantedEffectId = "G1" },
            }
        """;

        var gems = GemDataLoader.LoadFromText(lua);
        Assert.Equal(20, gems["Gem1"].NaturalMaxLevel);
    }

    // --- Real file integration tests ---

    [Fact]
    public void LoadFromFile_RealGemsLua_CountExceeds700()
    {
        var path = TestDataHelper.GetGemsLuaPath();
        if (!File.Exists(path))
            return; // Skip if not available

        var gems = GemDataLoader.LoadFromFile(path);
        Assert.True(gems.Count > 700, $"Expected > 700 gems, got {gems.Count}");
    }

    [Fact]
    public void LoadFromFile_RealGemsLua_FireballFields()
    {
        var path = TestDataHelper.GetGemsLuaPath();
        if (!File.Exists(path))
            return;

        var gems = GemDataLoader.LoadFromFile(path);
        var fireball = gems["Metadata/Items/Gems/SkillGemFireball"];

        Assert.Equal("Fireball", fireball.Name);
        Assert.Equal("Fireball", fireball.BaseTypeName);
        Assert.Equal("Fireball", fireball.GrantedEffectId);
        Assert.Equal("Fireball", fireball.VariantId);
        Assert.Equal(100, fireball.ReqInt);
        Assert.Equal(0, fireball.ReqStr);
        Assert.Equal(0, fireball.ReqDex);
        Assert.False(fireball.VaalGem);
        Assert.True(fireball.Tags.ContainsKey("intelligence"));
        Assert.True(fireball.Tags.ContainsKey("spell"));
        Assert.True(fireball.Tags.ContainsKey("fire"));
    }

    [Fact]
    public void LoadFromFile_RealGemsLua_VaalFireball()
    {
        var path = TestDataHelper.GetGemsLuaPath();
        if (!File.Exists(path))
            return;

        var gems = GemDataLoader.LoadFromFile(path);
        var vaal = gems["Metadata/Items/Gems/SkillGemVaalFireball"];

        Assert.True(vaal.VaalGem);
        Assert.Equal("Fireball", vaal.SecondaryGrantedEffectId);
        Assert.Equal("VaalFireball", vaal.GrantedEffectId);
    }

    [Fact]
    public void LoadFromFile_RealGemsLua_AllHaveGrantedEffectId()
    {
        var path = TestDataHelper.GetGemsLuaPath();
        if (!File.Exists(path))
            return;

        var gems = GemDataLoader.LoadFromFile(path);
        foreach (var (id, gem) in gems)
        {
            Assert.False(string.IsNullOrEmpty(gem.GrantedEffectId),
                $"Gem {id} has empty GrantedEffectId");
        }
    }
}
