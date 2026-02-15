using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Represents pools available to absorb damage.
/// Used by the iterative number-of-hits-to-die calculation.
/// </summary>
public class PoolTable
{
    public double Ward { get; set; }
    public double EnergyShield { get; set; }
    public double Mana { get; set; }
    public double Life { get; set; }
    public double OverkillDamage { get; set; }
}

/// <summary>
/// EHP (Effective Hit Pool) calculation: damage shifting, taken multipliers,
/// enemy damage, hit reduction pipeline, pool reduction, number-of-hits-to-die, final EHP.
/// Ported from CalcDefence.lua lines 119-404 (helpers) and lines 1604-2880 (buildDefenceEstimations).
/// </summary>
public static class CalcEHP
{
    private static readonly string[] DmgTypeList = DamageTypeFlags.DmgTypeList;
    private static bool IsElemental(string t) => t is "Fire" or "Cold" or "Lightning";

    /// <summary>
    /// Main entry point: builds all defence estimations including damage shifting,
    /// taken multipliers, enemy damage, hit reduction, and final EHP.
    /// </summary>
    public static void BuildDefenceEstimations(Actor actor)
    {
        var config = actor.Config;
        string damageCategoryConfig = config.EnemyDamageType;

        BuildDamageShiftTables(actor);
        DamageTakenMultipliers(actor);
        EnemyDamageCalculation(actor);
        IncomingHitDamage(actor);

        // ES bypass (simplified)
        SetupEnergyShieldBypass(actor);
        // MoM
        SetupMindOverMatter(actor);
        // Life recoverable
        SetupLifeRecoverable(actor);
        // Total pools
        SetupTotalPools(actor);

        if (damageCategoryConfig != "DamageOverTime")
        {
            NotHitChance(actor);
            ComputeHitsAndEHP(actor);
        }
    }

    // ─── Step 6: Damage Shift Tables ───

    internal static void BuildDamageShiftTables(Actor actor)
    {
        var modDB = actor.ModDB;
        actor.DamageShiftTable = new Dictionary<string, Dictionary<string, double>>();

        foreach (string damageType in DmgTypeList)
        {
            var shiftTable = new Dictionary<string, double>();
            double destTotal = 0;

            foreach (string destType in DmgTypeList)
            {
                if (destType != damageType)
                {
                    double shift = modDB.Sum(ModType.Base, null,
                        $"{damageType}DamageTakenAs{destType}",
                        IsElemental(damageType) ? $"ElementalDamageTakenAs{destType}" : "");
                    shift += modDB.Sum(ModType.Base, null,
                        $"{damageType}DamageFromHitsTakenAs{destType}",
                        IsElemental(damageType) ? $"ElementalDamageFromHitsTakenAs{destType}" : "");
                    shiftTable[destType] = shift;
                    destTotal += shift;
                }
            }

            shiftTable[damageType] = Math.Max(100 - destTotal, 0);
            actor.DamageShiftTable[damageType] = shiftTable;
        }
    }

    // ─── Step 6: Damage Taken Multipliers ───

