using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Seeds a ModDB with base game constants.
/// Ported from CalcSetup.lua:calcs.initModDB() (lines 18-105)
/// and player/enemy initialization (lines 464-559).
/// </summary>
public static class CalcSetup
{
    /// <summary>
    /// Initialize a ModDB with all base game constants (resist caps, charge maximums,
    /// leech rates, action times, buff flags, shrine effects, etc.).
    /// </summary>
    /// <param name="modDB">The ModDB to seed.</param>
    /// <param name="isEnemy">If true, use monster constants instead of character constants for shared keys.</param>
    public static void InitModDB(ModDB modDB, bool isEnemy = false)
    {
        var constants = isEnemy ? GameConstants.Monster : GameConstants.Character;

        // Resistance caps
        double maxResist = constants["base_maximum_all_resistances_%"];
        modDB.NewMod("FireResistMax", ModType.Base, maxResist, "Base");
        modDB.NewMod("ColdResistMax", ModType.Base, maxResist, "Base");
        modDB.NewMod("LightningResistMax", ModType.Base, maxResist, "Base");
        modDB.NewMod("ChaosResistMax", ModType.Base, maxResist, "Base");
        modDB.NewMod("TotemFireResistMax", ModType.Base, maxResist, "Base");
        modDB.NewMod("TotemColdResistMax", ModType.Base, maxResist, "Base");
        modDB.NewMod("TotemLightningResistMax", ModType.Base, maxResist, "Base");
        modDB.NewMod("TotemChaosResistMax", ModType.Base, maxResist, "Base");

        // Block caps
        modDB.NewMod("BlockChanceMax", ModType.Base, constants["maximum_block_%"], "Base");
        modDB.NewMod("SpellBlockChanceMax", ModType.Base, constants["base_maximum_spell_block_%"], "Base");
        modDB.NewMod("SpellDodgeChanceMax", ModType.Base, 75, "Base");

        // Charge duration and maximums
        modDB.NewMod("ChargeDuration", ModType.Base, 10, "Base");
        modDB.NewMod("PowerChargesMax", ModType.Base, constants["max_power_charges"], "Base");
        modDB.NewMod("FrenzyChargesMax", ModType.Base, constants["max_frenzy_charges"], "Base");
        modDB.NewMod("EnduranceChargesMax", ModType.Base, constants["max_endurance_charges"], "Base");
        modDB.NewMod("SiphoningChargesMax", ModType.Base, 0, "Base");
        modDB.NewMod("ChallengerChargesMax", ModType.Base, 0, "Base");
        modDB.NewMod("BlitzChargesMax", ModType.Base, 0, "Base");

        // Inspiration charges (righteous charges in data)
        if (constants.TryGetValue("maximum_righteous_charges", out double righteous))
            modDB.NewMod("InspirationChargesMax", ModType.Base, righteous, "Base");
        else
            modDB.NewMod("InspirationChargesMax", ModType.Base, 0, "Base");

        modDB.NewMod("CrabBarriersMax", ModType.Base, 0, "Base");
        modDB.NewMod("BrutalChargesMax", ModType.Base, 0, "Base");
        modDB.NewMod("AbsorptionChargesMax", ModType.Base, 0, "Base");
        modDB.NewMod("AfflictionChargesMax", ModType.Base, 0, "Base");

        // Blood charges
        if (constants.TryGetValue("maximum_blood_scythe_charges", out double blood))
            modDB.NewMod("BloodChargesMax", ModType.Base, blood, "Base");
        else
            modDB.NewMod("BloodChargesMax", ModType.Base, 0, "Base");

        // Leech rates
        modDB.NewMod("MaxLifeLeechRate", ModType.Base, constants["maximum_life_leech_rate_%_per_minute"] / 60.0, "Base");
        modDB.NewMod("MaxManaLeechRate", ModType.Base, constants["maximum_mana_leech_rate_%_per_minute"] / 60.0, "Base");

        // Impale
        modDB.NewMod("ImpaleStacksMax", ModType.Base, constants["impaled_debuff_number_of_reflected_hits"], "Base");
        modDB.NewMod("BleedStacksMax", ModType.Base, 1, "Base");

        // ES leech rate and instance limits
        modDB.NewMod("MaxEnergyShieldLeechRate", ModType.Base, 10, "Base");
        modDB.NewMod("MaxLifeLeechInstance", ModType.Base, constants["maximum_life_leech_amount_per_leech_%_max_life"], "Base");
        modDB.NewMod("MaxManaLeechInstance", ModType.Base, constants["maximum_mana_leech_amount_per_leech_%_max_mana"], "Base");
        modDB.NewMod("MaxEnergyShieldLeechInstance", ModType.Base, constants["maximum_energy_shield_leech_amount_per_leech_%_max_energy_shield"], "Base");

        // Action times
        modDB.NewMod("TrapThrowingTime", ModType.Base, 0.6, "Base");
        modDB.NewMod("MineLayingTime", ModType.Base, 0.3, "Base");
        modDB.NewMod("WarcryCastTime", ModType.Base, 0.8, "Base");
        modDB.NewMod("TotemPlacementTime", ModType.Base, 0.6, "Base");
        modDB.NewMod("BallistaPlacementTime", ModType.Base, 0.35, "Base");

        // Totem limits
        modDB.NewMod("ActiveTotemLimit", ModType.Base, constants["base_number_of_totems_allowed"], "Base");

        // Ailment stacks
        modDB.NewMod("ShockStacksMax", ModType.Base, 1, "Base");
        modDB.NewMod("ScorchStacksMax", ModType.Base, 1, "Base");

        // Conditional debuffs: Maimed → -30% MovementSpeed
        modDB.NewMod("MovementSpeed", ModType.Inc, -30, "Base", tags: new ConditionTag { Var = "Maimed" });

        // Intimidated → +10% Attack DamageTaken
        modDB.NewMod("DamageTaken", ModType.Inc, 10, "Base", ModFlag.Attack, tags: new ConditionTag { Var = "Intimidated" });
        modDB.NewMod("DamageTaken", ModType.Inc, 10, "Base", ModFlag.Attack, tags:
            new ModTag[] { new ConditionTag { Var = "Intimidated", Neg = true }, new ConditionTag { Var = "Party:Intimidated" } });

        // Unnerved → +10% Spell DamageTaken
        modDB.NewMod("DamageTaken", ModType.Inc, 10, "Base", ModFlag.Spell, tags: new ConditionTag { Var = "Unnerved" });
        modDB.NewMod("DamageTaken", ModType.Inc, 10, "Base", ModFlag.Spell, tags:
            new ModTag[] { new ConditionTag { Var = "Unnerved", Neg = true }, new ConditionTag { Var = "Party:Unnerved" } });

        // Debilitated
        modDB.NewMod("Damage", ModType.More, -10, "Base", tags: new ConditionTag { Var = "Debilitated" });
        modDB.NewMod("MovementSpeed", ModType.More, -20, "Base", tags: new ConditionTag { Var = "Debilitated" });

        // Condition flags
        modDB.NewMod("Condition:Burning", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Ignited" });
        modDB.NewMod("Condition:Poisoned", ModType.Flag, true, "Base", tags: new MultiplierThresholdTag { Var = "PoisonStack", Threshold = 1 });

        // Buff flags (gated on conditions)
        modDB.NewMod("Blind", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Blinded" });
        modDB.NewMod("Chill", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Chilled" });
        modDB.NewMod("Freeze", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Frozen" });
        modDB.NewMod("Fortify", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Fortify" });
        modDB.NewMod("Fortified", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Fortified" });
        modDB.NewMod("Excommunicated", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Excommunicated" });
        modDB.NewMod("Fanaticism", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Fanaticism" });
        modDB.NewMod("Onslaught", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Onslaught" });
        modDB.NewMod("UnholyMight", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "UnholyMight" });
        modDB.NewMod("ChaoticMight", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "ChaoticMight" });
        modDB.NewMod("Tailwind", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Tailwind" });
        modDB.NewMod("Adrenaline", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Adrenaline" });

        // Shrine flags
        modDB.NewMod("AccelerationShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "AccelerationShrine" });
        modDB.NewMod("BrutalShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "BrutalShrine" });
        modDB.NewMod("DiamondShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "DiamondShrine" });
        modDB.NewMod("DivineShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "DivineShrine" });
        modDB.NewMod("EchoingShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "EchoingShrine" });
        modDB.NewMod("GloomShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "GloomShrine" });
        modDB.NewMod("ImpenetrableShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "ImpenetrableShrine" });
        modDB.NewMod("MassiveShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "MassiveShrine" });
        modDB.NewMod("ReplenishingShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "ReplenishingShrine" });
        modDB.NewMod("ResistanceShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "ResistanceShrine" });
        modDB.NewMod("ResonatingShrine", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "ResonatingShrine" });

        // Lesser shrine flags (gated on NOT having the full shrine)
        modDB.NewMod("LesserAccelerationShrine", ModType.Flag, true, "Base", tags:
            new ModTag[] { new ConditionTag { Var = "LesserAccelerationShrine" }, new ConditionTag { Var = "AccelerationShrine", Neg = true } });
        modDB.NewMod("LesserBrutalShrine", ModType.Flag, true, "Base", tags:
            new ModTag[] { new ConditionTag { Var = "LesserBrutalShrine" }, new ConditionTag { Var = "BrutalShrine", Neg = true } });
        modDB.NewMod("LesserImpenetrableShrine", ModType.Flag, true, "Base", tags:
            new ModTag[] { new ConditionTag { Var = "LesserImpenetrableShrine" }, new ConditionTag { Var = "ImpenetrableShrine", Neg = true } });
        modDB.NewMod("LesserMassiveShrine", ModType.Flag, true, "Base", tags:
            new ModTag[] { new ConditionTag { Var = "LesserMassiveShrine" }, new ConditionTag { Var = "MassiveShrine", Neg = true } });
        modDB.NewMod("LesserReplenishingShrine", ModType.Flag, true, "Base", tags:
            new ModTag[] { new ConditionTag { Var = "LesserReplenishingShrine" }, new ConditionTag { Var = "ReplenishingShrine", Neg = true } });
        modDB.NewMod("LesserResistanceShrine", ModType.Flag, true, "Base", tags:
            new ModTag[] { new ConditionTag { Var = "LesserResistanceShrine" }, new ConditionTag { Var = "ResistanceShrine", Neg = true } });

        // Misc buff flags
        modDB.NewMod("AlchemistsGenius", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "AlchemistsGenius" });
        modDB.NewMod("LuckyHits", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "LuckyHits" });
        modDB.NewMod("Convergence", ModType.Flag, true, "Base", tags: new ConditionTag { Var = "Convergence" });

        // Crushed
        modDB.NewMod("PhysicalDamageReduction", ModType.Base, -15, "Base", tags: new ConditionTag { Var = "Crushed" });

        // Crit chance cap
        modDB.NewMod("CritChanceCap", ModType.Base, 100, "Base");
    }

    /// <summary>
    /// Sets up player ModDB with class stats, level, bandit, resistance penalty,
    /// charge bonuses, and misc base mods.
    /// Ported from CalcSetup.lua lines 464-537.
    /// </summary>
    public static void InitPlayerModDB(ModDB modDB, string className, int level,
        string bandit, int resistancePenalty = -60)
    {
        // Base game constants first
        InitModDB(modDB);

        var cc = GameConstants.Character;

        // Class base attributes
        var (str, dex, int_) = ClassBaseStats.GetBaseAttributes(className);
        modDB.NewMod("Str", ModType.Base, str, "Base");
        modDB.NewMod("Dex", ModType.Base, dex, "Base");
        modDB.NewMod("Int", ModType.Base, int_, "Base");

        // Level multiplier (clamped 1-100)
        modDB.Multipliers["Level"] = Math.Clamp(level, 1, 100);

        // Per-level scaling
        modDB.NewMod("Life", ModType.Base, cc["life_per_level"], "Base",
            tags: new MultiplierTag { Var = "Level", Base = 38 });
        modDB.NewMod("Mana", ModType.Base, cc["mana_per_level"], "Base",
            tags: new MultiplierTag { Var = "Level", Base = 34 });
        modDB.NewMod("ManaRegen", ModType.Base, MiscConstants.ManaRegenBase, "Base",
            tags: new PerStatTag { Stat = "Mana", Div = 1 });
        modDB.NewMod("Devotion", ModType.Base, 0, "Base");
        modDB.NewMod("Evasion", ModType.Base, cc["base_evasion_rating"], "Base");
        modDB.NewMod("Accuracy", ModType.Base, cc["accuracy_rating_per_level"], "Base",
            tags: new MultiplierTag { Var = "Level", Base = -cc["accuracy_rating_per_level"] });
        modDB.NewMod("CritMultiplier", ModType.Base, cc["base_critical_strike_multiplier"] - 100, "Base");
        modDB.NewMod("DotMultiplier", ModType.Base, cc["critical_ailment_dot_multiplier_+"], "Base",
            tags: new ConditionTag { Var = "CriticalStrike" });

        // Resistance penalty (from config, default -60)
        modDB.NewMod("FireResist", ModType.Base, resistancePenalty, "Base");
        modDB.NewMod("ColdResist", ModType.Base, resistancePenalty, "Base");
        modDB.NewMod("LightningResist", ModType.Base, resistancePenalty, "Base");
        modDB.NewMod("ChaosResist", ModType.Base, resistancePenalty, "Base");

        // Totem resists (fixed, not affected by penalty)
        modDB.NewMod("TotemFireResist", ModType.Base, 40, "Base");
        modDB.NewMod("TotemColdResist", ModType.Base, 40, "Base");
        modDB.NewMod("TotemLightningResist", ModType.Base, 40, "Base");
        modDB.NewMod("TotemChaosResist", ModType.Base, 20, "Base");

        // Charge per-charge bonuses
        modDB.NewMod("CritChance", ModType.Inc, cc["critical_strike_chance_+%_per_power_charge"], "Base",
            tags: new MultiplierTag { Var = "PowerCharge" });
        modDB.NewMod("Speed", ModType.Inc, cc["base_attack_speed_+%_per_frenzy_charge"], "Base", ModFlag.Attack,
            tags: new MultiplierTag { Var = "FrenzyCharge" });
        modDB.NewMod("Speed", ModType.Inc, cc["base_cast_speed_+%_per_frenzy_charge"], "Base", ModFlag.Cast,
            tags: new MultiplierTag { Var = "FrenzyCharge" });
        modDB.NewMod("Damage", ModType.More, cc["object_inherent_damage_+%_final_per_frenzy_charge"], "Base",
            tags: new MultiplierTag { Var = "FrenzyCharge" });
        modDB.NewMod("PhysicalDamageReduction", ModType.Base, cc["physical_damage_reduction_%_per_endurance_charge"], "Base",
            tags: new MultiplierTag { Var = "EnduranceCharge" });
        modDB.NewMod("ElementalDamageReduction", ModType.Base, cc["elemental_damage_reduction_%_per_endurance_charge"], "Base",
            tags: new MultiplierTag { Var = "EnduranceCharge" });

        // Rage, Gale Force, Fortification, Valour, Soul Eater limits
        modDB.NewMod("MaximumRage", ModType.Base, cc["maximum_rage"], "Base");
        modDB.NewMod("Multiplier:GaleForce", ModType.Base, 0, "Base");
        modDB.NewMod("MaximumGaleForce", ModType.Base, 10, "Base");
        modDB.NewMod("MaximumFortification", ModType.Base, cc["base_max_fortification"], "Base");
        modDB.NewMod("MaximumValour", ModType.Base, 50, "Base");
        modDB.NewMod("SoulEaterMax", ModType.Base, cc["soul_eater_maximum_stacks"], "Base");
        modDB.NewMod("Multiplier:IntensityLimit", ModType.Base, 3, "Base");

        // Rampage scaling
        modDB.NewMod("Damage", ModType.Inc, cc["damage_+%_per_10_rampage_stacks"], "Base",
            tags: new MultiplierTag { Var = "Rampage", Limit = cc["max_rampage_stacks"] / 20, Div = 20 });
        modDB.NewMod("MovementSpeed", ModType.Inc, cc["movement_velocity_+%_per_10_rampage_stacks"], "Base",
            tags: new MultiplierTag { Var = "Rampage", Limit = cc["max_rampage_stacks"] / 20, Div = 20 });

        // Soul Eater speed bonuses
        modDB.NewMod("Speed", ModType.Inc, 5, "Base", ModFlag.Attack,
            tags: new MultiplierTag { Var = "SoulEater" });
        modDB.NewMod("Speed", ModType.Inc, 5, "Base", ModFlag.Cast,
            tags: new MultiplierTag { Var = "SoulEater" });

        // Trap/Mine/Brand/Curse limits
        modDB.NewMod("ActiveTrapLimit", ModType.Base, cc["base_number_of_traps_allowed"], "Base");
        modDB.NewMod("ActiveMineLimit", ModType.Base, cc["base_number_of_remote_mines_allowed"], "Base");
        modDB.NewMod("MineThrowCount", ModType.Base, 1, "Base");
        modDB.NewMod("TrapThrowCount", ModType.Base, 1, "Base");
        modDB.NewMod("ActiveBrandLimit", ModType.Base, 3, "Base");
        modDB.NewMod("EnemyCurseLimit", ModType.Base, 1, "Base");
        modDB.NewMod("SocketedCursesHexLimitValue", ModType.Base, 1, "Base");
        modDB.NewMod("ProjectileCount", ModType.Base, 1, "Base");

        // Dual wield bonuses
        modDB.NewMod("Speed", ModType.More, cc["dual_wield_inherent_attack_speed_+%_final"], "Base", ModFlag.Attack,
            tags: new ModTag[] { new ConditionTag { Var = "DualWielding" }, new ConditionTag { Var = "DoubledInherentDualWieldingSpeed", Neg = true } });
        modDB.NewMod("Speed", ModType.More, 2 * cc["dual_wield_inherent_attack_speed_+%_final"], "Base", ModFlag.Attack,
            tags: new ModTag[] { new ConditionTag { Var = "DualWielding" }, new ConditionTag { Var = "DoubledInherentDualWieldingSpeed" } });
        modDB.NewMod("BlockChance", ModType.Base, cc["inherent_block_while_dual_wielding_%"], "Base",
            tags: new ModTag[] { new ConditionTag { Var = "DualWielding" }, new ConditionTag { Var = "NoInherentBlock", Neg = true }, new ConditionTag { Var = "DoubledInherentDualWieldingBlock", Neg = true } });
        modDB.NewMod("BlockChance", ModType.Base, 2 * cc["inherent_block_while_dual_wielding_%"], "Base",
            tags: new ModTag[] { new ConditionTag { Var = "DualWielding" }, new ConditionTag { Var = "NoInherentBlock", Neg = true }, new ConditionTag { Var = "DoubledInherentDualWieldingBlock" } });

        // Bleed bonus vs moving enemies
        modDB.NewMod("Damage", ModType.More, 200, "Base", ModFlag.None, KeywordFlag.Bleed,
            new ActorConditionTag { Actor = "enemy", Var = "Moving" }, new ConditionTag { Var = "NoExtraBleedDamageToMovingEnemy", Neg = true });

        // Stance conditions
        modDB.NewMod("Condition:BloodStance", ModType.Flag, true, "Base",
            tags: new ConditionTag { Var = "SandStance", Neg = true });
        modDB.NewMod("Condition:PrideMinEffect", ModType.Flag, true, "Base",
            tags: new ConditionTag { Var = "PrideMaxEffect", Neg = true });

        // Per-charge special bonuses
        modDB.NewMod("PerBrutalTripleDamageChance", ModType.Base, cc["chance_to_deal_triple_damage_%_per_brutal_charge"], "Base");
        modDB.NewMod("PerAfflictionAilmentDamage", ModType.Base, cc["ailment_damage_+%_final_per_affliction_charge"], "Base");
        modDB.NewMod("PerAfflictionNonDamageEffect", ModType.Base, cc["non_damaging_ailment_effect_+%_final_per_affliction_charge"], "Base");
        modDB.NewMod("PerAbsorptionElementalEnergyShieldRecoup", ModType.Base, cc["elemental_damage_taken_goes_to_energy_shield_over_4_seconds_%_per_absorption_charge"], "Base");

        // Misc
        modDB.NewMod("TinctureLimit", ModType.Base, 1, "Base");
        modDB.NewMod("ManaDegenPercent", ModType.Base, 1, "Base",
            tags: new MultiplierTag { Var = "EffectiveManaBurnStacks" });
        modDB.NewMod("LifeDegenPercent", ModType.Base, 1, "Base",
            tags: new MultiplierTag { Var = "WeepingWoundsStacks" });
        modDB.NewMod("PresenceRadius", ModType.Base, cc["base_presence_radius"], "Base");

        // Bandit mods
        switch (bandit)
        {
            case "Alira":
                modDB.NewMod("ElementalResist", ModType.Base, 15, "Bandit");
                break;
            case "Kraityn":
                modDB.NewMod("MovementSpeed", ModType.Inc, 8, "Bandit");
                break;
            case "Oak":
                modDB.NewMod("Life", ModType.Base, 40, "Bandit");
                break;
            default:
                modDB.NewMod("ExtraPoints", ModType.Base, 1, "Bandit");
                break;
        }
    }

    /// <summary>
    /// Sets up enemy ModDB with monster base stats.
    /// Ported from CalcSetup.lua lines 552-555.
    /// </summary>
    public static void InitEnemyModDB(ModDB enemyModDB, int enemyLevel)
    {
        InitModDB(enemyModDB, isEnemy: true);

        // Monster accuracy from level table
        enemyModDB.NewMod("Accuracy", ModType.Base, BossData.GetMonsterAccuracy(enemyLevel), "Base");

        // Damage over time condition flag (gated on player Combat condition)
        enemyModDB.NewMod("Condition:AgainstDamageOverTime", ModType.Flag, true, "Base", ModFlag.Dot,
            tags: new ActorConditionTag { Actor = "player", Var = "Combat" });
    }
}
