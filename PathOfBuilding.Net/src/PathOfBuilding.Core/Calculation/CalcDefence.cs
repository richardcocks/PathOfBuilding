using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Defence calculation functions: resistances, block, armour/evasion/ES, evade, suppression,
/// recovery rates, leech caps, regeneration, ES recharge, recoup, ward recharge,
/// damage reduction, movement speed, avoidance, immunities, ailment duration on self.
/// Ported from CalcDefence.lua.
/// </summary>
public static class CalcDefence
{
    private static readonly string[] ResistTypeList = { "Fire", "Cold", "Lightning", "Chaos" };

    /// <summary>Damage types in conversion order, used for damage reduction and avoidance.</summary>
    private static readonly string[] DmgTypeList = DamageTypeFlags.DmgTypeList;

    /// <summary>Resource names for regeneration loop (with space-separated display names).</summary>
    private static readonly (string Resource, string DisplayName)[] RegenResources =
    [
        ("Mana", "Mana"),
        ("Life", "Life"),
        ("EnergyShield", "Energy Shield"),
        ("Rage", "Rage"),
    ];

    private static bool IsElemental(string elem) => elem is "Fire" or "Cold" or "Lightning";

    // ─── Static formula helpers ───

    public static double HitChance(double evasion, double accuracy)
    {
        if (accuracy < 0)
            return 5;
        double rawChance = accuracy / (accuracy + Math.Pow(evasion / 5, 0.9)) * 125;
        return Math.Max(Math.Min(CalcLib.Round(rawChance), 100), 5);
    }

    public static double ArmourReductionF(double armour, double raw)
    {
        if (armour == 0 && raw == 0)
            return 0;
        return armour / (armour + raw * 5) * 100;
    }

    public static double ArmourReduction(double armour, double raw)
    {
        return CalcLib.Round(ArmourReductionF(armour, raw));
    }

    // ─── Resistances ───

    public static void Resistances(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        output["PhysicalResist"] = 0;

        // Process resistance conversion mods (max resist conversion)
        foreach (var resFrom in ResistTypeList)
        {
            double maxRes = -1;
            foreach (var resTo in ResistTypeList)
            {
                double conversionRate = modDB.Sum(ModType.Base, null, $"{resFrom}MaxResConvertTo{resTo}") / 100.0;
                if (conversionRate != 0)
                {
                    if (maxRes < 0)
                    {
                        maxRes = 0;
                        var tabulated = modDB.Tabulate(ModType.Base, null, $"{resFrom}ResistMax");
                        foreach (var entry in tabulated)
                        {
                            if (entry.Mod.Source != "Base")
                                maxRes += entry.Value.AsNumber();
                        }
                    }
                    if (maxRes != 0)
                    {
                        modDB.NewMod($"{resTo}ResistMax", ModType.Base, maxRes * conversionRate,
                            $"{resFrom} To {resTo} Max Resistance Conversion");
                    }
                }
            }
        }

        // Process resistance conversion mods (base resist conversion)
        foreach (var resFrom in ResistTypeList)
        {
            double res = -1;
            foreach (var resTo in ResistTypeList)
            {
                double conversionRate = modDB.Sum(ModType.Base, null, $"{resFrom}ResConvertTo{resTo}") / 100.0;
                if (conversionRate != 0)
                {
                    if (res < 0)
                    {
                        res = 0;
                        var tabulated = modDB.Tabulate(ModType.Base, null, $"{resFrom}Resist");
                        foreach (var entry in tabulated)
                        {
                            if (entry.Mod.Source != "Base")
                                res += entry.Value.AsNumber();
                        }
                    }
                    if (res != 0)
                    {
                        modDB.NewMod($"{resTo}Resist", ModType.Base, res * conversionRate,
                            $"{resFrom} To {resTo} Resistance Conversion");
                    }
                    foreach (var entry in modDB.Tabulate(ModType.Inc, null, $"{resFrom}Resist"))
                    {
                        modDB.NewMod($"{resTo}Resist", ModType.Inc, entry.Value.AsNumber() * conversionRate, entry.Mod.Source);
                    }
                    foreach (var entry in modDB.Tabulate(ModType.More, null, $"{resFrom}Resist"))
                    {
                        modDB.NewMod($"{resTo}Resist", ModType.More, entry.Value.AsNumber() * conversionRate, entry.Mod.Source);
                    }
                }
            }
        }

        // Melding of the Flesh
        if (modDB.Flag(null, "ElementalResistMaxIsHighestResistMax"))
        {
            double highestResistMax = 0;
            string highestResistMaxType = "";
            foreach (var elem in ResistTypeList)
            {
                if (!IsElemental(elem)) continue;
                double resistMax = modDB.Override(null, $"{elem}ResistMax")?.AsNumber()
                    ?? Math.Min(MiscConstants.MaxResistCap, modDB.Sum(ModType.Base, null, $"{elem}ResistMax", "ElementalResistMax"));
                if (resistMax > highestResistMax)
                {
                    highestResistMax = resistMax;
                    highestResistMaxType = elem;
                }
            }
            foreach (var elem in ResistTypeList)
            {
                if (IsElemental(elem))
                {
                    modDB.NewMod($"{elem}ResistMax", ModType.Override, highestResistMax, $"{highestResistMaxType} Melding of the Flesh");
                }
            }
        }

        // Calculate final resistances
        foreach (var elem in ResistTypeList)
        {
            double min = MiscConstants.ResistFloor;
            double max = modDB.Override(null, $"{elem}ResistMax")?.AsNumber()
                ?? Math.Min(MiscConstants.MaxResistCap,
                    modDB.Sum(ModType.Base, null, $"{elem}ResistMax", IsElemental(elem) ? "ElementalResistMax" : ""));

            double total;
            var totalOverride = modDB.Override(null, $"{elem}Resist");
            if (totalOverride.HasValue)
            {
                total = totalOverride.Value.AsNumber();
            }
            else
            {
                double baseResist = IsElemental(elem)
                    ? modDB.Sum(ModType.Base, null, $"{elem}Resist", "ElementalResist")
                    : modDB.Sum(ModType.Base, null, $"{elem}Resist");
                double inc = Math.Max(IsElemental(elem)
                    ? CalcLib.Mod(modDB, null, $"{elem}Resist", "ElementalResist")
                    : CalcLib.Mod(modDB, null, $"{elem}Resist"), 0);
                total = baseResist * inc;
            }

            total = Math.Truncate(total);
            min = Math.Truncate(min);
            max = Math.Truncate(max);

            double final_ = Math.Max(Math.Min(total, max), min);

            output[$"{elem}Resist"] = final_;
            output[$"{elem}ResistTotal"] = total;
            output[$"{elem}ResistOverCap"] = Math.Max(0, total - max);
            output[$"{elem}ResistOver75"] = Math.Max(0, final_ - 75);
            output[$"Missing{elem}Resist"] = Math.Max(0, max - final_);
        }
    }

