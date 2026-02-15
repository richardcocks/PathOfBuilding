using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Combat conditions and shrine buffs.
/// Ported from CalcPerform.lua lines 232-371.
/// Must run before DoActorLifeMana (shrines modify Life INC).
/// </summary>
public static class CalcConditions
{
    /// <summary>
    /// Set combat conditions from CalcConfig booleans, ailment infliction, and exposure.
    /// Ported from CalcPerform.lua lines 232-371.
    /// </summary>
    public static void CombatConditions(Actor actor)
    {
        var config = actor.Config;
        if (!config.ModeCombat)
            return;

        var modDB = actor.ModDB;
        var condList = modDB.Conditions;

        // Combat conditions from config
        if (config.AttackedRecently) condList["AttackedRecently"] = true;
        if (config.CastSpellRecently) condList["CastSpellRecently"] = true;
        if (config.UsedMovementSkillRecently) condList["UsedMovementSkillRecently"] = true;
        if (config.UsedMinionSkillRecently) condList["UsedMinionSkillRecently"] = true;
        if (config.UsedVaalSkillRecently) condList["UsedVaalSkillRecently"] = true;
        if (config.Channelling) condList["Channelling"] = true;
        if (config.HitRecently) condList["HitRecently"] = true;
        if (config.HitSpellRecently) condList["HitSpellRecently"] = true;
        if (config.HaveTotem) condList["HaveTotem"] = true;
        if (config.SummonedTotemRecently) condList["SummonedTotemRecently"] = true;
        if (config.TotemsHitRecently) condList["TotemsHitRecently"] = true;
        if (config.TotemsSpellHitRecently) condList["TotemsSpellHitRecently"] = true;
        if (config.DetonatedMinesRecently) condList["DetonatedMinesRecently"] = true;
        if (config.TriggeredTrapsRecently) condList["TriggeredTrapsRecently"] = true;

        // Ailment infliction conditions
        if (modDB.Sum(ModType.Base, null, "EnemyScorchChance") > 0
            || modDB.Flag(null, "CritAlwaysAltAilments")
            || modDB.Flag(null, "IgniteCanScorch"))
        {
            condList["CanInflictScorch"] = true;
        }

        if (modDB.Sum(ModType.Base, null, "EnemyBrittleChance") > 0
            || modDB.Flag(null, "CritAlwaysAltAilments"))
        {
            condList["CanInflictBrittle"] = true;
        }

        if (modDB.Sum(ModType.Base, null, "EnemySapChance") > 0
            || modDB.Flag(null, "CritAlwaysAltAilments"))
        {
            condList["CanInflictSap"] = true;
        }

        // Exposure conditions (gated on ModeEffective)
        if (config.ModeEffective)
        {
            if (modDB.Sum(ModType.Base, null, "FireExposureChance") > 0)
                condList["CanApplyFireExposure"] = true;
            if (modDB.Sum(ModType.Base, null, "ColdExposureChance") > 0)
                condList["CanApplyColdExposure"] = true;
            if (modDB.Sum(ModType.Base, null, "LightningExposureChance") > 0)
                condList["CanApplyLightningExposure"] = true;
        }
    }

