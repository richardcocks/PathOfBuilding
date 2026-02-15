using PathOfBuilding.Core.Config;

namespace PathOfBuilding.Core.Tests.Config;

public class ConfigVarRegistryTests
{
    [Fact]
    public void Find_KnownVar_ConditionMoving()
    {
        var handler = ConfigVarRegistry.Find("conditionMoving");
        Assert.NotNull(handler);
        Assert.Equal("conditionMoving", handler.VarName);
        Assert.Equal(ConfigPattern.ConditionFlag, handler.Pattern);
        Assert.Equal("Moving", handler.ModName);
    }

    [Fact]
    public void Find_KnownVar_BuffOnslaught()
    {
        var handler = ConfigVarRegistry.Find("buffOnslaught");
        Assert.NotNull(handler);
        Assert.Equal(ConfigPattern.ConditionFlag, handler.Pattern);
        Assert.True(handler.HasCombatTag);
    }

    [Fact]
    public void Find_KnownVar_EnemyIsBoss()
    {
        var handler = ConfigVarRegistry.Find("enemyIsBoss");
        Assert.NotNull(handler);
        Assert.Equal(ConfigPattern.Custom, handler.Pattern);
        Assert.NotNull(handler.CustomApply);
    }

    [Fact]
    public void Find_KnownVar_MultiplierRageStack()
    {
        var handler = ConfigVarRegistry.Find("multiplierRageStack");
        Assert.NotNull(handler);
        Assert.Equal(ConfigPattern.Multiplier, handler.Pattern);
        Assert.Equal("RageStack", handler.ModName);
        Assert.True(handler.HasCombatTag);
    }

    [Fact]
    public void Find_EnemyCondition_HasEffectiveTag()
    {
        var handler = ConfigVarRegistry.Find("conditionEnemyMoving");
        Assert.NotNull(handler);
        Assert.Equal(ConfigTarget.Enemy, handler.Target);
        Assert.True(handler.HasEffectiveTag);
    }

    [Fact]
    public void Find_UnknownVar_ReturnsNull()
    {
        Assert.Null(ConfigVarRegistry.Find("nonexistentVar"));
    }

    [Fact]
    public void Registry_HasAtLeast100Vars()
    {
        Assert.True(ConfigVarRegistry.Count >= 100, $"Registry has {ConfigVarRegistry.Count} vars, expected >= 100");
    }

    [Fact]
    public void Registry_NoDuplicateVarNames()
    {
        var names = ConfigVarRegistry.AllVarNames.ToList();
        var distinct = names.Distinct().ToList();
        Assert.Equal(distinct.Count, names.Count);
    }

    [Fact]
    public void Find_EnemyOverride_HasCorrectType()
    {
        var handler = ConfigVarRegistry.Find("enemyPhysicalDamage");
        Assert.NotNull(handler);
        Assert.Equal(ConfigPattern.Override, handler.Pattern);
        Assert.Equal(ConfigTarget.Enemy, handler.Target);
    }

    [Fact]
    public void Find_CritRecently_HasImpliedConditions()
    {
        var handler = ConfigVarRegistry.Find("conditionCritRecently");
        Assert.NotNull(handler);
        Assert.NotNull(handler.ImpliedConditions);
        Assert.Contains("SkillCritRecently", handler.ImpliedConditions);
    }
}