    // ─── Main Defence method ───

    public static void Defence(Actor actor)
    {
        var modDB = actor.ModDB;
        var enemyDB = actor.Enemy?.ModDB;
        var output = actor.Output;

        // Action Speed
        output["ActionSpeedMod"] = CalcPerform.ActionSpeedMod(actor);

        // Resistances
        Resistances(actor);

        // Block
        double blockChanceMax = Math.Min(modDB.Sum(ModType.Base, null, "BlockChanceMax"), MiscConstants.BlockChanceCap);
        output["BlockChanceMax"] = blockChanceMax;
        output["BlockChanceOverCap"] = 0;
        output["SpellBlockChanceOverCap"] = 0;

        double baseBlockChance = modDB.Sum(ModType.Base, null, "BlockChance");
        double totalBlockChance = baseBlockChance * CalcLib.Mod(modDB, null, "BlockChance");
        output["BlockChance"] = Math.Min(totalBlockChance, blockChanceMax);
        output["BlockChanceOverCap"] = Math.Max(0, totalBlockChance - blockChanceMax);

        output["ProjectileBlockChance"] = Math.Min(
            output["BlockChance"] + modDB.Sum(ModType.Base, null, "ProjectileBlockChance") * CalcLib.Mod(modDB, null, "BlockChance"),
            blockChanceMax);

        double spellBlockChanceMax;
        if (modDB.Flag(null, "SpellBlockChanceMaxIsBlockChanceMax"))
            spellBlockChanceMax = blockChanceMax;
        else
            spellBlockChanceMax = Math.Min(modDB.Sum(ModType.Base, null, "SpellBlockChanceMax"), MiscConstants.BlockChanceCap);
        output["SpellBlockChanceMax"] = spellBlockChanceMax;

        if (modDB.Flag(null, "SpellBlockChanceIsBlockChance"))
        {
            output["SpellBlockChance"] = output["BlockChance"];
            output["SpellProjectileBlockChance"] = output["ProjectileBlockChance"];
            output["SpellBlockChanceOverCap"] = output["BlockChanceOverCap"];
        }
        else
        {
            double totalSpellBlock = modDB.Sum(ModType.Base, null, "SpellBlockChance") * CalcLib.Mod(modDB, null, "SpellBlockChance");
            output["SpellBlockChance"] = Math.Min(totalSpellBlock, spellBlockChanceMax);
            output["SpellBlockChanceOverCap"] = Math.Max(0, totalSpellBlock - spellBlockChanceMax);
            output["SpellProjectileBlockChance"] = Math.Max(
                Math.Min(output["SpellBlockChance"] + modDB.Sum(ModType.Base, null, "ProjectileSpellBlockChance") * CalcLib.Mod(modDB, null, "SpellBlockChance"), spellBlockChanceMax), 0);
        }

        if (modDB.Flag(null, "CannotBlockAttacks"))
        {
            output["BlockChance"] = 0;
            output["ProjectileBlockChance"] = 0;
        }
        if (modDB.Flag(null, "CannotBlockSpells"))
        {
            output["SpellBlockChance"] = 0;
            output["SpellProjectileBlockChance"] = 0;
        }

        // Primary defences: armour, evasion, ES, ward (global only)
        bool ironReflexes = modDB.Flag(null, "IronReflexes");
        double armour = 0;
        double evasionVal = 0;
        double energyShield = 0;
        double wardVal = 0;

        double wardBase = modDB.Sum(ModType.Base, null, "Ward");
        if (wardBase > 0)
            wardVal += wardBase * CalcLib.Mod(modDB, null, "Ward", "Defences");

        double esBase = modDB.Sum(ModType.Base, null, "EnergyShield");
        if (esBase > 0)
            energyShield += esBase * CalcLib.Mod(modDB, null, "EnergyShield", "Defences");

        double armourBase = modDB.Sum(ModType.Base, null, "Armour", "ArmourAndEvasion");
        if (armourBase > 0)
            armour += armourBase * CalcLib.Mod(modDB, null, "Armour", "ArmourAndEvasion", "Defences");

        double evasionBase = modDB.Sum(ModType.Base, null, "Evasion", "ArmourAndEvasion");
        if (evasionBase > 0)
        {
            if (ironReflexes)
                armour += evasionBase * CalcLib.Mod(modDB, null, "Armour", "Evasion", "ArmourAndEvasion", "Defences");
            else
                evasionVal += evasionBase * CalcLib.Mod(modDB, null, "Evasion", "ArmourAndEvasion", "Defences");
        }

        double convManaToArmour = modDB.Sum(ModType.Base, null, "ManaConvertToArmour");
        if (convManaToArmour > 0)
        {
            double manaArmourBase = 2 * modDB.Sum(ModType.Base, null, "Mana") * convManaToArmour / 100.0;
            armour += manaArmourBase * CalcLib.Mod(modDB, null, "Mana", "Armour", "ArmourAndEvasion", "Defences");
        }

        output["EnergyShield"] = modDB.Override(null, "EnergyShield")?.AsNumber() ?? Math.Max(CalcLib.Round(energyShield), 0);
        output["Armour"] = Math.Max(CalcLib.Round(armour), 0);
        output["Evasion"] = Math.Max(CalcLib.Round(evasionVal), 0);
        output["MeleeEvasion"] = Math.Max(CalcLib.Round(evasionVal * CalcLib.Mod(modDB, null, "MeleeEvasion")), 0);
        output["ProjectileEvasion"] = Math.Max(CalcLib.Round(evasionVal * CalcLib.Mod(modDB, null, "ProjectileEvasion")), 0);
        output["LowestOfArmourAndEvasion"] = Math.Min(output["Armour"], output["Evasion"]);
        output["Ward"] = Math.Max(Math.Floor(wardVal), 0);

        // Evade chance
        if (modDB.Flag(null, "CannotEvade") || (enemyDB != null && enemyDB.Flag(null, "CannotBeEvaded")))
        {
            output["EvadeChance"] = 0;
            output["MeleeEvadeChance"] = 0;
            output["ProjectileEvadeChance"] = 0;
        }
        else
        {
            double enemyAccuracy = enemyDB != null ? CalcLib.Round(CalcLib.Val(enemyDB, "Accuracy")) : 0;
            double evadeChanceBase = modDB.Sum(ModType.Base, null, "EvadeChance");
            double hitChanceMod = enemyDB != null ? CalcLib.Mod(enemyDB, null, "HitChance") : 1;

            output["EvadeChance"] = 100 - (HitChance(output["Evasion"], enemyAccuracy) - evadeChanceBase) * hitChanceMod;
            output["MeleeEvadeChance"] = Math.Max(0, Math.Min(MiscConstants.EvadeChanceCap,
                (100 - (HitChance(output["MeleeEvasion"], enemyAccuracy) - evadeChanceBase) * hitChanceMod)
                * CalcLib.Mod(modDB, null, "EvadeChance", "MeleeEvadeChance")));
            output["ProjectileEvadeChance"] = Math.Max(0, Math.Min(MiscConstants.EvadeChanceCap,
                (100 - (HitChance(output["ProjectileEvasion"], enemyAccuracy) - evadeChanceBase) * hitChanceMod)
                * CalcLib.Mod(modDB, null, "EvadeChance", "ProjectileEvadeChance")));

            if (output["MeleeEvadeChance"] != output["ProjectileEvadeChance"])
                output["SplitEvade"] = 1;
            else
                output["EvadeChance"] = output["MeleeEvadeChance"];
        }

        // Spell Suppression
        double spellSuppressionChance = modDB.Sum(ModType.Base, null, "SpellSuppressionChance");
        double totalSpellSuppression = modDB.Override(null, "SpellSuppressionChance")?.AsNumber() ?? spellSuppressionChance;

        output["SpellSuppressionChance"] = Math.Min(totalSpellSuppression, MiscConstants.SuppressionChanceCap);
        output["SpellSuppressionEffect"] = Math.Max(MiscConstants.SuppressionEffect + modDB.Sum(ModType.Base, null, "SpellSuppressionEffect"), 0);
        output["SpellSuppressionChanceOverCap"] = Math.Max(0, totalSpellSuppression - MiscConstants.SuppressionChanceCap);

        output["EffectiveSpellSuppressionChance"] = (enemyDB != null && enemyDB.Flag(null, "CannotBeSuppressed"))
            ? 0
            : output["SpellSuppressionChance"];

        // Physical damage reduction from armour (against a reference hit)
        double referenceHit = 1000;
        output["PhysicalDamageReduction"] = Math.Min(
            ArmourReduction(output["Armour"], referenceHit) + modDB.Sum(ModType.Base, null, "PhysicalDamageReduction"),
            MiscConstants.DamageReductionCap);

        // ═══ Phase 4: Remaining defence calculations ═══

        // Recovery rate modifiers (lines 1172-1178)
        RecoveryRates(actor);

        // Leech caps (lines 1180-1213)
        LeechCaps(actor);

        // Regeneration (lines 1215-1299)
        Regeneration(actor);

        // ES Recharge (lines 1301-1360)
        EnergyShieldRecharge(actor);

        // Recoup (lines 1362-1450)
        Recoup(actor);

        // Ward recharge (lines 1452-1462)
        WardRecharge(actor);

        // Damage reduction per type (lines 1464-1470)
        DamageReduction(actor);

        // Movement speed (lines 1472-1489)
        MovementSpeed(actor);

        // Recovery on block/suppress (lines 1491-1503)
        BlockSuppressRecovery(actor);

        // Damage avoidance (lines 1505-1559)
        Avoidance(actor);

        // Ailment duration on self (lines 1570-1600)
        AilmentDurationOnSelf(actor);
    }

