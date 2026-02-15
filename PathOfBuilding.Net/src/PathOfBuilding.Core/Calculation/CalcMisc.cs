using PathOfBuilding.Core.Data;
using PathOfBuilding.Core.Modifiers;
using PathOfBuilding.Core.Tags;

namespace PathOfBuilding.Core.Calculation;

/// <summary>
/// Miscellaneous actor calculations: fortification, rage, simple buffs,
/// self-applied ailments, and enemy debuffs.
/// Ported from CalcPerform.lua doActorMisc (lines 594-889).
/// </summary>
public static class CalcMisc
{
    /// <summary>
    /// Main entry point. Runs all misc calculations if combat mode is active.
    /// </summary>
    public static void DoActorMisc(Actor actor)
    {
        if (!actor.Config.ModeCombat)
            return;

        Fortification(actor);
        Rage(actor);
        SimpleBuffs(actor);
        SelfAilments(actor);
        EnemyDebuffs(actor);
        MultiplierStacks(actor);
    }

    // ─── Fortification ───

    internal static void Fortification(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        bool hasFortify = modDB.Flag(null, "Fortified")
            || modDB.Sum(ModType.Base, null, "Multiplier:Fortification") > 0;
        bool hasMinimumFortification = modDB.Sum(ModType.Base, null, "MinimumFortification") > 0;

        if (hasMinimumFortification)
            modDB.Conditions["Fortified"] = true;

        if (!hasFortify && !hasMinimumFortification)
            return;

        double maxStacks = Math.Max(
            modDB.Override(null, "MaximumFortification")?.AsNumber()
                ?? modDB.Sum(ModType.Base, null, "MaximumFortification"),
            0);
        double minStacks = Math.Min(
            modDB.Flag(null, "Condition:HaveMaxFortification") ? maxStacks
                : modDB.Sum(ModType.Base, null, "MinimumFortification"),
            maxStacks);
        double stacks = Math.Min(
            modDB.Override(null, "FortificationStacks")?.AsNumber()
                ?? (minStacks > 0 ? minStacks : maxStacks),
            maxStacks);

        output["MaximumFortification"] = maxStacks;
        output["MinimumFortification"] = minStacks;
        output["FortificationStacks"] = stacks;
        output["FortificationStacksOver20"] = Math.Min(Math.Max(0, stacks - 20), maxStacks - 20);

        double increasedDuration = modDB.Sum(ModType.Inc, null, "FortifyDuration");
        output["FortifyDuration"] = (modDB.Override(null, "FortifyDuration")?.AsNumber()
            ?? MiscConstants.FortifyBaseDuration) * (1 + increasedDuration / 100);

        if (!modDB.Flag(null, "Condition:NoFortificationMitigation"))
        {
            output["FortificationEffect"] = stacks;
            modDB.NewMod("DamageTakenWhenHit", ModType.More, -stacks, "Fortification");
        }
        else
        {
            output["FortificationEffect"] = 0;
        }

        if (stacks >= maxStacks)
        {
            modDB.NewMod("Condition:HaveMaximumFortification", ModType.Flag, true, "");
        }

        modDB.Multipliers["BuffOnSelf"] = (modDB.Multipliers.TryGetValue("BuffOnSelf", out double buf) ? buf : 0) + 1;
    }

    // ─── Rage ───

    internal static void Rage(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;

        bool canHaveRage = modDB.Flag(null, "Condition:CanGainRage")
            || modDB.Sum(ModType.Base, null, "RageRegen") > 0;
        if (!canHaveRage)
            return;

        double maxStacks = Math.Floor(modDB.Sum(ModType.Base, null, "MaximumRage") * modDB.More(null, "MaximumRage"));
        double minStacks = Math.Min(modDB.Sum(ModType.Base, null, "MinimumRage"), maxStacks);
        double rageConfig = modDB.Sum(ModType.Base, null, "Multiplier:RageStack");
        // Apply minimum from MinimumRage mod (already handled above as actor-level)
        double fromMinRage = modDB.Sum(ModType.Base, null, "MinimumRage");
        double currentMultiplier = modDB.Multipliers.TryGetValue("Rage", out double r) ? r : 0;
        if (fromMinRage > currentMultiplier)
            modDB.Multipliers["Rage"] = fromMinRage;

        double stacks = Math.Max(Math.Min(rageConfig, maxStacks), minStacks > 0 ? minStacks : 0);

        double rageEffect = Math.Floor(stacks * CalcLib.Mod(modDB, null, "RageEffect"));
        output["RageEffect"] = rageEffect;
        modDB.NewMod("Multiplier:RageEffect", ModType.Base, rageEffect, "Base");
        output["Rage"] = stacks;
        output["MaximumRage"] = maxStacks;
        modDB.NewMod("Multiplier:Rage", ModType.Base, stacks, "Base");

        if (modDB.Flag(null, "Condition:RageSpellDamage"))
            modDB.NewMod("Damage", ModType.More, rageEffect, "Rage", ModFlag.Spell);
        else
            modDB.NewMod("Damage", ModType.More, rageEffect, "Rage", ModFlag.Attack);

        if (stacks >= maxStacks)
            modDB.NewMod("Condition:HaveMaximumRage", ModType.Flag, true, "");

        output["InherentRageLossDelay"] = 2 + modDB.Sum(ModType.Base, null, "InherentRageLossDelay");
        output["InherentRageLoss"] = !modDB.Flag(null, "InherentRageLossIsPrevented")
            ? 10 * (1 + modDB.Sum(ModType.Inc, null, "InherentRageLoss") / 100)
            : 0;
    }

