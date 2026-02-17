using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Skills;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Core offence calculation pipeline — spell hit path.
/// Ported from CalcOffence.lua calcs.offence() (lines 319-5873).
/// Phase 12 implements the spell hit path only (single "Skill" pass).
/// Attack passes (MH/OH, accuracy, weapon data) deferred to Phase 13.
/// </summary>
public static class CalcOffence
{
    /// <summary>
    /// Calculate offence outputs for a given active skill.
    /// Populates actor.Output with Speed, CritChance, CritMultiplier, AverageHit,
    /// TotalDPS, CombinedDPS, leech values, and per-type damage outputs.
    /// </summary>
    public static void Offence(Actor actor, ActiveSkill activeSkill)
    {
        var modDB = actor.ModDB;
        var enemyDB = actor.Enemy?.ModDB;
        var output = actor.Output;
        var skillModList = activeSkill.SkillModList;
        var skillData = activeSkill.SkillData;
        var skillFlags = activeSkill.SkillFlags;
        var skillCfg = activeSkill.SkillCfg;

        // Early exit for disabled skills
        if (skillFlags.ContainsKey("disable"))
        {
            output["CombinedDPS"] = 0;
            return;
        }

        // Spells always hit (no accuracy check) — attacks deferred to Phase 13
        bool isAttack = skillFlags.ContainsKey("attack");
        if (isAttack)
        {
            // Attack path not yet implemented — set zero DPS and return
            output["CombinedDPS"] = 0;
            return;
        }

        output["HitChance"] = 100;
        output["AccuracyHitChance"] = 100;

        // Enemy block chance reduces hit chance
        double enemyBlockChance = 0;
        if (enemyDB != null)
        {
            enemyBlockChance = Math.Max(
                Math.Min(enemyDB.Sum(ModType.Base, skillCfg, "BlockChance"), 100)
                - skillModList.Sum(ModType.Base, skillCfg, "reduceEnemyBlock"), 0);
        }
        output["enemyBlockChance"] = enemyBlockChance;
        output["HitChance"] = output["AccuracyHitChance"] * (1 - enemyBlockChance / 100);

        // ─── Update skillData from LIST mods ───
        UpdateSkillData(activeSkill);

        // ─── Stat bonuses (IronWill, Transfiguration) ───
        ApplyStatBonuses(activeSkill, actor);

        // ─── Conversion table ───
        var convTable = ConversionTable.Build(skillModList, skillCfg);

        // ─── Configure pass (spell: single "Skill" pass) ───
        // For spells, source = skillData, cfg = skillCfg, output = actor.Output
        var source = skillData;
        var cfg = skillCfg;

        // ─── Repeats ───
        double repeats = 1 + skillModList.Sum(ModType.Base, skillCfg, "RepeatCount");
        output["Repeats"] = repeats;

        // ─── Cast speed ───
        ComputeCastSpeed(activeSkill, actor, output, skillModList, skillData, skillFlags, cfg, repeats);

        // ─── dpsMultiplier ───
        double dpsMultiplier = skillData.TryGetValue("dpsMultiplier", out double dpm) ? dpm : 1;
        dpsMultiplier *= (1 + skillModList.Sum(ModType.Inc, skillCfg, "DPS") / 100) * skillModList.More(skillCfg, "DPS");
        skillData["dpsMultiplier"] = dpsMultiplier;

        // ─── quantityMultiplier ───
        double quantityMultiplier = Math.Max(skillModList.Sum(ModType.Base, skillCfg, "QuantityMultiplier"), 1);

        // ─── Crit ───
        ComputeCrit(output, skillModList, skillData, cfg, skillCfg, enemyDB);

        // ─── Double/Triple damage ───
        ComputeDoubleDamage(output, skillModList, cfg, skillCfg, enemyDB);

        // ─── hitRate (for leech) ───
        double hitRate = output["HitChance"] / 100 * output["Speed"] * dpsMultiplier;

        // ─── Culling ───
        double criticalCull = skillModList.Max(cfg, "CriticalCullPercent") ?? 0;
        if (criticalCull > 0 && hitRate > 0)
            criticalCull = Math.Min(criticalCull, criticalCull * (1 - Math.Pow(1 - output["CritChance"] / 100, hitRate)));
        double regularCull = skillModList.Max(cfg, "CullPercent") ?? 0;
        double maxCullPercent = Math.Max(criticalCull, regularCull);
        output["CullPercent"] = maxCullPercent;
        output["CullMultiplier"] = 100 / (100 - maxCullPercent);

        // ─── Reservation DPS multiplier ───
        double enemyLifeResPct = enemyDB?.Sum(ModType.Base, null, "LifeReservationPercent") ?? 0;
        output["ReservationDpsMultiplier"] = 100 / (100 - enemyLifeResPct);

        // ─── Base hit damage per type ───
        var baseDamage = new Dictionary<string, double>();
        bool hasHitFlag = skillFlags.ContainsKey("hit");

        // canDeal: which types this skill can deal
        var canDeal = new Dictionary<string, bool>();
        foreach (string dmgType in DamageTypeFlags.DmgTypeList)
        {
            canDeal[dmgType] = !skillModList.Flag(skillCfg, $"DealNo{dmgType}", "DealNoDamage");
        }

        double baseMultiplier = skillData.TryGetValue("baseMultiplier", out double bm) ? bm : 1;
        double damageEffectiveness = skillData.TryGetValue("damageEffectiveness", out double de) ? de : 1;

        foreach (string damageType in DamageTypeFlags.DmgTypeList)
        {
            double addedMin = skillModList.Sum(ModType.Base, cfg, $"{damageType}Min");
            if (enemyDB != null)
                addedMin += enemyDB.Sum(ModType.Base, cfg, $"Self{damageType}Min");

            double addedMax = skillModList.Sum(ModType.Base, cfg, $"{damageType}Max");
            if (enemyDB != null)
                addedMax += enemyDB.Sum(ModType.Base, cfg, $"Self{damageType}Max");

            double addedMult = CalcLib.Mod(skillModList, cfg, $"Added{damageType}Damage", "AddedDamage");

            source.TryGetValue($"{damageType}Min", out double sourceMin);
            source.TryGetValue($"{damageType}Max", out double sourceMax);
            source.TryGetValue($"{damageType}BonusMin", out double bonusMin);
            source.TryGetValue($"{damageType}BonusMax", out double bonusMax);

            double baseMin = (sourceMin + bonusMin) * baseMultiplier + addedMin * damageEffectiveness * addedMult;
            double baseMax = (sourceMax + bonusMax) * baseMultiplier + addedMax * damageEffectiveness * addedMult;

            output[$"{damageType}MinBase"] = baseMin;
            output[$"{damageType}MaxBase"] = baseMax;

            baseDamage[$"{damageType}MinBase"] = baseMin;
            baseDamage[$"{damageType}MaxBase"] = baseMax;
        }

        // ─── Hit damage loop (2 passes: crit then non-crit) ───
        double totalHitMin = 0, totalHitMax = 0, totalHitAvg = 0;
        double totalCritMin = 0, totalCritMax = 0, totalCritAvg = 0;

        bool ghostReaver = skillModList.Flag(null, "GhostReaver");
        output["LifeLeech"] = 0;
        output["LifeLeechInstant"] = 0;
        output["EnergyShieldLeech"] = 0;
        output["EnergyShieldLeechInstant"] = 0;
        output["ManaLeech"] = 0;
        output["ManaLeechInstant"] = 0;

        double critChancePct = output["CritChance"];
        double critMultiplier = output["CritMultiplier"];
        double scaledDamageEffect = output["ScaledDamageEffect"];

        for (int pass = 1; pass <= 2; pass++)
        {
            // Pass 1 = crit, pass 2 = non-crit
            bool isCrit = (pass == 1);
            if (skillCfg?.SkillCond != null)
                skillCfg.SkillCond["CriticalStrike"] = isCrit;

            double lifeLeechTotal = 0;
            double esLeechTotal = 0;
            double manaLeechTotal = 0;

            bool noLifeLeech = skillModList.Flag(cfg, "CannotLeechLife")
                || (enemyDB?.Flag(null, "CannotLeechLifeFromSelf") ?? false)
                || skillModList.Flag(cfg, "CannotGainLife");
            bool noEsLeech = skillModList.Flag(cfg, "CannotLeechEnergyShield")
                || (enemyDB?.Flag(null, "CannotLeechEnergyShieldFromSelf") ?? false)
                || skillModList.Flag(cfg, "CannotGainEnergyShield");
            bool noManaLeech = skillModList.Flag(cfg, "CannotLeechMana")
                || (enemyDB?.Flag(null, "CannotLeechManaFromSelf") ?? false)
                || skillModList.Flag(cfg, "CannotGainMana");

            foreach (string damageType in DamageTypeFlags.DmgTypeList)
            {
                double damageTypeHitMin = 0, damageTypeHitMax = 0, damageTypeHitAvg = 0;

                if (hasHitFlag && canDeal[damageType])
                {
                    (damageTypeHitMin, damageTypeHitMax) = CalcDamage.CalcDamageMinMax(
                        convTable, skillModList, cfg, baseDamage, damageType, 0);

                    double convMult = convTable[damageType].Mult;

                    // allMult = convMult * scaledDamageEffect * (crit ? critMultiplier : 1)
                    double allMult = convMult * scaledDamageEffect;
                    if (isCrit)
                        allMult *= critMultiplier;

                    damageTypeHitMin *= allMult;
                    damageTypeHitMax *= allMult;

                    // Lucky damage
                    double luckyChance = 0;
                    if (skillModList.Flag(skillCfg, "LuckyHits")
                        || (isCrit && skillModList.Flag(skillCfg, "CritLucky"))
                        || (!isCrit && damageType == "Lightning" && skillModList.Flag(skillCfg, "LightningNoCritLucky"))
                        || (damageType == "Lightning" && modDB.Flag(null, "LightningLuckHits"))
                        || (damageType == "Chaos" && modDB.Flag(null, "ChaosLuckyHits"))
                        || (damageType == "Fire" && modDB.Flag(null, "FireLuckyHits"))
                        || (damageType == "Cold" && modDB.Flag(null, "ColdLuckyHits"))
                        || (DamageTypeFlags.IsElemental(damageType) && skillModList.Flag(skillCfg, "ElementalLuckHits")))
                    {
                        luckyChance = 1;
                    }
                    else
                    {
                        luckyChance = Math.Min(skillModList.Sum(ModType.Base, skillCfg, "LuckyHitsChance"), 100) / 100;
                    }

                    if (skillModList.Flag(skillCfg, "UnluckyHits"))
                        luckyChance -= 1;

                    damageTypeHitAvg = CalcHitDamage.LuckyDamage(damageTypeHitMin, damageTypeHitMax, luckyChance);

                    // ─── Apply resist/pen ───
                    if (damageTypeHitMin != 0 || damageTypeHitMax != 0)
                    {
                        double effMult = CalcEffMult(damageType, skillModList, cfg, skillCfg, skillFlags, enemyDB, modDB, damageTypeHitAvg);

                        damageTypeHitMin *= effMult;
                        damageTypeHitMax *= effMult;
                        damageTypeHitAvg *= effMult;
                        output[$"{damageType}EffMult"] = effMult;
                    }

                    // ─── Leech accumulation ───
                    double lifeLeech = 0;
                    double esLeech = 0;
                    double manaLeech = 0;

                    bool isEle = DamageTypeFlags.IsElemental(damageType);

                    if (skillModList.Flag(null, "LifeLeechBasedOnChaosDamage"))
                    {
                        if (damageType == "Chaos")
                        {
                            lifeLeech = skillModList.Sum(ModType.Base, cfg,
                                "DamageLeech", "DamageLifeLeech",
                                "PhysicalDamageLifeLeech", "LightningDamageLifeLeech",
                                "ColdDamageLifeLeech", "FireDamageLifeLeech",
                                "ChaosDamageLifeLeech", "ElementalDamageLifeLeech");
                            if (enemyDB != null)
                                lifeLeech += enemyDB.Sum(ModType.Base, cfg, "SelfDamageLifeLeech") / 100;
                        }
                    }
                    else
                    {
                        string[] leechNames = isEle
                            ? ["DamageLeech", "DamageLifeLeech", $"{damageType}DamageLifeLeech", "ElementalDamageLifeLeech"]
                            : ["DamageLeech", "DamageLifeLeech", $"{damageType}DamageLifeLeech"];
                        lifeLeech = skillModList.Sum(ModType.Base, cfg, leechNames);
                        if (enemyDB != null)
                            lifeLeech += enemyDB.Sum(ModType.Base, cfg, "SelfDamageLifeLeech") / 100;
                    }

                    {
                        string[] esLeechNames = isEle
                            ? ["DamageEnergyShieldLeech", $"{damageType}DamageEnergyShieldLeech", "ElementalDamageEnergyShieldLeech"]
                            : ["DamageEnergyShieldLeech", $"{damageType}DamageEnergyShieldLeech"];
                        esLeech = skillModList.Sum(ModType.Base, cfg, esLeechNames);
                        if (enemyDB != null)
                            esLeech += enemyDB.Sum(ModType.Base, cfg, "SelfDamageEnergyShieldLeech") / 100;
                    }

                    {
                        string[] manaLeechNames = isEle
                            ? ["DamageLeech", "DamageManaLeech", $"{damageType}DamageManaLeech", "ElementalDamageManaLeech"]
                            : ["DamageLeech", "DamageManaLeech", $"{damageType}DamageManaLeech"];
                        manaLeech = skillModList.Sum(ModType.Base, cfg, manaLeechNames);
                        if (enemyDB != null)
                            manaLeech += enemyDB.Sum(ModType.Base, cfg, "SelfDamageManaLeech") / 100;
                    }

                    if (ghostReaver && !noLifeLeech)
                    {
                        esLeech += lifeLeech;
                        lifeLeech = 0;
                    }

                    if (lifeLeech > 0 && !noLifeLeech)
                        lifeLeechTotal += damageTypeHitAvg * lifeLeech / 100;
                    if (manaLeech > 0 && !noManaLeech)
                        manaLeechTotal += damageTypeHitAvg * manaLeech / 100;
                    if (esLeech > 0 && !noEsLeech)
                        esLeechTotal += damageTypeHitAvg * esLeech / 100;
                }

                if (isCrit)
                {
                    output[$"{damageType}CritAverage"] = damageTypeHitAvg;
                    totalCritAvg += damageTypeHitAvg;
                    totalCritMin += damageTypeHitMin;
                    totalCritMax += damageTypeHitMax;
                }
                else
                {
                    output[$"{damageType}Min"] = damageTypeHitMin;
                    output[$"{damageType}Max"] = damageTypeHitMax;
                    output[$"{damageType}HitAverage"] = damageTypeHitAvg;
                    totalHitAvg += damageTypeHitAvg;
                    totalHitMin += damageTypeHitMin;
                    totalHitMax += damageTypeHitMax;
                }
            }

            // Per-use leech from skillData
            if (skillData.TryGetValue("lifeLeechPerUse", out double lifeLeechPerUse))
                lifeLeechTotal += lifeLeechPerUse;
            if (skillData.TryGetValue("manaLeechPerUse", out double manaLeechPerUse))
                manaLeechTotal += manaLeechPerUse;

            // Leech caps per instance
            output.TryGetValue("MaxLifeLeechInstance", out double maxLifeInst);
            output.TryGetValue("MaxEnergyShieldLeechInstance", out double maxEsInst);
            output.TryGetValue("MaxManaLeechInstance", out double maxManaInst);
            lifeLeechTotal = Math.Min(lifeLeechTotal, maxLifeInst);
            esLeechTotal = Math.Min(esLeechTotal, maxEsInst);
            manaLeechTotal = Math.Min(manaLeechTotal, maxManaInst);

            double portion = isCrit ? (critChancePct / 100) : (1 - critChancePct / 100);
            output["LifeLeech"] += lifeLeechTotal * portion;
            output["EnergyShieldLeech"] += esLeechTotal * portion;
            output["ManaLeech"] += manaLeechTotal * portion;
        }

        output["TotalMin"] = totalHitMin;
        output["TotalMax"] = totalHitMax;

        // ─── Instant leech ───
        ComputeInstantLeech(output, skillModList, cfg, hitRate);

        // ─── Gain on hit ───
        ComputeGainOnHit(output, skillModList, skillFlags, cfg, enemyDB, hitRate);

        // ─── Gain on kill ───
        ComputeGainOnKill(output, skillModList, skillFlags, cfg);

        // ─── Average damage and final DPS ───
        output["AverageHit"] = totalHitAvg * (1 - critChancePct / 100) + totalCritAvg * (critChancePct / 100);
        output["AverageDamage"] = output["AverageHit"] * output["HitChance"] / 100;
        output["TotalDPS"] = output["AverageDamage"] * output["Speed"] * dpsMultiplier * quantityMultiplier;

        // ─── CombinedDPS ───
        output["CombinedDPS"] = output["TotalDPS"];
        double bestCull = output["CullMultiplier"];
        output["CullingDPS"] = output["CombinedDPS"] * (bestCull - 1);
        output["ReservationDPS"] = output["CombinedDPS"] * (output["ReservationDpsMultiplier"] - 1);
        output["CombinedDPS"] = output["CombinedDPS"] * bestCull * output["ReservationDpsMultiplier"];
    }