    // ─── Recovery Rates (lines 1172-1178) ───

    private static void RecoveryRates(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        output["LifeRecoveryRateMod"] = modDB.Flag(null, "CannotRecoverLifeOutsideLeech")
            ? 1
            : CalcLib.Mod(modDB, null, "LifeRecoveryRate");

        output["ManaRecoveryRateMod"] = CalcLib.Mod(modDB, null, "ManaRecoveryRate");
        output["EnergyShieldRecoveryRateMod"] = CalcLib.Mod(modDB, null, "EnergyShieldRecoveryRate");
    }

    // ─── Leech Caps (lines 1180-1213) ───

    private static void LeechCaps(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        double life = output.TryGetValue("Life", out double l) ? l : 0;
        double es = output.TryGetValue("EnergyShield", out double e) ? e : 0;
        double mana = output.TryGetValue("Mana", out double m) ? m : 0;

        output["MaxLifeLeechInstance"] = life * CalcLib.Val(modDB, "MaxLifeLeechInstance") / 100;

        double maxLifeLeechRatePercent = CalcLib.Val(modDB, "MaxLifeLeechRate");
        if (modDB.Flag(null, "MaximumLifeLeechIsEqualToParent") && actor.Parent != null)
        {
            maxLifeLeechRatePercent = actor.Parent.Output.TryGetValue("MaxLifeLeechRatePercent", out double parentRate)
                ? parentRate : maxLifeLeechRatePercent;
        }
        output["MaxLifeLeechRatePercent"] = maxLifeLeechRatePercent;
        output["MaxLifeLeechRate"] = life * maxLifeLeechRatePercent / 100;

        output["MaxEnergyShieldLeechInstance"] = es * CalcLib.Val(modDB, "MaxEnergyShieldLeechInstance") / 100;
        output["MaxEnergyShieldLeechRate"] = es * CalcLib.Val(modDB, "MaxEnergyShieldLeechRate") / 100;

        output["MaxManaLeechInstance"] = mana * CalcLib.Val(modDB, "MaxManaLeechInstance") / 100;
        output["MaxManaLeechRate"] = mana * CalcLib.Val(modDB, "MaxManaLeechRate") / 100;
    }