    // ─── Simple Buffs ───

    internal static void SimpleBuffs(Actor actor)
    {
        var modDB = actor.ModDB;
        var output = actor.Output;
        var condList = modDB.Conditions;

        // Onslaught (simplified — skips flask search)
        if (modDB.Flag(null, "Onslaught"))
        {
            double onslaughtEffectInc = modDB.Sum(ModType.Inc, null, "OnslaughtEffect", "BuffEffectOnSelf") / 100;
            double effect = Math.Floor(20 * (1 + onslaughtEffectInc));
            modDB.NewMod("Speed", ModType.Inc, effect, "Onslaught", ModFlag.Attack);
            modDB.NewMod("Speed", ModType.Inc, effect, "Onslaught", ModFlag.Cast);
            modDB.NewMod("MovementSpeed", ModType.Inc, effect, "Onslaught");
        }

        // Arcane Surge
        if (condList.TryGetValue("AffectedByArcaneSurge", out bool aas) && aas
            || modDB.Flag(null, "Condition:ArcaneSurge"))
        {
            condList["AffectedByArcaneSurge"] = true;
            double effect = 1 + modDB.Sum(ModType.Inc, null, "ArcaneSurgeEffect", "BuffEffectOnSelf") / 100;
            double manaRegen = (modDB.Max(null, "ArcaneSurgeManaRegen") ?? 30) * effect;
            modDB.NewMod("ManaRegen", ModType.Inc, manaRegen, "Arcane Surge");
            double castSpeed = (modDB.Max(null, "ArcaneSurgeCastSpeed") ?? 20) * effect;
            modDB.NewMod("Speed", ModType.Inc, castSpeed, "Arcane Surge", ModFlag.Cast);
            if (modDB.Flag(null, "ArcaneSurgeCastSpeedToMovementSpeed"))
                modDB.NewMod("MovementSpeed", ModType.Inc, castSpeed, "Arcane Surge");
            double arcaneSurgeDamage = modDB.Max(null, "ArcaneSurgeDamage") ?? 0;
            if (arcaneSurgeDamage != 0)
                modDB.NewMod("Damage", ModType.More, arcaneSurgeDamage * effect, "Arcane Surge", ModFlag.Spell);
        }

        // Tailwind
        if (modDB.Flag(null, "Tailwind"))
        {
            double effect = Math.Floor(8 * (1 + modDB.Sum(ModType.Inc, null, "TailwindEffectOnSelf", "BuffEffectOnSelf") / 100));
            modDB.NewMod("ActionSpeed", ModType.Inc, effect, "Tailwind");
        }
        if (modDB.Flag(null, "Condition:TotemTailwind"))
        {
            modDB.NewMod("TotemActionSpeed", ModType.Inc, 8, "Tailwind");
        }

        // Adrenaline
        if (modDB.Flag(null, "Adrenaline"))
        {
            double effectMod = 1 + modDB.Sum(ModType.Inc, null, "BuffEffectOnSelf") / 100;
            modDB.NewMod("Damage", ModType.Inc, Math.Floor(100 * effectMod), "Adrenaline");
            modDB.NewMod("Speed", ModType.Inc, Math.Floor(25 * effectMod), "Adrenaline", ModFlag.Attack);
            modDB.NewMod("Speed", ModType.Inc, Math.Floor(25 * effectMod), "Adrenaline", ModFlag.Cast);
            modDB.NewMod("MovementSpeed", ModType.Inc, Math.Floor(25 * effectMod), "Adrenaline");
            modDB.NewMod("PhysicalDamageReduction", ModType.Base, Math.Floor(10 * effectMod), "Adrenaline");
        }

        // Unholy Might
        if (modDB.Flag(null, "UnholyMight"))
        {
            double effect = 1 + modDB.Sum(ModType.Inc, null, "BuffEffectOnSelf") / 100;
            modDB.NewMod("PhysicalDamageConvertToChaos", ModType.Base, Math.Floor(100 * effect), "Unholy Might");
            modDB.NewMod("Condition:CanWither", ModType.Flag, true, "Unholy Might");
        }

        // Chaotic Might
        if (modDB.Flag(null, "ChaoticMight"))
        {
            double effect = Math.Floor(30 * (1 + modDB.Sum(ModType.Inc, null, "BuffEffectOnSelf") / 100));
            modDB.NewMod("PhysicalDamageGainAsChaos", ModType.Base, effect, "Chaotic Might");
        }

        // Convergence
        if (modDB.Flag(null, "Convergence"))
        {
            double effect = Math.Floor(30 * (1 + modDB.Sum(ModType.Inc, null, "BuffEffectOnSelf") / 100));
            modDB.NewMod("ElementalDamage", ModType.More, effect, "Convergence");
        }

        // Her Embrace
        if (modDB.Flag(null, "HerEmbrace"))
        {
            condList["HerEmbrace"] = true;
            modDB.NewMod("AvoidStun", ModType.Base, 100, "Her Embrace");
            modDB.NewMod("PhysicalDamageGainAsFire", ModType.Base, 123, "Her Embrace", ModFlag.Sword);
            modDB.NewMod("AvoidFreeze", ModType.Base, 100, "Her Embrace");
            modDB.NewMod("AvoidChill", ModType.Base, 100, "Her Embrace");
            modDB.NewMod("AvoidIgnite", ModType.Base, 100, "Her Embrace");
            modDB.NewMod("Speed", ModType.Inc, 20, "Her Embrace", ModFlag.Attack);
            modDB.NewMod("Speed", ModType.Inc, 20, "Her Embrace", ModFlag.Cast);
            modDB.NewMod("MovementSpeed", ModType.Inc, 20, "Her Embrace");
        }

        // Elusive (simplified — skips Nightblade)
        if (modDB.Flag(null, "Elusive"))
        {
            double maxSkillInc = modDB.Max(null, "ElusiveEffect") ?? 0;
            double inc = modDB.Sum(ModType.Inc, null, "ElusiveEffect", "BuffEffectOnSelf") + maxSkillInc;
            double elusiveEffectMod = (1 + inc / 100) * modDB.More(null, "ElusiveEffect", "BuffEffectOnSelf") * 100;
            double minThreshold = modDB.Override(null, "ElusiveEffectMinThreshold")?.AsNumber() ?? 0;
            output["ElusiveEffectMod"] = (elusiveEffectMod + minThreshold) / 2;

            // Override if set
            var elusiveOverride = modDB.Override(null, "ElusiveEffect");
            if (elusiveOverride.HasValue)
                output["ElusiveEffectMod"] = Math.Min(elusiveOverride.Value.AsNumber(), elusiveEffectMod);

            double eff = output["ElusiveEffectMod"] / 100;
            condList["Elusive"] = true;
            modDB.NewMod("AvoidAllDamageFromHitsChance", ModType.Base, Math.Floor(15 * eff), "Elusive");
            modDB.NewMod("MovementSpeed", ModType.Inc, Math.Floor(30 * eff), "Elusive");
        }
    }

