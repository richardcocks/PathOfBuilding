using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcDefenceTests
{
    private static Actor CreateActorWithEnemy()
    {
        var actor = new Actor();
        var enemy = new Actor();
        actor.Enemy = enemy;
        enemy.Enemy = actor;
        return actor;
    }

    private static Actor CreateFullPipelineActor()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Life", ModType.Base, 5000, "Test");
        actor.ModDB.NewMod("Mana", ModType.Base, 1000, "Test");
        actor.ModDB.NewMod("EnergyShield", ModType.Base, 2000, "Test");
        CalcPerform.DoActorAttributes(actor);
        CalcPerform.DoActorLifeMana(actor);
        return actor;
    }

    // ─── HitChance formula ───

    [Fact]
    public void HitChance_HighAccuracy_Returns100()
    {
        Assert.Equal(100, CalcDefence.HitChance(100, 10000));
    }

    [Fact]
    public void HitChance_NegativeAccuracy_Returns5()
    {
        Assert.Equal(5, CalcDefence.HitChance(100, -1));
    }

    [Fact]
    public void HitChance_ZeroEvasion_Returns100()
    {
        Assert.Equal(100, CalcDefence.HitChance(0, 100));
    }

    [Fact]
    public void HitChance_EqualEvasionAndAccuracy()
    {
        double result = CalcDefence.HitChance(1000, 1000);
        Assert.True(result > 5 && result <= 100);
    }

    [Fact]
    public void HitChance_MinimumIs5()
    {
        Assert.Equal(5, CalcDefence.HitChance(100000, 1));
    }

    // ─── ArmourReduction formula ───

    [Fact]
    public void ArmourReduction_ZeroArmourAndRaw_ReturnsZero()
    {
        Assert.Equal(0, CalcDefence.ArmourReductionF(0, 0));
    }

    [Fact]
    public void ArmourReduction_HighArmour_HighReduction()
    {
        Assert.Equal(66.67, CalcDefence.ArmourReductionF(10000, 1000), 1);
    }

    [Fact]
    public void ArmourReduction_LowArmour_LowReduction()
    {
        Assert.Equal(1.96, CalcDefence.ArmourReductionF(100, 1000), 1);
    }

    [Fact]
    public void ArmourReduction_RoundedVersion()
    {
        Assert.Equal(67, CalcDefence.ArmourReduction(10000, 1000));
    }

    // ─── Resistances ───

    [Fact]
    public void Resistances_BaseFireResist()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("FireResist", ModType.Base, 30, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(30, actor.Output["FireResist"]);
    }

    [Fact]
    public void Resistances_CappedAt75()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("FireResist", ModType.Base, 100, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(75, actor.Output["FireResist"]);
        Assert.Equal(25, actor.Output["FireResistOverCap"]);
    }

    [Fact]
    public void Resistances_NegativeResist_FlooredAtMinus200()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("FireResist", ModType.Base, -250, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(-200, actor.Output["FireResist"]);
    }

    [Fact]
    public void Resistances_IncreasedMaxResist()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("FireResist", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("FireResistMax", ModType.Base, 5, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(80, actor.Output["FireResist"]);
    }

    [Fact]
    public void Resistances_MaxResistCappedAt90()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("FireResist", ModType.Base, 200, "Test");
        actor.ModDB.NewMod("FireResistMax", ModType.Base, 50, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(90, actor.Output["FireResist"]);
    }

    [Fact]
    public void Resistances_ChaosResist_Independent()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("ChaosResist", ModType.Base, 40, "Test");
        actor.ModDB.NewMod("FireResist", ModType.Base, 50, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(40, actor.Output["ChaosResist"]);
        Assert.Equal(50, actor.Output["FireResist"]);
    }

    [Fact]
    public void Resistances_ElementalResist_AppliestoAllElemental()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("ElementalResist", ModType.Base, 30, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(30, actor.Output["FireResist"]);
        Assert.Equal(30, actor.Output["ColdResist"]);
        Assert.Equal(30, actor.Output["LightningResist"]);
        Assert.Equal(0, actor.Output["ChaosResist"]);
    }

    [Fact]
    public void Resistances_MeldingOfTheFlesh()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("FireResistMax", ModType.Base, 5, "Melding");
        actor.ModDB.NewMod("ElementalResistMaxIsHighestResistMax", ModType.Flag, true, "Melding");
        actor.ModDB.NewMod("FireResist", ModType.Base, 85, "Test");
        actor.ModDB.NewMod("ColdResist", ModType.Base, 85, "Test");
        actor.ModDB.NewMod("LightningResist", ModType.Base, 85, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(80, actor.Output["FireResist"]);
        Assert.Equal(80, actor.Output["ColdResist"]);
        Assert.Equal(80, actor.Output["LightningResist"]);
    }

    [Fact]
    public void Resistances_OverCap_Tracking()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("ColdResist", ModType.Base, 100, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(75, actor.Output["ColdResist"]);
        Assert.Equal(25, actor.Output["ColdResistOverCap"]);
        Assert.Equal(0, actor.Output["MissingColdResist"]);
    }

    [Fact]
    public void Resistances_MissingResist_Tracking()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("ColdResist", ModType.Base, 50, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(50, actor.Output["ColdResist"]);
        Assert.Equal(25, actor.Output["MissingColdResist"]);
    }

    [Fact]
    public void Resistances_ResistOver75()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("FireResist", ModType.Base, 80, "Test");
        actor.ModDB.NewMod("FireResistMax", ModType.Base, 5, "Test");
        CalcDefence.Resistances(actor);
        Assert.Equal(80, actor.Output["FireResist"]);
        Assert.Equal(5, actor.Output["FireResistOver75"]);
    }

    // ─── Block ───

    [Fact]
    public void Defence_BaseBlockChance()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("BlockChance", ModType.Base, 30, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(30, actor.Output["BlockChance"]);
    }

    [Fact]
    public void Defence_BlockChance_Capped()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("BlockChance", ModType.Base, 100, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(75, actor.Output["BlockChance"]);
        Assert.Equal(25, actor.Output["BlockChanceOverCap"]);
    }

    [Fact]
    public void Defence_SpellBlockChance()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("SpellBlockChance", ModType.Base, 40, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(40, actor.Output["SpellBlockChance"]);
    }

    [Fact]
    public void Defence_CannotBlockAttacks()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("BlockChance", ModType.Base, 50, "Test");
        actor.ModDB.NewMod("CannotBlockAttacks", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(0, actor.Output["BlockChance"]);
    }

    // ─── Defences ───

    [Fact]
    public void Defence_BaseArmour()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Armour", ModType.Base, 1000, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1000, actor.Output["Armour"]);
    }

    [Fact]
    public void Defence_ArmourIncMore()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Armour", ModType.Base, 1000, "Test");
        actor.ModDB.NewMod("Armour", ModType.Inc, 100, "Test");
        actor.ModDB.NewMod("Armour", ModType.More, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(3000, actor.Output["Armour"]);
    }

    [Fact]
    public void Defence_IronReflexes_EvasionToArmour()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Evasion", ModType.Base, 500, "Test");
        actor.ModDB.NewMod("Armour", ModType.Base, 500, "Test");
        actor.ModDB.NewMod("IronReflexes", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1000, actor.Output["Armour"]);
        Assert.Equal(0, actor.Output["Evasion"]);
    }

    [Fact]
    public void Defence_BaseEvasion()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Evasion", ModType.Base, 800, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(800, actor.Output["Evasion"]);
    }

    [Fact]
    public void Defence_BaseEnergyShield()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("EnergyShield", ModType.Base, 500, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(500, actor.Output["EnergyShield"]);
    }

    [Fact]
    public void Defence_ESIncMore()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("EnergyShield", ModType.Base, 500, "Test");
        actor.ModDB.NewMod("EnergyShield", ModType.Inc, 100, "Test");
        actor.ModDB.NewMod("EnergyShield", ModType.More, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1500, actor.Output["EnergyShield"]);
    }

    [Fact]
    public void Defence_Ward()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Ward", ModType.Base, 200, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(200, actor.Output["Ward"]);
    }

    // ─── Evade ───

    [Fact]
    public void Defence_EvadeChance_FromEvasion()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Evasion", ModType.Base, 2000, "Test");
        actor.Enemy!.ModDB.NewMod("Accuracy", ModType.Base, 500, "Test");
        CalcDefence.Defence(actor);
        Assert.True(actor.Output["EvadeChance"] > 0);
    }

    [Fact]
    public void Defence_CannotEvade_Flag()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Evasion", ModType.Base, 5000, "Test");
        actor.ModDB.NewMod("CannotEvade", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(0, actor.Output["EvadeChance"]);
    }

    [Fact]
    public void Defence_EnemyCannotBeEvaded()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Evasion", ModType.Base, 5000, "Test");
        actor.Enemy!.ModDB.NewMod("CannotBeEvaded", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(0, actor.Output["EvadeChance"]);
    }

    // ─── Suppression ───

    [Fact]
    public void Defence_BaseSpellSuppression()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("SpellSuppressionChance", ModType.Base, 60, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(60, actor.Output["SpellSuppressionChance"]);
    }

    [Fact]
    public void Defence_SpellSuppressionCap_At100()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("SpellSuppressionChance", ModType.Base, 120, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["SpellSuppressionChance"]);
        Assert.Equal(20, actor.Output["SpellSuppressionChanceOverCap"]);
    }

    [Fact]
    public void Defence_SpellSuppressionEffect_Default40()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        CalcDefence.Defence(actor);
        Assert.Equal(40, actor.Output["SpellSuppressionEffect"]);
    }

    [Fact]
    public void Defence_SpellSuppressionEffect_WithBonus()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("SpellSuppressionEffect", ModType.Base, 10, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(50, actor.Output["SpellSuppressionEffect"]);
    }

    [Fact]
    public void Defence_PhysicalDamageReduction_FromArmour()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Armour", ModType.Base, 10000, "Test");
        CalcDefence.Defence(actor);
        Assert.True(actor.Output["PhysicalDamageReduction"] > 0);
        Assert.True(actor.Output["PhysicalDamageReduction"] <= MiscConstants.DamageReductionCap);
    }

    [Fact]
    public void Defence_PhysicalDamageReduction_CappedAt90()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Armour", ModType.Base, 10000000, "Test");
        actor.ModDB.NewMod("PhysicalDamageReduction", ModType.Base, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(90, actor.Output["PhysicalDamageReduction"]);
    }

    // ═══ Phase 4: Recovery Rates ═══

    [Fact]
    public void RecoveryRate_BaseLifeRecoveryRate()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        Assert.Equal(1, actor.Output["LifeRecoveryRateMod"]);
    }

    [Fact]
    public void RecoveryRate_IncreasedLifeRecoveryRate()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRecoveryRate", ModType.Inc, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1.5, actor.Output["LifeRecoveryRateMod"]);
    }

    [Fact]
    public void RecoveryRate_CannotRecoverLifeOutsideLeech_ReturnsOne()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRecoveryRate", ModType.Inc, 50, "Test");
        actor.ModDB.NewMod("CannotRecoverLifeOutsideLeech", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1, actor.Output["LifeRecoveryRateMod"]);
    }

    [Fact]
    public void RecoveryRate_ManaRecoveryRate()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("ManaRecoveryRate", ModType.Inc, 30, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1.3, actor.Output["ManaRecoveryRateMod"], 5);
    }

    [Fact]
    public void RecoveryRate_ESRecoveryRate()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("EnergyShieldRecoveryRate", ModType.Inc, 20, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1.2, actor.Output["EnergyShieldRecoveryRateMod"], 5);
    }

    // ═══ Phase 4: Leech Caps ═══

    [Fact]
    public void LeechCaps_MaxLifeLeechInstance()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        double life = actor.Output["Life"];
        double maxInstance = CalcLib.Val(actor.ModDB, "MaxLifeLeechInstance");
        Assert.Equal(life * maxInstance / 100, actor.Output["MaxLifeLeechInstance"]);
    }

    [Fact]
    public void LeechCaps_MaxLifeLeechRate()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        double life = actor.Output["Life"];
        Assert.True(actor.Output["MaxLifeLeechRate"] > 0);
        Assert.True(actor.Output["MaxLifeLeechRatePercent"] > 0);
        Assert.Equal(life * actor.Output["MaxLifeLeechRatePercent"] / 100, actor.Output["MaxLifeLeechRate"]);
    }

    [Fact]
    public void LeechCaps_ESLeech()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        Assert.True(actor.Output["MaxEnergyShieldLeechRate"] > 0);
    }

    [Fact]
    public void LeechCaps_ManaLeech()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        Assert.True(actor.Output["MaxManaLeechRate"] > 0);
    }

    // ═══ Phase 4: Regeneration ═══

    [Fact]
    public void Regen_FlatLifeRegen()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.True(actor.Output["LifeRegen"] > 0);
    }

    [Fact]
    public void Regen_PercentLifeRegen()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegenPercent", ModType.Base, 2, "Test");
        CalcDefence.Defence(actor);
        double life = actor.Output["Life"];
        // baseRegen = 0 + life * 2 / 100 = life * 0.02
        Assert.True(actor.Output["LifeRegen"] > 0);
    }

    [Fact]
    public void Regen_WithIncMore()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("LifeRegen", ModType.Inc, 50, "Test");
        actor.ModDB.NewMod("LifeRegen", ModType.More, 20, "Test");
        CalcDefence.Defence(actor);
        // 100 * (1 + 50/100) * (1 + 20/100) = 100 * 1.5 * 1.2 = 180
        Assert.Equal(180, actor.Output["LifeRegen"], 1);
    }

    [Fact]
    public void Regen_ZealotsOath_RedirectsLifeRegenToES()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("ZealotsOath", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(0, actor.Output["LifeRegen"]);
        // ES regen should have the life regen added
        Assert.True(actor.Output["EnergyShieldRegen"] > 0);
    }

    [Fact]
    public void Regen_NoLifeRegen_Flag()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("NoLifeRegen", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(0, actor.Output["LifeRegen"]);
    }

    [Fact]
    public void Regen_Degen_SubtractsFromRegen()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("LifeDegen", ModType.Base, 30, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["LifeRegen"], 1);
        Assert.True(actor.Output["LifeDegen"] > 0);
        Assert.True(actor.Output["LifeRegenRecovery"] < actor.Output["LifeRegen"]);
    }

    [Fact]
    public void Regen_NetPositive_SetsCondition()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 100, "Test");
        CalcDefence.Defence(actor);
        Assert.True(actor.Output["LifeRegenRecovery"] > 0);
        Assert.True(actor.ModDB.Flag(null, "Condition:CanGainLife"));
    }

    [Fact]
    public void Regen_ManaRegen()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("ManaRegen", ModType.Base, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.True(actor.Output["ManaRegen"] > 0);
    }

    [Fact]
    public void Regen_RegenPercent_ComputedCorrectly()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 50, "Test");
        CalcDefence.Defence(actor);
        double life = actor.Output["Life"];
        if (life > 0)
        {
            double expected = CalcLib.Round(actor.Output["LifeRegenRecovery"] / life * 100, 1);
            Assert.Equal(expected, actor.Output["LifeRegenPercent"]);
        }
    }

    // ═══ Phase 4: ES Recharge ═══

    [Fact]
    public void ESRecharge_BaseRate()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        double es = actor.Output["EnergyShield"];
        // Default: ES * 0.33 * 1 * 1 = ES * 0.33
        double expected = CalcLib.Round(es * MiscConstants.EnergyShieldRechargeBase);
        Assert.Equal(expected, actor.Output["EnergyShieldRecharge"]);
    }

    [Fact]
    public void ESRecharge_WithIncMore()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("EnergyShieldRecharge", ModType.Inc, 100, "Test");
        actor.ModDB.NewMod("EnergyShieldRecharge", ModType.More, 50, "Test");
        CalcDefence.Defence(actor);
        double es = actor.Output["EnergyShield"];
        // ES * 0.33 * (1 + 100/100) * (1 + 50/100) = ES * 0.33 * 2 * 1.5 = ES * 0.99
        double expected = CalcLib.Round(es * MiscConstants.EnergyShieldRechargeBase * 2 * 1.5);
        Assert.Equal(expected, actor.Output["EnergyShieldRecharge"]);
    }

    [Fact]
    public void ESRecharge_Delay()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        Assert.Equal(MiscConstants.EnergyShieldRechargeDelay, actor.Output["EnergyShieldRechargeDelay"]);
    }

    [Fact]
    public void ESRecharge_FasterDelay()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("EnergyShieldRechargeFaster", ModType.Inc, 100, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(MiscConstants.EnergyShieldRechargeDelay / 2, actor.Output["EnergyShieldRechargeDelay"]);
    }

    [Fact]
    public void ESRecharge_AppliesToLife()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("EnergyShieldRechargeAppliesToLife", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1, actor.Output["EnergyShieldRechargeAppliesToLife"]);
        Assert.True(actor.Output.ContainsKey("LifeRecharge"));
        Assert.True(actor.Output["LifeRecharge"] > 0);
    }

    [Fact]
    public void ESRecharge_NoESRecharge_Flag()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("NoEnergyShieldRecharge", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(0, actor.Output["EnergyShieldRecharge"]);
    }

    // ═══ Phase 4: Recoup ═══

    [Fact]
    public void Recoup_BaseLifeRecoup()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRecoup", ModType.Base, 10, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(10, actor.Output["LifeRecoup"]);
        Assert.True(actor.Output["anyRecoup"] > 0);
    }

    [Fact]
    public void Recoup_ESRecoupInsteadOfLife()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRecoup", ModType.Base, 10, "Test");
        actor.ModDB.NewMod("EnergyShieldRecoupInsteadOfLife", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(0, actor.Output["LifeRecoup"]);
    }

    [Fact]
    public void Recoup_DamageTypeSpecific()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("FireLifeRecoup", ModType.Base, 5, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(5, actor.Output["FireLifeRecoup"]);
    }

    // ═══ Phase 4: Ward Recharge ═══

    [Fact]
    public void WardRecharge_BaseDelay()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        Assert.Equal(MiscConstants.WardRechargeDelay, actor.Output["WardRechargeDelay"]);
    }

    [Fact]
    public void WardRecharge_FasterDelay()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("WardRechargeFaster", ModType.Inc, 100, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(MiscConstants.WardRechargeDelay / 2, actor.Output["WardRechargeDelay"]);
    }

    // ═══ Phase 4: Damage Reduction ═══

    [Fact]
    public void DamageReduction_PerType()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("FireDamageReduction", ModType.Base, 10, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(10, actor.Output["BaseFireDamageReduction"]);
    }

    [Fact]
    public void DamageReduction_Capped()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("FireDamageReduction", ModType.Base, 100, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(MiscConstants.DamageReductionCap, actor.Output["BaseFireDamageReduction"]);
    }

    [Fact]
    public void DamageReduction_ElementalShared()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("ElementalDamageReduction", ModType.Base, 5, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(5, actor.Output["BaseFireDamageReduction"]);
        Assert.Equal(5, actor.Output["BaseColdDamageReduction"]);
        Assert.Equal(5, actor.Output["BaseLightningDamageReduction"]);
    }

    [Fact]
    public void DamageReduction_WhenHit()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("PhysicalDamageReduction", ModType.Base, 10, "Test");
        actor.ModDB.NewMod("PhysicalDamageReductionWhenHit", ModType.Base, 5, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(10, actor.Output["BasePhysicalDamageReduction"]);
        Assert.Equal(15, actor.Output["BasePhysicalDamageReductionWhenHit"]);
    }

    // ═══ Phase 4: Movement Speed ═══

    [Fact]
    public void MovementSpeed_Base()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        Assert.Equal(1, actor.Output["MovementSpeedMod"]);
    }

    [Fact]
    public void MovementSpeed_WithInc()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("MovementSpeed", ModType.Inc, 30, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1.3, actor.Output["MovementSpeedMod"], 5);
    }

    [Fact]
    public void MovementSpeed_CannotBeBelowBase()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("MovementSpeed", ModType.Inc, -50, "Test");
        actor.ModDB.NewMod("MovementSpeedCannotBeBelowBase", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.True(actor.Output["MovementSpeedMod"] >= 1);
    }

    [Fact]
    public void MovementSpeed_EffectiveWithActionSpeed()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        Assert.Equal(actor.Output["MovementSpeedMod"] * actor.Output["ActionSpeedMod"],
            actor.Output["EffectiveMovementSpeedMod"]);
    }

    // ═══ Phase 4: Block/Suppress Recovery ═══

    [Fact]
    public void BlockRecovery_LifeOnBlock()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeOnBlock", ModType.Base, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(50, actor.Output["LifeOnBlock"]);
    }

    [Fact]
    public void BlockRecovery_GatedByCannotRecoverLife()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeOnBlock", ModType.Base, 50, "Test");
        actor.ModDB.NewMod("CannotRecoverLifeOutsideLeech", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(0, actor.Output["LifeOnBlock"]);
    }

    [Fact]
    public void BlockRecovery_ManaOnBlock()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("ManaOnBlock", ModType.Base, 30, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(30, actor.Output["ManaOnBlock"]);
    }

    [Fact]
    public void BlockRecovery_ESOnBlock()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("EnergyShieldOnBlock", ModType.Base, 100, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["EnergyShieldOnBlock"]);
    }

    // ═══ Phase 4: Damage Avoidance ═══

    [Fact]
    public void Avoidance_PerType_CappedAt75()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("AvoidPhysicalDamageChance", ModType.Base, 100, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(MiscConstants.AvoidChanceCap, actor.Output["AvoidPhysicalDamageChance"]);
    }

    [Fact]
    public void Avoidance_AllDamageFromHits()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("AvoidAllDamageFromHitsChance", ModType.Base, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(50, actor.Output["AvoidAllDamageFromHitsChance"]);
    }

    [Fact]
    public void Avoidance_Projectiles()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("AvoidProjectilesChance", ModType.Base, 30, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(30, actor.Output["AvoidProjectilesChance"]);
    }

    // ═══ Phase 4: Immunities ═══

    [Fact]
    public void Immunity_CorruptedBlood()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("CorruptedBloodImmune", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1, actor.Output["CorruptedBloodImmunity"]);
    }

    [Fact]
    public void Immunity_Maim()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("MaimImmune", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(1, actor.Output["MaimImmunity"]);
    }

    [Fact]
    public void Avoidance_BlindImmune_100()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("BlindImmune", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["BlindAvoidChance"]);
    }

    [Fact]
    public void Avoidance_ImpaleImmune_100()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("ImpaleImmune", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["ImpaleAvoidChance"]);
    }

    // ═══ Phase 4: Ailment Avoidance ═══

    [Fact]
    public void AilmentAvoidance_NonElemental_Bleed()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("AvoidBleed", ModType.Base, 50, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(50, actor.Output["BleedAvoidChance"]);
    }

    [Fact]
    public void AilmentAvoidance_NonElemental_Immune100()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("BleedImmune", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["BleedAvoidChance"]);
    }

    [Fact]
    public void AilmentAvoidance_Elemental_Ignite()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("AvoidIgnite", ModType.Base, 40, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(40, actor.Output["IgniteAvoidChance"]);
    }

    [Fact]
    public void AilmentAvoidance_Elemental_AvoidElementalAilments()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("AvoidElementalAilments", ModType.Base, 30, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(30, actor.Output["IgniteAvoidChance"]);
        Assert.Equal(30, actor.Output["ChillAvoidChance"]);
        Assert.Equal(30, actor.Output["ShockAvoidChance"]);
    }

    [Fact]
    public void AilmentAvoidance_ShockAvoidAppliesToAll()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("ShockAvoidAppliesToElementalAilments", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("AvoidShock", ModType.Base, 50, "Test");
        CalcDefence.Defence(actor);
        // Shock avoidance for Shock itself
        Assert.Equal(50, actor.Output["ShockAvoidChance"]);
        // Also applies to other elemental ailments
        Assert.Equal(50, actor.Output["IgniteAvoidChance"]);
        Assert.Equal(50, actor.Output["ChillAvoidChance"]);
    }

    [Fact]
    public void AilmentAvoidance_ElementalImmune()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("ElementalAilmentImmune", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["IgniteAvoidChance"]);
        Assert.Equal(100, actor.Output["ChillAvoidChance"]);
        Assert.Equal(100, actor.Output["ShockAvoidChance"]);
        Assert.Equal(100, actor.Output["FreezeAvoidChance"]);
    }

    // ═══ Phase 4: Curse Avoidance ═══

    [Fact]
    public void CurseAvoidance_Base()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("AvoidCurse", ModType.Base, 60, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(60, actor.Output["CurseAvoidChance"]);
    }

    [Fact]
    public void CurseAvoidance_Immune()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("CurseImmune", ModType.Flag, true, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["CurseAvoidChance"]);
    }

    // ═══ Phase 4: Ailment Duration on Self ═══

    [Fact]
    public void AilmentDuration_DebuffExpirationRate()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("SelfDebuffExpirationRate", ModType.Base, 100, "Test");
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["DebuffExpirationRate"]);
        // 10000 / (100 + 100) = 50
        Assert.Equal(50, actor.Output["DebuffExpirationModifier"]);
    }

    [Fact]
    public void AilmentDuration_SelfBlindDuration()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        // Default: More=1, Inc=0, DebuffExpMod=100, so: 1 * (100 + 0) * 100 / 100 = 100
        Assert.Equal(100, actor.Output["SelfBlindDuration"]);
    }

    [Fact]
    public void AilmentDuration_NonElemental()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        // Default: more=1, inc=(100+0)*100=10000, rate=100+0+0=100
        // 10000 * 1 / 100 = 100
        Assert.Equal(100, actor.Output["SelfBleedDuration"]);
        Assert.Equal(100, actor.Output["SelfPoisonDuration"]);
    }

    [Fact]
    public void AilmentDuration_Elemental()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        Assert.Equal(100, actor.Output["SelfIgniteDuration"]);
        Assert.Equal(100, actor.Output["SelfChillDuration"]);
    }

    [Fact]
    public void AilmentDuration_Firesong_IgniteAppliesToAll()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("IgniteDurationAppliesToElementalAilments", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("SelfIgniteDuration", ModType.Inc, -50, "Test"); // -50% ignite duration
        CalcDefence.Defence(actor);
        // Chill should also get the -50% from ignite (Firesong)
        Assert.True(actor.Output["SelfChillDuration"] < 100);
    }

    [Fact]
    public void AilmentDuration_SelfAilmentEffect()
    {
        var actor = CreateFullPipelineActor();
        CalcDefence.Defence(actor);
        // Default: selfMod=1, enemyEffect=1, so 1 * 1 * 100 = 100
        Assert.Equal(100, actor.Output["SelfBleedEffect"]);
    }

    // ─── Integration tests ───

    [Fact]
    public void Integration_FullPipeline()
    {
        var actor = CreateActorWithEnemy();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Life", ModType.Base, 50, "Test");
        actor.ModDB.NewMod("Life", ModType.Inc, 10, "Test");
        actor.ModDB.NewMod("FireResist", ModType.Base, 30, "Test");

        CalcPerform.DoActorAttributes(actor);
        CalcPerform.DoActorLifeMana(actor);
        CalcDefence.Defence(actor);

        Assert.Equal(100, actor.Output["Str"]);
        Assert.Equal(110, actor.Output["Life"]);
        Assert.Equal(30, actor.Output["FireResist"]);
    }

    [Fact]
    public void Integration_RegenAndRecovery()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 50, "Test");
        actor.ModDB.NewMod("LifeRegenPercent", ModType.Base, 2, "Test");

        CalcDefence.Defence(actor);

        double life = actor.Output["Life"];
        // baseRegen = 50 + life * 2/100
        double expectedBaseRegen = 50 + life * 0.02;
        Assert.True(actor.Output["LifeRegen"] > 0);
        Assert.True(actor.Output["LifeRegenRecovery"] > 0);

        // Verify ES recharge also works
        Assert.True(actor.Output["EnergyShieldRecharge"] > 0);
    }

    [Fact]
    public void Integration_FullDefencePipeline()
    {
        var actor = CreateFullPipelineActor();
        actor.ModDB.NewMod("LifeRegen", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("AvoidPhysicalDamageChance", ModType.Base, 30, "Test");
        actor.ModDB.NewMod("FireDamageReduction", ModType.Base, 10, "Test");
        actor.ModDB.NewMod("MovementSpeed", ModType.Inc, 20, "Test");

        CalcDefence.Defence(actor);

        Assert.True(actor.Output["LifeRegen"] > 0);
        Assert.Equal(30, actor.Output["AvoidPhysicalDamageChance"]);
        Assert.Equal(10, actor.Output["BaseFireDamageReduction"]);
        Assert.Equal(1.2, actor.Output["MovementSpeedMod"], 5);
        Assert.True(actor.Output["WardRechargeDelay"] > 0);
        Assert.True(actor.Output["EnergyShieldRechargeDelay"] > 0);
    }
}