    // ─── Regeneration (lines 1215-1299) ───

    private static void Regeneration(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        for (int i = 0; i < RegenResources.Length; i++)
        {
            var (resource, displayName) = RegenResources[i];
            double pool = output.TryGetValue(resource, out double p) ? p : 0;
            double baseRegen = 0;
            double inc = modDB.Sum(ModType.Inc, null, $"{resource}Regen");
            double more = modDB.More(null, $"{resource}Regen");
            double regenRate = 0;
            double recoveryRateMod = output.TryGetValue($"{resource}RecoveryRateMod", out double rrm) ? rrm : 1;

            if (modDB.Flag(null, $"No{resource}Regen") || modDB.Flag(null, $"CannotGain{resource}"))
            {
                output[$"{resource}Regen"] = 0;
            }
            else if (resource == "Life" && modDB.Flag(null, "ZealotsOath"))
            {
                output["LifeRegen"] = 0;
                double lifeBase = modDB.Sum(ModType.Base, null, "LifeRegen");
                if (lifeBase > 0)
                    modDB.NewMod("EnergyShieldRegen", ModType.Base, lifeBase, "Zealot's Oath");
                double lifePercent = modDB.Sum(ModType.Base, null, "LifeRegenPercent");
                if (lifePercent > 0)
                    modDB.NewMod("EnergyShieldRegenPercent", ModType.Base, lifePercent, "Zealot's Oath");
            }
            else
            {
                // Legacy chain breaker: INC regen redirects to another resource
                if (inc != 0)
                {
                    for (int j = i + 1; j < RegenResources.Length; j++)
                    {
                        string otherResource = RegenResources[j].Resource;
                        if (modDB.Flag(null, $"{resource}RegenTo{otherResource}Regen"))
                        {
                            modDB.NewMod($"{otherResource}Regen", ModType.Inc, inc, $"{displayName} instead applies to {RegenResources[j].DisplayName}");
                            inc = 0;
                        }
                    }
                }

                // Life regen applies to ES conversion
                if (resource == "Life" && modDB.Sum(ModType.Base, null, "LifeRegenAppliesToEnergyShield") > 0)
                {
                    double conversion = Math.Min(modDB.Sum(ModType.Base, null, "LifeRegenAppliesToEnergyShield"), 100) / 100.0;
                    double lifeBase = modDB.Sum(ModType.Base, null, "LifeRegen");
                    double lifePercent = modDB.Sum(ModType.Base, null, "LifeRegenPercent");
                    modDB.NewMod("EnergyShieldRegen", ModType.Base, CalcLib.Round(lifeBase * conversion, 2), "Life Regen to ES Regen");
                    modDB.NewMod("EnergyShieldRegenPercent", ModType.Base, CalcLib.Round(lifePercent * conversion, 2), "Life Regen to ES Regen");
                }

                baseRegen = modDB.Sum(ModType.Base, null, $"{resource}Regen") + pool * modDB.Sum(ModType.Base, null, $"{resource}RegenPercent") / 100;
                double regen = baseRegen * (1 + inc / 100) * more;

                // Pious Path: regeneration recovers other resources
                if (regen != 0)
                {
                    for (int j = i + 1; j < RegenResources.Length; j++)
                    {
                        string otherResource = RegenResources[j].Resource;
                        if (modDB.Flag(null, $"{resource}RegenerationRecovers{otherResource}"))
                        {
                            modDB.NewMod($"{otherResource}Recovery", ModType.Base, regen,
                                $"{displayName} Regeneration Recovers {RegenResources[j].DisplayName}");
                        }
                    }
                }

                regenRate = CalcLib.Round(regen * recoveryRateMod, 1);
                output[$"{resource}Regen"] = regenRate;
            }

            output[$"{resource}RegenInc"] = inc;

            // Degen
            double baseDegen = modDB.Sum(ModType.Base, null, $"{resource}Degen") + pool * modDB.Sum(ModType.Base, null, $"{resource}DegenPercent") / 100;
            double degenRate = baseDegen > 0 ? baseDegen * CalcLib.Mod(modDB, null, $"{resource}Degen") : 0;
            output[$"{resource}Degen"] = degenRate;

            // Recovery
            double recoveryRate = modDB.Sum(ModType.Base, null, $"{resource}Recovery") * recoveryRateMod;
            output[$"{resource}Recovery"] = recoveryRate;

            // Net recovery
            double effectiveRegen = modDB.Flag(null, $"UnaffectedBy{resource}Regen") ? 0 : regenRate;
            output[$"{resource}RegenRecovery"] = effectiveRegen - degenRate + recoveryRate;

            if (output[$"{resource}RegenRecovery"] > 0)
                modDB.NewMod($"Condition:CanGain{resource}", ModType.Flag, true, $"{displayName}Regen");

            output[$"{resource}RegenPercent"] = pool > 0 ? CalcLib.Round(output[$"{resource}RegenRecovery"] / pool * 100, 1) : 0;
        }
    }

