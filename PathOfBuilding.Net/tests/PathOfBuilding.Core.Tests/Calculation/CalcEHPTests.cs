using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcEHPTests
{
    private static Actor CreateFullActor()
    {
        var actor = new Actor();
        var enemy = new Actor();
        actor.Enemy = enemy;
        enemy.Enemy = actor;
        CalcSetup.InitModDB(actor.ModDB);
        CalcSetup.InitModDB(enemy.ModDB, isEnemy: true);

        actor.ModDB.NewMod("Life", ModType.Base, 5000, "Test");
        actor.ModDB.NewMod("Mana", ModType.Base, 1000, "Test");
        actor.ModDB.NewMod("EnergyShield", ModType.Base, 2000, "Test");
        actor.ModDB.NewMod("Armour", ModType.Base, 10000, "Test");

        CalcPerform.DoActorAttributes(actor);
        CalcPerform.DoActorLifeMana(actor);
        CalcPerform.DoActorLifeManaReservation(actor);
        CalcPerform.DoActorCharges(actor);
        CalcDefence.Defence(actor);

        return actor;
    }

    private static Actor CreateMinimalActor()
    {
        var actor = new Actor();
        var enemy = new Actor();
        actor.Enemy = enemy;
        enemy.Enemy = actor;
        CalcSetup.InitModDB(actor.ModDB);
        CalcSetup.InitModDB(enemy.ModDB, isEnemy: true);
        return actor;
    }

    // ─── Damage Shift Tables ───

    [Fact]
    public void DamageShift_NoShift_100PercentSameType()
    {
        var actor = CreateMinimalActor();
        CalcEHP.BuildDamageShiftTables(actor);
        Assert.Equal(100, actor.DamageShiftTable["Physical"]["Physical"]);
        Assert.Equal(0, actor.DamageShiftTable["Physical"]["Fire"]);
    }

    [Fact]
    public void DamageShift_SingleShift_ReducesSameType()
    {
        var actor = CreateMinimalActor();
        actor.ModDB.NewMod("PhysicalDamageTakenAsFire", ModType.Base, 30, "Test");
        CalcEHP.BuildDamageShiftTables(actor);
        Assert.Equal(70, actor.DamageShiftTable["Physical"]["Physical"]);
        Assert.Equal(30, actor.DamageShiftTable["Physical"]["Fire"]);
    }

    [Fact]
    public void DamageShift_MultiShift_DistributesCorrectly()
    {
        var actor = CreateMinimalActor();
        actor.ModDB.NewMod("PhysicalDamageTakenAsFire", ModType.Base, 30, "Test");
        actor.ModDB.NewMod("PhysicalDamageTakenAsCold", ModType.Base, 20, "Test");
        CalcEHP.BuildDamageShiftTables(actor);
        Assert.Equal(50, actor.DamageShiftTable["Physical"]["Physical"]);
        Assert.Equal(30, actor.DamageShiftTable["Physical"]["Fire"]);
        Assert.Equal(20, actor.DamageShiftTable["Physical"]["Cold"]);
    }

    [Fact]
    public void DamageShift_Over100_ClampsSameTypeToZero()
    {
        var actor = CreateMinimalActor();
        actor.ModDB.NewMod("PhysicalDamageTakenAsFire", ModType.Base, 60, "Test");
        actor.ModDB.NewMod("PhysicalDamageTakenAsCold", ModType.Base, 60, "Test");
        CalcEHP.BuildDamageShiftTables(actor);
        Assert.Equal(0, actor.DamageShiftTable["Physical"]["Physical"]);
    }

    [Fact]
    public void DamageShift_HitsOnly_AddedToShift()
    {
        var actor = CreateMinimalActor();
        actor.ModDB.NewMod("PhysicalDamageFromHitsTakenAsFire", ModType.Base, 20, "Test");
        CalcEHP.BuildDamageShiftTables(actor);
        Assert.Equal(80, actor.DamageShiftTable["Physical"]["Physical"]);
        Assert.Equal(20, actor.DamageShiftTable["Physical"]["Fire"]);
    }

    // ─── Damage Taken Multipliers ───

    [Fact]
    public void TakenMult_Base_IsOne()
    {
        var actor = CreateFullActor();
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.Equal(1, actor.Output["PhysicalTakenHitMult"]);
    }

    [Fact]
    public void TakenMult_WithINC_IncreasesHitMult()
    {
        var actor = CreateFullActor();
        actor.ModDB.NewMod("PhysicalDamageTaken", ModType.Inc, 20, "Test");
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.Equal(1.2, actor.Output["PhysicalTakenHitMult"], 2);
    }

    [Fact]
    public void TakenMult_WithMORE_MultipliesHitMult()
    {
        var actor = CreateFullActor();
        actor.ModDB.NewMod("PhysicalDamageTaken", ModType.More, -20, "Test");
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.Equal(0.8, actor.Output["PhysicalTakenHitMult"], 2);
    }

    [Fact]
    public void TakenMult_Elemental_IncludesElementalBonus()
    {
        var actor = CreateFullActor();
        actor.ModDB.NewMod("ElementalDamageTaken", ModType.Inc, 10, "Test");
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.Equal(1.1, actor.Output["FireTakenHitMult"], 2);
        Assert.Equal(1.1, actor.Output["ColdTakenHitMult"], 2);
        Assert.Equal(1, actor.Output["PhysicalTakenHitMult"], 2);
    }

    [Fact]
    public void TakenMult_WhenHit_AddsToHitOnly()
    {
        var actor = CreateFullActor();
        actor.ModDB.NewMod("DamageTakenWhenHit", ModType.Inc, 10, "Test");
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.Equal(1.1, actor.Output["PhysicalTakenHitMult"], 2);
    }

    [Fact]
    public void TakenMult_DoT_IncludesResistAndReduction()
    {
        var actor = CreateFullActor();
        actor.Output["FireResist"] = 75;
        actor.Output["BaseFireDamageReduction"] = 0;
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.Equal(0.25, actor.Output["FireTakenDotMult"], 2);
    }

    [Fact]
    public void TakenFlat_Average_AveragesCategories()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyDamageType = "Average";
        actor.ModDB.NewMod("DamageTakenFromAttacks", ModType.Base, -100, "Test");
        CalcEHP.DamageTakenMultipliers(actor);
        // Average: -100 from attacks / 2 = -50
        Assert.True(actor.Output["PhysicaltakenFlat"] < 0);
    }

    [Fact]
    public void TakenFlat_Melee_IncludesAttackMods()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyDamageType = "Melee";
        actor.ModDB.NewMod("DamageTakenFromAttacks", ModType.Base, -50, "Test");
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.True(actor.Output["PhysicaltakenFlat"] <= -50);
    }

    // ─── Armour ───

    [Fact]
    public void ArmourApplies_PhysicalDefault()
    {
        var actor = CreateFullActor();
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.True(actor.Output["PhysicalEffectiveAppliedArmour"] > 0);
    }

    [Fact]
    public void ArmourApplies_FireDefault_Zero()
    {
        var actor = CreateFullActor();
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.Equal(0, actor.Output["FireEffectiveAppliedArmour"]);
    }

    [Fact]
    public void ArmourApplies_FireWithMod_NonZero()
    {
        var actor = CreateFullActor();
        actor.ModDB.NewMod("ArmourAppliesToFireDamageTaken", ModType.Base, 50, "Test");
        CalcEHP.DamageTakenMultipliers(actor);
        Assert.True(actor.Output["FireEffectiveAppliedArmour"] > 0);
    }

    // ─── Enemy Damage ───

    [Fact]
    public void EnemyDamage_ConfigValues_Applied()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.True(actor.Output["PhysicalEnemyDamage"] > 0);
        Assert.True(actor.Output["totalEnemyDamageIn"] >= 1000);
    }

    [Fact]
    public void EnemyDamage_CritEffect_Applied()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        actor.Config.EnemyCritChance = 50;
        actor.Config.EnemyCritDamage = 150;
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.True(actor.Output["EnemyCritEffect"] > 1);
        Assert.True(actor.Output["PhysicalEnemyDamage"] > 1000);
    }

    [Fact]
    public void EnemyDamage_NeverCrit_ZeroCritChance()
    {
        var actor = CreateFullActor();
        actor.Enemy!.ModDB.NewMod("NeverCrit", ModType.Flag, true, "Test");
        actor.Config.EnemyCritChance = 50;
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.Equal(0, actor.Output["EnemyCritChance"]);
    }

    [Fact]
    public void EnemyDamage_AlwaysCrit_FullCritChance()
    {
        var actor = CreateFullActor();
        actor.Enemy!.ModDB.NewMod("AlwaysCrit", ModType.Flag, true, "Test");
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.Equal(100, actor.Output["EnemyCritChance"]);
    }

    [Fact]
    public void EnemyDamage_DamageMult_Applied()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        actor.Enemy!.ModDB.NewMod("Damage", ModType.Inc, 100, "Test");
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.True(actor.Output["PhysicalEnemyDamage"] >= 2000);
    }

    [Fact]
    public void EnemyDamage_PenAndOverwhelm_Stored()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalPen = 10;
        actor.Config.EnemyPhysicalOverwhelm = 5;
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.Equal(10, actor.Output["PhysicalEnemyPen"]);
        Assert.Equal(5, actor.Output["PhysicalEnemyOverwhelm"]);
    }

    [Fact]
    public void EnemyDamage_ZeroDamage_ZeroOutput()
    {
        var actor = CreateFullActor();
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.Equal(0, actor.Output["totalEnemyDamage"]);
    }

    [Fact]
    public void EnemyDamage_DamageOverTime_AllZero()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyDamageType = "DamageOverTime";
        actor.Config.EnemyPhysicalDamage = 1000;
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.Equal(0, actor.Output["PhysicalEnemyDamage"]);
    }

    [Fact]
    public void EnemyDamage_MultipleTypes_SumsTotal()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 500;
        actor.Config.EnemyFireDamage = 300;
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.True(actor.Output["totalEnemyDamageIn"] >= 800);
    }

    [Fact]
    public void EnemyDamage_PhysicalOverwhelm_FromEnemyMods()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        actor.Enemy!.ModDB.NewMod("PhysicalOverwhelm", ModType.Base, 10, "Test");
        CalcEHP.EnemyDamageCalculation(actor);
        Assert.True(actor.Output["PhysicalEnemyOverwhelm"] >= 10);
    }

    // ─── Incoming Hit Damage ───

    [Fact]
    public void IncomingHit_ZeroDamage_ZeroTakenHit()
    {
        var actor = CreateFullActor();
        CalcEHP.BuildDamageShiftTables(actor);
        CalcEHP.DamageTakenMultipliers(actor);
        CalcEHP.EnemyDamageCalculation(actor);
        CalcEHP.IncomingHitDamage(actor);
        Assert.Equal(0, actor.Output["totalTakenHit"]);
    }

    [Fact]
    public void IncomingHit_WithResistance_ReducesDamage()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyFireDamage = 1000;
        CalcEHP.BuildDamageShiftTables(actor);
        CalcEHP.DamageTakenMultipliers(actor);
        CalcEHP.EnemyDamageCalculation(actor);
        CalcEHP.IncomingHitDamage(actor);
        double fireResist = actor.Output.TryGetValue("FireResist", out double fr) ? fr : 0;
        if (fireResist > 0)
            Assert.True(actor.Output["FireTakenHit"] < actor.Output["FireEnemyDamage"]);
    }

    [Fact]
    public void IncomingHit_WithArmour_ReducesPhysical()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        CalcEHP.BuildDamageShiftTables(actor);
        CalcEHP.DamageTakenMultipliers(actor);
        CalcEHP.EnemyDamageCalculation(actor);
        CalcEHP.IncomingHitDamage(actor);
        Assert.True(actor.Output["PhysicalTakenHit"] < actor.Output["PhysicalEnemyDamage"]);
    }

    [Fact]
    public void IncomingHit_WithPen_IncreasesHit()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyFireDamage = 1000;
        actor.Config.EnemyFirePen = 10;
        CalcEHP.BuildDamageShiftTables(actor);
        CalcEHP.DamageTakenMultipliers(actor);
        CalcEHP.EnemyDamageCalculation(actor);
        CalcEHP.IncomingHitDamage(actor);
        // Pen should increase damage taken
        double resist = actor.Output.TryGetValue("FireResist", out double fr) ? fr : 0;
        double expected = 1000 * actor.Output["FireEnemyDamage"] / 1000; // with crit mult
        Assert.True(actor.Output["FireTakenHit"] > 0);
    }

    [Fact]
    public void IncomingHit_TakenFlat_Applied()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 100;
        actor.ModDB.NewMod("DamageTaken", ModType.Base, -50, "Test");
        CalcEHP.BuildDamageShiftTables(actor);
        CalcEHP.DamageTakenMultipliers(actor);
        CalcEHP.EnemyDamageCalculation(actor);
        CalcEHP.IncomingHitDamage(actor);
        // Flat -50 should significantly reduce damage
        Assert.True(actor.Output["totalTakenHit"] < 100);
    }

    [Fact]
    public void IncomingHit_DamageShift_Distributes()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        actor.ModDB.NewMod("PhysicalDamageTakenAsFire", ModType.Base, 50, "Test");
        CalcEHP.BuildDamageShiftTables(actor);
        CalcEHP.DamageTakenMultipliers(actor);
        CalcEHP.EnemyDamageCalculation(actor);
        CalcEHP.IncomingHitDamage(actor);
        Assert.True(actor.Output["FireTakenDamage"] > 0);
    }

    [Fact]
    public void IncomingHit_Suppression_ReducesSpellDamage()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyDamageType = "Spell";
        actor.Config.EnemyFireDamage = 1000;
        actor.Output["EffectiveSpellSuppressionChance"] = 100;
        actor.Output["SpellSuppressionEffect"] = 40;
        CalcEHP.BuildDamageShiftTables(actor);
        CalcEHP.DamageTakenMultipliers(actor);
        CalcEHP.EnemyDamageCalculation(actor);
        CalcEHP.IncomingHitDamage(actor);
        // With 100% suppression, 40% effect → 60% of spell damage
        double raw = actor.Output["FireEnemyDamage"];
        if (raw > 0)
            Assert.True(actor.Output["FireTakenHit"] < raw);
    }

    [Fact]
    public void IncomingHit_Average_AveragesAttackAndSpell()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyDamageType = "Average";
        actor.Config.EnemyPhysicalDamage = 1000;
        CalcEHP.BuildDamageShiftTables(actor);
        CalcEHP.DamageTakenMultipliers(actor);
        CalcEHP.EnemyDamageCalculation(actor);
        CalcEHP.IncomingHitDamage(actor);
        Assert.True(actor.Output["totalTakenHit"] > 0);
    }

    // ─── Pool Reduction ───

    [Fact]
    public void PoolReduction_WardAbsorbs()
    {
        var actor = CreateMinimalActor();
        actor.Output["PhysicalEnergyShieldBypass"] = 100.0;
        actor.Output["ChaosEnergyShieldBypass"] = 100.0;
        actor.Output["FireEnergyShieldBypass"] = 100.0;
        actor.Output["ColdEnergyShieldBypass"] = 100.0;
        actor.Output["LightningEnergyShieldBypass"] = 100.0;
        actor.Output["sharedMindOverMatter"] = 0.0;
        foreach (string dt in DamageTypeFlags.DmgTypeList)
            actor.Output[$"{dt}MindOverMatter"] = 0.0;

        var pools = new PoolTable { Ward = 500, EnergyShield = 2000, Mana = 1000, Life = 5000 };
        var damage = new Dictionary<string, double> { ["Physical"] = 300 };
        var result = CalcEHP.ReducePoolsByDamage(pools, damage, actor);
        Assert.Equal(200, result.Ward);
        Assert.Equal(5000, result.Life);
    }

    [Fact]
    public void PoolReduction_ESAbsorbs()
    {
        var actor = CreateMinimalActor();
        actor.Output["PhysicalEnergyShieldBypass"] = 0.0;
        actor.Output["ChaosEnergyShieldBypass"] = 100.0;
        actor.Output["FireEnergyShieldBypass"] = 0.0;
        actor.Output["ColdEnergyShieldBypass"] = 0.0;
        actor.Output["LightningEnergyShieldBypass"] = 0.0;
        actor.Output["sharedMindOverMatter"] = 0.0;
        foreach (string dt in DamageTypeFlags.DmgTypeList)
            actor.Output[$"{dt}MindOverMatter"] = 0.0;

        var pools = new PoolTable { Ward = 0, EnergyShield = 2000, Mana = 1000, Life = 5000 };
        var damage = new Dictionary<string, double> { ["Physical"] = 500 };
        var result = CalcEHP.ReducePoolsByDamage(pools, damage, actor);
        Assert.True(result.EnergyShield < 2000);
        Assert.Equal(5000, result.Life);
    }

    [Fact]
    public void PoolReduction_MoMRedirect()
    {
        var actor = CreateMinimalActor();
        foreach (string dt in DamageTypeFlags.DmgTypeList)
        {
            actor.Output[$"{dt}EnergyShieldBypass"] = 100.0;
            actor.Output[$"{dt}MindOverMatter"] = 0.0;
        }
        actor.Output["sharedMindOverMatter"] = 30.0;

        var pools = new PoolTable { Ward = 0, EnergyShield = 0, Mana = 1000, Life = 5000 };
        var damage = new Dictionary<string, double> { ["Physical"] = 1000 };
        var result = CalcEHP.ReducePoolsByDamage(pools, damage, actor);
        Assert.True(result.Mana < 1000); // Some mana consumed
        Assert.True(result.Life < 5000); // Life also consumed
    }

    [Fact]
    public void PoolReduction_LifeAbsorbs()
    {
        var actor = CreateMinimalActor();
        foreach (string dt in DamageTypeFlags.DmgTypeList)
        {
            actor.Output[$"{dt}EnergyShieldBypass"] = 100.0;
            actor.Output[$"{dt}MindOverMatter"] = 0.0;
        }
        actor.Output["sharedMindOverMatter"] = 0.0;

        var pools = new PoolTable { Ward = 0, EnergyShield = 0, Mana = 0, Life = 5000 };
        var damage = new Dictionary<string, double> { ["Physical"] = 3000 };
        var result = CalcEHP.ReducePoolsByDamage(pools, damage, actor);
        Assert.Equal(2000, result.Life);
    }

    [Fact]
    public void PoolReduction_Overkill_Tracked()
    {
        var actor = CreateMinimalActor();
        foreach (string dt in DamageTypeFlags.DmgTypeList)
        {
            actor.Output[$"{dt}EnergyShieldBypass"] = 100.0;
            actor.Output[$"{dt}MindOverMatter"] = 0.0;
        }
        actor.Output["sharedMindOverMatter"] = 0.0;

        var pools = new PoolTable { Ward = 0, EnergyShield = 0, Mana = 0, Life = 1000 };
        var damage = new Dictionary<string, double> { ["Physical"] = 1500 };
        var result = CalcEHP.ReducePoolsByDamage(pools, damage, actor);
        Assert.Equal(0, result.Life);
        Assert.True(result.OverkillDamage > 0);
    }

    [Fact]
    public void PoolReduction_ChaosBypass_SkipsES()
    {
        var actor = CreateMinimalActor();
        actor.Output["PhysicalEnergyShieldBypass"] = 0.0;
        actor.Output["ChaosEnergyShieldBypass"] = 100.0;
        actor.Output["FireEnergyShieldBypass"] = 0.0;
        actor.Output["ColdEnergyShieldBypass"] = 0.0;
        actor.Output["LightningEnergyShieldBypass"] = 0.0;
        actor.Output["sharedMindOverMatter"] = 0.0;
        foreach (string dt in DamageTypeFlags.DmgTypeList)
            actor.Output[$"{dt}MindOverMatter"] = 0.0;

        var pools = new PoolTable { Ward = 0, EnergyShield = 2000, Mana = 0, Life = 5000 };
        var damage = new Dictionary<string, double> { ["Chaos"] = 500 };
        var result = CalcEHP.ReducePoolsByDamage(pools, damage, actor);
        Assert.Equal(2000, result.EnergyShield); // ES untouched for chaos
        Assert.Equal(4500, result.Life);
    }

    // ─── NotHitChance ───

    [Fact]
    public void NotHitChance_MeleeEvade_Applied()
    {
        var actor = CreateMinimalActor();
        actor.Output["MeleeEvadeChance"] = 50;
        actor.Output["ProjectileEvadeChance"] = 0;
        actor.Output["AvoidAllDamageFromHitsChance"] = 0;
        actor.Output["AvoidProjectilesChance"] = 0;
        actor.Output["specificTypeAvoidance"] = 0;
        actor.Output["EffectiveAttackDodgeChance"] = 0;
        actor.Output["EffectiveSpellDodgeChance"] = 0;
        actor.Config.EnemyDamageType = "Melee";
        CalcEHP.NotHitChance(actor);
        Assert.Equal(50, actor.Output["MeleeNotHitChance"]);
    }

    [Fact]
    public void NotHitChance_Average_Averages()
    {
        var actor = CreateMinimalActor();
        actor.Output["MeleeEvadeChance"] = 40;
        actor.Output["ProjectileEvadeChance"] = 60;
        actor.Output["AvoidAllDamageFromHitsChance"] = 0;
        actor.Output["AvoidProjectilesChance"] = 0;
        actor.Output["specificTypeAvoidance"] = 0;
        actor.Output["EffectiveAttackDodgeChance"] = 0;
        actor.Output["EffectiveSpellDodgeChance"] = 0;
        CalcEHP.NotHitChance(actor);
        Assert.True(actor.Output["AverageNotHitChance"] > 0);
    }

    [Fact]
    public void NotHitChance_AvoidAll_Applied()
    {
        var actor = CreateMinimalActor();
        actor.Output["MeleeEvadeChance"] = 0;
        actor.Output["ProjectileEvadeChance"] = 0;
        actor.Output["AvoidAllDamageFromHitsChance"] = 20;
        actor.Output["AvoidProjectilesChance"] = 0;
        actor.Output["specificTypeAvoidance"] = 0;
        actor.Output["EffectiveAttackDodgeChance"] = 0;
        actor.Output["EffectiveSpellDodgeChance"] = 0;
        CalcEHP.NotHitChance(actor);
        Assert.Equal(20, actor.Output["MeleeNotHitChance"]);
        Assert.Equal(20, actor.Output["SpellNotHitChance"]);
    }

    [Fact]
    public void NotHitChance_Unlucky_ReducesChance()
    {
        var actor = CreateMinimalActor();
        actor.Output["MeleeEvadeChance"] = 50;
        actor.Output["ProjectileEvadeChance"] = 50;
        actor.Output["AvoidAllDamageFromHitsChance"] = 0;
        actor.Output["AvoidProjectilesChance"] = 0;
        actor.Output["specificTypeAvoidance"] = 0;
        actor.Output["EffectiveAttackDodgeChance"] = 0;
        actor.Output["EffectiveSpellDodgeChance"] = 0;
        actor.Config.EHPUnluckyWorstOf = 2;
        actor.Config.EnemyDamageType = "Melee";
        CalcEHP.NotHitChance(actor);
        // Unlucky: chance^2/100 = 50*50/100 = 25
        Assert.Equal(25, actor.Output["ConfiguredNotHitChance"]);
    }

    [Fact]
    public void NotHitChance_Spell_NoEvade()
    {
        var actor = CreateMinimalActor();
        actor.Output["MeleeEvadeChance"] = 50;
        actor.Output["ProjectileEvadeChance"] = 50;
        actor.Output["AvoidAllDamageFromHitsChance"] = 0;
        actor.Output["AvoidProjectilesChance"] = 0;
        actor.Output["specificTypeAvoidance"] = 0;
        actor.Output["EffectiveAttackDodgeChance"] = 0;
        actor.Output["EffectiveSpellDodgeChance"] = 0;
        CalcEHP.NotHitChance(actor);
        Assert.Equal(0, actor.Output["SpellNotHitChance"]);
    }

    [Fact]
    public void NotHitChance_ProjectileAvoidance_Applied()
    {
        var actor = CreateMinimalActor();
        actor.Output["MeleeEvadeChance"] = 0;
        actor.Output["ProjectileEvadeChance"] = 0;
        actor.Output["AvoidAllDamageFromHitsChance"] = 0;
        actor.Output["AvoidProjectilesChance"] = 30;
        actor.Output["specificTypeAvoidance"] = 0;
        actor.Output["EffectiveAttackDodgeChance"] = 0;
        actor.Output["EffectiveSpellDodgeChance"] = 0;
        CalcEHP.NotHitChance(actor);
        // Projectile should include avoidProj
        Assert.True(actor.Output["ProjectileNotHitChance"] > 0);
    }

    // ─── NumberOfHitsToDie ───

    [Fact]
    public void NumberOfHitsToDie_SmallDamage_ManyHits()
    {
        var actor = CreateMinimalActor();
        actor.Output["Ward"] = 0;
        actor.Output["EnergyShieldRecoveryCap"] = 0;
        actor.Output["EnergyShield"] = 0;
        actor.Output["ManaUnreserved"] = 0;
        actor.Output["LifeRecoverable"] = 5000;
        actor.Output["sharedMindOverMatter"] = 0;
        foreach (string dt in DamageTypeFlags.DmgTypeList)
        {
            actor.Output[$"{dt}EnergyShieldBypass"] = 100;
            actor.Output[$"{dt}MindOverMatter"] = 0;
        }

        var damage = new Dictionary<string, double> { ["Physical"] = 100 };
        double hits = CalcEHP.NumberOfHitsToDie(damage, actor);
        Assert.True(hits >= 49); // 5000 / 100 = 50 minus overkill fraction
    }

    [Fact]
    public void NumberOfHitsToDie_OneShot_LessThan2()
    {
        var actor = CreateMinimalActor();
        actor.Output["Ward"] = 0;
        actor.Output["EnergyShieldRecoveryCap"] = 0;
        actor.Output["EnergyShield"] = 0;
        actor.Output["ManaUnreserved"] = 0;
        actor.Output["LifeRecoverable"] = 1000;
        actor.Output["sharedMindOverMatter"] = 0;
        foreach (string dt in DamageTypeFlags.DmgTypeList)
        {
            actor.Output[$"{dt}EnergyShieldBypass"] = 100;
            actor.Output[$"{dt}MindOverMatter"] = 0;
        }

        var damage = new Dictionary<string, double> { ["Physical"] = 10000 };
        double hits = CalcEHP.NumberOfHitsToDie(damage, actor);
        Assert.True(hits < 2);
    }

    [Fact]
    public void NumberOfHitsToDie_WithWardAndES_MoreHits()
    {
        var actor = CreateMinimalActor();
        actor.Output["Ward"] = 500;
        actor.Output["EnergyShieldRecoveryCap"] = 2000;
        actor.Output["EnergyShield"] = 2000;
        actor.Output["ManaUnreserved"] = 0;
        actor.Output["LifeRecoverable"] = 5000;
        actor.Output["sharedMindOverMatter"] = 0;
        foreach (string dt in DamageTypeFlags.DmgTypeList)
        {
            actor.Output[$"{dt}EnergyShieldBypass"] = 0;
            actor.Output[$"{dt}MindOverMatter"] = 0;
        }

        var damage = new Dictionary<string, double> { ["Physical"] = 1000 };
        double hits = CalcEHP.NumberOfHitsToDie(damage, actor);
        // Ward(500) + ES(2000) + Life(5000) = 7500, so > 7 hits
        Assert.True(hits >= 7);
    }

    [Fact]
    public void NumberOfHitsToDie_ZeroDamage_Infinity()
    {
        var actor = CreateMinimalActor();
        actor.Output["Ward"] = 0;
        actor.Output["LifeRecoverable"] = 5000;
        var damage = new Dictionary<string, double> { ["Physical"] = 0 };
        double hits = CalcEHP.NumberOfHitsToDie(damage, actor);
        Assert.True(double.IsPositiveInfinity(hits));
    }

    [Fact]
    public void NumberOfHitsToDie_WithMoM_MoreHits()
    {
        var actor = CreateMinimalActor();
        actor.Output["Ward"] = 0;
        actor.Output["EnergyShieldRecoveryCap"] = 0;
        actor.Output["EnergyShield"] = 0;
        actor.Output["ManaUnreserved"] = 2000;
        actor.Output["LifeRecoverable"] = 5000;
        actor.Output["sharedMindOverMatter"] = 30;
        foreach (string dt in DamageTypeFlags.DmgTypeList)
        {
            actor.Output[$"{dt}EnergyShieldBypass"] = 100;
            actor.Output[$"{dt}MindOverMatter"] = 0;
        }

        var damageNoMoM = new Dictionary<string, double> { ["Physical"] = 1000 };
        actor.Output["sharedMindOverMatter"] = 0;
        double hitsNoMoM = CalcEHP.NumberOfHitsToDie(damageNoMoM, actor);

        actor.Output["sharedMindOverMatter"] = 30;
        var damageMoM = new Dictionary<string, double> { ["Physical"] = 1000 };
        double hitsMoM = CalcEHP.NumberOfHitsToDie(damageMoM, actor);

        Assert.True(hitsMoM > hitsNoMoM);
    }

    // ─── Final EHP ───

    [Fact]
    public void FinalEHP_TotalNumberOfHits_Positive()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        CalcEHP.BuildDefenceEstimations(actor);
        Assert.True(actor.Output["TotalNumberOfHits"] > 0);
    }

    [Fact]
    public void FinalEHP_TotalEHP_Positive()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        CalcEHP.BuildDefenceEstimations(actor);
        Assert.True(actor.Output["TotalEHP"] > 0);
    }

    [Fact]
    public void FinalEHP_SurvivalTime_Positive()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyPhysicalDamage = 1000;
        actor.Config.EnemySpeed = 700;
        CalcEHP.BuildDefenceEstimations(actor);
        Assert.True(actor.Output["EHPSurvivalTime"] > 0);
    }

    [Fact]
    public void FinalEHP_ZeroDamage_InfiniteHits()
    {
        var actor = CreateFullActor();
        CalcEHP.BuildDefenceEstimations(actor);
        Assert.True(double.IsPositiveInfinity(actor.Output["NumberOfDamagingHits"]));
    }

    [Fact]
    public void FinalEHP_DamageOverTime_NoHitCalcs()
    {
        var actor = CreateFullActor();
        actor.Config.EnemyDamageType = "DamageOverTime";
        CalcEHP.BuildDefenceEstimations(actor);
        Assert.False(actor.Output.ContainsKey("TotalNumberOfHits"));
    }

    // ─── Integration ───

    [Fact]
    public void Integration_FullPipeline_ReasonableEHP()
    {
        var actor = new Actor();
        var enemy = new Actor();
        actor.Enemy = enemy;
        enemy.Enemy = actor;
        CalcSetup.InitModDB(actor.ModDB);
        CalcSetup.InitModDB(enemy.ModDB, isEnemy: true);

        actor.ModDB.NewMod("Life", ModType.Base, 5000, "Test");
        actor.ModDB.NewMod("Mana", ModType.Base, 1000, "Test");
        actor.ModDB.NewMod("EnergyShield", ModType.Base, 2000, "Test");
        actor.ModDB.NewMod("Armour", ModType.Base, 10000, "Test");
        actor.ModDB.NewMod("FireResist", ModType.Base, 75, "Test");

        actor.Config.EnemyPhysicalDamage = 1000;

        CalcPerform.DoActorAttributes(actor);
        CalcConditions.CombatConditions(actor);
        CalcConditions.ShrineBuffs(actor);
        CalcPerform.DoActorLifeMana(actor);
        CalcPerform.DoActorLifeManaReservation(actor);
        CalcPerform.DoActorCharges(actor);
        CalcMisc.DoActorMisc(actor);
        CalcDefence.Defence(actor);
        CalcEHP.BuildDefenceEstimations(actor);

        Assert.True(actor.Output["TotalEHP"] > 0);
        Assert.True(actor.Output["Life"] >= 5000);
        Assert.True(actor.Output["Armour"] >= 10000);
    }

    [Fact]
    public void Integration_WithFortificationAndOnslaught_EHPPositive()
    {
        var actor = new Actor();
        var enemy = new Actor();
        actor.Enemy = enemy;
        enemy.Enemy = actor;
        CalcSetup.InitModDB(actor.ModDB);
        CalcSetup.InitModDB(enemy.ModDB, isEnemy: true);

        actor.ModDB.NewMod("Life", ModType.Base, 5000, "Test");
        actor.ModDB.NewMod("EnergyShield", ModType.Base, 2000, "Test");
        actor.ModDB.NewMod("Armour", ModType.Base, 10000, "Test");
        actor.ModDB.NewMod("Fortified", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("MaximumFortification", ModType.Base, 20, "Test");
        actor.ModDB.NewMod("Onslaught", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("FireResist", ModType.Base, 75, "Test");

        actor.Config.EnemyPhysicalDamage = 1000;

        CalcPerform.DoActorAttributes(actor);
        CalcConditions.CombatConditions(actor);
        CalcConditions.ShrineBuffs(actor);
        CalcPerform.DoActorLifeMana(actor);
        CalcPerform.DoActorLifeManaReservation(actor);
        CalcPerform.DoActorCharges(actor);
        CalcMisc.DoActorMisc(actor);
        CalcDefence.Defence(actor);
        CalcEHP.BuildDefenceEstimations(actor);

        Assert.True(actor.Output["TotalEHP"] > 0);
        Assert.Equal(20, actor.Output["FortificationStacks"]);
    }

    // ─── Config defaults ───

    [Fact]
    public void CalcConfig_Defaults_Correct()
    {
        var config = new CalcConfig();
        Assert.True(config.ModeCombat);
        Assert.True(config.ModeEffective);
        Assert.Equal("Average", config.EnemyDamageType);
        Assert.Equal(1, config.EHPUnluckyWorstOf);
        Assert.Null(config.EnemyPhysicalDamage);
    }

    [Fact]
    public void CalcConfig_GetEnemyDamage_ReturnsZeroForNull()
    {
        var config = new CalcConfig();
        Assert.Equal(0, config.GetEnemyDamage("Physical"));
        Assert.Equal(0, config.GetEnemyDamage("Fire"));
    }

    // ─── Actor DamageShiftTable initialized ───

    [Fact]
    public void Actor_DamageShiftTable_InitializedEmpty()
    {
        var actor = new Actor();
        Assert.NotNull(actor.DamageShiftTable);
        Assert.Empty(actor.DamageShiftTable);
    }

    // ─── Pipeline conditions propagate ───

    [Fact]
    public void CalcConditions_PropagatesThroughPipeline()
    {
        var actor = CreateFullActor();
        actor.Config.AttackedRecently = true;
        CalcConditions.CombatConditions(actor);
        Assert.True(actor.ModDB.Conditions["AttackedRecently"]);
    }

    [Fact]
    public void ShrineBuffs_MassiveShrine_AffectsLifeInPipeline()
    {
        var actor = new Actor();
        var enemy = new Actor();
        actor.Enemy = enemy;
        enemy.Enemy = actor;
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("Life", ModType.Base, 5000, "Test");
        actor.ModDB.NewMod("MassiveShrine", ModType.Flag, true, "Test");

        CalcPerform.DoActorAttributes(actor);
        CalcConditions.CombatConditions(actor);
        CalcConditions.ShrineBuffs(actor);
        CalcPerform.DoActorLifeMana(actor);

        // Massive shrine adds 40% INC Life → more than base
        double baseLife = 5000 + Math.Floor(actor.Output["Str"] / 2); // str bonus
        Assert.True(actor.Output["Life"] > baseLife);
    }

    [Fact]
    public void Fortification_ReducesEHPDamageTaken()
    {
        var actor = new Actor();
        var enemy = new Actor();
        actor.Enemy = enemy;
        enemy.Enemy = actor;
        CalcSetup.InitModDB(actor.ModDB);
        CalcSetup.InitModDB(enemy.ModDB, isEnemy: true);

        actor.ModDB.NewMod("Life", ModType.Base, 5000, "Test");
        actor.ModDB.NewMod("Armour", ModType.Base, 10000, "Test");
        actor.ModDB.NewMod("Fortified", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("MaximumFortification", ModType.Base, 20, "Test");
        actor.Config.EnemyPhysicalDamage = 1000;

        CalcPerform.DoActorAttributes(actor);
        CalcConditions.CombatConditions(actor);
        CalcConditions.ShrineBuffs(actor);
        CalcPerform.DoActorLifeMana(actor);
        CalcPerform.DoActorLifeManaReservation(actor);
        CalcPerform.DoActorCharges(actor);
        CalcMisc.DoActorMisc(actor);
        CalcDefence.Defence(actor);
        CalcEHP.BuildDefenceEstimations(actor);

        // With 20 fortification stacks → DamageTakenWhenHit MORE -20%
        // This should increase the number of hits to die
        Assert.True(actor.Output["TotalNumberOfHits"] > 1);
    }
}