    // ─── Update skillData from LIST mods ───

    private static void UpdateSkillData(ActiveSkill activeSkill)
    {
        var skillModList = activeSkill.SkillModList;
        var skillCfg = activeSkill.SkillCfg;
        var skillData = activeSkill.SkillData;

        var listValues = skillModList.List(skillCfg, "SkillData");
        foreach (var value in listValues)
        {
            if (value.Kind != ModValueKind.Complex || value.Complex == null)
                continue;

            if (value.Complex is Dictionary<string, object?> dict)
            {
                if (dict.TryGetValue("key", out var keyObj) && keyObj is string key &&
                    dict.TryGetValue("value", out var valObj))
                {
                    double numVal = valObj switch
                    {
                        double d => d,
                        int i => i,
                        float f => f,
                        bool b => b ? 1 : 0,
                        _ => 0,
                    };

                    if (dict.TryGetValue("merge", out var mergeObj) && mergeObj is string merge && merge == "MAX")
                    {
                        skillData.TryGetValue(key, out double existing);
                        skillData[key] = Math.Max(numVal, existing);
                    }
                    else
                    {
                        skillData[key] = numVal;
                    }
                }
            }
        }
    }

    // ─── Stat bonuses ───

    private static void ApplyStatBonuses(ActiveSkill activeSkill, Actor actor)
    {
        var skillModList = activeSkill.SkillModList;

        // IronWill: Spell damage bonus from Strength
        if (skillModList.Flag(null, "IronWill"))
        {
            double strDmgBonus = actor.Output.TryGetValue("StrDmgBonus", out double s) ? s : 0;
            skillModList.AddMod(new Mod
            {
                Name = "Damage",
                Type = ModType.Inc,
                Value = strDmgBonus,
                Flags = ModFlag.Spell,
                Source = "Strength",
            });
        }

        // Transfiguration of Mind: damage bonus from increased Mana
        if (skillModList.Flag(null, "TransfigurationOfMind"))
        {
            double manaInc = skillModList.Sum(ModType.Inc, null, "Mana");
            double bonus = Math.Floor(manaInc * MiscConstants.Transfiguration);
            skillModList.AddMod(new Mod
            {
                Name = "Damage",
                Type = ModType.Inc,
                Value = bonus,
                Source = "Transfiguration of Mind",
            });
        }

        // Transfiguration of Body: damage bonus from increased Life (attack only)
        if (skillModList.Flag(null, "TransfigurationOfBody"))
        {
            double lifeInc = skillModList.Sum(ModType.Inc, null, "Life");
            double bonus = Math.Floor(lifeInc * MiscConstants.Transfiguration);
            skillModList.AddMod(new Mod
            {
                Name = "Damage",
                Type = ModType.Inc,
                Value = bonus,
                Flags = ModFlag.Attack,
                Source = "Transfiguration of Body",
            });
        }

        // Transfiguration of Soul: damage bonus from increased ES (spell only)
        if (skillModList.Flag(null, "TransfigurationOfSoul"))
        {
            double esInc = skillModList.Sum(ModType.Inc, null, "EnergyShield");
            double bonus = Math.Floor(esInc * MiscConstants.Transfiguration);
            skillModList.AddMod(new Mod
            {
                Name = "Damage",
                Type = ModType.Inc,
                Value = bonus,
                Flags = ModFlag.Spell,
                Source = "Transfiguration of Soul",
            });
        }
    }