    // ─── Self-Applied Ailments ───

    internal static void SelfAilments(Actor actor)
    {
        var modDB = actor.ModDB;
        var enemyDB = actor.Enemy?.ModDB;
        var output = actor.Output;
        var condList = modDB.Conditions;

        // Withered (on enemy)
        var witherEffectStack = modDB.Max(null, "WitherEffectStack");
        if (witherEffectStack.HasValue && enemyDB != null)
        {
            modDB.NewMod("Condition:CanWither", ModType.Flag, true, "Config");
            enemyDB.NewMod("ChaosDamageTaken", ModType.Inc, witherEffectStack.Value, "Withered",
                tags: new MultiplierTag { Var = "WitheredStack", Limit = 15 });
        }

        // Withered on self
        if (modDB.Flag(null, "Condition:CanBeWithered"))
        {
            double effect = 6 * (100 + modDB.Sum(ModType.Inc, null, "WitherEffectOnSelf")) / 100 * modDB.More(null, "WitherEffectOnSelf");
            modDB.NewMod("ChaosDamageTaken", ModType.Inc, effect, "Withered",
                tags: new MultiplierTag { Var = "WitheredStack", Limit = 15 });
        }

        // Blind
        if (modDB.Flag(null, "Blind") && !modDB.Flag(null, "CannotBeBlinded"))
        {
            if (!modDB.Flag(null, "IgnoreBlindHitChance"))
            {
                double effect = 1 + modDB.Sum(ModType.Inc, null, "BlindEffect", "BuffEffectOnSelf") / 100;
                var blindOverride = modDB.Override(null, "BlindEffect");
                if (blindOverride.HasValue)
                    effect = Math.Min(blindOverride.Value.AsNumber() / 100, effect);
                modDB.NewMod("Accuracy", ModType.More, Math.Floor(-20 * effect), "Blind");
                modDB.NewMod("Evasion", ModType.More, Math.Floor(-20 * effect), "Blind");
            }
        }

        // Chill
        if (modDB.Flag(null, "Chill"))
        {
            var ailmentInfo = AilmentData.NonDamagingAilment["Chill"];
            double chillValue = Math.Max(
                modDB.Sum(ModType.Base, null, "SelfChillOverride"),
                modDB.Override(null, "ChillVal")?.AsNumber() ?? ailmentInfo.Default ?? 10);
            double totalChillSelfEffect = CalcLib.Mod(modDB, null, "SelfChillEffect");
            double avoidChill = modDB.Flag(null, "ChillImmune", "ElementalAilmentImmune") ? 100
                : Math.Floor(Math.Min(modDB.Sum(ModType.Base, null, "AvoidChill", "AvoidAilments", "AvoidElementalAilments")
                    + (modDB.Flag(null, "ShockAvoidAppliesToElementalAilments") ? modDB.Sum(ModType.Base, null, "AvoidShock") : 0), 100));
            double effect = avoidChill == 100 ? 0
                : Math.Min(Math.Max(Math.Floor(chillValue * totalChillSelfEffect), 0),
                    modDB.Override(null, "ChillMax")?.AsNumber() ?? ailmentInfo.Max);

            bool reversed = modDB.Flag(null, "SelfChillEffectIsReversed");
            if (modDB.Flag(null, "SkitterbotBonechill"))
                modDB.NewMod("ColdDamageTaken", ModType.Inc, effect * (reversed ? -1 : 1), "Bonechill");
            modDB.NewMod("ActionSpeed", ModType.Inc, effect * (reversed ? 1 : -1), "Chill");
        }

        // Shock
        if (modDB.Flag(null, "Shock"))
        {
            var ailmentInfo = AilmentData.NonDamagingAilment["Shock"];
            double shockValue = Math.Max(
                modDB.Sum(ModType.Base, null, "SelfShockOverride"),
                modDB.Override(null, "ShockVal")?.AsNumber() ?? ailmentInfo.Default ?? 15);
            double totalShockSelfEffect = CalcLib.Mod(modDB, null, "SelfShockEffect");
            double avoidShock = modDB.Flag(null, "ShockImmune", "ElementalAilmentImmune") ? 100
                : Math.Floor(Math.Min(modDB.Sum(ModType.Base, null, "AvoidShock", "AvoidAilments", "AvoidElementalAilments"), 100));
            double effect = avoidShock == 100 ? 0
                : Math.Min(Math.Max(Math.Floor(shockValue * totalShockSelfEffect), 0),
                    modDB.Override(null, "ShockMax")?.AsNumber() ?? ailmentInfo.Max);
            modDB.NewMod("DamageTaken", ModType.Inc, effect, "Shock");
        }

        // Scorch
        if (modDB.Flag(null, "Scorch"))
        {
            var ailmentInfo = AilmentData.NonDamagingAilment["Scorch"];
            double scorchValue = Math.Max(
                modDB.Sum(ModType.Base, null, "SelfScorchOverride"),
                modDB.Override(null, "ScorchVal")?.AsNumber() ?? ailmentInfo.Default ?? 10);
            double totalScorchSelfEffect = CalcLib.Mod(modDB, null, "SelfScorchEffect");
            double avoidScorch = modDB.Flag(null, "ScorchImmune", "ElementalAilmentImmune") ? 100
                : Math.Floor(Math.Min(modDB.Sum(ModType.Base, null, "AvoidScorch", "AvoidAilments", "AvoidElementalAilments")
                    + (modDB.Flag(null, "ShockAvoidAppliesToElementalAilments") ? modDB.Sum(ModType.Base, null, "AvoidShock") : 0), 100));
            double effect = avoidScorch == 100 ? 0
                : Math.Min(Math.Max(Math.Floor(scorchValue * totalScorchSelfEffect), 0),
                    modDB.Override(null, "ScorchMax")?.AsNumber() ?? ailmentInfo.Max);
            modDB.NewMod("ElementalResist", ModType.Base, -effect, "Scorch");
        }

        // Freeze
        if (modDB.Flag(null, "Freeze"))
        {
            double effect = Math.Max(Math.Floor(70 * CalcLib.Mod(modDB, null, "SelfChillEffect")), 0);
            modDB.NewMod("ActionSpeed", ModType.Inc, -effect, "Freeze");
        }

        // Leech conditions
        if (modDB.Flag(null, "CanLeechLifeOnFullLife") && !modDB.Flag(null, "GhostReaver"))
        {
            condList["Leeching"] = true;
            condList["LeechingLife"] = true;
        }
        if (modDB.Flag(null, "CanLeechEnergyShieldOnFullEnergyShield"))
        {
            condList["Leeching"] = true;
            condList["LeechingEnergyShield"] = true;
        }
    }

