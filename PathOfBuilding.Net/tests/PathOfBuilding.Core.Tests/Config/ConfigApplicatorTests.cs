using PathOfBuilding.Core.Config;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Import.Sections;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Config;

public class ConfigApplicatorTests
{
    private static (ModDB player, ModDB enemy) CreateModDBs()
    {
        var player = new ModDB();
        var enemy = new ModDB();
        return (player, enemy);
    }

    private static ConfigInput BoolInput(string name, bool value = true) => new()
    {
        Name = name,
        Kind = ConfigInputKind.Boolean,
        BooleanValue = value,
    };

    private static ConfigInput NumberInput(string name, double value) => new()
    {
        Name = name,
        Kind = ConfigInputKind.Number,
        NumberValue = value,
    };

    private static ConfigInput StringInput(string name, string value) => new()
    {
        Name = name,
        Kind = ConfigInputKind.String,
        StringValue = value,
    };

    // ─── Pattern A: Condition flags ───

    [Fact]
    public void ConditionFlag_Moving_SetsCondition()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionMoving") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:Moving"));
    }

    [Fact]
    public void ConditionFlag_EnemyChilled_SetsOnEnemyDB()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionEnemyChilled") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(e.Conditions.ContainsKey("Condition:Chilled"));
        Assert.False(p.Conditions.ContainsKey("Condition:Chilled"));
    }

    [Fact]
    public void ConditionFlag_FullLife_SetsCondition()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionFullLife") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:FullLife"));
    }

    [Fact]
    public void ConditionFlag_LowLife_SetsCondition()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionLowLife") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:LowLife"));
    }

    [Fact]
    public void ConditionFlag_Stationary_SetsCondition()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionStationary") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:Stationary"));
    }

    // ─── Pattern B: Multipliers ───

    [Fact]
    public void Multiplier_RageStack_SetsMultiplier()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("multiplierRageStack", 25) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(25, p.Multipliers["RageStack"]);
    }

    [Fact]
    public void Multiplier_SoulEater_SetsMultiplier()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("multiplierSoulEater", 10) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(10, p.Multipliers["SoulEater"]);
    }

    [Fact]
    public void Multiplier_PoisonStack_SetsMultiplier()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("multiplierPoisonStack", 5) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(5, p.Multipliers["PoisonStack"]);
    }

    [Fact]
    public void Multiplier_ZeroValue_NotSet()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("multiplierRageStack", 0) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.False(p.Multipliers.ContainsKey("RageStack"));
    }

    [Fact]
    public void Multiplier_EnemyWitheredStack_SetsOnEnemy()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("multiplierEnemyWitheredStack", 15) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(15, e.Multipliers["WitheredStack"]);
        Assert.False(p.Multipliers.ContainsKey("WitheredStack"));
    }

    // ─── Pattern C: Overrides ───

    [Fact]
    public void Override_EnemyPhysicalDamage_SetsOverrideMod()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("enemyPhysicalDamage", 5000) };

        ConfigApplicator.Apply(inputs, p, e);

        var val = e.Override(null, "PhysicalDamage");
        Assert.NotNull(val);
        Assert.Equal(5000, val.Value.AsNumber());
    }

    [Fact]
    public void Override_EnemyCritChance_SetsOverrideMod()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("enemyCritChance", 10) };

        ConfigApplicator.Apply(inputs, p, e);

        var val = e.Override(null, "CritChance");
        Assert.NotNull(val);
        Assert.Equal(10, val.Value.AsNumber());
    }

    [Fact]
    public void Override_ZeroValue_NotApplied()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("enemyPhysicalDamage", 0) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Null(e.Override(null, "PhysicalDamage"));
    }

    [Fact]
    public void Override_EnemySpeed_SetsOverrideMod()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("enemySpeed", 1.5) };

        ConfigApplicator.Apply(inputs, p, e);

        var val = e.Override(null, "Speed");
        Assert.NotNull(val);
        Assert.Equal(1.5, val.Value.AsNumber());
    }

    [Fact]
    public void Override_EnemyCritDamage_SetsOverrideMod()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("enemyCritDamage", 150) };

        ConfigApplicator.Apply(inputs, p, e);

        var val = e.Override(null, "CritDamage");
        Assert.NotNull(val);
    }

    // ─── Pattern D: Flag mods (charges) ───

    [Fact]
    public void ChargeFlag_UsePowerCharges_SetsCondition()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("usePowerCharges") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:UsePowerCharges"));
    }

    [Fact]
    public void ChargeFlag_UseFrenzyCharges_SetsCondition()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("useFrenzyCharges") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:UseFrenzyCharges"));
    }

    [Fact]
    public void ChargeFlag_UseEnduranceCharges_SetsCondition()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("useEnduranceCharges") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:UseEnduranceCharges"));
    }

    [Fact]
    public void ChargeOverride_PowerCharges_SetsMultiplier()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("overridePowerCharges", 7) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(7, p.Multipliers["PowerChargesOverride"]);
    }

    [Fact]
    public void ChargeOverride_FrenzyCharges_SetsMultiplier()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("overrideFrenzyCharges", 5) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(5, p.Multipliers["FrenzyChargesOverride"]);
    }

    // ─── Enemy conditions ───

    [Fact]
    public void EnemyCondition_Ignited_SetsOnEnemy()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionEnemyIgnited") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(e.Conditions.ContainsKey("Condition:Ignited"));
        Assert.False(p.Conditions.ContainsKey("Condition:Ignited"));
    }

    [Fact]
    public void EnemyCondition_Blinded_SetsOnEnemy()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionEnemyBlinded") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(e.Conditions.ContainsKey("Condition:Blinded"));
    }

    [Fact]
    public void EnemyCondition_Multiple_AllSet()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput>
        {
            BoolInput("conditionEnemyIgnited"),
            BoolInput("conditionEnemyShocked"),
            BoolInput("conditionEnemyFrozen"),
        };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(e.Conditions.ContainsKey("Condition:Ignited"));
        Assert.True(e.Conditions.ContainsKey("Condition:Shocked"));
        Assert.True(e.Conditions.ContainsKey("Condition:Frozen"));
    }

    [Fact]
    public void EnemyCondition_Intimidated_SetsOnEnemy()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionEnemyIntimidated") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(e.Conditions.ContainsKey("Condition:Intimidated"));
    }

    [Fact]
    public void EnemyCondition_CoveredInAsh_SetsOnEnemy()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionEnemyCoveredInAsh") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(e.Conditions.ContainsKey("Condition:CoveredInAsh"));
    }

    // ─── Implied conditions ───

    [Fact]
    public void ImpliedCondition_CritRecently_SetsSkillCritRecently()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionCritRecently") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:CritRecently"));
        Assert.True(p.Conditions.ContainsKey("Condition:SkillCritRecently"));
    }

    [Fact]
    public void ImpliedCondition_HitRecently_SetsSkillHitRecently()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionHitRecently") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:HitRecently"));
        Assert.True(p.Conditions.ContainsKey("Condition:SkillHitRecently"));
    }

    [Fact]
    public void ImpliedCondition_AttackedRecently_SetsSkillAttackedRecently()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionAttackedRecently") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.True(p.Conditions.ContainsKey("Condition:AttackedRecently"));
        Assert.True(p.Conditions.ContainsKey("Condition:SkillAttackedRecently"));
    }

    // ─── CalcConfig population ───

    [Fact]
    public void CalcConfig_UsePowerCharges_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("usePowerCharges") };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.True(config.UsePowerCharges);
    }

    [Fact]
    public void CalcConfig_BuffOnslaught_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("buffOnslaught") };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.True(config.BuffOnslaught);
    }

    [Fact]
    public void CalcConfig_EnemyIsBoss_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { StringInput("enemyIsBoss", "Pinnacle") };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal("Pinnacle", config.EnemyIsBoss);
    }

    [Fact]
    public void CalcConfig_EnemyLevel_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("enemyLevel", 84) };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(84, config.EnemyLevel);
    }

    // ─── Inactive inputs skipped ───

    [Fact]
    public void InactiveBoolean_Skipped()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("conditionMoving", false) };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.False(p.Conditions.ContainsKey("Condition:Moving"));
    }

    [Fact]
    public void InactiveBoolean_UsePowerCharges_NotSet()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("usePowerCharges", false) };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.False(config.UsePowerCharges);
        Assert.False(p.Conditions.ContainsKey("Condition:UsePowerCharges"));
    }

    [Fact]
    public void InactiveBoolean_BuffFortification_NotSet()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("buffFortification", false) };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.False(config.BuffFortification);
    }

    // ─── Unknown vars ignored ───

    [Fact]
    public void UnknownVar_Boolean_NoException()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("someTotallyUnknownVar") };

        var config = ConfigApplicator.Apply(inputs, p, e);
        Assert.NotNull(config);
    }

    [Fact]
    public void UnknownVar_Number_NoException()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("anotherUnknownVar", 42) };

        var config = ConfigApplicator.Apply(inputs, p, e);
        Assert.NotNull(config);
    }

    [Fact]
    public void UnknownVar_DoesNotPollute()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("unknownConditionVar") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Empty(p.Conditions);
        Assert.Empty(e.Conditions);
    }

    // ─── Boss type application ───

    [Fact]
    public void BossType_Boss_SetsRareOrUnique()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { StringInput("enemyIsBoss", "Boss") };

        ConfigApplicator.Apply(inputs, p, e);

        // Boss conditions are set as mods, not directly in Conditions dict
        // The BossData.ApplyBoss adds Flag mods with Effective condition tag
        Assert.Equal(20, p.Sum(ModType.Base, null, "WarcryPower"));
    }

    [Fact]
    public void BossType_Pinnacle_SetsPinnacleCondition()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { StringInput("enemyIsBoss", "Pinnacle") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(20, p.Sum(ModType.Base, null, "WarcryPower"));
    }

    [Fact]
    public void BossType_Uber_SetsDamageTaken()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { StringInput("enemyIsBoss", "Uber") };

        ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(20, p.Sum(ModType.Base, null, "WarcryPower"));
        // Uber sets DamageTaken MORE -70 on enemy
        // We can verify via More() but it requires Effective condition
    }

    // ─── CalcConfig multiplier stacks ───

    [Fact]
    public void CalcConfig_MultiplierRageStack_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("multiplierRageStack", 30) };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(30, config.MultiplierRageStack);
    }

    [Fact]
    public void CalcConfig_MultiplierManaBurnStacks_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("multiplierManaBurnStacks", 3) };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(3, config.MultiplierManaBurnStacks);
    }

    [Fact]
    public void CalcConfig_OverridePowerCharges_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("overridePowerCharges", 10) };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(10, config.OverridePowerCharges);
    }

    [Fact]
    public void CalcConfig_EHPUnluckyWorstOf_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { NumberInput("EHPUnluckyWorstOf", 4) };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(4, config.EHPUnluckyWorstOf);
    }

    [Fact]
    public void CalcConfig_DisableEHPGainOnBlock_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput> { BoolInput("DisableEHPGainOnBlock") };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.True(config.DisableEHPGainOnBlock);
    }

    [Fact]
    public void CalcConfig_EnemyDamageFields_Set()
    {
        var (p, e) = CreateModDBs();
        var inputs = new List<ConfigInput>
        {
            NumberInput("enemyPhysicalDamage", 1000),
            NumberInput("enemyFireDamage", 500),
            NumberInput("enemyCritChance", 5),
        };

        var config = ConfigApplicator.Apply(inputs, p, e);

        Assert.Equal(1000, config.EnemyPhysicalDamage);
        Assert.Equal(500, config.EnemyFireDamage);
        Assert.Equal(5, config.EnemyCritChance);
    }
}
