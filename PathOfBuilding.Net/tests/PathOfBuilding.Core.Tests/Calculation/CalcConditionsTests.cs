using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcConditionsTests
{
    private static Actor CreateActor(bool modeCombat = true, bool modeEffective = true)
    {
        var actor = new Actor();
        var enemy = new Actor();
        actor.Enemy = enemy;
        enemy.Enemy = actor;
        actor.Config.ModeCombat = modeCombat;
        actor.Config.ModeEffective = modeEffective;
        CalcSetup.InitModDB(actor.ModDB);
        return actor;
    }

    // ─── Combat conditions from config ───

    [Fact]
    public void CombatConditions_AttackedRecently_SetsCondition()
    {
        var actor = CreateActor();
        actor.Config.AttackedRecently = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["AttackedRecently"]);
    }

    [Fact]
    public void CombatConditions_CastSpellRecently_SetsCondition()
    {
        var actor = CreateActor();
        actor.Config.CastSpellRecently = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["CastSpellRecently"]);
    }

    [Fact]
    public void CombatConditions_UsedMovementSkillRecently_SetsCondition()
    {
        var actor = CreateActor();
        actor.Config.UsedMovementSkillRecently = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["UsedMovementSkillRecently"]);
    }

    [Fact]
    public void CombatConditions_Channelling_SetsCondition()
    {
        var actor = CreateActor();
        actor.Config.Channelling = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["Channelling"]);
    }

    [Fact]
    public void CombatConditions_HitRecently_SetsCondition()
    {
        var actor = CreateActor();
        actor.Config.HitRecently = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["HitRecently"]);
    }

    [Fact]
    public void CombatConditions_HaveTotem_SetsCondition()
    {
        var actor = CreateActor();
        actor.Config.HaveTotem = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["HaveTotem"]);
    }

    [Fact]
    public void CombatConditions_DetonatedMines_SetsCondition()
    {
        var actor = CreateActor();
        actor.Config.DetonatedMinesRecently = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["DetonatedMinesRecently"]);
    }

    [Fact]
    public void CombatConditions_TriggeredTraps_SetsCondition()
    {
        var actor = CreateActor();
        actor.Config.TriggeredTrapsRecently = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["TriggeredTrapsRecently"]);
    }

    // ─── Ailment infliction conditions ───

    [Fact]
    public void CombatConditions_EnemyScorchChance_SetsCanInflictScorch()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("EnemyScorchChance", ModType.Base, 10, "Test");
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["CanInflictScorch"]);
    }

    [Fact]
    public void CombatConditions_IgniteCanScorch_SetsCanInflictScorch()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("IgniteCanScorch", ModType.Flag, true, "Test");
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["CanInflictScorch"]);
    }

    [Fact]
    public void CombatConditions_EnemyBrittleChance_SetsCanInflictBrittle()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("EnemyBrittleChance", ModType.Base, 10, "Test");
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["CanInflictBrittle"]);
    }

    [Fact]
    public void CombatConditions_CritAlwaysAltAilments_SetsAllAltAilments()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("CritAlwaysAltAilments", ModType.Flag, true, "Test");
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["CanInflictScorch"]);
        Assert.True(actor.ModDB.Conditions["CanInflictBrittle"]);
        Assert.True(actor.ModDB.Conditions["CanInflictSap"]);
    }

    // ─── Exposure conditions ───

    [Fact]
    public void CombatConditions_FireExposureChance_SetsCanApply()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("FireExposureChance", ModType.Base, 10, "Test");
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["CanApplyFireExposure"]);
    }

    [Fact]
    public void CombatConditions_ColdExposureChance_SetsCanApply()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("ColdExposureChance", ModType.Base, 10, "Test");
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["CanApplyColdExposure"]);
    }

    [Fact]
    public void CombatConditions_LightningExposureChance_SetsCanApply()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("LightningExposureChance", ModType.Base, 10, "Test");
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["CanApplyLightningExposure"]);
    }

    [Fact]
    public void CombatConditions_ModeEffectiveFalse_SkipsExposure()
    {
        var actor = CreateActor(modeEffective: false);
        actor.ModDB.NewMod("FireExposureChance", ModType.Base, 10, "Test");
        CalcConditions.CombatConditions(actor);
        Assert.False(actor.ModDB.Conditions.ContainsKey("CanApplyFireExposure"));
    }

    // ─── ModeCombat gate ───

    [Fact]
    public void CombatConditions_ModeCombatFalse_SkipsAll()
    {
        var actor = CreateActor(modeCombat: false);
        actor.Config.AttackedRecently = true;
        CalcConditions.CombatConditions(actor);
        Assert.False(actor.ModDB.Conditions.ContainsKey("AttackedRecently"));
    }

    // ─── Shrine Buffs ───

    [Fact]
    public void ShrineBuffs_AccelerationShrine_AddsActionSpeedAndProjectileSpeed()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("AccelerationShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "ActionSpeed") >= 50);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "ProjectileSpeed") >= 80);
    }

    [Fact]
    public void ShrineBuffs_BrutalShrine_AddsDamageAndStunDuration()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("BrutalShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "Damage") >= 50);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "EnemyStunDuration") >= 30);
    }

    [Fact]
    public void ShrineBuffs_DiamondShrine_OverridesCritChance()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("DiamondShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.Equal(100, actor.ModDB.Override(null, "CritChance")?.AsNumber());
    }

    [Fact]
    public void ShrineBuffs_DivineShrine_AddsDamageTakenMore()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("DivineShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.More(null, "DamageTaken") < 1);
    }

    [Fact]
    public void ShrineBuffs_EchoingShrine_AddsSpeedMore()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("EchoingShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Base, null, "RepeatCount") >= 1);
    }

    [Fact]
    public void ShrineBuffs_GloomShrine_AddsNonChaosDamageGainAsChaos()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("GloomShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Base, null, "NonChaosDamageGainAsChaos") >= 10);
    }

    [Fact]
    public void ShrineBuffs_ImpenetrableShrine_AddsDefences()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("ImpenetrableShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "Armour") >= 100);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "Evasion") >= 100);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "EnergyShield") >= 100);
    }

    [Fact]
    public void ShrineBuffs_MassiveShrine_AddsLifeAndAreaOfEffect()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("MassiveShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "Life") >= 40);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "AreaOfEffect") >= 40);
    }

    [Fact]
    public void ShrineBuffs_ReplenishingShrine_AddsRegenPercent()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("ReplenishingShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Base, null, "ManaRegenPercent") > 0);
        Assert.True(actor.ModDB.Sum(ModType.Base, null, "LifeRegenPercent") > 0);
    }

    [Fact]
    public void ShrineBuffs_ResistanceShrine_AddsElementalResistAndMax()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("ResistanceShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Base, null, "ElementalResist") >= 50);
        Assert.True(actor.ModDB.Sum(ModType.Base, null, "ElementalResistMax") >= 10);
    }

    [Fact]
    public void ShrineBuffs_ResonatingShrine_AddsChargeScaledMods()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("ResonatingShrine", ModType.Flag, true, "Test");
        // Give some charges for multiplier tags to matter
        actor.ModDB.Multipliers["PowerCharge"] = 3;
        CalcConditions.ShrineBuffs(actor);
        // CritChance INC should have been added with multiplier tag
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "CritChance") > 0);
    }

    [Fact]
    public void ShrineBuffs_BuffEffectScaling_IncreasesValues()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("MassiveShrine", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("BuffEffectOnSelf", ModType.Inc, 50, "Test");
        CalcConditions.ShrineBuffs(actor);
        // With 50% increased buff effect, 40 * 1.5 = 60
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "Life") >= 60);
    }

    [Fact]
    public void ShrineBuffs_ShrineBuffEffectScaling()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("MassiveShrine", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("ShrineBuffEffect", ModType.Inc, 100, "Test");
        CalcConditions.ShrineBuffs(actor);
        // With 100% increased shrine buff effect, 40 * 2 = 80
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "Life") >= 80);
    }

    [Fact]
    public void ShrineBuffs_LesserAcceleration_OverriddenByFull()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("AccelerationShrine", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("LesserAccelerationShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        // Should get 50 (full), not 50+10 (full+lesser)
        Assert.Equal(50, actor.ModDB.Sum(ModType.Inc, null, "ActionSpeed"));
    }

    [Fact]
    public void ShrineBuffs_NoFlags_NoMods()
    {
        var actor = CreateActor();
        double lifeBefore = actor.ModDB.Sum(ModType.Inc, null, "Life");
        CalcConditions.ShrineBuffs(actor);
        Assert.Equal(lifeBefore, actor.ModDB.Sum(ModType.Inc, null, "Life"));
    }

    [Fact]
    public void ShrineBuffs_MultipleActive_AllApply()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("MassiveShrine", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("DiamondShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "Life") >= 40);
        Assert.Equal(100, actor.ModDB.Override(null, "CritChance")?.AsNumber());
    }

    [Fact]
    public void ShrineBuffs_ModeCombatFalse_SkipsAll()
    {
        var actor = CreateActor(modeCombat: false);
        actor.ModDB.NewMod("MassiveShrine", ModType.Flag, true, "Test");
        double lifeBefore = actor.ModDB.Sum(ModType.Inc, null, "Life");
        CalcConditions.ShrineBuffs(actor);
        Assert.Equal(lifeBefore, actor.ModDB.Sum(ModType.Inc, null, "Life"));
    }

    [Fact]
    public void ShrineBuffs_LesserMassiveShrine_Works()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("LesserMassiveShrine", ModType.Flag, true, "Test");
        CalcConditions.ShrineBuffs(actor);
        Assert.True(actor.ModDB.Sum(ModType.Inc, null, "Life") >= 20);
    }
}