    /// <summary>
    /// Apply shrine buffs to modDB.
    /// Must run before life pool calculations (MassiveShrine modifies Life INC).
    /// Ported from CalcPerform.lua lines 284-359.
    /// </summary>
    public static void ShrineBuffs(Actor actor)
    {
        if (!actor.Config.ModeCombat)
            return;

        var modDB = actor.ModDB;
        double shrineEffectMod = 1 + modDB.Sum(ModType.Inc, null, "BuffEffectOnSelf", "ShrineBuffEffect") / 100;

        if (modDB.Flag(null, "AccelerationShrine"))
        {
            modDB.NewMod("ActionSpeed", ModType.Inc, Math.Floor(50 * shrineEffectMod), "Acceleration Shrine");
            modDB.NewMod("ProjectileSpeed", ModType.Inc, Math.Floor(80 * shrineEffectMod), "Acceleration Shrine");
        }
        if (modDB.Flag(null, "BrutalShrine"))
        {
            modDB.NewMod("Damage", ModType.Inc, Math.Floor(50 * shrineEffectMod), "Brutal Shrine");
            modDB.NewMod("EnemyStunDuration", ModType.Inc, Math.Floor(30 * shrineEffectMod), "Brutal Shrine");
            modDB.NewMod("EnemyKnockbackChance", ModType.Inc, 100, "Brutal Shrine");
        }
        if (modDB.Flag(null, "DiamondShrine"))
        {
            modDB.NewMod("CritChance", ModType.Override, 100, "Diamond Shrine");
        }
        if (modDB.Flag(null, "DivineShrine"))
        {
            modDB.NewMod("DamageTaken", ModType.More, -100, "Divine Shrine");
        }
        if (modDB.Flag(null, "EchoingShrine"))
        {
            modDB.NewMod("Speed", ModType.More, Math.Floor(100 * shrineEffectMod), "Echoing Shrine", ModFlag.Attack);
            modDB.NewMod("Speed", ModType.More, Math.Floor(100 * shrineEffectMod), "Echoing Shrine", ModFlag.Cast);
            modDB.NewMod("RepeatCount", ModType.Base, Math.Floor(1 * shrineEffectMod), "Echoing Shrine");
        }
        if (modDB.Flag(null, "GloomShrine"))
        {
            modDB.NewMod("NonChaosDamageGainAsChaos", ModType.Base, Math.Floor(10 * shrineEffectMod), "Gloom Shrine");
        }
        if (modDB.Flag(null, "ImpenetrableShrine"))
        {
            modDB.NewMod("Armour", ModType.Inc, Math.Floor(100 * shrineEffectMod), "Impenetrable Shrine");
            modDB.NewMod("Evasion", ModType.Inc, Math.Floor(100 * shrineEffectMod), "Impenetrable Shrine");
            modDB.NewMod("EnergyShield", ModType.Inc, Math.Floor(100 * shrineEffectMod), "Impenetrable Shrine");
        }
        if (modDB.Flag(null, "MassiveShrine"))
        {
            modDB.NewMod("Life", ModType.Inc, Math.Floor(40 * shrineEffectMod), "Massive Shrine");
            modDB.NewMod("AreaOfEffect", ModType.Inc, Math.Floor(40 * shrineEffectMod), "Massive Shrine");
        }
        if (modDB.Flag(null, "ReplenishingShrine"))
        {
            modDB.NewMod("ManaRegenPercent", ModType.Base, 10 * shrineEffectMod, "Replenishing Shrine");
            modDB.NewMod("LifeRegenPercent", ModType.Base, 6.7 * shrineEffectMod, "Replenishing Shrine");
        }
        if (modDB.Flag(null, "ResistanceShrine"))
        {
            modDB.NewMod("ElementalResist", ModType.Base, Math.Floor(50 * shrineEffectMod), "Resistance Shrine");
            modDB.NewMod("ElementalResistMax", ModType.Base, Math.Floor(10 * shrineEffectMod), "Resistance Shrine");
        }
        if (modDB.Flag(null, "ResonatingShrine"))
        {
            modDB.NewMod("CritChance", ModType.Inc, Math.Floor(50 * shrineEffectMod), "Resonating Shrine",
                tags: new MultiplierTag { Var = "PowerCharge" });
            modDB.NewMod("Speed", ModType.Inc, Math.Floor(4 * shrineEffectMod), "Resonating Shrine", ModFlag.Attack,
                tags: new MultiplierTag { Var = "FrenzyCharge" });
            modDB.NewMod("Speed", ModType.Inc, Math.Floor(4 * shrineEffectMod), "Resonating Shrine", ModFlag.Cast,
                tags: new MultiplierTag { Var = "FrenzyCharge" });
            modDB.NewMod("Damage", ModType.More, Math.Floor(4 * shrineEffectMod), "Resonating Shrine",
                tags: new MultiplierTag { Var = "FrenzyCharge" });
            modDB.NewMod("PhysicalDamageReduction", ModType.Base, Math.Floor(4 * shrineEffectMod), "Resonating Shrine",
                tags: new MultiplierTag { Var = "EnduranceCharge" });
            modDB.NewMod("ElementalDamageReduction", ModType.Base, Math.Floor(4 * shrineEffectMod), "Resonating Shrine",
                tags: new MultiplierTag { Var = "EnduranceCharge" });
            modDB.NewMod("Damage", ModType.Inc, Math.Floor(5 * shrineEffectMod), "Resonating Shrine",
                tags: new MultiplierTag { VarList = ["PowerCharge", "FrenzyCharge", "EnduranceCharge"] });
        }

        // Lesser shrine variants (gated on not having the full version)
        if (modDB.Flag(null, "LesserAccelerationShrine") && !modDB.Flag(null, "AccelerationShrine"))
        {
            modDB.NewMod("ActionSpeed", ModType.Inc, Math.Floor(10 * shrineEffectMod), "Lesser Acceleration Shrine");
            modDB.NewMod("ProjectileSpeed", ModType.Inc, Math.Floor(30 * shrineEffectMod), "Lesser Acceleration Shrine");
        }
        if (modDB.Flag(null, "LesserBrutalShrine"))
        {
            modDB.NewMod("Damage", ModType.Inc, Math.Floor(20 * shrineEffectMod), "Lesser Brutal Shrine");
            modDB.NewMod("EnemyStunDuration", ModType.Inc, Math.Floor(20 * shrineEffectMod), "Lesser Brutal Shrine");
            modDB.NewMod("EnemyKnockbackChance", ModType.Inc, 100, "Lesser Brutal Shrine");
        }
        if (modDB.Flag(null, "LesserImpenetrableShrine"))
        {
            modDB.NewMod("Armour", ModType.Inc, Math.Floor(50 * shrineEffectMod), "Lesser Impenetrable Shrine");
            modDB.NewMod("Evasion", ModType.Inc, Math.Floor(50 * shrineEffectMod), "Lesser Impenetrable Shrine");
            modDB.NewMod("EnergyShield", ModType.Inc, Math.Floor(50 * shrineEffectMod), "Lesser Impenetrable Shrine");
        }
        if (modDB.Flag(null, "LesserMassiveShrine"))
        {
            modDB.NewMod("Life", ModType.Inc, Math.Floor(20 * shrineEffectMod), "Lesser Massive Shrine");
            modDB.NewMod("AreaOfEffect", ModType.Inc, Math.Floor(20 * shrineEffectMod), "Lesser Massive Shrine");
        }
        if (modDB.Flag(null, "LesserReplenishingShrine"))
        {
            modDB.NewMod("ManaRegenPercent", ModType.Base, 3.3 * shrineEffectMod, "Lesser Replenishing Shrine");
            modDB.NewMod("LifeRegenPercent", ModType.Base, 3.3 * shrineEffectMod, "Lesser Replenishing Shrine");
        }
        if (modDB.Flag(null, "LesserResistanceShrine"))
        {
            modDB.NewMod("ElementalResist", ModType.Base, Math.Floor(25 * shrineEffectMod), "Lesser Resistance Shrine");
            modDB.NewMod("ElementalResistMax", ModType.Base, Math.Floor(2 * shrineEffectMod), "Lesser Resistance Shrine");
        }
    }
}