    // ─── Enemy Debuffs ───

    internal static void EnemyDebuffs(Actor actor)
    {
        var modDB = actor.ModDB;
        var enemyDB = actor.Enemy?.ModDB;
        if (enemyDB == null)
            return;

        // Covered in Ash
        double ashEffect = modDB.Sum(ModType.Base, null, "CoveredInAshEffect");
        if (ashEffect > 0)
            enemyDB.NewMod("FireDamageTaken", ModType.Inc, Math.Min(ashEffect, 20), "Covered in Ash");

        // Covered in Frost
        double frostEffect = modDB.Sum(ModType.Base, null, "CoveredInFrostEffect");
        if (frostEffect > 0)
            enemyDB.NewMod("ColdDamageTaken", ModType.Inc, Math.Min(frostEffect, 20), "Covered in Frost");

        // Malediction
        if (modDB.Flag(null, "HasMalediction"))
        {
            modDB.NewMod("DamageTaken", ModType.Inc, 10, "Malediction");
            modDB.NewMod("Damage", ModType.Inc, -10, "Malediction");
        }

        // Maddening Presence
        if (modDB.Flag(null, "HasMaddeningPresence"))
        {
            modDB.NewMod("ActionSpeed", ModType.Inc, -10, "Maddening Presence");
            modDB.NewMod("Damage", ModType.Inc, -10, "Maddening Presence");
        }

        // Shaper's Presence
        if (modDB.Flag(null, "HasShapersPresence"))
        {
            modDB.NewMod("BuffExpireFaster", ModType.More, -20, "Shapers Presence");
        }
    }