    // ─── Cast speed ───

    private static void ComputeCastSpeed(ActiveSkill activeSkill, Actor actor,
        Dictionary<string, double> output, ModStore skillModList,
        Dictionary<string, double> skillData, Dictionary<string, bool> skillFlags,
        ModConfig? cfg, double repeats)
    {
        double castTime = activeSkill.GrantedEffect.CastTime;

        // Zero cast time with no override and not triggered → instant (speed=0)
        if (castTime == 0
            && !skillData.ContainsKey("castTimeOverride")
            && !skillData.ContainsKey("triggered"))
        {
            output["Time"] = 0;
            output["Speed"] = 0;
            return;
        }

        // Time override
        if (skillData.TryGetValue("timeOverride", out double timeOverride))
        {
            output["Time"] = timeOverride;
            output["Speed"] = 1 / timeOverride;
            return;
        }

        // Fixed cast time
        if (skillData.ContainsKey("fixedCastTime"))
        {
            output["Time"] = castTime > 0 ? castTime : 1;
            output["Speed"] = 1 / output["Time"];
            return;
        }

        // Trigger time
        if (skillData.TryGetValue("triggerTime", out double triggerTime) && skillData.ContainsKey("triggered"))
        {
            double cooldownRecInc = 1 + skillModList.Sum(ModType.Inc, cfg, "CooldownRecovery") / 100;
            double activeLinked = skillModList.Sum(ModType.Base, cfg, "ActiveSkillsLinkedToTrigger");
            double time = triggerTime / cooldownRecInc;
            if (activeLinked > 0)
                time *= activeLinked;
            output["Time"] = time;
            output["TriggerTime"] = time;
            output["Speed"] = 1 / time;
            return;
        }

        // Trigger rate
        if (skillData.TryGetValue("triggerRate", out double triggerRate) && skillData.ContainsKey("triggered"))
        {
            output["Time"] = 1 / triggerRate;
            output["TriggerTime"] = output["Time"];
            output["Speed"] = triggerRate;
            return;
        }

        // Normal cast speed computation
        double baseTime;
        if (skillData.TryGetValue("castTimeOverride", out double cto))
            baseTime = cto;
        else if (castTime > 0)
            baseTime = castTime;
        else
            baseTime = 1;

        double more = skillModList.More(cfg, "Speed");
        double inc = skillModList.Sum(ModType.Inc, cfg, "Speed");

        double speed = 1 / (baseTime / CalcLib.Round((1 + inc / 100) * more, 2)
            + skillModList.Sum(ModType.Base, cfg, "TotalAttackTime")
            + skillModList.Sum(ModType.Base, cfg, "TotalCastTime"));

        output["CastRate"] = speed;

        // selfCast: not triggered, not totem, not trap, not mine
        bool selfCast = skillFlags.ContainsKey("selfCast");
        if (selfCast)
        {
            double actionSpeedMod = output.TryGetValue("ActionSpeedMod", out double asm) ? asm : 1;
            speed *= actionSpeedMod;
            output["CastRate"] = speed;
        }

        // Cooldown cap
        if (output.TryGetValue("Cooldown", out double cooldown) && cooldown > 0)
        {
            output["Cooldown"] = cooldown;
            speed = Math.Min(speed, 1 / cooldown * repeats);
        }
        else if (skillData.TryGetValue("cooldown", out double cdVal) && cdVal > 0)
        {
            int storedUses = skillData.TryGetValue("storedUses", out double su) ? (int)su : 1;
            var (cd, _) = CalcSpeed.CalcSkillCooldown(skillModList, cfg, cdVal, storedUses);
            output["Cooldown"] = cd;
            speed = Math.Min(speed, 1 / cd * repeats);
        }

        // Server tick rate cap (non-channel skills)
        bool isChannel = activeSkill.SkillTypes.ContainsKey(SkillType.Channel);
        if (!isChannel)
        {
            speed = Math.Min(speed, MiscConstants.ServerTickRate * repeats);
        }

        if (speed == 0)
        {
            output["Time"] = 0;
        }
        else
        {
            output["Time"] = 1 / speed;
        }
        output["Speed"] = speed;
    }