    internal static void DamageTakenMultipliers(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;
        string damageCategoryConfig = actor.Config.EnemyDamageType;

        output["AnyTakenReflect"] = 0;

        foreach (string damageType in DmgTypeList)
        {
            double baseTakenInc = modDB.Sum(ModType.Inc, null, "DamageTaken", $"{damageType}DamageTaken");
            double baseTakenMore = modDB.More(null, "DamageTaken", $"{damageType}DamageTaken");
            if (IsElemental(damageType))
            {
                baseTakenInc += modDB.Sum(ModType.Inc, null, "ElementalDamageTaken");
                baseTakenMore *= modDB.More(null, "ElementalDamageTaken");
            }

            // Hit
            {
                double takenInc = baseTakenInc + modDB.Sum(ModType.Inc, null, "DamageTakenWhenHit", $"{damageType}DamageTakenWhenHit");
                double takenMore = baseTakenMore * modDB.More(null, "DamageTakenWhenHit", $"{damageType}DamageTakenWhenHit");
                if (IsElemental(damageType))
                {
                    takenInc += modDB.Sum(ModType.Inc, null, "ElementalDamageTakenWhenHit");
                    takenMore *= modDB.More(null, "ElementalDamageTakenWhenHit");
                }
                output[$"{damageType}TakenHitMult"] = Math.Max((1 + takenInc / 100) * takenMore, 0);

                // Per hit-source-type multipliers
                foreach (string hitType in new[] { "Attack", "Spell" })
                {
                    double htInc = takenInc + modDB.Sum(ModType.Inc, null, $"{hitType}DamageTaken");
                    double htMore = takenMore * modDB.More(null, $"{hitType}DamageTaken");
                    output[$"{hitType}TakenHitMult"] = Math.Max((1 + htInc / 100) * htMore, 0);
                    output[$"{damageType}{hitType}TakenHitMult"] = output[$"{hitType}TakenHitMult"];
                }
            }

            // DoT
            {
                double takenInc = baseTakenInc + modDB.Sum(ModType.Inc, null, "DamageTakenOverTime", $"{damageType}DamageTakenOverTime");
                double takenMore = baseTakenMore * modDB.More(null, "DamageTakenOverTime", $"{damageType}DamageTakenOverTime");
                if (IsElemental(damageType))
                {
                    takenInc += modDB.Sum(ModType.Inc, null, "ElementalDamageTakenOverTime");
                    takenMore *= modDB.More(null, "ElementalDamageTakenOverTime");
                }
                double resist = modDB.Flag(null, $"SelfIgnore{damageType}Resistance") ? 0
                    : (output.TryGetValue($"{damageType}ResistOverTime", out double rot) ? rot
                        : output.TryGetValue($"{damageType}Resist", out double r) ? r : 0);
                double reduction = modDB.Flag(null, $"SelfIgnoreBase{damageType}DamageReduction") ? 0
                    : (output.TryGetValue($"Base{damageType}DamageReduction", out double red) ? red : 0);
                output[$"{damageType}TakenDotMult"] = Math.Max(
                    (1 - resist / 100) * (1 - reduction / 100) * (1 + takenInc / 100) * takenMore, 0);
            }

            // takenFlat
            double takenFlat = modDB.Sum(ModType.Base, null, "DamageTaken", $"{damageType}DamageTaken",
                "DamageTakenWhenHit", $"{damageType}DamageTakenWhenHit");
            if (damageCategoryConfig == "Melee" || damageCategoryConfig == "Projectile")
            {
                takenFlat += modDB.Sum(ModType.Base, null, "DamageTakenFromAttacks", $"{damageType}DamageTakenFromAttacks",
                    $"{damageType}DamageTakenFrom{damageCategoryConfig}Attacks");
            }
            else if (damageCategoryConfig == "Spell" || damageCategoryConfig == "SpellProjectile")
            {
                takenFlat += modDB.Sum(ModType.Base, null, "DamageTakenFromSpells", $"{damageType}DamageTakenFromSpells",
                    $"{damageType}DamageTakenFromSpellProjectiles");
            }
            else if (damageCategoryConfig == "Average")
            {
                takenFlat += modDB.Sum(ModType.Base, null, "DamageTakenFromAttacks", $"{damageType}DamageTakenFromAttacks") / 2
                    + modDB.Sum(ModType.Base, null, $"{damageType}DamageTakenFromProjectileAttacks") / 4
                    + modDB.Sum(ModType.Base, null, "DamageTakenFromSpells", $"{damageType}DamageTakenFromSpells") / 2
                    + modDB.Sum(ModType.Base, null, "DamageTakenFromSpellProjectiles", $"{damageType}DamageTakenFromSpellProjectiles") / 4;
            }
            output[$"{damageType}takenFlat"] = takenFlat;

            // Armour applies
            double percentOfArmourApplies = Math.Min(
                !modDB.Flag(null, $"ArmourDoesNotApplyTo{damageType}DamageTaken")
                    ? modDB.Sum(ModType.Base, null, $"ArmourAppliesTo{damageType}DamageTaken")
                    : 0, 100);
            double armourDefense = output.TryGetValue("ArmourDefense", out double ad) ? ad : 0;
            double armour = output.TryGetValue("Armour", out double arm) ? arm : 0;
            double effectiveAppliedArmour = (armour * percentOfArmourApplies / 100) * (1 + armourDefense);
            output[$"{damageType}EffectiveAppliedArmour"] = effectiveAppliedArmour;
        }
    }

    // ─── Step 7: Enemy Damage Calculation ───