    // ─── ES Recharge (lines 1301-1360) ───

    private static void EnergyShieldRecharge(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        bool appliesToLife = modDB.Flag(null, "EnergyShieldRechargeAppliesToLife")
            && !modDB.Flag(null, "CannotRecoverLifeOutsideLeech");
        bool appliesToES = !modDB.Flag(null, "NoEnergyShieldRecharge")
            && !modDB.Flag(null, "CannotGainEnergyShield")
            && !appliesToLife;

        output["EnergyShieldRechargeAppliesToLife"] = appliesToLife ? 1 : 0;
        output["EnergyShieldRechargeAppliesToEnergyShield"] = appliesToES ? 1 : 0;

        if (appliesToLife || appliesToES)
        {
            double inc = modDB.Sum(ModType.Inc, null, "EnergyShieldRecharge");
            double moreVal = modDB.More(null, "EnergyShieldRecharge");
            double baseRate = modDB.Override(null, "EnergyShieldRecharge")?.AsNumber()
                ?? MiscConstants.EnergyShieldRechargeBase;

            if (appliesToLife)
            {
                double pool = output.TryGetValue("Life", out double lifeVal) ? lifeVal : 0;
                double recharge = pool * baseRate * (1 + inc / 100) * moreVal;
                double recoveryRateMod = output.TryGetValue("LifeRecoveryRateMod", out double lrrm) ? lrrm : 1;
                output["LifeRecharge"] = CalcLib.Round(recharge * recoveryRateMod);
            }
            else
            {
                double pool = output.TryGetValue("EnergyShield", out double esVal) ? esVal : 0;
                double recharge = pool * baseRate * (1 + inc / 100) * moreVal;
                double recoveryRateMod = output.TryGetValue("EnergyShieldRecoveryRateMod", out double esrrm) ? esrrm : 1;
                output["EnergyShieldRecharge"] = CalcLib.Round(recharge * recoveryRateMod);
            }

            double rechargeFaster = modDB.Sum(ModType.Inc, null, "EnergyShieldRechargeFaster");
            output["EnergyShieldRechargeDelay"] = MiscConstants.EnergyShieldRechargeDelay / (1 + rechargeFaster / 100);
        }
        else
        {
            output["EnergyShieldRecharge"] = 0;
        }
    }

    // ─── Recoup (lines 1362-1450) ───