    // ─── Critical strike ───

    private static void ComputeCrit(Dictionary<string, double> output, ModStore skillModList,
        Dictionary<string, double> skillData, ModConfig? cfg, ModConfig? skillCfg,
        ModStore? enemyDB)
    {
        double baseCrit = skillData.TryGetValue("CritChance", out double cc) ? cc : 0;

        var critOverride = skillModList.Override(cfg, "CritChance");
        if (critOverride.HasValue && critOverride.Value.AsNumber() == 100)
        {
            output["PreEffectiveCritChance"] = 100;
            output["CritChance"] = 100;
        }
        else
        {
            double baseAdd = 0;
            double inc = 0;
            double more = 1;

            if (!critOverride.HasValue)
            {
                baseAdd = skillModList.Sum(ModType.Base, cfg, "CritChance")
                    + (enemyDB?.Sum(ModType.Base, null, "SelfCritChance") ?? 0);
                inc = skillModList.Sum(ModType.Inc, cfg, "CritChance")
                    + (enemyDB?.Sum(ModType.Inc, null, "SelfCritChance") ?? 0);
                more = skillModList.More(cfg, "CritChance");
            }

            double critChance = (baseCrit + baseAdd) * (1 + inc / 100) * more;

            // Cap
            var capOverride = skillModList.Override(null, "CritChanceCap");
            double critCap = capOverride?.AsNumber() ?? skillModList.Sum(ModType.Base, cfg, "CritChanceCap");
            if (critCap <= 0) critCap = 100;
            critChance = Math.Min(critChance, critCap);

            // Floor at 0 if base > 0
            if ((baseCrit + baseAdd) > 0)
                critChance = Math.Max(critChance, 0);

            output["PreEffectiveCritChance"] = critChance;

            // Lucky crit
            if (skillModList.Flag(cfg, "CritChanceLucky"))
            {
                critChance = (1 - Math.Pow(1 - critChance / 100, 2)) * 100;
            }

            // Every 3rd/5th use
            if (skillModList.Flag(skillCfg, "Every3UseCrit"))
                critChance = (2 * critChance + 100) / 3;
            if (skillModList.Flag(skillCfg, "Every5UseCrit"))
                critChance = (4 * critChance + 100) / 5;

            // Crit confirmation for spells: AccuracyHitChance is 100, so no change
            output["CritChance"] = critChance;
        }

        // Crit multiplier
        if (skillModList.Flag(cfg, "NoCritMultiplier"))
        {
            output["CritMultiplier"] = 1;
        }
        else
        {
            double extraDamage = skillModList.Sum(ModType.Base, cfg, "CritMultiplier") / 100;
            var multiOverride = skillModList.Override(skillCfg, "CritMultiplier");
            if (multiOverride.HasValue)
                extraDamage = (multiOverride.Value.AsNumber() - 100) / 100;

            // Enemy increased crit damage taken
            if (enemyDB != null)
            {
                double enemyInc = 1 + enemyDB.Sum(ModType.Inc, null, "SelfCritMultiplier") / 100;
                extraDamage += enemyDB.Sum(ModType.Base, null, "SelfCritMultiplier") / 100;
                extraDamage = CalcLib.Round(extraDamage * enemyInc, 2);
            }

            output["CritMultiplier"] = 1 + Math.Max(0, extraDamage);
        }

        // Crit effect
        double critPct = output["CritChance"] / 100;
        output["CritEffect"] = (1 - critPct) + critPct * output["CritMultiplier"];

        // Track crit condition
        if (output["CritChance"] != 0 && skillModList is ModList ml)
            ml.Conditions["CritInPast8Sec"] = true;
    }