    internal static void EnemyDamageCalculation(Actor actor)
    {
        var modDB = actor.ModDB;
        var enemyDB = actor.Enemy?.ModDB;
        var output = actor.Output;
        var config = actor.Config;

        output["totalEnemyDamage"] = 0;
        output["totalEnemyDamageIn"] = 0;

        if (config.EnemyDamageType == "DamageOverTime")
        {
            foreach (string damageType in DmgTypeList)
            {
                output[$"{damageType}EnemyPen"] = 0;
                output[$"{damageType}EnemyDamageMult"] = enemyDB != null
                    ? CalcLib.Mod(enemyDB, null, "Damage", $"{damageType}Damage",
                        IsElemental(damageType) ? "ElementalDamage" : "")
                    : 1;
                output[$"{damageType}EnemyOverwhelm"] = 0;
                output[$"{damageType}EnemyDamage"] = 0;
            }
            return;
        }

        // Enemy crit
        double enemyCritChance;
        if (enemyDB != null && enemyDB.Flag(null, "NeverCrit"))
            enemyCritChance = 0;
        else if (enemyDB != null && enemyDB.Flag(null, "AlwaysCrit"))
            enemyCritChance = 100;
        else
        {
            double configCrit = config.EnemyCritChance ?? 0;
            double critIncMod = 1 + (modDB.Sum(ModType.Inc, null, "EnemyCritChance")
                + (enemyDB?.Sum(ModType.Inc, null, "CritChance") ?? 0)) / 100;
            double evadeChance = output.TryGetValue("ConfiguredEvadeChance", out double ec) ? ec : 0;
            enemyCritChance = Math.Max(Math.Min(configCrit * critIncMod * (1 - evadeChance / 100), 100), 0);
        }
        output["EnemyCritChance"] = enemyCritChance;

        double enemyCritDamage = Math.Max((config.EnemyCritDamage ?? 0) + (enemyDB?.Sum(ModType.Base, null, "CritMultiplier") ?? 0), 0);
        double critExtraDmgReduction = output.TryGetValue("CritExtraDamageReduction", out double cedr) ? cedr : 0;
        output["EnemyCritEffect"] = 1 + enemyCritChance / 100 * (enemyCritDamage / 100) * (1 - critExtraDmgReduction / 100);

        foreach (string damageType in DmgTypeList)
        {
            double enemyDamageMult = enemyDB != null
                ? CalcLib.Mod(enemyDB, null, "Damage", $"{damageType}Damage",
                    IsElemental(damageType) ? "ElementalDamage" : "")
                : 1;
            double enemyDamage = config.GetEnemyDamage(damageType);
            double enemyPen = config.GetEnemyPen(damageType);
            double enemyOverwhelm = config.GetEnemyOverwhelm(damageType);

            // Add enemy overwhelm from mods
            if (damageType == "Physical")
            {
                enemyOverwhelm += (enemyDB?.Sum(ModType.Base, null, "PhysicalOverwhelm") ?? 0)
                    + modDB.Sum(ModType.Base, null, "EnemyPhysicalOverwhelm");
            }

            output[$"{damageType}EnemyPen"] = enemyPen;
            output[$"{damageType}EnemyDamageMult"] = enemyDamageMult;
            output[$"{damageType}EnemyOverwhelm"] = enemyOverwhelm;
            output["totalEnemyDamageIn"] = output["totalEnemyDamageIn"] + enemyDamage;
            output[$"{damageType}EnemyDamage"] = enemyDamage * enemyDamageMult * output["EnemyCritEffect"];
            output["totalEnemyDamage"] = output["totalEnemyDamage"] + output[$"{damageType}EnemyDamage"];
        }
    }

    // ─── Step 8: Incoming Hit Damage ───

    internal static void IncomingHitDamage(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;
        string damageCategoryConfig = actor.Config.EnemyDamageType;

        // First pass: calculate TakenDamage from shift tables (same-type)
        foreach (string damageType in DmgTypeList)
        {
            double enemyDamage = output.TryGetValue($"{damageType}EnemyDamage", out double ed) ? ed : 0;
            double selfPercent = actor.DamageShiftTable.TryGetValue(damageType, out var st)
                ? (st.TryGetValue(damageType, out double sp) ? sp : 100) : 100;
            output[$"{damageType}TakenDamage"] = enemyDamage * selfPercent / 100;
        }

        // Second pass: add converted damage
        foreach (string damageType in DmgTypeList)
        {
            foreach (string destType in DmgTypeList)
            {
                if (damageType == destType) continue;
                double enemyDamage = output.TryGetValue($"{damageType}EnemyDamage", out double ed) ? ed : 0;
                double shiftPercent = actor.DamageShiftTable.TryGetValue(damageType, out var st)
                    ? (st.TryGetValue(destType, out double sp) ? sp : 0) : 0;
                if (shiftPercent > 0)
                    output[$"{destType}TakenDamage"] = output[$"{destType}TakenDamage"] + enemyDamage * shiftPercent / 100;
            }
        }

        // Third pass: apply mitigation per type
        output["totalTakenHit"] = 0;
        foreach (string damageType in DmgTypeList)
        {
            double damage = output[$"{damageType}TakenDamage"];
            double resist = modDB.Flag(null, $"SelfIgnore{damageType}Resistance") ? 0
                : (output.TryGetValue($"{damageType}ResistWhenHit", out double rwh) ? rwh
                    : output.TryGetValue($"{damageType}Resist", out double r) ? r : 0);
            double reduction = modDB.Flag(null, $"SelfIgnoreBase{damageType}DamageReduction") ? 0
                : (output.TryGetValue($"Base{damageType}DamageReductionWhenHit", out double rdwh) ? rdwh
                    : output.TryGetValue($"Base{damageType}DamageReduction", out double rd) ? rd : 0);
            double enemyPen = modDB.Flag(null, $"SelfIgnore{damageType}Resistance", $"EnemyCannotPen{damageType}Resistance") ? 0
                : (output.TryGetValue($"{damageType}EnemyPen", out double ep) ? ep : 0);
            double enemyOverwhelm = modDB.Flag(null, $"SelfIgnore{damageType}DamageReduction") ? 0
                : (output.TryGetValue($"{damageType}EnemyOverwhelm", out double eo) ? eo : 0);

            double resMult = 1 - (resist - enemyPen) / 100;
            double armourReduct = 0;
            double effectiveAppliedArmour = output.TryGetValue($"{damageType}EffectiveAppliedArmour", out double eaa) ? eaa : 0;
            double dmgReductionMax = output.TryGetValue("DamageReductionMax", out double drm) ? drm : MiscConstants.DamageReductionCap;

            if (effectiveAppliedArmour > 0 && damage * resMult > 0)
            {
                armourReduct = CalcDefence.ArmourReduction(effectiveAppliedArmour, damage * resMult);
                armourReduct = Math.Min(dmgReductionMax, armourReduct);
            }

            double totalReduct = Math.Min(dmgReductionMax, armourReduct + reduction);
            double reductMult = 1 - Math.Max(Math.Min(dmgReductionMax, totalReduct - enemyOverwhelm), 0) / 100;
            output[$"{damageType}DamageReduction"] = 100 - reductMult * 100;

            double takenFlat = output.TryGetValue($"{damageType}takenFlat", out double tf) ? tf : 0;
            double takenMult = output[$"{damageType}TakenHitMult"];
            double spellSuppressMult = 1;

            if (damageCategoryConfig == "Melee" || damageCategoryConfig == "Projectile")
            {
                takenMult = output.TryGetValue($"{damageType}AttackTakenHitMult", out double atm) ? atm : takenMult;
            }
            else if (damageCategoryConfig == "Spell" || damageCategoryConfig == "SpellProjectile")
            {
                takenMult = output.TryGetValue($"{damageType}SpellTakenHitMult", out double stm) ? stm : takenMult;
                double effSuppress = output.TryGetValue("EffectiveSpellSuppressionChance", out double esc) ? esc : 0;
                double suppressEffect = output.TryGetValue("SpellSuppressionEffect", out double se) ? se : 0;
                spellSuppressMult = effSuppress == 100 ? (1 - suppressEffect / 100) : 1;
            }
            else if (damageCategoryConfig == "Average")
            {
                double spellMult = output.TryGetValue($"{damageType}SpellTakenHitMult", out double stm) ? stm : takenMult;
                double attackMult = output.TryGetValue($"{damageType}AttackTakenHitMult", out double atm) ? atm : takenMult;
                takenMult = (spellMult + attackMult) / 2;
                double effSuppress = output.TryGetValue("EffectiveSpellSuppressionChance", out double esc) ? esc : 0;
                double suppressEffect = output.TryGetValue("SpellSuppressionEffect", out double se) ? se : 0;
                spellSuppressMult = effSuppress == 100 ? (1 - suppressEffect / 100 / 2) : 1;
            }

            output[$"{damageType}ResistTakenHitMulti"] = resMult;
            output[$"{damageType}AfterReductionTakenHitMulti"] = takenMult * spellSuppressMult;
            output[$"{damageType}BaseTakenHitMult"] = resMult * reductMult * takenMult * spellSuppressMult;

            output[$"{damageType}TakenHit"] = Math.Max(damage * resMult * reductMult + takenFlat, 0) * takenMult * spellSuppressMult;
            output[$"{damageType}TakenHitMult"] = damage > 0 ? output[$"{damageType}TakenHit"] / damage : 0;
            output["totalTakenHit"] += output[$"{damageType}TakenHit"];
        }
    }