    private static void Recoup(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;
        string[] recoupTypes = ["Life", "Mana", "EnergyShield"];

        output["anyRecoup"] = 0;

        // Generic recoup
        foreach (string recoupType in recoupTypes)
        {
            double baseRecoup = modDB.Sum(ModType.Base, null, $"{recoupType}Recoup");
            double recoveryRateMod = output.TryGetValue($"{recoupType}RecoveryRateMod", out double rrm) ? rrm : 1;

            if (recoupType == "Life" && modDB.Flag(null, "EnergyShieldRecoupInsteadOfLife"))
            {
                output["LifeRecoup"] = 0;
                double lifeRecoup = modDB.Sum(ModType.Base, null, "LifeRecoup");
                modDB.NewMod("EnergyShieldRecoup", ModType.Base, lifeRecoup, "Life Recoup Conversion");
            }
            else
            {
                output[$"{recoupType}Recoup"] = baseRecoup * recoveryRateMod;
                output["anyRecoup"] = output["anyRecoup"] + output[$"{recoupType}Recoup"];
            }
        }

        // Absorption charge elemental ES recoup
        if (modDB.Flag(null, "UsePowerCharges") && modDB.Flag(null, "PowerChargesConvertToAbsorptionCharges"))
        {
            double perAbsorption = modDB.Sum(ModType.Base, null, "PerAbsorptionElementalEnergyShieldRecoup");
            modDB.NewMod("ColdEnergyShieldRecoup", ModType.Base, perAbsorption, "Absorption Charges",
                tags: new Tags.MultiplierTag { Var = "AbsorptionCharge" });
            modDB.NewMod("FireEnergyShieldRecoup", ModType.Base, perAbsorption, "Absorption Charges",
                tags: new Tags.MultiplierTag { Var = "AbsorptionCharge" });
            modDB.NewMod("LightningEnergyShieldRecoup", ModType.Base, perAbsorption, "Absorption Charges",
                tags: new Tags.MultiplierTag { Var = "AbsorptionCharge" });
        }

        // Damage-type-specific recoup
        foreach (string recoupType in recoupTypes)
        {
            double recoveryRateMod = output.TryGetValue($"{recoupType}RecoveryRateMod", out double rrm) ? rrm : 1;

            foreach (string damageType in DmgTypeList)
            {
                if (recoupType == "Life" && modDB.Flag(null, "EnergyShieldRecoupInsteadOfLife"))
                {
                    output[$"{damageType}LifeRecoup"] = 0;
                    double lifeRecoup = modDB.Sum(ModType.Base, null, $"{damageType}LifeRecoup");
                    modDB.NewMod($"{damageType}EnergyShieldRecoup", ModType.Base, lifeRecoup, "Life Recoup Conversion");
                }
                else
                {
                    double recoup = modDB.Sum(ModType.Base, null, $"{damageType}{recoupType}Recoup");
                    output[$"{damageType}{recoupType}Recoup"] = recoup * recoveryRateMod;
                    output["anyRecoup"] = output["anyRecoup"] + output[$"{damageType}{recoupType}Recoup"];
                }
            }
        }

        // Pseudo recoup (e.g., % physical damage prevented from hits regenerated)
        foreach (string resource in recoupTypes)
        {
            if (modDB.Flag(null, $"No{resource}Regen") || modDB.Flag(null, $"CannotGain{resource}"))
                continue;

            double pseudoRecoup = modDB.Sum(ModType.Base, null, $"PhysicalDamageMitigated{resource}PseudoRecoup");
            if (pseudoRecoup > 0)
            {
                double duration = modDB.Sum(ModType.Base, null, $"PhysicalDamageMitigated{resource}PseudoRecoupDuration");
                if (duration == 0) duration = 4;
                output[$"PhysicalDamageMitigated{resource}PseudoRecoupDuration"] = duration;

                double incRegen = modDB.Sum(ModType.Inc, null, $"{resource}Regen");
                double moreRegen = modDB.More(null, $"{resource}Regen");
                double recoveryRateMod = output.TryGetValue($"{resource}RecoveryRateMod", out double rrm) ? rrm : 1;
                output[$"PhysicalDamageMitigated{resource}PseudoRecoup"] = pseudoRecoup * (1 + incRegen / 100) * moreRegen * recoveryRateMod;
                output["anyRecoup"] = output["anyRecoup"] + output[$"PhysicalDamageMitigated{resource}PseudoRecoup"];
            }
        }
    }

    // ─── Ward Recharge (lines 1452-1462) ───

    private static void WardRecharge(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        double rechargeFaster = modDB.Sum(ModType.Inc, null, "WardRechargeFaster");
        output["WardRechargeDelay"] = MiscConstants.WardRechargeDelay / (1 + rechargeFaster / 100);
    }

    // ─── Damage Reduction (lines 1464-1470) ───

    private static void DamageReduction(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        double damageReductionMax = modDB.Override(null, "DamageReductionMax")?.AsNumber()
            ?? MiscConstants.DamageReductionCap;
        output["DamageReductionMax"] = damageReductionMax;

        modDB.NewMod("ArmourAppliesToPhysicalDamageTaken", ModType.Base, 100);

        foreach (string damageType in DmgTypeList)
        {
            double baseReduction = modDB.Sum(ModType.Base, null,
                $"{damageType}DamageReduction",
                IsElemental(damageType) ? "ElementalDamageReduction" : "");
            output[$"Base{damageType}DamageReduction"] = Math.Min(Math.Max(0, baseReduction), damageReductionMax);

            double whenHit = modDB.Sum(ModType.Base, null, $"{damageType}DamageReductionWhenHit");
            output[$"Base{damageType}DamageReductionWhenHit"] = Math.Min(
                Math.Max(0, output[$"Base{damageType}DamageReduction"] + whenHit),
                damageReductionMax);
        }
    }

    // ─── Movement Speed (lines 1472-1489) ───

