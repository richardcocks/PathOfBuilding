using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Calculation functions for attributes, life/mana, charges, and action speed.
/// Ported from CalcPerform.lua: doActorAttribsConditions, doActorLifeMana,
/// doActorLifeManaReservation, doActorCharges, calcs.actionSpeedMod.
/// </summary>
public static class CalcPerform
{
    /// <summary>
    /// Calculate attributes (Str, Dex, Int), set attribute conditions, and apply attribute bonuses.
    /// Ported from CalcPerform.lua doActorAttribsConditions (lines 374-503).
    /// Simplified: skips item/weapon conditions (no items yet).
    /// </summary>
    public static void DoActorAttributes(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;
        var condList = modDB.Conditions;

        // Calculate twice because of circular dependency (X attribute higher than Y attribute)
        for (int pass = 0; pass < 2; pass++)
        {
            output["Str"] = Math.Max(CalcLib.Round(CalcLib.Val(modDB, "Str")), 0);
            output["Dex"] = Math.Max(CalcLib.Round(CalcLib.Val(modDB, "Dex")), 0);
            output["Int"] = Math.Max(CalcLib.Round(CalcLib.Val(modDB, "Int")), 0);

            double[] stats = { output["Str"], output["Dex"], output["Int"] };
            Array.Sort(stats);
            output["LowestAttribute"] = stats[0];
            condList["TwoHighestAttributesEqual"] = stats[1] == stats[2];

            condList["DexHigherThanInt"] = output["Dex"] > output["Int"];
            condList["StrHigherThanInt"] = output["Str"] > output["Int"];
            condList["IntHigherThanDex"] = output["Int"] > output["Dex"];
            condList["StrHigherThanDex"] = output["Str"] > output["Dex"];
            condList["IntHigherThanStr"] = output["Int"] > output["Str"];
            condList["DexHigherThanStr"] = output["Dex"] > output["Str"];

            condList["StrHighestAttribute"] = output["Str"] >= output["Dex"] && output["Str"] >= output["Int"];
            condList["IntHighestAttribute"] = output["Int"] >= output["Str"] && output["Int"] >= output["Dex"];
            condList["DexHighestAttribute"] = output["Dex"] >= output["Str"] && output["Dex"] >= output["Int"];
        }

        // Total attributes
        output["TotalAttr"] = output["Str"] + output["Dex"] + output["Int"];

        // Devotion
        output["Devotion"] = modDB.Sum(ModType.Base, null, "Devotion");

        // Add attribute bonuses
        if (!modDB.Flag(null, "NoAttributeBonuses"))
        {
            if (!modDB.Flag(null, "NoStrengthAttributeBonuses"))
            {
                if (!modDB.Flag(null, "NoStrBonusToLife"))
                {
                    modDB.NewMod("Life", ModType.Base, Math.Floor(output["Str"] / 2), "Strength");
                }

                double strDmgBonusRatioOverride = modDB.Sum(ModType.Base, null, "StrDmgBonusRatioOverride");
                double strDmgBonus;
                if (strDmgBonusRatioOverride > 0)
                {
                    strDmgBonus = Math.Floor((output["Str"] + modDB.Sum(ModType.Base, null, "DexIntToMeleeBonus")) * strDmgBonusRatioOverride);
                }
                else
                {
                    strDmgBonus = Math.Floor((output["Str"] + modDB.Sum(ModType.Base, null, "DexIntToMeleeBonus")) / 5);
                }
                modDB.NewMod("PhysicalDamage", ModType.Inc, strDmgBonus, "Strength", ModFlag.Melee);
            }

            if (!modDB.Flag(null, "NoDexterityAttributeBonuses"))
            {
                var dexAccOverride = modDB.Override(null, "DexAccBonusOverride");
                double accPerDex = dexAccOverride.HasValue ? dexAccOverride.Value.AsNumber() : MiscConstants.AccuracyPerDexBase;
                modDB.NewMod("Accuracy", ModType.Base, output["Dex"] * accPerDex, "Dexterity");
                if (!modDB.Flag(null, "NoDexBonusToEvasion"))
                {
                    modDB.NewMod("Evasion", ModType.Inc, Math.Floor(output["Dex"] / 5), "Dexterity");
                }
            }

            if (!modDB.Flag(null, "NoIntelligenceAttributeBonuses"))
            {
                if (!modDB.Flag(null, "NoIntBonusToMana"))
                {
                    modDB.NewMod("Mana", ModType.Base, Math.Floor(output["Int"] / 2), "Intelligence");
                }
                if (!modDB.Flag(null, "NoIntBonusToES"))
                {
                    modDB.NewMod("EnergyShield", ModType.Inc, Math.Floor(output["Int"] / 5), "Intelligence");
                }
            }
        }
    }