    // ─── ES Bypass setup ───

    private static void SetupEnergyShieldBypass(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        output["AnyBypass"] = 0;
        output["MinimumBypass"] = 100;
        foreach (string damageType in DmgTypeList)
        {
            double bypass;
            if (modDB.Flag(null, "UnblockedDamageDoesBypassES"))
            {
                bypass = 100;
                output["AnyBypass"] = 1;
            }
            else
            {
                bypass = modDB.Override(null, $"{damageType}EnergyShieldBypass")?.AsNumber()
                    ?? modDB.Sum(ModType.Base, null, $"{damageType}EnergyShieldBypass");
                if (bypass != 0) output["AnyBypass"] = 1;

                if (damageType == "Chaos" && !modDB.Flag(null, "ChaosNotBypassEnergyShield"))
                    bypass += 100;
                else if (damageType == "Chaos")
                    output["AnyBypass"] = 1;
            }
            output[$"{damageType}EnergyShieldBypass"] = Math.Max(Math.Min(bypass, 100), 0);
            output["MinimumBypass"] = Math.Min(output["MinimumBypass"], output[$"{damageType}EnergyShieldBypass"]);
        }
    }

    // ─── Mind over Matter setup ───

    private static void SetupMindOverMatter(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        output["sharedMindOverMatter"] = Math.Min(modDB.Sum(ModType.Base, null, "DamageTakenFromManaBeforeLife"), 100);
        foreach (string damageType in DmgTypeList)
        {
            output[$"{damageType}MindOverMatter"] = Math.Min(
                modDB.Sum(ModType.Base, null, $"{damageType}DamageTakenFromManaBeforeLife"),
                100 - output["sharedMindOverMatter"]);
        }
    }

    // ─── Life Recoverable ───