    private static void MovementSpeed(Actor actor)
    {
        var modDB = actor.ModDB;
        var enemyDB = actor.Enemy?.ModDB;
        var output = actor.Output;

        output["MovementSpeedMod"] = modDB.Override(null, "MovementSpeed")?.AsNumber()
            ?? CalcLib.Mod(modDB, null, "MovementSpeed");

        if (modDB.Flag(null, "MovementSpeedCannotBeBelowBase"))
            output["MovementSpeedMod"] = Math.Max(output["MovementSpeedMod"], 1);

        output["EffectiveMovementSpeedMod"] = output["MovementSpeedMod"] * output["ActionSpeedMod"];

        if (enemyDB != null && enemyDB.Flag(null, "Blind"))
        {
            output["BlindEffectMod"] = CalcLib.Mod(enemyDB, null, "BlindEffect", "BuffEffectOnSelf") * 100;
        }
    }

    // ─── Block/Suppress Recovery (lines 1491-1503) ───

    private static void BlockSuppressRecovery(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;
        bool cannotRecoverLife = modDB.Flag(null, "CannotRecoverLifeOutsideLeech");

        output["LifeOnBlock"] = cannotRecoverLife ? 0 : modDB.Sum(ModType.Base, null, "LifeOnBlock");
        output["LifeOnSuppress"] = cannotRecoverLife ? 0 : modDB.Sum(ModType.Base, null, "LifeOnSuppress");
        output["ManaOnBlock"] = modDB.Sum(ModType.Base, null, "ManaOnBlock");
        output["EnergyShieldOnBlock"] = modDB.Sum(ModType.Base, null, "EnergyShieldOnBlock");
        output["EnergyShieldOnSpellBlock"] = modDB.Sum(ModType.Base, null, "EnergyShieldOnSpellBlock");
        output["EnergyShieldOnSuppress"] = modDB.Sum(ModType.Base, null, "EnergyShieldOnSuppress");
    }

    // ─── Avoidance (lines 1505-1567) ───

    private static void Avoidance(Actor actor)
    {
        var modDB = actor.ModDB;
        var enemyDB = actor.Enemy?.ModDB;
        var output = actor.Output;

        // Per-damage-type avoidance
        output["specificTypeAvoidance"] = 0;
        foreach (string damageType in DmgTypeList)
        {
            output[$"Avoid{damageType}DamageChance"] = Math.Min(
                modDB.Sum(ModType.Base, null, $"Avoid{damageType}DamageChance"),
                MiscConstants.AvoidChanceCap);
            if (output[$"Avoid{damageType}DamageChance"] > 0)
                output["specificTypeAvoidance"] = 1;
        }

        output["AvoidProjectilesChance"] = Math.Min(
            modDB.Sum(ModType.Base, null, "AvoidProjectilesChance"),
            MiscConstants.AvoidChanceCap);
        output["AvoidAllDamageFromHitsChance"] = Math.Min(
            modDB.Sum(ModType.Base, null, "AvoidAllDamageFromHitsChance"),
            MiscConstants.AvoidChanceCap);

        // Blind/Impale avoidance
        output["BlindAvoidChance"] = modDB.Flag(null, "BlindImmune")
            ? 100 : Math.Min(modDB.Sum(ModType.Base, null, "AvoidBlind"), 100);
        output["ImpaleAvoidChance"] = modDB.Flag(null, "ImpaleImmune")
            ? 100 : Math.Min(modDB.Sum(ModType.Base, null, "AvoidImpale"), 100);

        // Immunities
        output["CorruptedBloodImmunity"] = modDB.Flag(null, "CorruptedBloodImmune") ? 1 : 0;
        output["MaimImmunity"] = modDB.Flag(null, "MaimImmune") ? 1 : 0;
        output["HinderImmunity"] = modDB.Flag(null, "HinderImmune") ? 1 : 0;
        output["KnockbackImmunity"] = modDB.Flag(null, "KnockbackImmune") ? 1 : 0;

        // Spell suppression → ailment avoidance (Ancestral Vision)
        double spellSuppressionChance = output.TryGetValue("SpellSuppressionChance", out double ssc) ? ssc : 0;
        if (modDB.Flag(null, "SpellSuppressionAppliesToAilmentAvoidance"))
        {
            double percent = modDB.Sum(ModType.Base, null, "SpellSuppressionAppliesToAilmentAvoidancePercent") / 100.0;
            modDB.NewMod("AvoidElementalAilments", ModType.Base, Math.Floor(percent * spellSuppressionChance), "Ancestral Vision");
        }

        // ShockAvoid applies to elemental ailments (Stormshroud)
        if (modDB.Flag(null, "ShockAvoidAppliesToElementalAilments"))
        {
            double shockBase = modDB.Sum(ModType.Base, null, "AvoidShock");
            if (shockBase != 0)
                modDB.NewMod("AvoidShockAppliesToElementalAilments", ModType.Base, shockBase, "Stormshroud");
        }

        // Non-elemental ailment avoidance
        foreach (string ailment in AilmentData.NonElementalAilmentTypeList)
        {
            output[$"{ailment}AvoidChance"] = modDB.Flag(null, $"{ailment}Immune")
                ? 100 : Math.Floor(Math.Min(modDB.Sum(ModType.Base, null, $"Avoid{ailment}", "AvoidAilments"), 100));
        }

        // Elemental ailment avoidance
        foreach (string ailment in AilmentData.ElementalAilmentTypeList)
        {
            bool shockAvoidAppliesToAll = modDB.Flag(null, "ShockAvoidAppliesToElementalAilments") && ailment != "Shock";
            double shockExtra = shockAvoidAppliesToAll ? modDB.Sum(ModType.Base, null, "AvoidShock") : 0;

            output[$"{ailment}AvoidChance"] = modDB.Flag(null, $"{ailment}Immune", "ElementalAilmentImmune")
                ? 100
                : Math.Floor(Math.Min(
                    modDB.Sum(ModType.Base, null, $"Avoid{ailment}", "AvoidAilments", "AvoidElementalAilments") + shockExtra,
                    100));
        }

        // Curse/Silence avoidance
        output["CurseAvoidChance"] = modDB.Flag(null, "CurseImmune")
            ? 100 : Math.Min(modDB.Sum(ModType.Base, null, "AvoidCurse"), 100);
        output["SilenceAvoidChance"] = modDB.Flag(null, "SilenceImmune")
            ? 100 : output["CurseAvoidChance"];

        // Misc
        output["CritExtraDamageReduction"] = Math.Min(modDB.Sum(ModType.Base, null, "ReduceCritExtraDamage"), 100);
        output["LightRadiusMod"] = CalcLib.Mod(modDB, null, "LightRadius");
        output["LightRadiusInc"] = Math.Max(modDB.Sum(ModType.Inc, null, "LightRadius"), 0);

        output["CurseEffectOnSelf"] = Math.Max(
            modDB.More(null, "CurseEffectOnSelf") * (100 + modDB.Sum(ModType.Inc, null, "CurseEffectOnSelf")), 0);
        output["ExposureEffectOnSelf"] = modDB.More(null, "ExposureEffectOnSelf")
            * (100 + modDB.Sum(ModType.Inc, null, "ExposureEffectOnSelf"));
        output["WitherEffectOnSelf"] = modDB.More(null, "WitherEffectOnSelf")
            * (100 + modDB.Sum(ModType.Inc, null, "WitherEffectOnSelf"));
    }