    // ─── Double/Triple damage ───

    private static void ComputeDoubleDamage(Dictionary<string, double> output, ModStore skillModList,
        ModConfig? cfg, ModConfig? skillCfg, ModStore? enemyDB)
    {
        output["ScaledDamageEffect"] = 1;

        // Triple damage
        double tripleDmgOnCrit = Math.Min(skillModList.Sum(ModType.Base, cfg, "TripleDamageChanceOnCrit"), 100);
        double tripleDmgChance = Math.Min(
            skillModList.Sum(ModType.Base, cfg, "TripleDamageChance")
            + (enemyDB?.Sum(ModType.Base, cfg, "SelfTripleDamageChance") ?? 0)
            + tripleDmgOnCrit * output["CritChance"] / 100, 100);
        output["TripleDamageChance"] = tripleDmgChance;
        output["TripleDamageEffect"] = 2 * tripleDmgChance / 100;

        // Double damage
        double doubleDmgOnCrit = Math.Min(skillModList.Sum(ModType.Base, cfg, "DoubleDamageChanceOnCrit"), 100);
        double doubleDmgChance = Math.Min(
            skillModList.Sum(ModType.Base, cfg, "DoubleDamageChance")
            + (enemyDB?.Sum(ModType.Base, cfg, "SelfDoubleDamageChance") ?? 0)
            + doubleDmgOnCrit * output["CritChance"] / 100, 100);

        // Triple overrides double
        if (tripleDmgChance > 0)
            doubleDmgChance = Math.Max(doubleDmgChance - tripleDmgChance * doubleDmgChance / 100, 0);

        output["DoubleDamageChance"] = doubleDmgChance;
        output["DoubleDamageEffect"] = doubleDmgChance / 100;

        output["ScaledDamageEffect"] *= (1 + output["DoubleDamageEffect"] + output["TripleDamageEffect"]);
    }