    private static void SetupLifeRecoverable(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        double lifeUnreserved = output.TryGetValue("LifeUnreserved", out double lu) ? lu : output.TryGetValue("Life", out double l) ? l : 0;
        output["LifeRecoverable"] = Math.Max(lifeUnreserved, 1);

        if (actor.Config.ConditionLowLife)
        {
            double lowLifePerc = output.TryGetValue("LowLifePercentage", out double llp) ? llp : MiscConstants.LowPoolThreshold * 100;
            double life = output.TryGetValue("Life", out double li) ? li : 0;
            double capped = Math.Min(life * lowLifePerc / 100, lifeUnreserved);
            if (capped < lifeUnreserved)
                output["CappingLife"] = 1;
            output["LifeRecoverable"] = Math.Max(capped, 1);
        }

        if (modDB.Flag(null, "DamageInsteadReservesLife"))
        {
            double cancellable = output.TryGetValue("LifeCancellableReservation", out double lcr) ? lcr : 0;
            double life = output.TryGetValue("Life", out double li) ? li : 0;
            output["LifeRecoverable"] = (cancellable / 100) * life;
        }

        output["LifeRecoverable"] = Math.Max(output["LifeRecoverable"], 1);

        // Prevented life loss
        double preventedLifeLoss = Math.Min(modDB.Sum(ModType.Base, null, "LifeLossPrevented"), 100);
        output["preventedLifeLoss"] = preventedLifeLoss;
        double lifeLossBelowHalfPrevented = modDB.Sum(ModType.Base, null, "LifeLossBelowHalfPrevented");
        output["preventedLifeLossBelowHalf"] = (1 - preventedLifeLoss / 100) * lifeLossBelowHalfPrevented;
        output["preventedLifeLossTotal"] = preventedLifeLoss + output["preventedLifeLossBelowHalf"];

        // ES recovery cap
        double es = output.TryGetValue("EnergyShield", out double esVal) ? esVal : 0;
        if (!output.ContainsKey("EnergyShieldRecoveryCap"))
            output["EnergyShieldRecoveryCap"] = es;
    }

    // ─── Total Pools ───

    private static void SetupTotalPools(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        foreach (string damageType in DmgTypeList)
        {
            double manaEffLife = output.TryGetValue("LifeRecoverable", out double lr) ? lr : 0;
            double momShared = output.TryGetValue("sharedMindOverMatter", out double smom) ? smom : 0;
            double momTyped = output.TryGetValue($"{damageType}MindOverMatter", out double tmom) ? tmom : 0;

            // MoM pool calculation
            if (momShared + momTyped > 0)
            {
                double momEffect = (momShared + momTyped) / 100;
                double mana = Math.Max(output.TryGetValue("ManaUnreserved", out double mu) ? mu : 0, 0);
                double maxMoM = momEffect < 1 ? manaEffLife / (1 - momEffect) - manaEffLife : double.MaxValue;
                double momPool = Math.Min(mana, maxMoM);
                manaEffLife += momPool;
            }

            output[$"{damageType}ManaEffectiveLife"] = manaEffLife;
            output[$"{damageType}TotalPool"] = manaEffLife;

            // Add ES
            double esBypass = output.TryGetValue($"{damageType}EnergyShieldBypass", out double eb) ? eb : 100;
            double esCap = output.TryGetValue("EnergyShieldRecoveryCap", out double esc) ? esc : 0;
            if (esBypass < 100 && !modDB.Flag(null, "EnergyShieldProtectsMana"))
            {
                if (esBypass > 0)
                {
                    double poolProtected = esCap / (1 - esBypass / 100) * (esBypass / 100);
                    output[$"{damageType}TotalPool"] = Math.Max(output[$"{damageType}TotalPool"] - poolProtected, 0)
                        + Math.Min(output[$"{damageType}TotalPool"], poolProtected) / (esBypass / 100);
                }
                else
                {
                    output[$"{damageType}TotalPool"] += esCap;
                }
            }
        }
    }

    // ─── Step 9: Not Hit Chance ───

    internal static void NotHitChance(Actor actor)
    {
        var output = actor.Output;
        var config = actor.Config;
        int worstOf = config.EHPUnluckyWorstOf;

        double meleeEvade = output.TryGetValue("MeleeEvadeChance", out double me) ? me : 0;
        double projEvade = output.TryGetValue("ProjectileEvadeChance", out double pe) ? pe : 0;
        double avoidAll = output.TryGetValue("AvoidAllDamageFromHitsChance", out double aa) ? aa : 0;
        double avoidProj = output.TryGetValue("AvoidProjectilesChance", out double ap) ? ap : 0;
        double specificTypeAvoidance = output.TryGetValue("specificTypeAvoidance", out double sta) ? sta : 0;
        double attackDodge = output.TryGetValue("EffectiveAttackDodgeChance", out double ead) ? ead : 0;
        double spellDodge = output.TryGetValue("EffectiveSpellDodgeChance", out double esd) ? esd : 0;

        double avoidProjAdj = specificTypeAvoidance > 0 ? 0 : avoidProj;

        output["MeleeNotHitChance"] = 100 - (1 - meleeEvade / 100) * (1 - attackDodge / 100) * (1 - avoidAll / 100) * 100;
        output["ProjectileNotHitChance"] = 100 - (1 - projEvade / 100) * (1 - attackDodge / 100) * (1 - avoidAll / 100) * (1 - avoidProjAdj / 100) * 100;
        output["SpellNotHitChance"] = 100 - (1 - spellDodge / 100) * (1 - avoidAll / 100) * 100;
        output["SpellProjectileNotHitChance"] = 100 - (1 - spellDodge / 100) * (1 - avoidAll / 100) * (1 - avoidProjAdj / 100) * 100;
        output["UntypedNotHitChance"] = 100 - (1 - avoidAll / 100) * 100;
        output["AverageNotHitChance"] = (output["MeleeNotHitChance"] + output["ProjectileNotHitChance"]
            + output["SpellNotHitChance"] + output["SpellProjectileNotHitChance"]) / 4;
        output["AverageEvadeChance"] = (meleeEvade + projEvade) / 4;

        string cat = config.EnemyDamageType;
        output["ConfiguredNotHitChance"] = output.TryGetValue($"{cat}NotHitChance", out double cnhc) ? cnhc : 0;
        output["ConfiguredEvadeChance"] = output.TryGetValue($"{cat}EvadeChance", out double cec) ? cec : 0;

        // Unlucky worst-of
        if (worstOf > 1)
        {
            output["ConfiguredNotHitChance"] = output["ConfiguredNotHitChance"] / 100 * output["ConfiguredNotHitChance"];
            output["ConfiguredEvadeChance"] = output["ConfiguredEvadeChance"] / 100 * output["ConfiguredEvadeChance"];
            if (worstOf == 4)
            {
                output["ConfiguredNotHitChance"] = output["ConfiguredNotHitChance"] / 100 * output["ConfiguredNotHitChance"];
                output["ConfiguredEvadeChance"] = output["ConfiguredEvadeChance"] / 100 * output["ConfiguredEvadeChance"];
            }
        }
    }