    // ─── Ailment Duration on Self (lines 1570-1600) ───

    private static void AilmentDurationOnSelf(Actor actor)
    {
        var modDB = actor.ModDB;
        var enemyDB = actor.Enemy?.ModDB;
        var output = actor.Output;

        output["DebuffExpirationRate"] = modDB.Sum(ModType.Base, null, "SelfDebuffExpirationRate");
        output["DebuffExpirationModifier"] = 10000.0 / (100 + output["DebuffExpirationRate"]);
        output["showDebuffExpirationModifier"] = output["DebuffExpirationModifier"] != 100 ? 1 : 0;

        output["SelfBlindDuration"] = modDB.More(null, "SelfBlindDuration")
            * (100 + modDB.Sum(ModType.Inc, null, "SelfBlindDuration"))
            * output["DebuffExpirationModifier"] / 100;

        // Firesong: ignite duration applies to elemental ailments
        if (modDB.Flag(null, "IgniteDurationAppliesToElementalAilments"))
        {
            double igInc = modDB.Sum(ModType.Inc, null, "SelfIgniteDuration");
            double igMore = modDB.More(null, "SelfIgniteDuration");
            if (igInc != 0)
                modDB.NewMod("SelfIgniteDurationToElementalAilments", ModType.Inc, igInc, "Firesong");
            if (igMore != 1)
                modDB.NewMod("SelfIgniteDurationToElementalAilments", ModType.More, igMore, "Firesong");
        }

        // Non-elemental ailment duration
        foreach (string ailment in AilmentData.NonElementalAilmentTypeList)
        {
            double moreVal = modDB.More(null, $"Self{ailment}Duration", "SelfAilmentDuration");
            double incVal = (100 + modDB.Sum(ModType.Inc, null, $"Self{ailment}Duration", "SelfAilmentDuration")) * 100;
            double specificRate = modDB.Sum(ModType.Base, null, $"Self{ailment}DebuffExpirationRate");
            output[$"Self{ailment}Duration"] = incVal * moreVal / (100 + output["DebuffExpirationRate"] + specificRate);
        }

        // Elemental ailment duration
        foreach (string ailment in AilmentData.ElementalAilmentTypeList)
        {
            bool igniteAppliesToAll = modDB.Flag(null, "IgniteDurationAppliesToElementalAilments") && ailment != "Ignite";
            double moreVal = modDB.More(null, $"Self{ailment}Duration", "SelfAilmentDuration", "SelfElementalAilmentDuration")
                * (igniteAppliesToAll ? modDB.More(null, "SelfIgniteDuration") : 1);
            double incVal = (100 + modDB.Sum(ModType.Inc, null, $"Self{ailment}Duration", "SelfAilmentDuration", "SelfElementalAilmentDuration")
                + (igniteAppliesToAll ? modDB.Sum(ModType.Inc, null, "SelfIgniteDuration") : 0)) * 100;
            double specificRate = modDB.Sum(ModType.Base, null, $"Self{ailment}DebuffExpirationRate");
            output[$"Self{ailment}Duration"] = moreVal * incVal / (100 + output["DebuffExpirationRate"] + specificRate);
        }

        // Self ailment effect
        foreach (string ailment in AilmentData.AilmentTypeList)
        {
            double selfMod = CalcLib.Mod(modDB, null, $"Self{ailment}Effect");
            double enemyEffect = modDB.Flag(null, $"Condition:{ailment}edSelf")
                ? CalcLib.Mod(modDB, null, $"Enemy{ailment}Effect")
                : (enemyDB != null ? CalcLib.Mod(enemyDB, null, $"Enemy{ailment}Effect") : 1);
            output[$"Self{ailment}Effect"] = selfMod * enemyEffect * 100;
        }
    }
}