    // ─── Effective resist multiplier for a damage type ───

    private static double CalcEffMult(string damageType, ModStore skillModList,
        ModConfig? cfg, ModConfig? skillCfg, Dictionary<string, bool> skillFlags,
        ModStore? enemyDB, ModStore modDB, double hitAvg)
    {
        if (enemyDB == null)
            return 1;

        double resist = 0;
        double pen = 0;
        double takenInc = enemyDB.Sum(ModType.Inc, cfg, "DamageTaken", $"{damageType}DamageTaken");
        double takenMore = enemyDB.More(cfg, "DamageTaken", $"{damageType}DamageTaken");

        bool isEle = DamageTypeFlags.IsElemental(damageType);

        if (damageType == "Physical")
        {
            // Physical: enemy armour + phys DR
            if (skillModList.Flag(cfg, "IgnoreEnemyPhysicalDamageReduction"))
            {
                resist = 0;
            }
            else
            {
                double enemyArmour = Math.Max(CalcLib.Val(enemyDB, "Armour"), 0);
                double armourReduction = CalcDefence.ArmourReductionF(enemyArmour,
                    hitAvg * skillModList.More(cfg, "CalcArmourAsThoughDealing"));
                resist = Math.Min(
                    Math.Max(0,
                        enemyDB.Sum(ModType.Base, null, "PhysicalDamageReduction")
                        + skillModList.Sum(ModType.Base, cfg, "EnemyPhysicalDamageReduction")
                        + armourReduction),
                    MiscConstants.EnemyPhysicalDamageReductionCap);
            }
        }
        else
        {
            resist = CalcResistForType(damageType, enemyDB, cfg);

            if (isEle)
            {
                pen = skillModList.Sum(ModType.Base, cfg, $"{damageType}Penetration", "ElementalPenetration");
                takenInc += enemyDB.Sum(ModType.Inc, cfg, "ElementalDamageTaken");
            }
            else if (damageType == "Chaos")
            {
                pen = skillModList.Sum(ModType.Base, cfg, "ChaosPenetration");
            }
        }

        // Projectile damage taken
        if (skillFlags.ContainsKey("projectile"))
            takenInc += enemyDB.Sum(ModType.Inc, null, "ProjectileDamageTaken");
        if (skillFlags.ContainsKey("projectile") && skillFlags.ContainsKey("attack"))
            takenInc += enemyDB.Sum(ModType.Inc, null, "ProjectileAttackDamageTaken");

        // Trap/mine damage taken
        if (skillFlags.ContainsKey("trap") || skillFlags.ContainsKey("mine"))
            takenInc += enemyDB.Sum(ModType.Inc, null, "TrapMineDamageTaken");

        double effMult = (1 + takenInc / 100) * takenMore;

        // Check if we should ignore resistance
        bool useThisResist = !skillModList.Flag(cfg, $"Ignore{damageType}Resistance",
                isEle ? "IgnoreElementalResistances" : "")
            && !enemyDB.Flag(null, $"SelfIgnore{damageType}Resistance");

        if (useThisResist)
            effMult *= (1 - (resist - pen) / 100);

        return effMult;
    }