    // ─── Step 9-10: Pool Reduction + Number of Hits + EHP ───

    /// <summary>
    /// Simplified pool reduction: Ward → ES → MoM(Mana) → Life.
    /// Returns remaining pools after one hit.
    /// </summary>
    internal static PoolTable ReducePoolsByDamage(PoolTable pools, Dictionary<string, double> perTypeDamage, Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        double ward = pools.Ward;
        double es = pools.EnergyShield;
        double mana = pools.Mana;
        double life = pools.Life;
        double overkill = 0;

        // Ceil all positive damage entries
        var dmgCopy = new Dictionary<string, double>();
        foreach (var kv in perTypeDamage)
            dmgCopy[kv.Key] = kv.Value > 0 ? Math.Ceiling(kv.Value) : 0;

        foreach (string damageType in DmgTypeList)
        {
            if (!dmgCopy.TryGetValue(damageType, out double dmg) || dmg <= 0)
                continue;

            double remainder = dmg;

            // Ward absorbs first (all-or-nothing)
            if (ward > 0)
            {
                double wardAbsorb = Math.Min(remainder, ward);
                ward -= wardAbsorb;
                remainder -= wardAbsorb;
            }

            // ES absorbs (with chaos bypass)
            double esBypass = output.TryGetValue($"{damageType}EnergyShieldBypass", out double eb) ? eb / 100 : (damageType == "Chaos" ? 1 : 0);
            if (es > 0 && esBypass < 1)
            {
                double esAbsorb = Math.Min(remainder * (1 - esBypass), es);
                es -= esAbsorb;
                remainder -= esAbsorb;
            }

            // MoM: redirect to mana
            double momEffect = Math.Min(
                (output.TryGetValue("sharedMindOverMatter", out double smom) ? smom : 0)
                + (output.TryGetValue($"{damageType}MindOverMatter", out double tmom) ? tmom : 0), 100) / 100;
            if (momEffect > 0 && mana > 0)
            {
                double manaAbsorb = Math.Min(Math.Ceiling(remainder * momEffect), mana);
                mana -= manaAbsorb;
                remainder -= manaAbsorb;
            }

            // Life absorbs rest
            if (life > 0)
            {
                double lifeAbsorb = Math.Min(remainder, life);
                life -= lifeAbsorb;
                remainder -= lifeAbsorb;
            }

            overkill += remainder;
        }

        return new PoolTable
        {
            Ward = Math.Floor(ward),
            EnergyShield = Math.Floor(es),
            Mana = Math.Floor(mana),
            Life = Math.Floor(life),
            OverkillDamage = Math.Ceiling(overkill),
        };
    }

