using PathOfBuilding.Core.Calculation;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Tests.Calculation;

public class CalcPerformTests
{
    private static Actor CreateActor()
    {
        var actor = new Actor();
        actor.Enemy = new Actor();
        return actor;
    }

    // ─── Attributes ───

    [Fact]
    public void DoActorAttributes_BaseStrOnly()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");

        CalcPerform.DoActorAttributes(actor);

        Assert.Equal(100, actor.Output["Str"]);
    }

    [Fact]
    public void DoActorAttributes_StrWithInc()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Str", ModType.Inc, 50, "Test");

        CalcPerform.DoActorAttributes(actor);

        Assert.Equal(150, actor.Output["Str"]);
    }

    [Fact]
    public void DoActorAttributes_StrWithOverride()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Str", ModType.Override, 999, "Test");

        CalcPerform.DoActorAttributes(actor);

        // Val() uses override — but Override is checked in Sum flow, not Val.
        // Actually, calcLib.val computes base * mod, and override isn't used in val.
        // The Lua code uses round(calcLib.val(modDB, stat)). Override would only
        // apply if modStore has it. Let's check: base 100, override 999 → val = 100 * mod(INC=0, MORE=1) = 100
        // Override is a separate mechanism. In Lua, calcLib.val doesn't use override.
        // So the result should be 100 (base is still 100, val uses Sum(BASE) * mod)
        Assert.Equal(100, actor.Output["Str"]);
    }

    [Fact]
    public void DoActorAttributes_AllThreeAttributes()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Dex", ModType.Base, 80, "Test");
        actor.ModDB.NewMod("Int", ModType.Base, 120, "Test");

        CalcPerform.DoActorAttributes(actor);

        Assert.Equal(100, actor.Output["Str"]);
        Assert.Equal(80, actor.Output["Dex"]);
        Assert.Equal(120, actor.Output["Int"]);
        Assert.Equal(300, actor.Output["TotalAttr"]);
        Assert.Equal(80, actor.Output["LowestAttribute"]);
    }

    [Fact]
    public void DoActorAttributes_NegativeValue_ClampedToZero()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, -10, "Test");

        CalcPerform.DoActorAttributes(actor);

        Assert.Equal(0, actor.Output["Str"]);
    }

    // ─── Attribute bonuses ───

    [Fact]
    public void DoActorAttributes_StrBonusToLife()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");

        CalcPerform.DoActorAttributes(actor);

        // floor(100 / 2) = 50 added as base Life
        Assert.Equal(50, actor.ModDB.Sum(ModType.Base, null, "Life"));
    }

    [Fact]
    public void DoActorAttributes_DexBonusToAccuracy()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Dex", ModType.Base, 100, "Test");

        CalcPerform.DoActorAttributes(actor);

        // 100 * 2 (AccuracyPerDexBase) = 200
        Assert.Equal(200, actor.ModDB.Sum(ModType.Base, null, "Accuracy"));
    }

    [Fact]
    public void DoActorAttributes_DexBonusToEvasion()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Dex", ModType.Base, 100, "Test");

        CalcPerform.DoActorAttributes(actor);

        // floor(100 / 5) = 20% increased Evasion
        Assert.Equal(20, actor.ModDB.Sum(ModType.Inc, null, "Evasion"));
    }

    [Fact]
    public void DoActorAttributes_IntBonusToMana()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Int", ModType.Base, 100, "Test");

        CalcPerform.DoActorAttributes(actor);

        // floor(100 / 2) = 50 base Mana
        Assert.Equal(50, actor.ModDB.Sum(ModType.Base, null, "Mana"));
    }

    [Fact]
    public void DoActorAttributes_IntBonusToES()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Int", ModType.Base, 100, "Test");

        CalcPerform.DoActorAttributes(actor);

        // floor(100 / 5) = 20% increased ES
        Assert.Equal(20, actor.ModDB.Sum(ModType.Inc, null, "EnergyShield"));
    }

    [Fact]
    public void DoActorAttributes_StrMeleeDamageBonus()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");

        CalcPerform.DoActorAttributes(actor);

        // floor(100 / 5) = 20% increased melee phys damage
        var cfg = new ModConfig { Flags = ModFlag.Melee };
        Assert.Equal(20, actor.ModDB.Sum(ModType.Inc, cfg, "PhysicalDamage"));
    }

    // ─── Attribute conditions ───

    [Fact]
    public void DoActorAttributes_DexHigherThanInt_Condition()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Dex", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Int", ModType.Base, 50, "Test");

        CalcPerform.DoActorAttributes(actor);

        Assert.True(actor.ModDB.Conditions["DexHigherThanInt"]);
        Assert.False(actor.ModDB.Conditions["IntHigherThanDex"]);
    }

    [Fact]
    public void DoActorAttributes_StrHighestAttribute_Condition()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Dex", ModType.Base, 50, "Test");
        actor.ModDB.NewMod("Int", ModType.Base, 50, "Test");

        CalcPerform.DoActorAttributes(actor);

        Assert.True(actor.ModDB.Conditions["StrHighestAttribute"]);
    }

    [Fact]
    public void DoActorAttributes_TwoHighestEqual_Condition()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Dex", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Int", ModType.Base, 50, "Test");

        CalcPerform.DoActorAttributes(actor);

        Assert.True(actor.ModDB.Conditions["TwoHighestAttributesEqual"]);
    }

    [Fact]
    public void DoActorAttributes_NoAttributeBonuses_Flag()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Str", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("NoAttributeBonuses", ModType.Flag, true, "Test");

        CalcPerform.DoActorAttributes(actor);

        // Life bonus should NOT be applied
        Assert.Equal(0, actor.ModDB.Sum(ModType.Base, null, "Life"));
    }

    // ─── Life ───

    [Fact]
    public void DoActorLifeMana_BaseLifeOnly()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Life", ModType.Base, 100, "Test");

        CalcPerform.DoActorLifeMana(actor);

        Assert.Equal(100, actor.Output["Life"]);
    }

    [Fact]
    public void DoActorLifeMana_LifeWithInc()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Life", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Life", ModType.Inc, 50, "Test");

        CalcPerform.DoActorLifeMana(actor);

        Assert.Equal(150, actor.Output["Life"]);
    }

    [Fact]
    public void DoActorLifeMana_LifeWithMore()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Life", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Life", ModType.More, 20, "Test");

        CalcPerform.DoActorLifeMana(actor);

        Assert.Equal(120, actor.Output["Life"]);
    }

    [Fact]
    public void DoActorLifeMana_LifeWithIncAndMore()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Life", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("Life", ModType.Inc, 50, "Test");
        actor.ModDB.NewMod("Life", ModType.More, 20, "Test");

        CalcPerform.DoActorLifeMana(actor);

        // round(100 * 1.5 * 1.2) = round(180) = 180
        Assert.Equal(180, actor.Output["Life"]);
    }

    [Fact]
    public void DoActorLifeMana_ChaosInoculation_LifeIs1()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Life", ModType.Base, 5000, "Test");
        actor.ModDB.NewMod("ChaosInoculation", ModType.Flag, true, "Test");

        CalcPerform.DoActorLifeMana(actor);

        Assert.Equal(1, actor.Output["Life"]);
        Assert.True(actor.ModDB.Conditions["FullLife"]);
    }

    [Fact]
    public void DoActorLifeMana_LifeConvertToES()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Life", ModType.Base, 100, "Test");
        actor.ModDB.NewMod("LifeConvertToEnergyShield", ModType.Base, 50, "Test");

        CalcPerform.DoActorLifeMana(actor);

        // round(100 * 1.0 * 1.0 * (1 - 50/100)) = round(50) = 50
        Assert.Equal(50, actor.Output["Life"]);
    }

    [Fact]
    public void DoActorLifeMana_MinimumLife_Is1()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Life", ModType.Base, 1, "Test");
        actor.ModDB.NewMod("LifeConvertToEnergyShield", ModType.Base, 100, "Test");

        CalcPerform.DoActorLifeMana(actor);

        // round(1 * 1.0 * 0) = 0, clamped to 1
        Assert.Equal(1, actor.Output["Life"]);
    }

    // ─── Mana ───

    [Fact]
    public void DoActorLifeMana_BaseManaOnly()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Mana", ModType.Base, 200, "Test");

        CalcPerform.DoActorLifeMana(actor);

        Assert.Equal(200, actor.Output["Mana"]);
    }

    [Fact]
    public void DoActorLifeMana_ManaConvertToArmour()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Mana", ModType.Base, 200, "Test");
        actor.ModDB.NewMod("ManaConvertToArmour", ModType.Base, 50, "Test");

        CalcPerform.DoActorLifeMana(actor);

        // round(val(Mana) * (1 - 50/100)) = round(200 * 0.5) = 100
        Assert.Equal(100, actor.Output["Mana"]);
    }

    [Fact]
    public void DoActorLifeMana_LowestOfLifeAndMana()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("Life", ModType.Base, 300, "Test");
        actor.ModDB.NewMod("Mana", ModType.Base, 200, "Test");

        CalcPerform.DoActorLifeMana(actor);

        Assert.Equal(200, actor.Output["LowestOfMaximumLifeAndMaximumMana"]);
    }

    // ─── Reservation ───

    [Fact]
    public void DoActorLifeManaReservation_FlatReservation()
    {
        var actor = CreateActor();
        actor.Output["Life"] = 1000;
        actor.Output["Mana"] = 500;
        actor.ReservedLifeBase = 100;

        CalcPerform.DoActorLifeManaReservation(actor);

        Assert.Equal(100, actor.Output["LifeReserved"]);
        Assert.Equal(900, actor.Output["LifeUnreserved"]);
    }

    [Fact]
    public void DoActorLifeManaReservation_PercentReservation()
    {
        var actor = CreateActor();
        actor.Output["Life"] = 1000;
        actor.Output["Mana"] = 500;
        actor.ReservedManaPercent = 50;

        CalcPerform.DoActorLifeManaReservation(actor);

        // ceil(500 * 50 / 100) = 250
        Assert.Equal(250, actor.Output["ManaReserved"]);
        Assert.Equal(250, actor.Output["ManaUnreserved"]);
    }

    [Fact]
    public void DoActorLifeManaReservation_LowLife_Condition()
    {
        var actor = CreateActor();
        actor.Output["Life"] = 1000;
        actor.Output["Mana"] = 500;
        actor.ReservedLifePercent = 60;

        CalcPerform.DoActorLifeManaReservation(actor);

        // reserved = ceil(1000 * 60/100) = 600
        // unreserved = 400, 400/1000 = 0.4 < 0.5 → LowLife
        Assert.True(actor.ModDB.Conditions["LowLife"]);
    }

    [Fact]
    public void DoActorLifeManaReservation_NotLowLife()
    {
        var actor = CreateActor();
        actor.Output["Life"] = 1000;
        actor.Output["Mana"] = 500;
        actor.ReservedLifePercent = 10;

        CalcPerform.DoActorLifeManaReservation(actor);

        Assert.False(actor.ModDB.Conditions.TryGetValue("LowLife", out bool isLow) && isLow);
    }

    // ─── Charges ───

    [Fact]
    public void DoActorCharges_MaxPowerCharges()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(3, actor.Output["PowerChargesMax"]);
    }

    [Fact]
    public void DoActorCharges_ChargeOverride()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("PowerChargesMax", ModType.Override, 10, "Test");

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(10, actor.Output["PowerChargesMax"]);
    }

    [Fact]
    public void DoActorCharges_UsePowerCharges_SetsMultiplier()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("UsePowerCharges", ModType.Flag, true, "Config");

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(3, actor.Output["PowerCharges"]);
        Assert.Equal(3, actor.ModDB.Multipliers["PowerCharge"]);
    }

    [Fact]
    public void DoActorCharges_NoUseFlag_ChargesAreZero()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(0, actor.Output["PowerCharges"]);
        Assert.Equal(0, actor.Output["FrenzyCharges"]);
        Assert.Equal(0, actor.Output["EnduranceCharges"]);
    }

    [Fact]
    public void DoActorCharges_TotalCharges()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("UsePowerCharges", ModType.Flag, true, "Config");
        actor.ModDB.NewMod("UseFrenzyCharges", ModType.Flag, true, "Config");
        actor.ModDB.NewMod("UseEnduranceCharges", ModType.Flag, true, "Config");

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(9, actor.Output["TotalCharges"]); // 3 + 3 + 3
    }

    [Fact]
    public void DoActorCharges_EnduranceConvertToBrutal()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("UseEnduranceCharges", ModType.Flag, true, "Config");
        actor.ModDB.NewMod("EnduranceChargesConvertToBrutalCharges", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("MaximumEnduranceChargesEqualsMaximumBrutalCharges", ModType.Flag, true, "Test");

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(0, actor.Output["EnduranceCharges"]);
        Assert.Equal(3, actor.Output["BrutalCharges"]);
    }

    [Fact]
    public void DoActorCharges_PowerChargesConvertToAbsorption()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("UsePowerCharges", ModType.Flag, true, "Config");
        actor.ModDB.NewMod("PowerChargesConvertToAbsorptionCharges", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("MaximumPowerChargesEqualsMaximumAbsorptionCharges", ModType.Flag, true, "Test");

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(0, actor.Output["PowerCharges"]);
        Assert.Equal(3, actor.Output["AbsorptionCharges"]);
    }

    [Fact]
    public void DoActorCharges_FrenzyConvertToAffliction()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("UseFrenzyCharges", ModType.Flag, true, "Config");
        actor.ModDB.NewMod("FrenzyChargesConvertToAfflictionCharges", ModType.Flag, true, "Test");
        actor.ModDB.NewMod("MaximumFrenzyChargesEqualsMaximumAfflictionCharges", ModType.Flag, true, "Test");

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(0, actor.Output["FrenzyCharges"]);
        Assert.Equal(3, actor.Output["AfflictionCharges"]);
    }

    [Fact]
    public void DoActorCharges_HaveMaximumChargesFlag()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);
        actor.ModDB.NewMod("HaveMaximumPowerCharges", ModType.Flag, true, "Test");

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(3, actor.Output["PowerCharges"]);
    }

    [Fact]
    public void DoActorCharges_BloodChargesMax()
    {
        var actor = CreateActor();
        CalcSetup.InitModDB(actor.ModDB);

        CalcPerform.DoActorCharges(actor);

        Assert.Equal(5, actor.Output["BloodChargesMax"]);
        Assert.Equal(5, actor.Output["BloodCharges"]);
    }

    // ─── Action Speed ───

    [Fact]
    public void ActionSpeedMod_BaseValue()
    {
        var actor = CreateActor();

        double result = CalcPerform.ActionSpeedMod(actor);

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ActionSpeedMod_WithActionSpeed()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("ActionSpeed", ModType.Inc, 20, "Test");

        double result = CalcPerform.ActionSpeedMod(actor);

        Assert.Equal(1.2, result, 6);
    }

    [Fact]
    public void ActionSpeedMod_TemporalChainsCapped()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("TemporalChainsActionSpeed", ModType.Inc, -200, "Test");

        double result = CalcPerform.ActionSpeedMod(actor);

        // max(-75, -200) = -75. 1 + (-75)/100 = 0.25
        Assert.Equal(0.25, result, 6);
    }

    [Fact]
    public void ActionSpeedMod_WithMinimumActionSpeed()
    {
        var actor = CreateActor();
        actor.ModDB.NewMod("TemporalChainsActionSpeed", ModType.Inc, -200, "Test");
        actor.ModDB.NewMod("MinimumActionSpeed", ModType.Max, 50, "Test");

        double result = CalcPerform.ActionSpeedMod(actor);

        // Capped at 0.25, but minimum is 50/100 = 0.5
        Assert.Equal(0.5, result, 6);
    }
}