    // ─── Multiplier Stacks ───

    internal static void MultiplierStacks(Actor actor)
    {
        var modDB = actor.ModDB;
        var config = actor.Config;

        // ManaBurn stacks
        if (config.MultiplierManaBurnStacks > 0)
        {
            double maxManaBurn = modDB.Sum(ModType.Base, null, "MaxManaBurnStacks");
            if (maxManaBurn == 0) maxManaBurn = 9999;
            double manaBurnStacks = Math.Min(config.MultiplierManaBurnStacks, maxManaBurn);
            modDB.NewMod("Multiplier:ManaBurnStacks", ModType.Base, manaBurnStacks, "Config");
            manaBurnStacks += modDB.Sum(ModType.Base, null, "EffectiveManaBurnStacks");

            if (modDB.Flag(null, "Condition:WeepingWoundsInsteadOfManaBurn"))
                modDB.NewMod("Multiplier:WeepingWoundsStacks", ModType.Base, manaBurnStacks, "Config");
            else
                modDB.NewMod("Multiplier:EffectiveManaBurnStacks", ModType.Base, manaBurnStacks, "Config");
        }

        // Soul Eater
        if (modDB.Flag(null, "Condition:CanHaveSoulEater"))
        {
            double max = modDB.Override(null, "SoulEaterMax")?.AsNumber()
                ?? modDB.Sum(ModType.Base, null, "SoulEaterMax");
            modDB.NewMod("Multiplier:SoulEater", ModType.Base, 1, "Base",
                tags: new MultiplierTag { Var = "SoulEaterStack", Limit = (int)max });
        }
    }
}