    /// <summary>
    /// Calculate enemy resist for a non-physical damage type, clamped to [ResistFloor, maxResist].
    /// Ported from CalcOffence.lua calcResistForType (lines 457-470).
    /// </summary>
    internal static double CalcResistForType(string damageType, ModStore enemyDB, ModConfig? cfg)
    {
        bool isEle = DamageTypeFlags.IsElemental(damageType);
        double maxResist = MiscConstants.EnemyMaxResist;

        var resistOverride = enemyDB.Override(cfg, $"{damageType}Resist");
        double resist;
        if (resistOverride.HasValue)
        {
            resist = resistOverride.Value.AsNumber();
        }
        else
        {
            string[] resistNames = isEle
                ? [$"{damageType}Resist", "ElementalResist"]
                : [$"{damageType}Resist"];
            double resistBase = enemyDB.Sum(ModType.Base, cfg, resistNames);
            double resistMod = Math.Max(CalcLib.Mod(enemyDB, cfg, resistNames), 0);
            resist = resistBase * resistMod;
        }

        return Math.Max(Math.Min(resist, maxResist), MiscConstants.ResistFloor);
    }

    // ─── Instant leech ───

    private static void ComputeInstantLeech(Dictionary<string, double> output,
        ModStore skillModList, ModConfig? cfg, double hitRate)
    {
        // Instant life leech
        double instantLifeProp = Math.Max(Math.Min(
            skillModList.Sum(ModType.Base, cfg, "InstantLifeLeech"), 100), 0) / 100;
        if (instantLifeProp > 0)
        {
            output["LifeLeechInstant"] = output["LifeLeech"] * instantLifeProp;
            output["LifeLeech"] *= (1 - instantLifeProp);
        }
        output["LifeLeechInstantProportion"] = instantLifeProp;

        // Instant ES leech
        double instantEsProp = Math.Max(Math.Min(
            skillModList.Sum(ModType.Base, cfg, "InstantEnergyShieldLeech"), 100), 0) / 100;
        if (instantEsProp > 0)
        {
            output["EnergyShieldLeechInstant"] = output["EnergyShieldLeech"] * instantEsProp;
            output["EnergyShieldLeech"] *= (1 - instantEsProp);
        }
        output["EnergyShieldLeechInstantProportion"] = instantEsProp;

        // Instant mana leech
        double instantManaProp = Math.Max(Math.Min(
            skillModList.Sum(ModType.Base, cfg, "InstantManaLeech"), 100), 0) / 100;
        if (instantManaProp > 0)
        {
            output["ManaLeechInstant"] = output["ManaLeech"] * instantManaProp;
            output["ManaLeech"] *= (1 - instantManaProp);
        }
        output["ManaLeechInstantProportion"] = instantManaProp;

        // Leech durations and instance counts
        output.TryGetValue("Life", out double life);
        output.TryGetValue("EnergyShield", out double es);
        output.TryGetValue("Mana", out double mana);

        (output["LifeLeechDuration"], output["LifeLeechInstances"]) =
            GetLeechInstances(output["LifeLeech"], life);
        output["LifeLeechInstantRate"] = output["LifeLeechInstant"] * hitRate;

        (output["EnergyShieldLeechDuration"], output["EnergyShieldLeechInstances"]) =
            GetLeechInstances(output["EnergyShieldLeech"], es);
        output["EnergyShieldLeechInstantRate"] = output["EnergyShieldLeechInstant"] * hitRate;

        (output["ManaLeechDuration"], output["ManaLeechInstances"]) =
            GetLeechInstances(output["ManaLeech"], mana);
        output["ManaLeechInstantRate"] = output["ManaLeechInstant"] * hitRate;
    }