    /// <summary>
    /// Calculate life and mana pools.
    /// Ported from CalcPerform.lua doActorLifeMana (lines 68-130).
    /// </summary>
    public static void DoActorLifeMana(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;
        var condList = modDB.Conditions;

        double lowLifePerc = modDB.Sum(ModType.Base, null, "LowLifePercentage");
        output["LowLifePercentage"] = 100.0 * (lowLifePerc > 0 ? lowLifePerc : MiscConstants.LowPoolThreshold);
        double fullLifePerc = modDB.Sum(ModType.Base, null, "FullLifePercentage");
        output["FullLifePercentage"] = 100.0 * (fullLifePerc > 0 ? fullLifePerc : 1.0);

        bool chaosInoculation = modDB.Flag(null, "ChaosInoculation");
        output["ChaosInoculation"] = chaosInoculation ? 1 : 0;

        if (chaosInoculation)
        {
            output["Life"] = 1;
            condList["FullLife"] = true;
        }
        else
        {
            double baseLife = modDB.Sum(ModType.Base, null, "Life");
            double inc = modDB.Sum(ModType.Inc, null, "Life");
            double more = modDB.More(null, "Life");
            var overrideVal = modDB.Override(null, "Life");
            double conv = modDB.Sum(ModType.Base, null, "LifeConvertToEnergyShield");

            if (overrideVal.HasValue)
            {
                output["Life"] = overrideVal.Value.AsNumber();
            }
            else
            {
                output["Life"] = Math.Max(CalcLib.Round(baseLife * (1 + inc / 100.0) * more * (1 - conv / 100.0)), 1);
            }
        }

        double manaConv = modDB.Sum(ModType.Base, null, "ManaConvertToArmour");
        output["Mana"] = CalcLib.Round(CalcLib.Val(modDB, "Mana") * (1 - manaConv / 100.0));

        output["LowestOfMaximumLifeAndMaximumMana"] = Math.Min(output["Life"], output["Mana"]);
    }

    /// <summary>
    /// Calculate life/mana reservation.
    /// Ported from CalcPerform.lua doActorLifeManaReservation (lines 510-541).
    /// Simplified: uses Actor reserved fields instead of full aura system.
    /// </summary>
    public static void DoActorLifeManaReservation(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;
        var condList = modDB.Conditions;

        foreach (var pool in new[] { "Life", "Mana" })
        {
            double max = output.TryGetValue(pool, out double maxVal) ? maxVal : 0;
            double reserved;
            if (max > 0)
            {
                double reservedBase = actor.GetReservedBase(pool);
                double reservedPercent = actor.GetReservedPercent(pool);
                reserved = reservedBase + Math.Ceiling(max * reservedPercent / 100.0);
                output[$"{pool}Reserved"] = Math.Min(reserved, max);
                output[$"{pool}ReservedPercent"] = Math.Min(reserved / max * 100.0, 100);
                output[$"{pool}Unreserved"] = max - reserved;
                output[$"{pool}UnreservedPercent"] = (max - reserved) / max * 100.0;

                double lowPerc = modDB.Sum(ModType.Base, null, $"Low{pool}Percentage");
                double threshold = lowPerc > 0 ? lowPerc : MiscConstants.LowPoolThreshold;
                if ((max - reserved) / max <= threshold)
                {
                    condList[$"Low{pool}"] = true;
                }
            }
            else
            {
                reserved = 0;
            }
        }
    }