    /// <summary>
    /// Iteratively reduce pools until life hits 0 to determine number of hits to die.
    /// </summary>
    internal static double NumberOfHitsToDie(Dictionary<string, double> damageIn, Actor actor)
    {
        var output = actor.Output;
        var modDB = actor.ModDB;

        // Check if any damage
        double totalDmg = 0;
        foreach (string dt in DmgTypeList)
            totalDmg += damageIn.TryGetValue(dt, out double d) ? d : 0;
        if (totalDmg == 0)
            return double.PositiveInfinity;

        // Check ward absorbs all
        double wardVal = output.TryGetValue("Ward", out double w) ? w : 0;
        if (modDB.Flag(null, "WardNotBreak") && wardVal > 0 && totalDmg < wardVal)
            return double.PositiveInfinity;

        var pools = new PoolTable
        {
            Ward = wardVal,
            EnergyShield = output.TryGetValue("EnergyShieldRecoveryCap", out double esc) ? esc : output.TryGetValue("EnergyShield", out double esv) ? esv : 0,
            Mana = output.TryGetValue("ManaUnreserved", out double mu) ? mu : 0,
            Life = output.TryGetValue("LifeRecoverable", out double lr) ? lr : 0,
        };

        pools.Ward = Math.Max(pools.Ward, 0);
        pools.EnergyShield = Math.Max(pools.EnergyShield, 0);
        pools.Mana = Math.Max(pools.Mana, 0);
        pools.Life = Math.Max(pools.Life, 0);

        double numHits = 0;
        int iterations = 0;
        int maxIterations = MiscConstants.EhpCalcMaxIterationsToCalc;

        // Gain-on-hit values (from block/suppress recovery)
        double lifeOnHit = damageIn.TryGetValue("LifeWhenHit", out double lwh) ? lwh : 0;
        double manaOnHit = damageIn.TryGetValue("ManaWhenHit", out double mwh) ? mwh : 0;
        double esOnHit = damageIn.TryGetValue("EnergyShieldWhenHit", out double eswh) ? eswh : 0;
        bool hasGainOnHit = damageIn.TryGetValue("GainWhenHit", out double gwh) && gwh > 0;
        double maxLife = output.TryGetValue("LifeRecoverable", out double mlr) ? mlr : 0;
        double maxMana = output.TryGetValue("ManaUnreserved", out double mmu) ? mmu : 0;
        double maxES = output.TryGetValue("EnergyShieldRecoveryCap", out double mesc) ? mesc : output.TryGetValue("EnergyShield", out double mes) ? mes : 0;

        while (pools.Life > 0 && iterations < maxIterations)
        {
            iterations++;
            var damage = new Dictionary<string, double>();
            foreach (string dt in DmgTypeList)
                if (damageIn.TryGetValue(dt, out double d) && d > 0)
                    damage[dt] = d;

            pools = ReducePoolsByDamage(pools, damage, actor);

            if (pools.Life > 0 && totalDmg >= MiscConstants.EhpCalcMaxDamage)
                return double.PositiveInfinity;

            // Apply gain-on-hit
            if (hasGainOnHit && pools.Life > 0)
            {
                pools.Life = Math.Min(pools.Life + lifeOnHit, maxLife);
                pools.Mana = Math.Min(pools.Mana + manaOnHit, maxMana);
                pools.EnergyShield = Math.Min(pools.EnergyShield + esOnHit, maxES);
            }

            numHits++;
        }

        // Subtract fractional overkill for precision
        if (pools.Life == 0 && totalDmg > 0)
            numHits -= pools.OverkillDamage / totalDmg;

        return numHits;
    }

    // ─── Step 10: Final EHP + Survival Time ───