    private static (double Duration, double Instances) GetLeechInstances(double amount, double total)
    {
        if (total == 0)
            return (0, 0);
        double duration = amount / total / MiscConstants.LeechRateBase;
        return (duration, duration);
    }

    // ─── Gain on hit ───

    private static void ComputeGainOnHit(Dictionary<string, double> output,
        ModStore skillModList, Dictionary<string, bool> skillFlags, ModConfig? cfg,
        ModStore? enemyDB, double hitRate)
    {
        bool isMineOrTrap = skillFlags.ContainsKey("mine") || skillFlags.ContainsKey("trap") || skillFlags.ContainsKey("totem");

        if (isMineOrTrap)
        {
            output["LifeOnHit"] = 0;
            output["EnergyShieldOnHit"] = 0;
            output["ManaOnHit"] = 0;
        }
        else
        {
            output["LifeOnHit"] =
                !skillModList.Flag(cfg, "CannotGainLife") && !skillModList.Flag(cfg, "CannotRecoverLifeOutsideLeech")
                    ? skillModList.Sum(ModType.Base, cfg, "LifeOnHit") + (enemyDB?.Sum(ModType.Base, cfg, "SelfLifeOnHit") ?? 0)
                    : 0;
            output["EnergyShieldOnHit"] =
                !skillModList.Flag(cfg, "CannotGainEnergyShield")
                    ? skillModList.Sum(ModType.Base, cfg, "EnergyShieldOnHit") + (enemyDB?.Sum(ModType.Base, cfg, "SelfEnergyShieldOnHit") ?? 0)
                    : 0;
            output["ManaOnHit"] =
                !skillModList.Flag(cfg, "CannotGainMana")
                    ? skillModList.Sum(ModType.Base, cfg, "ManaOnHit") + (enemyDB?.Sum(ModType.Base, cfg, "SelfManaOnHit") ?? 0)
                    : 0;
        }

        output["LifeOnHitRate"] = output["LifeOnHit"] * hitRate;
        output["EnergyShieldOnHitRate"] = output["EnergyShieldOnHit"] * hitRate;
        output["ManaOnHitRate"] = output["ManaOnHit"] * hitRate;
    }

    // ─── Gain on kill ───

    private static void ComputeGainOnKill(Dictionary<string, double> output,
        ModStore skillModList, Dictionary<string, bool> skillFlags, ModConfig? cfg)
    {
        bool isMineOrTrap = skillFlags.ContainsKey("mine") || skillFlags.ContainsKey("trap") || skillFlags.ContainsKey("totem");

        if (isMineOrTrap)
        {
            output["LifeOnKill"] = 0;
            output["EnergyShieldOnKill"] = 0;
            output["ManaOnKill"] = 0;
        }
        else
        {
            output["LifeOnKill"] =
                !skillModList.Flag(cfg, "CannotGainLife") && !skillModList.Flag(cfg, "CannotRecoverLifeOutsideLeech")
                    ? Math.Floor(skillModList.Sum(ModType.Base, cfg, "LifeOnKill"))
                    : 0;
            output["EnergyShieldOnKill"] =
                !skillModList.Flag(cfg, "CannotGainEnergyShield")
                    ? Math.Floor(skillModList.Sum(ModType.Base, cfg, "EnergyShieldOnKill"))
                    : 0;
            output["ManaOnKill"] =
                !skillModList.Flag(cfg, "CannotGainMana")
                    ? Math.Floor(skillModList.Sum(ModType.Base, cfg, "ManaOnKill"))
                    : 0;
        }
    }
}