    /// <summary>
    /// Calculate current and maximum charges, set multipliers.
    /// Ported from CalcPerform.lua doActorCharges (lines 892-1038).
    /// Simplified: no party members, no env.player distinction.
    /// </summary>
    public static void DoActorCharges(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        // Calculate max charges
        output["PowerChargesMin"] = Math.Max(modDB.Sum(ModType.Base, null, "PowerChargesMin"), 0);
        output["PowerChargesMax"] = modDB.Override(null, "PowerChargesMax")?.AsNumber()
            ?? Math.Max(modDB.Sum(ModType.Base, null, "PowerChargesMax"), 0);
        output["PowerChargesDuration"] = Math.Floor(modDB.Sum(ModType.Base, null, "ChargeDuration") * CalcLib.Mod(modDB, null, "PowerChargesDuration", "ChargeDuration"));

        if (modDB.Flag(null, "MaximumFrenzyChargesIsMaximumPowerCharges"))
        {
            modDB.ReplaceMod("FrenzyChargesMax", ModType.Override, output["PowerChargesMax"], "MaximumFrenzyChargesIsMaximumPowerCharges");
        }

        output["FrenzyChargesMin"] = Math.Max(modDB.Sum(ModType.Base, null, "FrenzyChargesMin"), 0);
        output["FrenzyChargesMax"] = modDB.Override(null, "FrenzyChargesMax")?.AsNumber()
            ?? Math.Max(
                modDB.Flag(null, "MaximumFrenzyChargesIsMaximumPowerCharges")
                    ? output["PowerChargesMax"]
                    : modDB.Sum(ModType.Base, null, "FrenzyChargesMax"),
                0);
        output["FrenzyChargesDuration"] = Math.Floor(modDB.Sum(ModType.Base, null, "ChargeDuration") * CalcLib.Mod(modDB, null, "FrenzyChargesDuration", "ChargeDuration"));

        if (modDB.Flag(null, "MaximumEnduranceChargesIsMaximumFrenzyCharges"))
        {
            modDB.ReplaceMod("EnduranceChargesMax", ModType.Override, output["FrenzyChargesMax"], "MaximumEnduranceChargesIsMaximumFrenzyCharges");
        }

        output["EnduranceChargesMin"] = Math.Max(modDB.Sum(ModType.Base, null, "EnduranceChargesMin"), 0);
        output["EnduranceChargesMax"] = modDB.Override(null, "EnduranceChargesMax")?.AsNumber()
            ?? Math.Max(
                modDB.Flag(null, "MaximumEnduranceChargesIsMaximumFrenzyCharges")
                    ? output["FrenzyChargesMax"]
                    : modDB.Sum(ModType.Base, null, "EnduranceChargesMax"),
                0);
        output["EnduranceChargesDuration"] = Math.Floor(modDB.Sum(ModType.Base, null, "ChargeDuration") * CalcLib.Mod(modDB, null, "EnduranceChargesDuration", "ChargeDuration"));

        output["SiphoningChargesMax"] = Math.Max(modDB.Sum(ModType.Base, null, "SiphoningChargesMax"), 0);
        output["ChallengerChargesMax"] = Math.Max(modDB.Sum(ModType.Base, null, "ChallengerChargesMax"), 0);
        output["BlitzChargesMax"] = Math.Max(modDB.Sum(ModType.Base, null, "BlitzChargesMax"), 0);
        output["InspirationChargesMax"] = Math.Max(modDB.Sum(ModType.Base, null, "InspirationChargesMax"), 0);
        output["CrabBarriersMax"] = Math.Max(modDB.Sum(ModType.Base, null, "CrabBarriersMax"), 0);

        // Brutal/Absorption/Affliction charges (converted from endurance/power/frenzy)
        output["BrutalChargesMin"] = Math.Max(
            modDB.Flag(null, "MinimumEnduranceChargesEqualsMinimumBrutalCharges")
                ? (modDB.Flag(null, "MinimumEnduranceChargesIsMaximumEnduranceCharges") ? output["EnduranceChargesMax"] : output["EnduranceChargesMin"])
                : 0,
            0);
        output["BrutalChargesMax"] = Math.Max(
            modDB.Flag(null, "MaximumEnduranceChargesEqualsMaximumBrutalCharges") ? output["EnduranceChargesMax"] : 0, 0);

        output["AbsorptionChargesMin"] = Math.Max(
            modDB.Flag(null, "MinimumPowerChargesEqualsMinimumAbsorptionCharges")
                ? (modDB.Flag(null, "MinimumPowerChargesIsMaximumPowerCharges") ? output["PowerChargesMax"] : output["PowerChargesMin"])
                : 0,
            0);
        output["AbsorptionChargesMax"] = Math.Max(
            modDB.Flag(null, "MaximumPowerChargesEqualsMaximumAbsorptionCharges") ? output["PowerChargesMax"] : 0, 0);

        output["AfflictionChargesMin"] = Math.Max(
            modDB.Flag(null, "MinimumFrenzyChargesEqualsMinimumAfflictionCharges")
                ? (modDB.Flag(null, "MinimumFrenzyChargesIsMaximumFrenzyCharges") ? output["FrenzyChargesMax"] : output["FrenzyChargesMin"])
                : 0,
            0);
        output["AfflictionChargesMax"] = Math.Max(
            modDB.Flag(null, "MaximumFrenzyChargesEqualsMaximumAfflictionCharges") ? output["FrenzyChargesMax"] : 0, 0);

        output["BloodChargesMax"] = Math.Max(modDB.Sum(ModType.Base, null, "BloodChargesMax"), 0);
        output["SpiritChargesMax"] = Math.Max(modDB.Sum(ModType.Base, null, "SpiritChargesMax"), 0);

        // Initialize current charges to 0
        output["PowerCharges"] = 0;
        output["FrenzyCharges"] = 0;
        output["EnduranceCharges"] = 0;
        output["SiphoningCharges"] = 0;
        output["ChallengerCharges"] = 0;
        output["BlitzCharges"] = 0;
        output["InspirationCharges"] = 0;
        output["GhostShrouds"] = 0;
        output["BrutalCharges"] = 0;
        output["AbsorptionCharges"] = 0;
        output["AfflictionCharges"] = 0;
        output["BloodCharges"] = 0;
        output["SpiritCharges"] = 0;

        // Min = Max overrides
        if (modDB.Flag(null, "MinimumFrenzyChargesIsMaximumFrenzyCharges"))
            output["FrenzyChargesMin"] = output["FrenzyChargesMax"];
        if (modDB.Flag(null, "MinimumEnduranceChargesIsMaximumEnduranceCharges"))
            output["EnduranceChargesMin"] = output["EnduranceChargesMax"];
        if (modDB.Flag(null, "MinimumPowerChargesIsMaximumPowerCharges"))
            output["PowerChargesMin"] = output["PowerChargesMax"];

        // Use charges flags
        if (modDB.Flag(null, "UsePowerCharges"))
            output["PowerCharges"] = modDB.Override(null, "PowerCharges")?.AsNumber() ?? output["PowerChargesMax"];

        if (modDB.Flag(null, "PowerChargesConvertToAbsorptionCharges"))
        {
            output["AbsorptionCharges"] = Math.Max(output["PowerCharges"], Math.Min(output["AbsorptionChargesMax"], output["AbsorptionChargesMin"]));
            output["PowerCharges"] = 0;
        }
        else
        {
            output["PowerCharges"] = Math.Max(output["PowerCharges"], Math.Min(output["PowerChargesMax"], output["PowerChargesMin"]));
        }
        output["RemovablePowerCharges"] = Math.Max(output["PowerCharges"] - output["PowerChargesMin"], 0);

        if (modDB.Flag(null, "UseFrenzyCharges"))
            output["FrenzyCharges"] = modDB.Override(null, "FrenzyCharges")?.AsNumber() ?? output["FrenzyChargesMax"];

        if (modDB.Flag(null, "FrenzyChargesConvertToAfflictionCharges"))
        {
            output["AfflictionCharges"] = Math.Max(output["FrenzyCharges"], Math.Min(output["AfflictionChargesMax"], output["AfflictionChargesMin"]));
            output["FrenzyCharges"] = 0;
        }
        else
        {
            output["FrenzyCharges"] = Math.Max(output["FrenzyCharges"], Math.Min(output["FrenzyChargesMax"], output["FrenzyChargesMin"]));
        }
        output["RemovableFrenzyCharges"] = Math.Max(output["FrenzyCharges"] - output["FrenzyChargesMin"], 0);

        if (modDB.Flag(null, "UseEnduranceCharges"))
            output["EnduranceCharges"] = modDB.Override(null, "EnduranceCharges")?.AsNumber() ?? output["EnduranceChargesMax"];

        if (modDB.Flag(null, "EnduranceChargesConvertToBrutalCharges"))
        {
            output["BrutalCharges"] = Math.Max(output["EnduranceCharges"], Math.Min(output["BrutalChargesMax"], output["BrutalChargesMin"]));
            output["EnduranceCharges"] = 0;
        }
        else
        {
            output["EnduranceCharges"] = Math.Max(output["EnduranceCharges"], Math.Min(output["EnduranceChargesMax"], output["EnduranceChargesMin"]));
        }
        output["RemovableEnduranceCharges"] = Math.Max(output["EnduranceCharges"] - output["EnduranceChargesMin"], 0);

        if (modDB.Flag(null, "UseSiphoningCharges"))
            output["SiphoningCharges"] = modDB.Override(null, "SiphoningCharges")?.AsNumber() ?? output["SiphoningChargesMax"];
        if (modDB.Flag(null, "UseChallengerCharges"))
            output["ChallengerCharges"] = modDB.Override(null, "ChallengerCharges")?.AsNumber() ?? output["ChallengerChargesMax"];
        if (modDB.Flag(null, "UseBlitzCharges"))
            output["BlitzCharges"] = modDB.Override(null, "BlitzCharges")?.AsNumber() ?? output["BlitzChargesMax"];

        // Inspiration charges (simplified — always grant max for player)
        output["InspirationCharges"] = modDB.Override(null, "InspirationCharges")?.AsNumber() ?? output["InspirationChargesMax"];

        if (modDB.Flag(null, "UseGhostShrouds"))
            output["GhostShrouds"] = modDB.Override(null, "GhostShrouds")?.AsNumber() ?? 3;

        output["BloodCharges"] = Math.Min(modDB.Override(null, "BloodCharges")?.AsNumber() ?? output["BloodChargesMax"], output["BloodChargesMax"]);
        output["SpiritCharges"] = Math.Min(modDB.Override(null, "SpiritCharges")?.AsNumber() ?? 0, output["SpiritChargesMax"]);
        output["CrabBarriers"] = Math.Min(modDB.Override(null, "CrabBarriers")?.AsNumber() ?? output["CrabBarriersMax"], output["CrabBarriersMax"]);

        // Force max charge flags
        if (modDB.Flag(null, "HaveMaximumPowerCharges"))
            output["PowerCharges"] = output["PowerChargesMax"];
        if (modDB.Flag(null, "HaveMaximumFrenzyCharges"))
            output["FrenzyCharges"] = output["FrenzyChargesMax"];
        if (modDB.Flag(null, "HaveMaximumEnduranceCharges"))
            output["EnduranceCharges"] = output["EnduranceChargesMax"];

        // Totals
        output["TotalCharges"] = output["PowerCharges"] + output["FrenzyCharges"] + output["EnduranceCharges"];
        output["RemovableTotalCharges"] = output["RemovableEnduranceCharges"] + output["RemovableFrenzyCharges"] + output["RemovablePowerCharges"];

        // Set multipliers
        modDB.Multipliers["PowerCharge"] = output["PowerCharges"];
        modDB.Multipliers["PowerChargeMax"] = output["PowerChargesMax"];
        modDB.Multipliers["RemovablePowerCharge"] = output["RemovablePowerCharges"];
        modDB.Multipliers["FrenzyCharge"] = output["FrenzyCharges"];
        modDB.Multipliers["RemovableFrenzyCharge"] = output["RemovableFrenzyCharges"];
        modDB.Multipliers["EnduranceCharge"] = output["EnduranceCharges"];
        modDB.Multipliers["RemovableEnduranceCharge"] = output["RemovableEnduranceCharges"];
        modDB.Multipliers["TotalCharges"] = output["TotalCharges"];
        modDB.Multipliers["RemovableTotalCharges"] = output["RemovableTotalCharges"];
        modDB.Multipliers["SiphoningCharge"] = output["SiphoningCharges"];
        modDB.Multipliers["ChallengerCharge"] = output["ChallengerCharges"];
        modDB.Multipliers["BlitzCharge"] = output["BlitzCharges"];
        modDB.Multipliers["InspirationCharge"] = output["InspirationCharges"];
        modDB.Multipliers["GhostShroud"] = output["GhostShrouds"];
        modDB.Multipliers["CrabBarrier"] = output["CrabBarriers"];
        modDB.Multipliers["BrutalCharge"] = output["BrutalCharges"];
        modDB.Multipliers["AbsorptionCharge"] = output["AbsorptionCharges"];
        modDB.Multipliers["AfflictionCharge"] = output["AfflictionCharges"];
        modDB.Multipliers["BloodCharge"] = output["BloodCharges"];
        modDB.Multipliers["SpiritCharge"] = output["SpiritCharges"];
    }

    /// <summary>
    /// Calculate action speed modifier.
    /// Ported from CalcPerform.lua calcs.actionSpeedMod (lines 1041-1051).
    /// </summary>
    public static double ActionSpeedMod(Actor actor)
    {
        var modDB = actor.ModDB;
        double minimumActionSpeed = modDB.Max(null, "MinimumActionSpeed") ?? 0;
        double temporalChainsActionSpeed = modDB.Sum(ModType.Inc, null, "TemporalChainsActionSpeed");
        double actionSpeed = modDB.Sum(ModType.Inc, null, "ActionSpeed");

        double actionSpeedMod = 1 + (Math.Max(-MiscConstants.TemporalChainsEffectCap, temporalChainsActionSpeed) + actionSpeed) / 100.0;
        actionSpeedMod = Math.Max(minimumActionSpeed / 100.0, actionSpeedMod);

        double? maximumActionSpeedReduction = modDB.Max(null, "MaximumActionSpeedReduction");
        if (maximumActionSpeedReduction.HasValue)
        {
            actionSpeedMod = Math.Min((100 - maximumActionSpeedReduction.Value) / 100.0, actionSpeedMod);
        }

        return actionSpeedMod;
    }
}