    private static void ComputeHitsAndEHP(Actor actor)
    {
        var output = actor.Output;
        var enemyDB = actor.Enemy?.ModDB;
        var config = actor.Config;
        string damageCategoryConfig = config.EnemyDamageType;

        // Number of damaging hits (raw, no block/suppress)
        var rawDamageIn = new Dictionary<string, double>();
        foreach (string dt in DmgTypeList)
            rawDamageIn[dt] = output.TryGetValue($"{dt}TakenHit", out double d) ? d : 0;
        output["NumberOfDamagingHits"] = NumberOfHitsToDie(rawDamageIn, actor);

        // Block effect
        double blockChance;
        if (damageCategoryConfig == "Melee" || damageCategoryConfig == "Untyped")
            blockChance = output.TryGetValue("BlockChance", out double bc) ? bc / 100 : 0;
        else
        {
            string key = $"Effective{damageCategoryConfig}BlockChance";
            if (!output.TryGetValue(key, out double catBlock))
            {
                if (damageCategoryConfig == "Spell" || damageCategoryConfig == "SpellProjectile")
                    catBlock = output.TryGetValue("SpellBlockChance", out double sbc) ? sbc : 0;
                else if (damageCategoryConfig == "Projectile")
                    catBlock = output.TryGetValue("ProjectileBlockChance", out double pbc) ? pbc : 0;
                else
                    catBlock = output.TryGetValue("BlockChance", out double bc2) ? bc2 : 0;
            }
            blockChance = catBlock / 100;
        }
        if (enemyDB != null && enemyDB.Flag(null, "CannotBeBlocked"))
            blockChance = 0;
        double blockEffect = output.TryGetValue("BlockEffect", out double be) ? be : 100;
        double blockMult = 1 - blockChance * blockEffect / 100;

        // Suppression
        double suppressChance = 0;
        double suppressionEffect = 1;
        if (damageCategoryConfig is "Spell" or "SpellProjectile" or "Average")
        {
            suppressChance = output.TryGetValue("EffectiveSpellSuppressionChance", out double esc) ? esc / 100 : 0;
        }
        double suppressEffect = output.TryGetValue("SpellSuppressionEffect", out double se) ? se : 0;
        if (suppressChance < 1)
        {
            double adj = damageCategoryConfig == "Average" ? suppressChance / 2 : suppressChance;
            suppressionEffect = 1 - adj * suppressEffect / 100;
        }

        // Extra avoid per type
        double extraAvoidChance = 0;
        double avoidProj = output.TryGetValue("AvoidProjectilesChance", out double apv) ? apv : 0;
        if (damageCategoryConfig is "Projectile" or "SpellProjectile")
            extraAvoidChance += avoidProj;
        else if (damageCategoryConfig == "Average")
            extraAvoidChance += avoidProj / 2;

        // Build mitigated damage input
        var mitigatedDamageIn = new Dictionary<string, double>();
        double averageAvoidChance = 0;
        foreach (string dt in DmgTypeList)
        {
            double avoidChance = 0;
            double specificAvoid = output.TryGetValue("specificTypeAvoidance", out double sav) ? sav : 0;
            if (specificAvoid > 0)
            {
                avoidChance = Math.Min(
                    (output.TryGetValue($"Avoid{dt}DamageChance", out double adc) ? adc : 0) + extraAvoidChance,
                    MiscConstants.AvoidChanceCap);
                int worstOf = config.EHPUnluckyWorstOf;
                if (worstOf > 1)
                {
                    avoidChance = avoidChance / 100 * avoidChance;
                    if (worstOf == 4)
                        avoidChance = avoidChance / 100 * avoidChance;
                }
                averageAvoidChance += avoidChance;
            }
            double raw = output.TryGetValue($"{dt}TakenHit", out double r) ? r : 0;
            mitigatedDamageIn[dt] = raw * blockMult * suppressionEffect * (1 - avoidChance / 100);
        }
        averageAvoidChance /= 5;

        // Gain on block/suppress
        if (!config.DisableEHPGainOnBlock && output.TryGetValue("NumberOfDamagingHits", out double ndh) && ndh > 1)
        {
            double lifeOnBlock = output.TryGetValue("LifeOnBlock", out double lob) ? lob : 0;
            double manaOnBlock = output.TryGetValue("ManaOnBlock", out double mob) ? mob : 0;
            double esOnBlock = output.TryGetValue("EnergyShieldOnBlock", out double eob) ? eob : 0;
            double esOnSpellBlock = output.TryGetValue("EnergyShieldOnSpellBlock", out double eosb) ? eosb : 0;
            double lifeOnSuppress = output.TryGetValue("LifeOnSuppress", out double los) ? los : 0;
            double esOnSuppress = output.TryGetValue("EnergyShieldOnSuppress", out double eos) ? eos : 0;

            double lWH = lifeOnBlock * blockChance;
            double mWH = manaOnBlock * blockChance;
            double esWH = esOnBlock * blockChance;
            if (damageCategoryConfig is "Spell" or "SpellProjectile")
                esWH += esOnSpellBlock * blockChance;
            else if (damageCategoryConfig == "Average")
                esWH += esOnSpellBlock / 2 * blockChance;

            double adj = damageCategoryConfig == "Average" ? 0.5 : 1.0;
            if (suppressChance < 1)
            {
                double sc = damageCategoryConfig == "Average" ? suppressChance / 2 : suppressChance;
                esWH += esOnSuppress * sc;
                lWH += lifeOnSuppress * sc;
            }
            else
            {
                esWH += esOnSuppress * adj;
                lWH += lifeOnSuppress * adj;
            }

            if (lWH > 0 || mWH > 0 || esWH > 0)
            {
                mitigatedDamageIn["LifeWhenHit"] = lWH;
                mitigatedDamageIn["ManaWhenHit"] = mWH;
                mitigatedDamageIn["EnergyShieldWhenHit"] = esWH;
                mitigatedDamageIn["GainWhenHit"] = 1;
            }
        }

        output["ConfiguredDamageChance"] = 100 * blockMult * suppressionEffect * (1 - averageAvoidChance / 100);

        bool needsRecalc = output["ConfiguredDamageChance"] != 100
            || mitigatedDamageIn.ContainsKey("GainWhenHit");
        output["NumberOfMitigatedDamagingHits"] = needsRecalc
            ? NumberOfHitsToDie(mitigatedDamageIn, actor)
            : output["NumberOfDamagingHits"];

        // Total number of hits (accounting for not-hit chance)
        double configuredNotHitChance = output.TryGetValue("ConfiguredNotHitChance", out double cnhc) ? cnhc : 0;
        double divisor = 1 - configuredNotHitChance / 100;
        output["TotalNumberOfHits"] = divisor > 0
            ? output["NumberOfMitigatedDamagingHits"] / divisor
            : double.PositiveInfinity;

        // Total EHP
        output["TotalEHP"] = output["TotalNumberOfHits"] * output["totalEnemyDamageIn"];

        // Survival time
        double enemySpeed = config.EnemySpeed ?? 700;
        double enemySpeedInc = enemyDB?.Sum(ModType.Inc, null, "Speed") ?? 0;
        double enemySkillTime = enemySpeed / (1 + enemySpeedInc / 100) / 1000;
        if (actor.Enemy != null)
        {
            double enemyActionSpeed = CalcPerform.ActionSpeedMod(actor.Enemy);
            enemySkillTime /= enemyActionSpeed;
        }
        output["enemySkillTime"] = enemySkillTime;
        output["EHPSurvivalTime"] = output["TotalNumberOfHits"] * enemySkillTime;
    }
}
